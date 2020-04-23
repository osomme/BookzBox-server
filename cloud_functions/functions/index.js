const request = require('request');
const functions = require('firebase-functions');
const admin = require('firebase-admin');
admin.initializeApp();

const firestore = admin.firestore();
const mapBoxesRef = firestore.collection('map_boxes');
const usersRef = firestore.collection('users');
const likesRef = firestore.collection('likes');
const boxesRef = firestore.collection('boxes');
const matchRef = firestore.collection('matches');
const recommenderApiKey = ''; // TODO: add before deployment - not for version control
const recommenderApiUrl = 'http://13.48.105.244:80/api/';

/**
 * Listens for new boxes on the /boxes/ collection and creates derived versions.
 */
exports.onBoxUploaded = functions.firestore
    .document('/boxes/{box}')
    .onCreate((snapshot, _) => {
        const box = snapshot.data();
        const boxId = snapshot.id;
        console.log(`New box added with ID: ${boxId}, contents: ${box}`);
        // Add mapped box to map_boxes collection
        const mapBoxes = mapBoxesRef.doc(boxId)
            .set(boxToMapBoxItem(box));
        // Add mapped box to publishers' profile
        const userBoxes = usersRef.doc(box.publisher)
            .collection('boxes')
            .doc(boxId)
            .set(boxToProfileBoxItem(box));
        // Add mapped box to box feed collection
        uploadBoxToRecommendationSys(boxToRecommendationItems(boxId, box));

        return Promise.all([mapBoxes, userBoxes]);
    });

/**
 * Listens for user addition.
 */
exports.onUserAdded = functions.firestore
    .document('/users/{user}')
    .onCreate((snapshot, _) => {
        addUserToRecommenderSys(snapshot.id);
        return Promise.resolve();
    });

/**
 * Listens for a deletion on the main box collection and deletes
 * the box in all locations.
 */
exports.onBoxDeleted = functions.firestore
    .document('/boxes/{box}')
    .onDelete((snap, context) => {
        const box = snap.data();
        const boxId = snap.id;
        console.log(`Deleting box with ID: ${boxId} created by user: ${box.publisher}`);

        const mapBoxes = mapBoxesRef.doc(boxId).delete();
        const userBoxes = usersRef.doc(box.publisher).collection('boxes').doc(boxId).delete();
        const likes = likesRef.where('boxId', '==', boxId).get().then(likes => likes.docs.map(like => like.ref.delete()));
        // Delete all the "x has liked your box" activity feed items in the box owner activity feed for this particular box.
        const ownerLikeFeed = usersRef.doc(box.publisher).collection('activity')
            .where('typename', '==', 'like')
            .where('data.boxId', '==', boxId)
            .get()
            .then(l => l.docs.map(l => l.ref.delete()));

        deleteBoxInRecommenderys(boxId);

        return Promise.all([mapBoxes, userBoxes, likes, ownerLikeFeed]);
    });

/**
 * Listens for updates on existing boxes and updates the derived box versions also.
 * Only used for updating the box status.
 */
exports.onBoxUpdate = functions.firestore
    .document('/boxes/{box}')
    .onUpdate((change, _) => {
        const boxId = change.after.id;
        const boxStatus = change.after.data().status;
        const publisherId = change.after.data().publisher;
        console.log(`Updating box status for box with ID: ${boxId}, new status: ${boxStatus}`);

        const mapBoxes = updateBoxStatus(mapBoxesRef.doc(boxId), boxStatus);
        const userBoxes = updateBoxStatus(usersRef.doc(publisherId).collection('boxes').doc(boxId), boxStatus);

        // Delete likes and activity data related to the box, if it is no longer visible.
        const likes = boxStatus !== 0 ? likesRef.where('boxId', '==', boxId).get()
            .then(likes => likes.docs.map(like => like.ref.delete())) : Promise.resolve();


        updateBoxStatusInRecommendationSys(boxId, boxStatus);

        return Promise.all([mapBoxes, userBoxes, likes]);
    });


/**
 * 1. Creates a like notification for the owner of the box that is being liked
 * 2. Adds a reference to the like notification of the box owner, this is used for deletion when the like is removed.
 * 3. Adds the box that was liked to the user (the user that liked the box) box like activitiy feed.
 * 4. Checks to see if the two users have liked each others boxes. If that is the case, notify users of a match and start a chat.
 */
exports.onLikeUploaded = functions.firestore
    .document('/likes/{like}')
    .onCreate(async (snapshot, _) => {
        const likedById = snapshot.data().likedByUserId;
        const boxId = snapshot.data().boxId;
        const timestamp = snapshot.data().timestamp;
        console.log(`[NEW LIKE] liked by: ${likedById}, boxId: ${boxId}`);

        const likedByUser = await usersRef.doc(likedById).get();
        const box = await boxesRef.doc(boxId).get();
        const boxOwnerId = box.data().publisher;
        const boxOwnerUser = await usersRef.doc(boxOwnerId).get();
        const ownerActivityFeed = addToBoxOwnerActivityFeed(boxOwnerId, timestamp, likedById, likedByUser, box, boxId, snapshot);
        const userLikedFeed = addToLikerActivityFeed(likedById, boxId, box, boxOwnerId);
        const matchCheck = checkForPotentialMatch(boxOwnerId, likedById, timestamp, boxOwnerUser, likedByUser);

        likeBoxInRecommendationSys(boxId, likedById);

        return Promise.all([userLikedFeed, ownerActivityFeed, matchCheck]);
    });

/**
 * Listens for updates on the user.
 */
exports.onUserUpdate = functions.firestore
    .document('/users/{user}')
    .onUpdate((change, _) => {
        const userId = change.after.id;
        const subjects = change.after.data().favoriteGenres;

        updatePreferredSubjectsInRecommendationSys(userId, subjects);
        return Promise.resolve();
    });

/**
 * Checks to see if there is a match between two users.
 * 1. Checks if both users have liked each others boxes
 * 2. If yes, then check if they do not already have an active match.
 * 3. If yes, then add the match to their activity feeds as well as create a match document which will contain their chat messages.
 * @param {String} boxOwnerId The owner of the box which was just liked
 * @param {String} likedById The user that performed the like
 * @param {Timestamp} timestamp The timestamp of the like
 * @param {User} boxOwnerUser User object belonging to the owner of the box which was just liked
 * @param {User} likedByUser User object belonging to the user that liked the box
 */
async function checkForPotentialMatch(boxOwnerId, likedById, timestamp, boxOwnerUser, likedByUser) {
    console.log(`[MATCH CHECK] Checking for match between ${boxOwnerId} and ${likedById}`);
    const docs = await usersRef.doc(boxOwnerId)
        .collection('liked_boxes')
        .where('boxOwnerId', '==', likedById)
        .get();
    if (!docs.empty) {
        // The two users have matched since they have both liked at least one of the other users' boxes.
        // Check if they alreday have an active match with each other.
        const usersAreAlreadyMatched = await matchRef
            .where('participants', 'array-contains-any', [boxOwnerId, likedById])
            .where('active', '==', true)
            .get()
            .then(snap => snap.docs.filter(doc => {
                const participants = doc.data().participants;
                return participants.includes(boxOwnerId) && participants.includes(likedById);
            }).length > 0);

        console.log(`[MATCH CHECK] Do the users already have an active match? ${usersAreAlreadyMatched}`);

        if (!usersAreAlreadyMatched) {
            // Create new document in chat collection.
            return matchRef.add({
                participants: [likedById, boxOwnerId],
                active: true
            }).then(chatRef => {
                return Promise.all([
                    addMatchNotification(likedById, timestamp, chatRef.id, boxOwnerUser.data()),
                    addMatchNotification(boxOwnerId, timestamp, chatRef.id, likedByUser.data())
                ]);
            });
        }
        // Do not send notifications and create new chat since the two users are already matched.
        return Promise.resolve();
    } else {
        console.log('[MATCH CHECK] No match. Cause: one-directional like');
        return Promise.resolve();
    }
}

/**
 * Adds a recently liked box to the liked box feed of the user that performed the like.
 * @param {String} likedById The ID of the user that was liked
 * @param {String} boxId The ID of the box that was liked
 * @param {Object} box The box that was liked
 * @param {String} boxOwnerId The ID of the owner of the box
 */
function addToLikerActivityFeed(likedById, boxId, box, boxOwnerId) {
    return usersRef
        .doc(likedById)
        .collection('liked_boxes')
        .doc(boxId)
        .set({
            status: box.data().status,
            publishDateTime: box.data().publishDateTime,
            title: box.data().title,
            bookThumbnailUrl: box.data().books[0].thumbnailUrl,
            boxOwnerId: boxOwnerId
        });
}

/**
 * Adds a notification that a box was liked to the activity feed of the owner of the box which was liked.
 * @param {String} boxOwnerId The ID of the box owner
 * @param {Timestamp} timestamp The date and time of the like
 * @param {String} likedById The ID of the user that performed the like
 * @param {Object} likedByUser The user that performed the like
 * @param {Object} box The box which was liked
 * @param {String} boxId The ID of the box which was liked
 * @param {FirebaseFirestore.QuerySnapshot} snapshot The snapshot of the like item in Firestore ('likes' collection)
 */
async function addToBoxOwnerActivityFeed(boxOwnerId, timestamp, likedById,
    likedByUser, box, boxId, snapshot) {
    const ref = await usersRef
        .doc(boxOwnerId)
        .collection('activity')
        .add({
            timestamp: timestamp,
            read: false,
            typename: 'like',
            data: {
                likedByUserId: likedById,
                likedByUsername: likedByUser.data().displayName,
                boxTitle: box.data().title,
                boxId: boxId
            }
        });
    return snapshot.ref.set({
        // Create a reference to the activity item in the original like document.
        // This is used when deleting the like document
        activityFeedReference: ref.id
    }, {
        merge: true
    });
}

/**
 * Adds a match notification to the [userId] activity collection. Also adds an initial chat message activity item.
 * @param {String} userId The user id of the user that is being notified of the match.
 * @param {Timestamp} timestamp The timestamp for the match
 * @param {String} chatId The ID of the chat between the two users.
 * @param {User} matchUser The other user that the user has matched with. This is not the same user as in the [userId] parameter.
 */
async function addMatchNotification(userId, timestamp, chatId, matchUser) {
    console.log(`[MATCH ACTIVITY] Creating activity notification for user: ${userId}`);
    return usersRef
        .doc(userId)
        .collection('activity')
        .add({
            timestamp: timestamp,
            read: false,
            typename: 'match',
            data: {
                chatRef: chatId,
                matchUserName: matchUser.displayName
            }
        }).then(_ => {
            // Add initial chat activity item
            return usersRef.doc(userId).collection('activity').doc(chatId).set({
                typename: 'chat',
                timestamp: timestamp,
                read: false,
                data: {
                    otherUserName: matchUser.displayName,
                    otherUserThumbnail: matchUser.photoURL,
                    lastMessage: null,
                }
            });
        });
}

/**
 * Triggered when a like is deleted.
 * Deletes any reference to the like from both the box owner, as well as the user that the like belongs to.
 */
exports.onLikeDeleted = functions.firestore
    .document('/likes/{like}')
    .onDelete(async (snapshot, _) => {
        const likedById = snapshot.data().likedByUserId;
        const boxId = snapshot.data().boxId;
        console.log(`[DELETING LIKE] liked by: ${likedById}, boxId: ${boxId}`);

        const box = await boxesRef.doc(boxId).get();
        let ownerActivityFeed = Promise.resolve();
        // The box could be deleted before the like is removed. Check if it exists.
        // Deletion of owner activity feed reference is handled in onBoxDeleted trigger. This is for update of box status.
        if (box.exists) {
            const boxOwnerId = box.data().publisher;
            ownerActivityFeed = usersRef
                .doc(boxOwnerId)
                .collection('activity')
                .doc(snapshot.data().activityFeedReference)
                .delete();
        }

        const userLikedFeed = usersRef
            .doc(likedById)
            .collection('liked_boxes')
            .doc(boxId)
            .delete();

        deleteLikeInRecommenderSys(likedById, boxId);

        return Promise.all([ownerActivityFeed, userLikedFeed]);
    });


/**
 * Triggered when a user posts a trade offer request.
 * Adds a notification item in the activity feed of the recipient user.
 */
exports.onTradeRequestAdded = functions.firestore
    .document('/matches/{match}/trade_offers/{offer}')
    .onCreate(async (snapshot, context) => {
        const tradeReq = snapshot.data();
        const matchId = context.params.match;
        const recipientId = tradeReq.offerRecipientId;

        const username = await usersRef.doc(tradeReq.offerByUserId).get().then(doc => doc.data().displayName);

        return usersRef.doc(recipientId).collection('activity').add({
            read: false,
            typename: 'trade',
            timestamp: tradeReq.timestamp,
            data: {
                username: username,
                event: 'new',
                matchId: matchId
            }
        });
    });

/**
 * Triggered when a user accepts or rejects a trade request.
 * Adds a notification item in the activity feed of the request owner.
 */
exports.onTradeRequestChanged = functions.firestore
    .document('/matches/{match}/trade_offers/{offer}')
    .onUpdate(async (snapshot, context) => {
        const tradeReq = snapshot.after.data();
        const matchId = context.params.match;

        const username = await usersRef.doc(tradeReq.offerRecipientId).get().then(doc => doc.data().displayName);

        // Only check if both are accepted when the updated offer is accepted. Set to useless promise to start with.
        let checkIfBothAccepted = Promise.resolve(true);
        let eventType;
        if (tradeReq.status === 0) {
            console.log(`[TRADE COMPLETION CHECK] Trade offer in match ${matchId} updated with ACCEPTED status`);
            eventType = 'accepted';
            // Since offer was accepted, check if both users have an accepted offer.
            checkIfBothAccepted = checkIfTradeIsComplete(matchId);
        } else if (tradeReq.status === 1) {
            eventType = 'rejected';
        } else {
            eventType = 'unknown';
        }

        const addNotification = usersRef.doc(tradeReq.offerByUserId).collection('activity').add({
            read: false,
            typename: 'trade',
            timestamp: tradeReq.timestamp,
            data: {
                username: username,
                event: eventType,
                matchId: matchId
            }
        });

        return Promise.all([addNotification, checkIfBothAccepted]);
    });


/**
 * Checks if a match has two accepted trade offers, meaning that the trade is
 * complete and match will be marked as no longer active.
 * @param {String} matchId The ID of the match that is to be checked
 */
async function checkIfTradeIsComplete(matchId) {
    const acceptedOffers = await matchRef.doc(matchId)
        .collection('trade_offers')
        .where('status', '==', 0)
        .get();
    console.log(`[TRADE COMPLETION CHECK] Number of accepted trade offers: ${acceptedOffers.docs.length}`);
    if (acceptedOffers.docs.length >= 2) {
        console.log(`[TRADE COMPLETION CHECK] Match with ID ${matchId} is complete`);
        // Remove boxes involved in the trade.
        const boxRemoval = acceptedOffers.docs.map(doc => setBoxAsTraded(doc.data().boxId));
        // Trade is complete and the match is therefore no longer active.
        const matchUpdate = matchRef.doc(matchId).update({ active: false });
        return Promise.all([boxRemoval, matchUpdate]);
    }
    return Promise.resolve(true);
}



/**
 * Sets the status property on a box to traded (2).
 * This function starts a chain reaction which updates 
 * the status properties of all boxes collections as well as the external reccomender system.
 * @param {String} boxId The ID of the box to be removed.
 */
function setBoxAsTraded(boxId) {
    // status 2 represents a traded box.
    return boxesRef.doc(boxId).update({ status: 2 });
}

/**
 ` Triggered when a user posts a chat message.
 * Updates the activity item for a particular chat for both participants in the chat.
 */
exports.onMessagePosted = functions.firestore
    .document('/matches/{match}/messages/{message}')
    .onCreate(async (snapshot, context) => {
        const matchId = context.params.match;
        const message = snapshot.data();

        const match = await matchRef.doc(matchId).get();
        const [user1, user2] = await Promise.all(match.data().participants.map(uid => usersRef.doc(uid).get()));

        return Promise.all([
            updateChatActivityItem(user1.data(), user2.data(), matchId, message),
            updateChatActivityItem(user2.data(), user1.data(), matchId, message)
        ]);
    });

/**
 * Updates the chat activity item for one user in a match.
 * @param {Object} activityOwner The owner of the activity that is being updated
 * @param {Object} otherChatUser The user that the owner is chatting with
 * @param {String} matchId The match ID belonging to the match between the two users
 * @param {String} message The actual content of the newest message in the chat
 */
async function updateChatActivityItem(activityOwner, otherChatUser, matchId, message) {
    return usersRef.doc(activityOwner.uid).collection('activity').doc(matchId).set({
        read: false,
        timestamp: message.timestamp,
        data: {
            lastMessage: message.content,
            otherUserName: otherChatUser.displayName,
            otherUserThumbnail: otherChatUser.photoURL
        }
    }, {
        merge: true
    });
}

async function updateBoxStatus(docRef, status) {
    return docRef.set({
        status: status
    }, {
        merge: true
    });
}

function boxToMapBoxItem(box) {
    return {
        publisher: box.publisher,
        status: box.status,
        publishDateTime: box.publishDateTime,
        latitude: box.latitude,
        longitude: box.longitude,
        title: box.title,
        description: box.description,
        books: box.books.map(b => {
            return {
                thumbnailUrl: b.thumbnailUrl,
                categories: b.categories,
                authors: b.authors,
                title: b.title
            }
        })
    };
}

function boxToProfileBoxItem(box) {
    return {
        status: box.status,
        publishDateTime: box.publishDateTime,
        title: box.title,
        bookThumbnailUrl: box.books[0].thumbnailUrl
    };
}

function boxToRecommendationItems(id, box) {
    return {
        id: id,
        publisher: box.publisher,
        status: box.status,
        publishDateTime: box.publishDateTime,
        latitude: box.latitude,
        longitude: box.longitude,
        title: box.title,
        description: box.description,
        books: box.books.map(b => {
            if (b.categories.length > 0 && b.thumbnailUrl !== null && b.thumbnailUrl !== undefined) {
                return {
                    thumbnailUrl: b.thumbnailUrl,
                    categories: b.categories
                }
            }
        })
    };
}

function uploadBoxToRecommendationSys(boxFeedItem) {
    request.post({
        headers: { 'content-type': 'application/json' },
        url: recommenderApiUrl + 'box?key=' + recommenderApiKey,
        body: JSON.stringify(boxFeedItem)
    }, function (error, response, body) {
        if (error) {
            return console.error(`Uploading box to recommender system failed: ${error}`);
        }
        return console.log('Uploaded box to recommedner system.');
    });
}

function updateBoxStatusInRecommendationSys(boxId, boxStatus) {
    request.put(recommenderApiUrl + 'box/status?key=' + recommenderApiKey + '&boxId=' + boxId + '&status=' + boxStatus)
        .on('error', function (err) {
            console.error(`Failed to update box status in recommender system: ${err}`);
        });
}

/**
 * Calls the like endpoint of the recommender API.
 *  
 * @param {String} boxId Id of the box that was liked. 
 * @param {String} userId The id of the user of which liked the box.
 */
function likeBoxInRecommendationSys(boxId, userId) {
    request.get(`${recommenderApiUrl}like?key=${recommenderApiKey}&userId=${userId}&boxId=${boxId}`)
        .on('error', function (err) {
            console.error(`Failed to send box like to recommendation system: ${err}`);
        });
}

/**
 * Updates the preferred book subjects by passing the subjects to the
 * recommendation system API.
 * 
 * @param {String} userId The id of the user of whom to update for. 
 * @param {Array} subjects An array of book subjects 
 */
function updatePreferredSubjectsInRecommendationSys(userId, subjects) {
    var jsonSubjects;
    if (subjects === null || subjects === "") {
        jsonSubjects = null;
    } else {
        jsonSubjects = JSON.parse(subjects);
    }
    var jsonSubjectsObj = { subjects: jsonSubjects };
    var subjectString = JSON.stringify(jsonSubjectsObj);

    console.log('Updating prefered subjects to: ' + subjectString);
    request.put({
        headers: { 'content-type': 'application/json' },
        url: recommenderApiUrl + 'preferences?key=' + recommenderApiKey + '&userId=' + userId,
        body: subjectString
    }, function (error, response, body) {
        if (error) {
            return console.error(`Failed to update user preferences in recommender system: ${error}`);
        }
        return console.log('Uploaded new user preferences to recommender system.');
    });
}

function addUserToRecommenderSys(userId) {
    userJSON = { Id: userId };
    userString = JSON.stringify(userJSON);
    request.post({
        headers: { 'content-type': 'application/json' },
        url: recommenderApiUrl + 'users?key=' + recommenderApiKey,
        body: userString
    }, function (error, response, body) {
        if (error) {
            return console.error(`Uploading user(${userId}) to recommender system failed: ${error}`);
        }
        return console.log(`Uploaded user (${userId}) to recommedner system.`);
    });
}

function deleteLikeInRecommenderSys(userId, boxId) {
    request.delete(recommenderApiUrl + 'like?key=' + recommenderApiKey + '&userId=' + userId + '&boxId=' + boxId)
        .on('error', function (err) {
            console.error(`Failed to delete like with error: ${err}`);
        });
}

function deleteBoxInRecommenderys(boxId) {
    request.delete(recommenderApiUrl + 'box?key=' + recommenderApiKey + '&boxId=' + boxId)
        .on('error', function (err) {
            console.error(`Failed to delete box with error: ${err}`);
        });
}
