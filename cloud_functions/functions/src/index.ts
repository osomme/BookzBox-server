import * as functions from 'firebase-functions';
import * as req from 'request';

// // Start writing Firebase Functions
// // https://firebase.google.com/docs/functions/typescript
//

/*
* Takes a ISBN string as input and returns a json object containing information
* about the book which has the ISBN number which was passed in.
*/
export const isbnLookup = functions.https.onRequest(async (request, response) => {
    const isbn: string = request.query.isbn || request.query.body.isbn;
    if (!isbn) {
        response.json({ error: 'No ISBN string passed' });
        return;
    }
    await fetchISBNData(isbn, (data) => response.json(data));
});

/*
* Function which performs a ISBN lookup fetch in response to a cloud functions client API call.
*/
export const isbnLookupCall = functions.https.onCall(async (data, _) => {
    const isbn: string = data.isbn;
    await fetchISBNData(isbn, (data) => { return data });
});

const fetchISBNData = async (isbn: string, callback: (data: Book | null) => void) => {
    console.log(`Looking up the ISBN number: ${isbn}`);
    const url = `https://www.googleapis.com/books/v1/volumes?q=isbn=${isbn}&printTypes=books`;
    req(url, (error, _, body) => {
        if (error) {
            console.log(error);
            callback(null);
        } else {
            const res = JSON.parse(body);
            if (res.totalItems > 0) {
                const item = res.items.find((b: any) =>
                    b.volumeInfo.industryIdentifiers?.some((id: { identifier: string }) => id.identifier === isbn));
                console.log(`Requested item: ${item}`);
                callback(extractBookInfo(item));
            } else {
                console.log(`ISBN ${isbn} was not found`);
                callback(null);
            }
        }
    });
};

/*
* Maps a response from the Google Books API to a more consise and short format.
* Google Books API information can be found at: https://developers.google.com/books/docs/v1/using
*/
const extractBookInfo = (item: { volumeInfo: any }) => {
    const info = item.volumeInfo;
    const isbn10 = info.industryIdentifiers?.filter(
        (x: { type: string, identifier: string }) => x.type === "ISBN_10")[0]?.identifier;
    const isbn13 = info.industryIdentifiers?.filter(
        (x: { type: string, identifier: string }) => x.type === "ISBN_13")[0]?.identifier;
    const book: Book = {
        title: info.title, authors: info.authors, fullSizeImageUrl: info.imageLinks?.thumbnail,
        thumbnailUrl: info.imageLinks?.smallThumbnail, isbn10: isbn10, isbn13: isbn13, pageCount: info.pageCount,
        publishYear: parseInt(info.publishedDate), categories: info.categories, synopsis: info.description,
        publisher: info.publisher
    };
    return book;
};
