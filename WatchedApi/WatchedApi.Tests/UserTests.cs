using Xunit;
using WatchedApi.Infrastructure;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using WatchedApi.Ports.Rest.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Linq;


namespace WatchedApi.Tests
{
    public class UserTests
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;
        private readonly Authentication _authService;
        private readonly UserController _userController;

        public UserTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var inMemorySettings = new Dictionary<string, string> {
                {"JwtSettings:SecretKey", "ThisIsASuperLongSecretKey123456!"},
                {"JwtSettings:Issuer", "testIssuer"},
                {"JwtSettings:Audience", "testAudience"},
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _userService = new UserService(_context);
            _authService = new Authentication(configuration, _context);
            _userController = new UserController(_userService, _authService, _context);
        }

        [Fact]
        public async Task Register_User_Failure_UserExists()
        {
            var existingUser = new User { Username = "existinguser", PasswordHash = "hash" };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var newUser = new User
            {
                Username = "existinguser",
                PasswordHash = "newpass",
                FullName = "Test User",
                Email = "newemail@example.com",
                PhoneNumber = "0987654321",
                Address = "New Address"
            };

            var result = await _userController.Register(newUser);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_User_Failure_MissingFields()
        {
            var user = new User { Username = "", PasswordHash = "" };
            var result = await _userController.Register(user);
            var badRequest = Assert.IsAssignableFrom<ObjectResult>(result);
            var msg = badRequest?.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value);
            Assert.Equal("All fields are required!", msg?.ToString());
        }

        [Fact]
        public async Task Login_User_Success()
        {
            var hashedPass = BCrypt.Net.BCrypt.HashPassword("password");
            var user = new User { Username = "loginuser", PasswordHash = hashedPass };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _userController.Login(new User { Username = "loginuser", PasswordHash = "password" });

            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public async Task Login_User_Invalid_Password()
        {
            var hashedPass = BCrypt.Net.BCrypt.HashPassword("password");
            _context.Users.Add(new User { Username = "wrongpass", PasswordHash = hashedPass });
            await _context.SaveChangesAsync();

            var result = await _userController.Login(new User { Username = "wrongpass", PasswordHash = "wrongpassword" });
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_User_NotFound()
        {
            var result = await _userController.Login(new User { Username = "ghost", PasswordHash = "pw" });
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task RefreshToken_Success()
        {
            var hashedPass = BCrypt.Net.BCrypt.HashPassword("password");
            var user = new User { Username = "refreshuser", PasswordHash = hashedPass };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var refreshToken = await _authService.GenerateRefreshToken(user.UserId);

            var result = await _userController.RefreshToken(new RefreshTokenRequest
            {
                UserId = user.UserId,
                Token = refreshToken
            });

            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public async Task LoginAsGuest_ReturnsSuccess()
        {
            var result = await _userController.LoginAsGuest();
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(200, objectResult.StatusCode);
        }
        [Fact]
        public void CheckJwtValid_InvalidJwt_ReturnsOkWithNullClaims()
        {
            // Arrange
            var controller = new UserController(_userService, _authService, _context);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // no claims
                }
            };

            // Act
            var result = controller.CheckJwtValid();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var valueType = okResult.Value.GetType();
            var messageProp = valueType.GetProperty("message");
            var userProp = valueType.GetProperty("user");

            Assert.NotNull(messageProp);
            Assert.NotNull(userProp);

            var message = messageProp.GetValue(okResult.Value)?.ToString();
            var user = userProp.GetValue(okResult.Value);

            Assert.Equal("JWT is still valid", message);

            var userIdProp = user?.GetType().GetProperty("userId");
            var usernameProp = user?.GetType().GetProperty("username");

            Assert.NotNull(userIdProp);
            Assert.NotNull(usernameProp);

            Assert.Null(userIdProp.GetValue(user));
            Assert.Null(usernameProp.GetValue(user));
        }

        [Fact]
        public async Task Login_MissingCredentials_ReturnsBadRequest()
        {
            var result = await _userController.Login(new User { Username = "", PasswordHash = "" });
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var msg = badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value);
            Assert.Equal("Username and password are required!", msg?.ToString());
        }

        [Fact]
        public async Task RefreshToken_InvalidBody_ReturnsBadRequest()
        {
            var result = await _userController.RefreshToken(new RefreshTokenRequest { Token = "", UserId = 0 });
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var msg = badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value);
            Assert.Equal("Invalid request body! User ID and token are required.", msg?.ToString());
        }

        [Fact]
        public async Task RefreshToken_TokenExpired_ReturnsUnauthorized()
        {
            var expiredToken = new RefreshToken
            {
                UserId = 999,
                Token = "expired_token",
                Expires = DateTime.UtcNow.AddMinutes(-1),
                IsRevoked = false
            };
            _context.RefreshTokens.Add(expiredToken);
            await _context.SaveChangesAsync();

            var result = await _userController.RefreshToken(new RefreshTokenRequest { UserId = 999, Token = "expired_token" });
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var msg = unauthorized.Value?.GetType().GetProperty("message")?.GetValue(unauthorized.Value);
            Assert.Equal("Refresh token expired. Please log in again.", msg?.ToString());
        }

        [Fact]
        public async Task Register_User_Failure_UserAlreadyExists()
        {
            var existingUser = new User
            {
                Username = "existinguser",
                PasswordHash = "existingpassword",
                FullName = "Existing User",
                Email = "existinguser@example.com",
                PhoneNumber = "0987654321",
                Address = "Existing Address"
            };

            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var newUser = new User
            {
                Username = "existinguser",  // Same as existing user
                PasswordHash = "newpassword",
                FullName = "New User",
                Email = "newemail@example.com",
                PhoneNumber = "0987654321",
                Address = "New Address"
            };

            var result = await _userController.Register(newUser);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var msg = badRequest?.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value);
            Assert.Equal("User already exists!", msg?.ToString());
        }

    }
}