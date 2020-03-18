using System;
using System.Collections.Generic;
using Models;
using Neo4j.Driver;

public class BoxRecordMapper : IRecordMapper<Box>
{
    readonly BookMapper _bookMapper = new BookMapper();

    public Box Map(IRecord record)
    {

        object boxDictionaryObj;
        var hasBoxDict = record.Values.TryGetValue("box", out boxDictionaryObj);
        if (!hasBoxDict)
        {
            return null;
        }
        INode boxDictionary = (INode)boxDictionaryObj;

        // Map BOX
        Box box = new Box();
        box.Id = boxDictionary.Properties["boxId"] as string;
        box.PublisherId = boxDictionary.Properties["publisherId"] as string;
        box.PublishedOn = DateTimeOffset.FromUnixTimeSeconds((long)boxDictionary.Properties["publishedOn"]).DateTime;
        box.Title = boxDictionary.Properties["title"] as string;
        box.Description = boxDictionary.Properties["description"] as string;
        box.Status = (BoxStatus)((int)((long)boxDictionary.Properties["status"]));
        box.Lat = (double)boxDictionary.Properties["lat"];
        box.Lng = (double)boxDictionary.Properties["lng"];

        // MAP BOOKS
        box.Books = _bookMapper.MapAll(record).ToArray();

        return box;
    }

}