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
            //check if equals hard coded user and pass
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

        public class AdminLoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}
