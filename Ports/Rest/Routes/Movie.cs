using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WatchedApi.Ports.Rest.Controllers
{
    [ApiController]
    [Route("api/movies")]
    public class MovieController : ControllerBase
    {
        private readonly MovieService _movieService;
        private readonly ApplicationDbContext _context;

        public MovieController(MovieService movieService, ApplicationDbContext context)
        {
            _movieService = movieService;
            _context = context;

        }

        //fetch movies from ombdAPI
        [HttpPost("fetch")]
        public async Task<IActionResult> FetchMovies()
        {
            var movies = await _movieService.FetchMoviesFromAPI();
            return Ok(new { message = movies });
        }


        //rate movies
        [HttpPost("{id}/ratemovie")]
        [Authorize]
        public async Task<IActionResult> RateMovie(int id, [FromBody] MovieRating request)
        {
            //find userid
            var userIdClaim = User.FindFirst("userId")?.Value;
            //check id
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Missing id" });
            }

            //parse id
            int userId = int.Parse(userIdClaim);

            if (request == null || request.Rating < 1 || request.Rating > 10)
            {
                return BadRequest(new { message = "Rating must be between 1 and 10" });
            }

            //bool is userid, movieid and rating valid
            bool success = await _movieService.RateMovie(userId, id, request.Rating);

            //if rating already done show error
            if (!success)
            {
                return Conflict(new { message = "You have already rated this movie" });
            }

            return Ok(new { message = "Rating submitted successfully" });
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetMovieDetails(int id)
        {
            //get all movie data given movie id
            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.MovieId == id);

            if (movie == null)
            {
                return NotFound(new { message = "Movie not found" });
            }

            return Ok(movie);
        }
    }

}



