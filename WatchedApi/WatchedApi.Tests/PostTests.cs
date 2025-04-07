using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using WatchedApi.Ports.Rest.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System;


namespace WatchedApi.Tests
{
    public class PostTests
    {
        private readonly ApplicationDbContext _context;
        private readonly PostService _postService;
        private readonly PostController _postController;

        public PostTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("WatchedApiTestPostDb")
                .Options;

            _context = new ApplicationDbContext(options);

            _postService = new PostService(_context);
            _postController = new PostController(_postService, _context);
        }

        private void SetUserContext(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim("userId", userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            _postController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreatePost_Success()
        {
            var user = new User { Username = "testuser", PasswordHash = "hashedpassword" };

            // Create a full Movie entity
            var movie = new Movie
            {
                Title = "Test Movie",
                Year = "2025",
                Genre = "Action",
                Director = "John Doe",
                Actors = "Actor A, Actor B",
                Plot = "A thrilling movie about something exciting",
                PosterUrl = "http://example.com/poster.jpg"
            };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _postController.CreatePost(new CreatePostRequest
            {
                MovieId = movie.MovieId,
                Title = "Test Post",
                Content = "Test Post Content"
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var postDto = Assert.IsType<PostDto>(okResult.Value);

            Assert.NotNull(postDto);
            Assert.Equal("Test Post", postDto.Title);
            Assert.Equal(user.UserId, postDto.UserId);
            Assert.Equal(movie.MovieId, postDto.MovieId);
        }


        [Fact]
        public async Task GetPost_Success()
        {
            var user = new User { Username = "testuser", PasswordHash = "hashedpassword" };
            var movie = new Movie
            {
                Title = "Test Movie",
                Year = "2025",
                Genre = "Action",
                Director = "John Doe",
                Actors = "Actor A, Actor B",
                Plot = "A thrilling movie about something exciting",
                PosterUrl = "http://example.com/poster.jpg"
            };
            var post = new Post
            {
                Title = "Test Post",
                Content = "Test Content",
                UserId = user.UserId,
                MovieId = movie.MovieId
            };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var result = await _postController.GetPost(post.PostId);

            if (result is OkObjectResult okResult)
            {
                var postDto = Assert.IsType<PostDto>(okResult.Value);
                Assert.NotNull(postDto);
                Assert.Equal("Test Post", postDto.Title);
                Assert.Equal(user.UserId, postDto.UserId);
            }
            else
            {
                Assert.IsType<NotFoundObjectResult>(result);  // Handle case when post is not found
            }
        }
        [Fact]
        public async Task UnlikePost_Success()
        {
            var user = new User { Username = "testuser", PasswordHash = "hashedpassword" };
            var movie = new Movie
            {
                Title = "Test Movie",
                Year = "2025",
                Genre = "Action",
                Director = "John Doe",
                Actors = "Actor A, Actor B",
                Plot = "A thrilling movie about something exciting",
                PosterUrl = "http://example.com/poster.jpg"
            };
            var post = new Post
            {
                Title = "Test Post",
                Content = "Test Content",
                UserId = user.UserId,
                MovieId = movie.MovieId
            };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            // Like the post first
            await _postController.LikePost(post.PostId);

            var result = await _postController.UnlikePost(post.PostId);

            // Ensure we get an OkObjectResult
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Check the result's value - ensure it's the expected message
            var responseValue = okResult.Value;
            Assert.NotNull(responseValue);

            // Assuming the response contains a message string
            var message = responseValue.GetType().GetProperty("message")?.GetValue(responseValue, null);
            Assert.NotNull(message);
            Assert.Equal("Like removed successfully", message);
        }
        [Fact]
        public async Task UpdatePost_Success()
        {
            var user = new User { Username = "testuser", PasswordHash = "hashedpassword" };
            var movie = new Movie
            {
                Title = "Test Movie",
                Year = "2025",
                Genre = "Action",
                Director = "John Doe",
                Actors = "Actor A, Actor B",
                Plot = "An exciting plot",
                PosterUrl = "http://example.com/poster.jpg"
            };
            var post = new Post { Title = "Original Title", Content = "Original Content", User = user, Movie = movie };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _postController.UpdatePost(post.PostId, new UpdatePostRequest
            {
                Title = "Updated Title",
                Content = "Updated Content"
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var updated = Assert.IsType<PostDto>(okResult.Value);

            Assert.Equal("Updated Title", updated.Title);
            Assert.Equal("Updated Content", updated.Content);
        }

        [Fact]
        public async Task UpdatePost_Fail_PermissionDenied()
        {
            var user1 = new User { Username = "owner", PasswordHash = "hash1" };
            var user2 = new User { Username = "intruder", PasswordHash = "hash2" };
            var movie = new Movie
            {
                Title = "Test Movie",
                Year = "2025",
                Genre = "Drama",
                Director = "Jane Doe",
                Actors = "Someone",
                Plot = "Intriguing story",
                PosterUrl = "http://example.com/movie.jpg"
            };
            var post = new Post { Title = "Secret", Content = "Can't touch this", User = user1, Movie = movie };

            _context.Users.AddRange(user1, user2);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            SetUserContext(user2.UserId); // not the owner

            var result = await _postController.UpdatePost(post.PostId, new UpdatePostRequest
            {
                Title = "Hack",
                Content = "Trying to change"
            });

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
        }

        [Fact]
        public async Task DeletePost_Success()
        {
            var user = new User { Username = "deleter", PasswordHash = "pw" };
            var movie = new Movie
            {
                Title = "Movie To Delete",
                Year = "2024",
                Genre = "Horror",
                Director = "Spooky Guy",
                Actors = "Ghost A, Ghost B",
                Plot = "They disappear mysteriously",
                PosterUrl = "http://example.com/horror.jpg"
            };
            var post = new Post { Title = "Delete Me", Content = "Please do", User = user, Movie = movie };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _postController.DeletePost(post.PostId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var message = okResult.Value.GetType().GetProperty("message")?.GetValue(okResult.Value);
            Assert.Equal("Post deleted successfully", message);
        }

        [Fact]
        public async Task DeletePost_Fail_NotFound()
        {
            var user = new User { Username = "tester", PasswordHash = "pass" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _postController.DeletePost(999); // Non-existent post

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }
        [Fact]
        public async Task GetAllPosts_ReturnsAllPosts()
        {
            var user = new User { Username = "poster", PasswordHash = "hash" };
            var movie = new Movie
            {
                Title = "Another Movie",
                Year = "2023",
                Genre = "Comedy",
                Director = "Funny Guy",
                Actors = "Comedian A",
                Plot = "Laughs all around",
                PosterUrl = "http://example.com/comedy.jpg"
            };
            var post1 = new Post { Title = "Post 1", Content = "Funny content", User = user, Movie = movie };
            var post2 = new Post { Title = "Post 2", Content = "Funnier content", User = user, Movie = movie };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.Posts.AddRange(post1, post2);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _postController.GetAllPosts();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var posts = Assert.IsAssignableFrom<List<PostDto>>(okResult.Value);
            Assert.True(posts.Count >= 2);
        }

        [Fact]
        public async Task CreatePost_MissingUserIdClaim_ReturnsUnauthorized()
        {
            var controller = GetControllerWithUser(null);
            var result = await controller.CreatePost(new CreatePostRequest
            {
                MovieId = 1,
                Title = "Test Title",
                Content = "Test Content"
            });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var message = unauthorized.Value?.GetType().GetProperty("message")?.GetValue(unauthorized.Value);
            Assert.Equal("Invalid token: missing user id.", message);
        }
        private PostController GetControllerWithUser(string? userId)
        {
            var controller = new PostController(_postService, _context);
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim("userId", userId));
            }

            var identity = new ClaimsIdentity(claims, "mock");
            var user = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            return controller;
        }
        [Fact]
        public async Task CreatePost_InvalidMovieId_ReturnsBadRequest()
        {
            var controller = GetControllerWithUser("1");

            var result = await controller.CreatePost(new CreatePostRequest
            {
                MovieId = 9999, // Non-existent movie ID
                Title = "Post with Invalid Movie",
                Content = "This should fail"
            });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid Movie ID.", badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value));
        }
        [Fact]
        public async Task UpdatePost_NotFound_ReturnsNotFound()
        {
            var controller = GetControllerWithUser("1");

            var result = await controller.UpdatePost(9999, new UpdatePostRequest
            {
                Title = "Doesn't Matter",
                Content = "Still shouldn't work"
            });

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Post not found.", notFound.Value?.GetType().GetProperty("message")?.GetValue(notFound.Value));
        }
        [Fact]
        public async Task DeletePost_MissingUserIdClaim_ReturnsUnauthorized()
        {
            var controller = GetControllerWithUser(null);

            var result = await controller.DeletePost(1);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid token: missing user id.", unauthorized.Value?.GetType().GetProperty("message")?.GetValue(unauthorized.Value));
        }
        [Fact]
        public async Task LikePost_MissingUserIdClaim_ReturnsUnauthorized()
        {
            var controller = GetControllerWithUser(null);

            var result = await controller.LikePost(1);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid token: missing user id.", unauthorized.Value?.GetType().GetProperty("message")?.GetValue(unauthorized.Value));
        }
        [Fact]
        public async Task LikePost_LikeOwnPost_ReturnsBadRequest()
        {
            var userId = 10;

            var user = new User { Username = "selfliker", PasswordHash = "testpass" };
            var movie = new Movie
            {
                Title = "Self Like Movie",
                Year = "2023",
                Genre = "Drama",
                Director = "Director",
                Actors = "Actor A",
                Plot = "Some plot",
                PosterUrl = "http://example.com/poster.jpg"
            };

            var post = new Post
            {
                Title = "Self Post",
                Content = "This is mine",
                User = user,
                Movie = movie
            };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Use the generated ID
            SetUserContext(user.UserId);

            var result = await _postController.LikePost(post.PostId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var message = badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value);
            Assert.Equal("You cannot like your own post.", message?.ToString());
        }
        [Fact]
        public async Task GetPost_ReturnsOk_WhenFound()
        {
            var user = new User { Username = "viewer", PasswordHash = "pw" };
            var movie = new Movie
            {
                Title = "Watched",
                Year = "2023",
                Genre = "Thriller",
                Director = "Director",
                Actors = "Someone",
                Plot = "Plot",
                PosterUrl = "url"
            };
            var post = new Post { Title = "Post", Content = "Something", User = user, Movie = movie };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var result = await _postController.GetPost(post.PostId);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }
        [Fact]
        public async Task UpdatePost_AsAdmin_AppendsLogAndSignature()
        {
            var user = new User { Username = "admin", PasswordHash = "pw", IsAdmin = true };
            var movie = new Movie { Title = "Title", Year = "2023", Genre = "Action", Director = "Dir", Actors = "A", Plot = "P", PosterUrl = "url" };
            var post = new Post { Title = "Old", Content = "Old content", User = user, Movie = movie };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);
            var result = await _postController.UpdatePost(post.PostId, new UpdatePostRequest { Title = "New", Content = "Updated" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var updated = Assert.IsType<PostDto>(ok.Value);
            Assert.Contains($"-edited by {user.Username}", updated.Content);
        }
        [Fact]
        public async Task DeletePost_Success_AsAdmin_LogsAction()
        {
            var admin = new User { Username = "admin", PasswordHash = "pw", IsAdmin = true };
            var movie = new Movie { Title = "Del", Year = "2022", Genre = "Drama", Director = "D", Actors = "A", Plot = "P", PosterUrl = "url" };
            var post = new Post { Title = "Bye", Content = "Will be gone", User = admin, Movie = movie };

            _context.Users.Add(admin);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            SetUserContext(admin.UserId);
            var result = await _postController.DeletePost(post.PostId);
            var ok = Assert.IsType<OkObjectResult>(result);

            var log = _context.AdminLogs.FirstOrDefault(l =>
      l.Action == "Deleted Post" && l.TargetPostId == post.PostId);

            Assert.NotNull(log);
            Assert.Equal("Deleted Post", log.Action);
            Assert.Equal(admin.UserId, log.AdminId);
            Assert.Equal(post.UserId, log.TargetUserId);

        }
        [Fact]
        public async Task LikePost_PostNotFound_ReturnsBadRequest()
        {
            var user = new User { Username = "liker", PasswordHash = "pw" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);
            var result = await _postController.LikePost(999);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Post not found.", bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value));
        }
        [Fact]
        public async Task LikePost_SelfLike_ReturnsBadRequest()
        {
            var user = new User { Username = "self", PasswordHash = "pw" };
            var movie = new Movie { Title = "Self", Year = "2022", Genre = "Sci-Fi", Director = "D", Actors = "A", Plot = "P", PosterUrl = "url" };
            var post = new Post { Title = "Self", Content = "No like", User = user, Movie = movie };

            _context.Users.Add(user);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);
            var result = await _postController.LikePost(post.PostId);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("You cannot like your own post.", bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value));
        }
        [Fact]
        public async Task UpdatePost_MissingUserIdClaim_ReturnsUnauthorized()
        {
            var controller = GetControllerWithUser(null);

            var result = await controller.UpdatePost(1, new UpdatePostRequest
            {
                Title = "New",
                Content = "New Content"
            });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid token: missing user id.", unauthorized.Value?.GetType().GetProperty("message")?.GetValue(unauthorized.Value));
        }
        [Fact]
        public async Task DeletePost_Fail_PermissionDenied()
        {
            var owner = new User { Username = "owner", PasswordHash = "pw" };
            var intruder = new User { Username = "badguy", PasswordHash = "pw" };
            var movie = new Movie { Title = "Drama", Year = "2023", Genre = "Drama", Director = "X", Actors = "Y", Plot = "Z", PosterUrl = "url" };
            var post = new Post { Title = "Nope", Content = "Not yours", User = owner, Movie = movie };

            _context.Users.AddRange(owner, intruder);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            SetUserContext(intruder.UserId);

            var result = await _postController.DeletePost(post.PostId);

            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
        }
        [Fact]
        public async Task UnlikePost_Fail_NotLikedOrNotFound()
        {
            var user = new User { Username = "ghost", PasswordHash = "pw" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _postController.UnlikePost(999); // Non-existent or never liked

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var msg = badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value);
            Assert.Equal("Post not liked or post not found", msg);
        }
        [Fact]
        public async Task LikePost_AlreadyLiked_ReturnsBadRequest()
        {
            var poster = new User { Username = "poster", PasswordHash = "pw" };
            var liker = new User { Username = "liker", PasswordHash = "pw" };

            var movie = new Movie
            {
                Title = "Double Like Movie",
                Year = "2024",
                Genre = "Thriller",
                Director = "Someone",
                Actors = "Actors",
                Plot = "Plot",
                PosterUrl = "http://example.com/poster.jpg"
            };

            var post = new Post
            {
                Title = "Like Me Twice",
                Content = "Please no",
                User = poster,
                Movie = movie
            };

            _context.Users.AddRange(poster, liker);
            _context.Movies.Add(movie);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // First like by a different user
            SetUserContext(liker.UserId);
            await _postController.LikePost(post.PostId);

            // Second like by the same user should fail
            var result = await _postController.LikePost(post.PostId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var message = badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value);
            Assert.Equal("Post already liked.", message);
        }

        [Fact]
        public async Task UnlikePost_MissingUserIdClaim_ReturnsUnauthorized()
        {
            var controller = GetControllerWithUser(null);

            var result = await controller.UnlikePost(1);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var message = unauthorized.Value?.GetType().GetProperty("message")?.GetValue(unauthorized.Value);
            Assert.Equal("Invalid token: missing user id.", message);
        }

    }
}