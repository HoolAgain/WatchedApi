using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;

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

        [HttpGet("site-activity")]
        [Authorize]
        public async Task<IActionResult> GetSiteActivity([FromQuery] string filter = "all")
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
                return StatusCode(403, new { message = "You do not have permission to view site activity." });
            }

            // Get the Toronto time zone.
            TimeZoneInfo torontoZone;
            try
            {
                torontoZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                torontoZone = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");
            }

            // Build the query based on filter.
            IQueryable<SiteActivityLog> query = _context.SiteActivityLogs;
            if (filter == "past-month")
            {
                query = query.Where(s => s.TimeOf >= DateTime.UtcNow.AddMonths(-1));
            }
            else if (filter == "past-2-weeks")
            {
                query = query.Where(s => s.TimeOf >= DateTime.UtcNow.AddDays(-14));
            }

            // Load logs into memory.
            var logsList = await query.OrderByDescending(s => s.TimeOf).ToListAsync();
            var usersDict = await _context.Users.ToDictionaryAsync(u => u.UserId, u => u.Username);

            // Project the logs into a DTO
            var logs = logsList.Select(s => new {
                s.Id,
                s.Activity,
                s.Operation,
                TimeOf = s.TimeOf.HasValue
                           ? TimeZoneInfo.ConvertTimeFromUtc(s.TimeOf.Value, torontoZone)
                           : (DateTime?)null,
                Username = usersDict.ContainsKey(s.UserId) ? usersDict[s.UserId] : "Unknown"
            }).ToList();

            return Ok(logs);
        }
    }
}
