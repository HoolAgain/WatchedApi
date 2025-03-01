using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using WatchedApi.Infrastructure;

namespace WatchedApi.Ports.Rest.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostController : ControllerBase
    {
        private readonly PostService _postService;
        private readonly ApplicationDbContext _context;

        public PostController(PostService postService, ApplicationDbContext context)
        {
            _postService = postService;
            _context = context;
        }

        // requires a valid JWT.
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);

            //used to check if movie exists
            var movie = await _context.Movies.FindAsync(request.MovieId);
            if (movie == null)
            {
                return BadRequest(new { message = "Invalid Movie ID // Error!!" });
            }

            // Map the DTO to a new Post
            var post = new Post
            {
                Title = request.Title,
                Content = request.Content,
                MovieId = request.MovieId,
                // These collections are initialized to avoid model validation errors.
                Comments = new List<Comment>(),
                PostLikes = new List<PostLike>()
            };

            var createdPost = await _postService.CreatePostAsync(post, userId);
            return Ok(createdPost);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var post = await _postService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }
            return Ok(post);
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllPosts()
        {
            var posts = await _postService.GetAllPostsAsync();
            return Ok(posts);
        }


        // Updates a post. Only the owner or an admin
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] Post updatedPost)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);

            // Retrieve current user and check admin status.
            var currentUser = await _context.Users.FindAsync(userId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            var post = await _postService.UpdatePostAsync(id, updatedPost, userId, isAdmin);
            if (post == null)
            {
                return Forbid();
            }
            return Ok(post);
        }


        // Deletes a post. Only the owner or an admin
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);

            // Retrieve current user and check admin status.
            var currentUser = await _context.Users.FindAsync(userId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            var success = await _postService.DeletePostAsync(id, userId, isAdmin);
            if (!success)
            {
                return Forbid();
            }
            return Ok(new { message = "Post deleted successfully" });
        }


        [HttpPost("{id}/like")]
        [Authorize]
        public async Task<IActionResult> LikePost(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);

            var like = await _postService.LikePostAsync(id, userId);
            if (like == null)
            {
                return BadRequest(new { message = "Post already liked or post not found" });
            }
            return Ok(like);
        }
    }
}
