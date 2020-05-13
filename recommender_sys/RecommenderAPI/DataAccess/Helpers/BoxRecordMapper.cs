using System;
using System.Collections.Generic;
using Models;
using Neo4j.Driver;

public class BoxRecordMapper : IBoxRecordMapper
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
        box.publisher = boxDictionary.Properties["publisherId"] as string;
        box.publishDateTime = (long)boxDictionary.Properties["publishedOn"];
        box.Title = boxDictionary.Properties["title"] as string;
        box.Description = boxDictionary.Properties["description"] as string;
        box.Status = (BoxStatus)((int)((long)boxDictionary.Properties["status"]));
        try
        {
            box.Latitude = (double)boxDictionary.Properties["lat"];
            box.Longitude = (double)boxDictionary.Properties["lng"];
        }
        catch (System.InvalidCastException)
        {
            Int64 tmpLat = (Int64)boxDictionary.Properties["lat"];
            box.Latitude = (double)tmpLat;
            Int64 tmpLng = (Int64)boxDictionary.Properties["lng"];
            box.Longitude = (double)tmpLng;
        }

        // MAP BOOKS
        box.Books = _bookMapper.MapAll(record).ToArray();

        return box;
    }

}