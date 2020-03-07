const functions = require('firebase-functions');
const admin = require('firebase-admin');
admin.initializeApp();

const firestore = admin.firestore();
const mapBoxesRef = firestore.collection('map_boxes');
const usersRef = firestore.collection('users');
const boxFeedRef = firestore.collection('feed_boxes');
const likesRef = firestore.collection('likes');
const boxesRef = firestore.collection('boxes');
const matchRef = firestore.collection('matches');

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
        const boxFeed = boxFeedRef.doc(boxId).set(boxToFeedBoxItem(box));
        return Promise.all([mapBoxes, userBoxes, boxFeed]);
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
        const boxFeed = updateBoxStatus(boxFeedRef.doc(boxId), boxStatus);

        // Delete likes and activity data related to the box, if it is no longer visible.
        const likes = boxStatus !== 0 ? likesRef.where('boxId', '==', boxId).get()
            .then(likes => likes.forEach(like => like.ref.delete())) : Promise.resolve();

        return Promise.all([mapBoxes, userBoxes, boxFeed, likes]);
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

        return Promise.all([userLikedFeed, ownerActivityFeed, matchCheck]);
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
        const boxOwnerId = box.data().publisher;
        const userActivityFeed = usersRef
            .doc(boxOwnerId)
            .collection('activity')
            .doc(snapshot.data().activityFeedReference)
            .delete();

        const userLikedFeed = usersRef
            .doc(likedById)
            .collection('liked_boxes')
            .doc(boxId)
            .delete();

        return Promise.all([userActivityFeed, userLikedFeed]);
    });

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
                categories: b.categories
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

function boxToFeedBoxItem(box) {
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
                categories: b.categories
            }
        })
    };
}