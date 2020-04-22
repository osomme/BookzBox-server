using System;
using System.Collections.Generic;

static class SubjectMapper
{
    public static string[] ToStringArray(IEnumerable<BookSubject> subjects)
    {

        List<string> subjectStrings = new List<string>();

        if (subjects == null)
        {
            return subjectStrings.ToArray();
        }

        foreach (var subject in subjects)
        {
            subjectStrings.Add(ToString(subject));
        }
        return subjectStrings.ToArray();
    }

    static string ToString(BookSubject subject)
    {
        switch (subject)
        {
            case BookSubject.Action:
                return "Action";
            case BookSubject.Adventure:
                return "Adventure";
            case BookSubject.Anthology:
                return "Anthology";
            case BookSubject.Children:
                return "Children";
            case BookSubject.Comic:
                return "Comic";
            case BookSubject.ComingOfAge:
                return "Coming Of Age";
            case BookSubject.Crime:
                return "Crime";
            case BookSubject.Drama:
                return "Drama";
            case BookSubject.Fairytale:
                return "Fairytale";
            case BookSubject.GraphicNovel:
                return "GraphicNovel";
            case BookSubject.HistoricalFiction:
                return "HistoricalFiction";
            case BookSubject.Horror:
                return "Horror";
            case BookSubject.Mystery:
                return "Mystery";
            case BookSubject.Paranormal:
                return "Paranormal";
            case BookSubject.PictureBook:
                return "Picture Book";
            case BookSubject.Poetry:
                return "Poetry";
            case BookSubject.PoliticalThriller:
                return "Political Thriller";
            case BookSubject.Romance:
                return "Romance";
            case BookSubject.Satire:
                return "Satire";
            case BookSubject.ScienceFiction:
                return "Science Fiction";
            case BookSubject.ShortStory:
                return "Short-story";
            case BookSubject.Suspense:
                return "Suspense";
            case BookSubject.Thriller:
                return "Thriller";
            case BookSubject.YoungAdult:
                return "Young Adult";
            case BookSubject.Art:
                return "Art";
            case BookSubject.Autobiography:
                return "Autobiography";
            case BookSubject.Biography:
                return "Biography";
            case BookSubject.Cookbook:
                return "Cookbook";
            case BookSubject.Diary:
                return "Diary";
            case BookSubject.Dictionary:
                return "Dictionary";
            case BookSubject.Encyclopedia:
                return "Encyclopedia";
            case BookSubject.Educational:
                return "Educational";
            case BookSubject.Women:
                return "Women";
            case BookSubject.Guide:
                return "Guide";
            case BookSubject.Health:
                return "Health";
            case BookSubject.History:
                return "History";
            case BookSubject.Journal:
                return "Journal";
            case BookSubject.Math:
                return "Math";
            case BookSubject.Memoir:
                return "Memoir";
            case BookSubject.Prayer:
                return "Prayer";
            case BookSubject.Religion:
                return "Religion";
            case BookSubject.Textbook:
                return "Textbook";
            case BookSubject.Science:
                return "Science";
            case BookSubject.Selfhelp:
                return "Selfhelp";
            case BookSubject.Travel:
                return "Travel";
            case BookSubject.TrueCrime:
                return "True Crime";
            case BookSubject.Classic:
                return "Classic";
            case BookSubject.Humor:
                return "Humor";
            case BookSubject.Mythology:
                return "Mythology";
            case BookSubject.Fiction:
                return "Fiction";

            default:
                throw new ArgumentOutOfRangeException();
        }

    }
}