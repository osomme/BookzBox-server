using System;
using System.Collections.Generic;

namespace Models
{
    public enum BoxStatus
    {
        Public,
        Private,
        Traded,
    }

    public class MiniBox
    {
        private string _id;
        private string _publisherId;
        private DateTime _publishedOn;
        private string _title;
        private string _description;
        private double _lat;
        private double _lng;
        private IList<MiniBook> _books;
        private BoxStatus _status;

        public MiniBox()
        {

        }

        public MiniBox(string id, string publisherId, DateTime publishedOn, string title, string description, double lat, double lng, IList<MiniBook> books, BoxStatus status)
        {
            Id = id;
            PublisherId = publisherId;
            PublishedOn = publishedOn;
            Title = title;
            Description = description;
            Lat = lat;
            Lng = lng;
            Books = books;
            Status = status;
        }

        public string Id { get => _id; set => _id = value; }
        public string PublisherId { get => _publisherId; set => _publisherId = value; }
        public DateTime PublishedOn { get => _publishedOn; set => _publishedOn = value; }
        public string Title { get => _title; set => _title = value; }
        public string Description { get => _description; set => _description = value; }
        public double Lat { get => _lat; set => _lat = value; }
        public double Lng { get => _lng; set => _lng = value; }
        internal IList<MiniBook> Books { get => _books; set => _books = value; }
        internal BoxStatus Status { get => _status; set => _status = value; }
    }
}