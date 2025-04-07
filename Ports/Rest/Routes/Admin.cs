using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure.Data;

namespace WatchedApi.Ports.Rest.Routes
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("logs")]
        [Authorize]
        public async Task<IActionResult> GetAdminLogs()
        {
            // Check that the requester is an admin.
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);
            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                return StatusCode(403, new { message = "You do not have permission to view admin logs." });
            }

            var logs = await _context.AdminLogs
                .Include(al => al.Admin)
                .Include(al => al.TargetPost)
                .Include(al => al.TargetComment)
                .Include(al => al.TargetUser)
                .OrderByDescending(al => al.CreatedAt)
                .Select(al => new
                {
                    al.LogId,
                    al.Action,
                    al.CreatedAt,
                    AdminName = al.Admin.Username,
                    TargetPostTitle = al.TargetPost != null ? al.TargetPost.Title : null,
                    TargetCommentContent = al.TargetComment != null ? al.TargetComment.Content : null,
                    TargetUserName = al.TargetUser != null ? al.TargetUser.Username : null
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}
