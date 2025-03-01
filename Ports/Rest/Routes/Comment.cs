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
			return Ok(createdComment);
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
			var comments = await _commentService.GetCommentsByPostIdAsync(postId);
			return Ok(comments);
		}

        // PUT: api/comments/{id}
        // Update an existing comment. Only the comment owner or an admin can update.
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

            // Retrieve current user for admin check.
            var currentUser = await _context.Users.FindAsync(userId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            // Create a temporary Comment object to pass only the updated content.
            var updatedComment = new Comment
            {
                Content = updateRequest.Content
            };

            var comment = await _commentService.UpdateCommentAsync(id, updatedComment, userId, isAdmin);
            if (comment == null)
            {
                return Forbid();
            }
            return Ok(comment);
        }


        // DELETE: api/comments/{id}
        // Delete a comment. Only the comment owner or an admin can delete.
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

			// Retrieve current user for admin check.
			var currentUser = await _context.Users.FindAsync(userId);
			bool isAdmin = currentUser != null && currentUser.IsAdmin;

			bool success = await _commentService.DeleteCommentAsync(id, userId, isAdmin);
			if (!success)
			{
				return Forbid();
			}
			return Ok(new { message = "Comment deleted successfully" });
		}
	}
}
