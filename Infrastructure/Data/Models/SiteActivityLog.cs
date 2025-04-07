using System;
using System.ComponentModel.DataAnnotations;

namespace WatchedApi.Infrastructure.Data.Models
{
    public class SiteActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Activity { get; set; }

        [Required]
        public string Operation { get; set; }

        public DateTime? TimeOf { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}
