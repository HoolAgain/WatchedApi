using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WatchedApi.Data.Models
{
    public class PostLike
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LikeId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int PostId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("PostId")]
        public Post Post { get; set; }
    }
}
