using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class User
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Id { get; set; }
    }
}