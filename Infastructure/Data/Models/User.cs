using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WatchedApi.Data.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public bool IsAdmin { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Post> Posts { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<PostLike> PostLikes { get; set; }
    }
}
