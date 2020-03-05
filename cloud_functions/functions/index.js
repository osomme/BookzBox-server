const functions = require('firebase-functions');
const admin = require('firebase-admin');
admin.initializeApp();

const firestore = admin.firestore();
const mapBoxesRef = firestore.collection('map_boxes');
const usersRef = firestore.collection('users');
const boxFeedRef = firestore.collection('feed_boxes');
const likesRef = firestore.collection('likes');
const boxesRef = firestore.collection('boxes');

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


exports.onLikeUploaded = functions.firestore
    .document('/likes/{like}')
    .onCreate(async (snapshot, context) => {
        const likedById = snapshot.data().likedByUserId;
        const boxId = snapshot.data().boxId;
        const timestamp = snapshot.data().timestamp;
        console.log(`[NEW LIKE] liked by: ${likedById}, boxId: ${boxId}`);

        const likedByUser = await usersRef.doc(likedById).get();
        const box = await boxesRef.doc(boxId).get();
        const boxOwnerId = box.data().publisher;
        const userActivityFeed = await usersRef
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

        const activityReference = snapshot.ref.set({
            activityFeedReference: userActivityFeed.id
        }, {
            merge: true
        });

        const userLikedFeed = usersRef
            .doc(likedById)
            .collection('liked_boxes')
            .doc(boxId)
            .set({
                status: box.data().status,
                publishDateTime: box.data().publishDateTime,
                title: box.data().title,
                bookThumbnailUrl: box.data().books[0].thumbnailUrl
            });

        return Promise.all([userLikedFeed, activityReference]);
    });

exports.onLikeDeleted = functions.firestore
    .document('/likes/{like}')
    .onDelete(async (snapshot, context) => {
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