/*
* Data response object for a ISBN lookup search.
*/
interface Book {
    isbn13: string;
    isbn10: string;
    title: string;
    thumbnailUrl: string;
    fullSizeImageUrl: string;
    authors: string[];
    pageCount: number;
    categories: string[];
    publishYear: number;
    synopsis: string;
    publisher: string;
}