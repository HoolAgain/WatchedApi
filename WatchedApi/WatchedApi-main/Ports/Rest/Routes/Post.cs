using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;

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

        // Create a new post and return a PostDto with complete user info.
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

            // Check if movie exists.
            var movie = await _context.Movies.FindAsync(request.MovieId);
            if (movie == null)
            {
                return BadRequest(new { message = "Invalid Movie ID." });
            }

            // Map the DTO to a new Post.
            var post = new Post
            {
                Title = request.Title,
                Content = request.Content,
                MovieId = request.MovieId,
                Comments = new List<Comment>(),
                PostLikes = new List<PostLike>()
            };

            var createdPost = await _postService.CreatePostAsync(post, userId);

            // Log the activity
            _context.SiteActivityLogs.Add(new SiteActivityLog
            {
                Activity = "Post",
                Operation = "Create",
                TimeOf = createdPost.CreatedAt,
                UserId = userId
            });
            await _context.SaveChangesAsync();

            // Re-query the created post including the User info.
            var createdPostDto = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.PostId == createdPost.PostId)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    UserId = p.UserId,
                    MovieId = p.MovieId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Username = p.User.Username
                })
                .FirstOrDefaultAsync();

            return Ok(createdPostDto);
        }

        // Get a specific post by id as a PostDto.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var postDto = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.PostId == id)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    UserId = p.UserId,
                    MovieId = p.MovieId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Username = p.User.Username,
                    LikeCount = _context.PostLikes.Count(pl => pl.PostId == p.PostId)
                })
                .FirstOrDefaultAsync();

            if (postDto == null)
            {
                return NotFound(new { message = "Post not found" });
            }
            return Ok(postDto);
        }

        // Get all posts, projecting each to a PostDto.
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPosts()
        {
            var currentUserIdStr = User.FindFirst("userId")?.Value;
            int currentUserId = 0;
            if (!string.IsNullOrEmpty(currentUserIdStr))
            {
                currentUserId = int.Parse(currentUserIdStr);
            }

            var posts = await _context.Posts
                .Include(p => p.User)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    UserId = p.UserId,
                    MovieId = p.MovieId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Username = p.User.Username,
                    LikeCount = _context.PostLikes.Count(pl => pl.PostId == p.PostId),
                    HasLiked = _context.PostLikes.Any(pl => pl.PostId == p.PostId && pl.UserId == currentUserId)
                })
                .ToListAsync();

            return Ok(posts);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostRequest updateRequest)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);

            var currentUser = await _context.Users.FindAsync(userId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            // Append admin signature if the user is an admin.
            string updatedContent = updateRequest.Content;
            if (isAdmin)
            {
                updatedContent += $" -edited by {currentUser.Username}";
            }

            // Retrieve the original post first to capture the owner.
            var originalPost = await _context.Posts.FindAsync(id);
            if (originalPost == null)
            {
                return NotFound(new { message = "Post not found." });
            }

            var updatedPost = new Post
            {
                Title = updateRequest.Title,
                Content = updatedContent
            };

            var post = await _postService.UpdatePostAsync(id, updatedPost, userId, isAdmin);
            if (post == null)
            {
                return StatusCode(403, new { message = "You do not have permission to update this post." });
            }

            // Log the admin action if applicable.
            if (isAdmin)
            {
                _context.AdminLogs.Add(new AdminLog
                {
                    AdminId = currentUser.UserId,
                    Action = "Edited Post",
                    TargetPostId = post.PostId,
                    TargetUserId = originalPost.UserId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            // Log site activity for everyone.
            _context.SiteActivityLogs.Add(new SiteActivityLog
            {
                Activity = "Post",
                Operation = "Edit",
                TimeOf = post.UpdatedAt,
                UserId = userId
            });
            await _context.SaveChangesAsync();

            // Re-query the updated post to return the updated data.
            var updatedPostDto = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.PostId == post.PostId)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    UserId = p.UserId,
                    MovieId = p.MovieId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Username = p.User.Username
                })
                .FirstOrDefaultAsync();

            return Ok(updatedPostDto);
        }



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

            var currentUser = await _context.Users.FindAsync(userId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            // Retrieve the original post to get its owner's ID.
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound(new { message = "Post not found." });
            }

            var success = await _postService.DeletePostAsync(id, userId, isAdmin);
            if (!success)
            {
                return StatusCode(403, new { message = "You do not have permission to delete this post." });
            }

            // Log the deletion if performed by an admin.
            if (isAdmin)
            {
                _context.AdminLogs.Add(new AdminLog
                {
                    AdminId = currentUser.UserId,
                    Action = "Deleted Post",
                    TargetPostId = id,
                    TargetUserId = post.UserId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            // Log the deletion activity
            _context.SiteActivityLogs.Add(new SiteActivityLog
            {
                Activity = "Post",
                Operation = "Delete",
                TimeOf = DateTime.UtcNow,
                UserId = userId
            });
            await _context.SaveChangesAsync();

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

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return BadRequest(new { message = "Post not found." });
            }
            if (post.UserId == userId)
            {
                return BadRequest(new { message = "You cannot like your own post." });
            }

            var like = await _postService.LikePostAsync(id, userId);
            if (like == null)
            {
                return BadRequest(new { message = "Post already liked." });
            }

            _context.SiteActivityLogs.Add(new SiteActivityLog
            {
                Activity = "Like",
                Operation = "Create",
                TimeOf = DateTime.UtcNow,
                UserId = userId
            });
            await _context.SaveChangesAsync();

            return Ok(like);
        }


        [HttpDelete("{id}/like")]
        [Authorize]
        public async Task<IActionResult> UnlikePost(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);

            bool success = await _postService.UnlikePostAsync(id, userId);
            if (!success)
            {
                return BadRequest(new { message = "Post not liked or post not found" });
            }

            _context.SiteActivityLogs.Add(new SiteActivityLog
            {
                Activity = "Like",
                Operation = "Delete",
                TimeOf = DateTime.UtcNow,
                UserId = userId
            });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Like removed successfully" });
        }


    }
}
