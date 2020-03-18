using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public enum BoxStatus
    {
        Public = 0,
        Private = 1,
        Traded = 2,
    }

    public class Box : IComparable<Box>
    {
        public Box()
        {

        }

        public Box(string id, string publisherId, DateTime publishedOn, string title, string description,
                   double lat, double lng, Book[] books, BoxStatus status)
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

        [Required]
        public string Id { get; set; }

        [Required]
        public string PublisherId { get; set; }

        public DateTime PublishedOn { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Title { get; set; }
        public string Description { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public Book[] Books { get; set; }
        public BoxStatus Status { get; set; }

        public int CompareTo(Box other)
        {
            return this.Id.CompareTo(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (obj is Box other)
            {
                return this.Id.Equals(other.Id);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override string ToString()
        {
            return this.Id;
        }
    }
}