using System;
using System.Collections.Generic;
using Models;
using Neo4j.Driver;
using Newtonsoft.Json;

public class BookMapper
{
    public Book Map(INode node)
    {
        Book book = new Book();

        book.ThumbnailUrl = node.Properties["thumbnailUrl"] as string;
        book.Categories = parseSubjects(node.Properties["subjects"] as string);

        return book;
    }

    public List<Book> MapAll(IRecord record)
    {
        List<Book> books = new List<Book>();

        object bookDictionaryObj;
        var hasBookDict = record.Values.TryGetValue("books", out bookDictionaryObj);
        if (hasBookDict)
        {
            List<object> bookDictionary = (List<object>)bookDictionaryObj;
            foreach (object node in bookDictionary)
            {
                var book = Map(node as INode);
                if (!books.Contains(book))
                {
                    books.Add(book);
                }
            }
        }
        return books;
    }

    private string[] parseSubjects(string subjects)
    {
        //return subjects.Trim().Substring(1, subjects.Trim().Length - 2).Split(',');
        return JsonConvert.DeserializeObject<string[]>(subjects);
    }
}