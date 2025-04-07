using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WatchedApi.Infrastructure.Data.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        public string? Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public bool IsAdmin { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        //initialized with empty string so that these arent required
        public ICollection<Post>? Posts { get; set; } = new List<Post>();
        public ICollection<Comment>? Comments { get; set; } = new List<Comment>();
        public ICollection<PostLike>? PostLikes { get; set; } = new List<PostLike>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    }
}
