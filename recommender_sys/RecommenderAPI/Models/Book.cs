using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Book
    {
        public Book()
        {
        }

        public Book(string[] subjects, string thumbnailUrl)
        {
            this.Categories = subjects;
            this.ThumbnailUrl = thumbnailUrl;
        }

        public string[] Categories { get; set; }
        [Required]
        public string ThumbnailUrl { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Book book && ThumbnailUrl == book.ThumbnailUrl;
        }

    }
}