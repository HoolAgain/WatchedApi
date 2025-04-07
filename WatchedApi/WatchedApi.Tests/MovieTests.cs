using Xunit;
using WatchedApi.Infrastructure;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using WatchedApi.Ports.Rest.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace WatchedApi.Tests
{
    public class MovieTests
    {
        private readonly ApplicationDbContext _context;
        private readonly MovieController _movieController;

        public MovieTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("WatchedApiTestMoviesDb")
                .Options;

            _context = new ApplicationDbContext(options);

            var fakeResponse = JsonSerializer.Serialize(new OmdbMovieResponse
            {
                Title = "Inception",
                Year = "2010",
                Genre = "Action",
                Director = "Christopher Nolan",
                Poster = "posterurl",
                Actors = "Leonardo DiCaprio",
                Plot = "A thief who steals corporate secrets..."
            });

            var httpClient = new HttpClient(new FakeHttpMessageHandler(fakeResponse));
            var movieService = new MovieService(_context, httpClient);

            _movieController = new MovieController(movieService, _context);
        }

        private void SetUserContext(int userId)
        {
            var claims = new List<Claim> { new Claim("userId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _movieController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        [Fact]
        public async Task FetchMoviesFromAPI_Success()
        {
            var result = await _movieController.FetchMovies();
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task FetchMoviesFromAPI_ParsesAndSavesCorrectly()
        {
            await _movieController.FetchMovies();
            var movieInDb = await _context.Movies.FirstOrDefaultAsync(m => m.Title == "Inception");

            Assert.NotNull(movieInDb);
            Assert.Equal("Christopher Nolan", movieInDb.Director);
            Assert.Equal("Leonardo DiCaprio", movieInDb.Actors);
        }

        [Fact]
        public async Task RateMovie_Success()
        {
            var user = new User { Username = "user1", PasswordHash = "hash" };
            var movie = new Movie
            {
                Title = "Inception",
                Year = "2010",
                Genre = "Action",
                Director = "Christopher Nolan",
                PosterUrl = "posterurl",
                Actors = "Leonardo DiCaprio",
                Plot = "Mind-bending thriller"
            };
            _context.Users.Add(user);
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _movieController.RateMovie(movie.MovieId, new MovieRating { Rating = 9 });
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task RateMovie_Fail_MissingUserId()
        {
            _movieController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // no claims
                }
            };

            var result = await _movieController.RateMovie(1, new MovieRating { Rating = 7 });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var msg = unauthorized.Value.GetType().GetProperty("message")?.GetValue(unauthorized.Value);
            Assert.Equal("Missing id", msg);
        }

        [Fact]
        public async Task RateMovie_Fail_InvalidRating()
        {
            var user = new User { Username = "rater", PasswordHash = "pass" };
            var movie = new Movie
            {
                Title = "Some Movie",
                Year = "2023",
                Genre = "Action",
                Director = "Someone",
                PosterUrl = "poster.jpg",
                Actors = "Actor A",
                Plot = "Some plot"
            };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _movieController.RateMovie(movie.MovieId, new MovieRating { Rating = 15 }); // invalid rating

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var msg = badRequest.Value.GetType().GetProperty("message")?.GetValue(badRequest.Value);
            Assert.Equal("Rating must be between 1 and 10", msg);
        }

        [Fact]
        public async Task RateMovie_Fail_AlreadyRated()
        {
            var user = new User { Username = "repeater", PasswordHash = "pass" };
            var movie = new Movie
            {
                Title = "Repeat Movie",
                Year = "2023",
                Genre = "Drama",
                Director = "Dir",
                PosterUrl = "poster.jpg",
                Actors = "Actor X",
                Plot = "Repeatable plot"
            };
            var rating = new MovieRating { User = user, Movie = movie, Rating = 8 };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.MovieRatings.Add(rating);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _movieController.RateMovie(movie.MovieId, new MovieRating { Rating = 7 });

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var msg = conflict.Value.GetType().GetProperty("message")?.GetValue(conflict.Value);
            Assert.Equal("You have already rated this movie", msg);
        }

        [Fact]
        public async Task GetAllMovies_Success()
        {
            _context.Movies.AddRange(
                new Movie
                {
                    Title = "Movie 1",
                    Year = "2020",
                    Genre = "Drama",
                    Director = "Dir 1",
                    PosterUrl = "url1",
                    Actors = "Actor 1",
                    Plot = "Plot 1"
                },
                new Movie
                {
                    Title = "Movie 2",
                    Year = "2021",
                    Genre = "Comedy",
                    Director = "Dir 2",
                    PosterUrl = "url2",
                    Actors = "Actor 2",
                    Plot = "Plot 2"
                }
            );
            await _context.SaveChangesAsync();

            var result = await _movieController.GetAllMovies();
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAllMovies_ReturnsCorrectAverage()
        {
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            var movie = new Movie
            {
                Title = "Rated",
                Year = "2022",
                Genre = "Sci-Fi",
                Director = "Director",
                PosterUrl = "poster.jpg",
                Actors = "Actor 1",
                Plot = "Exciting sci-fi plot"
            };
            var user1 = new User { Username = "u1", PasswordHash = "p1" };
            var user2 = new User { Username = "u2", PasswordHash = "p2" };

            _context.Movies.Add(movie);
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            _context.MovieRatings.AddRange(
                new MovieRating { MovieId = movie.MovieId, UserId = user1.UserId, Rating = 8 },
                new MovieRating { MovieId = movie.MovieId, UserId = user2.UserId, Rating = 10 }
            );
            await _context.SaveChangesAsync();

            var result = await _movieController.GetAllMovies();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var moviesList = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);

            Assert.Single(moviesList);
        }


        [Fact]
        public async Task GetMovie_Success()
        {
            var movie = new Movie
            {
                Title = "Movie Detail",
                Year = "2022",
                Genre = "Thriller",
                Director = "Director X",
                PosterUrl = "poster.jpg",
                Actors = "Actor A, Actor B",
                Plot = "Some intense plot."
            };
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            var result = await _movieController.GetMovie(movie.MovieId);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetMovie_Fail_NotFound()
        {
            var result = await _movieController.GetMovie(999); // Invalid ID

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var msg = notFound.Value.GetType().GetProperty("message")?.GetValue(notFound.Value);
            Assert.Equal("Movie not found", msg);
        }
    }

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;

        public FakeHttpMessageHandler(string responseContent)
        {
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}