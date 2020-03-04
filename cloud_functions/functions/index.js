const functions = require('firebase-functions');
const admin = require('firebase-admin');
admin.initializeApp();

const mapBoxesRef = admin.firestore().collection('map_boxes');
const usersRef = admin.firestore().collection('users');
const boxFeedRef = admin.firestore().collection('feed_boxes');

/**
 * Listens for new boxes on the /boxes/ collection and creates derived versions.
 */
exports.onBoxUploaded = functions.firestore
    .document('/boxes/{box}')
    .onCreate(async (snapshot, _) => {
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
    .onUpdate(async (change, _) => {
        const boxId = change.after.id;
        const boxStatus = change.after.data().status;
        const publisherId = change.after.data().publisher;
        console.log(`Updating box status for box with ID: ${boxId}, new status: ${boxStatus}`);

        const mapBoxes = updateBoxStatus(mapBoxesRef.doc(boxId), boxStatus);
        const userBoxes = updateBoxStatus(usersRef.doc(publisherId).collection('boxes').doc(boxId), boxStatus);
        const boxFeed = updateBoxStatus(boxFeedRef.doc(boxId), boxStatus);
        return Promise.all([mapBoxes, userBoxes, boxFeed]);
        //TODO: Remove likes belonging to a box if the new status is NOT active.
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