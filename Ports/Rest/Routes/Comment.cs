using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchedApi.Infrastructure;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WatchedApi.Ports.Rest.Controllers
{
    [ApiController]
    [Route("api/comments")]
    public class CommentController : ControllerBase
    {
        private readonly CommentService _commentService;
        private readonly ApplicationDbContext _context;

        public CommentController(CommentService commentService, ApplicationDbContext context)
        {
            _commentService = commentService;
            _context = context;
        }

        // POST: api/comments/create
        // Allows any authenticated user to create a comment.
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest request)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);

            // Map the DTO to a Comment entity.
            var comment = new Comment
            {
                PostId = request.PostId,
                Content = request.Content
            };

            var createdComment = await _commentService.CreateCommentAsync(comment, userId);

            // Re-query to get full user info and project to CommentDto.
            var commentDto = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.CommentId == createdComment.CommentId)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    PostId = c.PostId,
                    UserId = c.UserId,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    Username = c.User.Username
                })
                .FirstOrDefaultAsync();

            return Ok(commentDto);
        }


        // GET: api/comments/{id}
        // Retrieve a specific comment by ID.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComment(int id)
        {
            var comment = await _commentService.GetCommentByIdAsync(id);
            if (comment == null)
            {
                return NotFound(new { message = "Comment not found" });
            }
            return Ok(comment);
        }

        // GET: api/comments/post/{postId}
        // Retrieve all comments for a given post.
        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetCommentsForPost(int postId)
        {
            var commentDtos = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostId == postId)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    PostId = c.PostId,
                    UserId = c.UserId,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    Username = c.User.Username
                })
                .ToListAsync();

            return Ok(commentDtos);
        }


        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentRequest updateRequest)
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

            // Retrieve the original comment to get its owner.
            var originalComment = await _context.Comments.FindAsync(id);
            if (originalComment == null)
            {
                return NotFound(new { message = "Comment not found." });
            }

            var updatedComment = new Comment
            {
                Content = updatedContent
            };

            var comment = await _commentService.UpdateCommentAsync(id, updatedComment, userId, isAdmin);
            if (comment == null)
            {
                return StatusCode(403, new { message = "You do not have permission to update this comment." });
            }

            // Log the admin action if applicable.
            if (isAdmin)
            {
                _context.AdminLogs.Add(new AdminLog
                {
                    AdminId = currentUser.UserId,
                    Action = "Edited Comment",
                    TargetCommentId = comment.CommentId,
                    TargetUserId = originalComment.UserId, // Capture original comment owner.
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return Ok(comment);
        }



        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);

            var currentUser = await _context.Users.FindAsync(userId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            // Retrieve the original comment.
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound(new { message = "Comment not found." });
            }

            var success = await _commentService.DeleteCommentAsync(id, userId, isAdmin);
            if (!success)
            {
                return StatusCode(403, new { message = "You do not have permission to delete this comment." });
            }

            // Log the admin action if performed by an admin.
            if (isAdmin)
            {
                _context.AdminLogs.Add(new AdminLog
                {
                    AdminId = currentUser.UserId,
                    Action = "Deleted Comment",
                    TargetCommentId = id,
                    TargetUserId = comment.UserId, // Capture original comment owner.
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Comment deleted successfully" });
        }
    }
}
