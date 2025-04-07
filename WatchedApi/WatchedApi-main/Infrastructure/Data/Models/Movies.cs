using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WatchedApi.Infrastructure.Data.Models
{
    public class Movie
    {
        [Key]
        public int MovieId { get; set; }

        [Required]
        public string Title { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Director { get; set; }
        public string PosterUrl { get; set; }
        public string Actors { get; set; }
        public string Plot { get; set; }

        public double AverageRating { get; set; } = 0.0;
    }
}