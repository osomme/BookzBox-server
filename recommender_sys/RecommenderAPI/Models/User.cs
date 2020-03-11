using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class User
    {
        public User()
        {

        }

        public User(string id)
        {
            Id = id;
        }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Id { get; set; }
    }
}