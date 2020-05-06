using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Book
    {
        public Book()
        {
        }

        public Book(string[] subjects, string thumbnailUrl, string isbn)
        {
            this.Categories = subjects;
            this.ThumbnailUrl = thumbnailUrl;
            this.Isbn = isbn;
        }

        public string[] Categories { get; set; }

        public string ThumbnailUrl { get; set; }

        [Required]
        public string Isbn { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Book book && Isbn == book.Isbn;
        }

        public override int GetHashCode()
        {
            return Isbn.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}