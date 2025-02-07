using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WatchedApi.Data.Models
{
    public class AdminLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogId { get; set; }

        [Required]
        public int AdminId { get; set; }

        [Required]
        public string Action { get; set; }

        public int? TargetUserId { get; set; }
        public int? TargetPostId { get; set; }
        public int? TargetCommentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("AdminId")]
        public User Admin { get; set; }

        [ForeignKey("TargetUserId")]
        public User TargetUser { get; set; }

        [ForeignKey("TargetPostId")]
        public Post TargetPost { get; set; }

        [ForeignKey("TargetCommentId")]
        public Comment TargetComment { get; set; }
    }
}
