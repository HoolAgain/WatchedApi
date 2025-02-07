using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WatchedApi.Data.Models
{
    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PostId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; }

        public ICollection<Comment> Comments { get; set; }
        public ICollection<PostLike> PostLikes { get; set; }
    }
}
