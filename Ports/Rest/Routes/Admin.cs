using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WatchedApi.Infrastructure;

namespace WatchedApi.Ports.Rest.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
        {
            bool isValidAdmin = await _adminService.ValidateAdminLogin(request.Username, request.Password);

            if (!isValidAdmin)
                return Unauthorized(new { message = "Invalid admin" });

            return Ok(new { message = "Admin login successful" });
        }

        [HttpDelete("deletepost/{postId}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            bool isDeleted = await _adminService.DeletePostById(postId);

            if (isDeleted)
                return Ok(new { message = "Post has been deleted successfully" });

            return NotFound(new { message = "Post with ID not found" });
        }

        [HttpGet("logs")]
        [Authorize]
        public async Task<IActionResult> GetAdminLogs()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token: missing user id." });
            }
            int userId = int.Parse(userIdClaim);

            // Ensure that the current user is an admin.
            bool isAdmin = await _adminService.ValidateUserIsAdmin(userId);
            if (!isAdmin)
            {
                return StatusCode(403, new { message = "You do not have permission to view admin logs." });
            }

            var logs = await _adminService.GetAdminLogsAsync();
            return Ok(logs);
        }

        public class AdminLoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}
