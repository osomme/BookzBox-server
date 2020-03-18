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
            this.Subjects = subjects;
            this.ThumbnailUrl = thumbnailUrl;
        }

        public string[] Subjects { get; set; }
        [Required]
        public string ThumbnailUrl { get; set; }
    }
}