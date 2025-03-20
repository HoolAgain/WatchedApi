using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WatchedApi.Infrastructure;
using WatchedApi.Infrastructure.Data.Models;
using WatchedApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;



namespace WatchedApi.Ports.Rest.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly Authentication _authService;
        private readonly ApplicationDbContext _context;

        public UserController(UserService userService, Authentication authService, ApplicationDbContext context)
        {
            _userService = userService;
            _authService = authService;
            _context = context;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.PasswordHash) ||
                string.IsNullOrWhiteSpace(user.FullName) || string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.PhoneNumber) || string.IsNullOrWhiteSpace(user.Address))
            {
                return BadRequest(new { message = "All fields are required!" });
            }

            var createdUser = await _userService.RegisterUserAsync(
                user.Username,
                user.PasswordHash,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.Address
            );

            if (createdUser == null)
            {
                return BadRequest(new { message = "User already exists!" });
            }

            return Ok(new
            {
                message = "User registered successfully",
                userId = createdUser.UserId,
                username = createdUser.Username,
                email = createdUser.Email,
                fullName = createdUser.FullName,
                phoneNumber = createdUser.PhoneNumber,
                address = createdUser.Address,
                password = createdUser.PasswordHash
            });
        }





        [Authorize] //makes it so this endpoint only works if valid jwt
        [HttpPost("checkJwtValid")]
        public IActionResult CheckJwtValid()
        {
            try
            {
                //pull those form my jwt
                var username = User.FindFirst("username")?.Value;
                var userId = User.FindFirst("userId")?.Value;

                return Ok(new
                {
                    message = "JWT is still valid",
                    user = new { userId, username }
                });
            }
            catch (Exception)
            {
                return Unauthorized(new { message = "JWT is invalid or expired" });
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User request)
        {
            try
            {
                //check if null
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.PasswordHash))
                {
                    return BadRequest(new { message = "Username and password are required!" });
                }

                //check username
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
                if (user == null)
                {
                    return Unauthorized(new { message = "Error Unable to find username!" });
                }

                //check pass
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.PasswordHash, user.PasswordHash);
                if (!isPasswordValid)
                {
                    return Unauthorized(new { message = "Error invalid password!" });
                }

                //find and remove old tokens
                var oldTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == user.UserId)
                    .ToListAsync();
                _context.RefreshTokens.RemoveRange(oldTokens);
                await _context.SaveChangesAsync();

                //generate a fresh refresh token
                string newRefreshToken = await _authService.GenerateRefreshToken(user.UserId);

                // generate a fresh access token
                var newJwtToken = _authService.GenerateJwtToken(user);

                return Ok(new
                {
                    token = newJwtToken,
                    refreshToken = newRefreshToken
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error logging in: {ex.Message}" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            //check body
            if (request == null || string.IsNullOrEmpty(request.Token) || request.UserId <= 0)
            {
                return BadRequest(new { message = "Invalid request body! User ID and token are required." });
            }

            //find in db
            var latestToken = await _context.RefreshTokens
                .Where(rt => rt.UserId == request.UserId && !rt.IsRevoked)
                .OrderByDescending(rt => rt.Expires)
                .FirstOrDefaultAsync();

            //if empty or expired say login
            if (latestToken == null || latestToken.Token != request.Token || latestToken.Expires < DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Refresh token expired. Please log in again." });
            }

            //generate new jwt
            var user = await _context.Users.FindAsync(request.UserId);
            string newJwtToken = _authService.GenerateJwtToken(user);

            return Ok(new
            {
                token = newJwtToken,
                refreshToken = latestToken.Token
            });
        }

        [HttpPost("guest")]
        public async Task<IActionResult> LoginAsGuest()
        {
            try
            {
                //generate random username
                string randomUsername = "Guest" + new string(Enumerable.Range(0, 8)
                //index all those and make to array
                    .Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"[Random.Shared.Next(52)])
                    .ToArray());

                //genrate randompass
                string randomPassword = new string(Enumerable.Range(0, 12)
                    .Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"[Random.Shared.Next(52)])
                    .ToArray());

                //hash pass
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(randomPassword);

                //create and save user
                var guestUser = new User
                {
                    Username = randomUsername,
                    PasswordHash = hashedPassword,
                };

                _context.Users.Add(guestUser);
                await _context.SaveChangesAsync(); //add to db

                //generate refresh
                string newRefreshToken = await _authService.GenerateRefreshToken(guestUser.UserId);

                //generate jwt
                var guestJwtToken = _authService.GenerateJwtToken(guestUser);

                return Ok(new
                {
                    message = "Login successful",
                    userId = guestUser.UserId,
                    username = randomUsername,
                    token = guestJwtToken,
                    refreshToken = newRefreshToken
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error logging in" });
            }
        }
    }

}


