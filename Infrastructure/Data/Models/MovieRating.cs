using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WatchedApi.Infrastructure.Data.Models
{
    public class MovieRating
    {
        [Key]
        public int RatingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int MovieId { get; set; }

        [Required]
        [Range(1, 10)] //between 1-10
        public int Rating { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("MovieId")]
        public Movie? Movie { get; set; }
    }
}
