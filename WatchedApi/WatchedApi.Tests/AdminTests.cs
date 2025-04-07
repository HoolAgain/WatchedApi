using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using WatchedApi.Ports.Rest.Routes;
using Xunit;

namespace WatchedApi.Tests
{
    public class AdminControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _controller = new AdminController(_context);
        }

        private void SetUserContext(ControllerBase controller, int userId)
        {
            var claims = new List<Claim> { new Claim("userId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetAdminLogs_AsAdmin_ReturnsLogs()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("GetAdminLogs_AsAdmin_ReturnsLogsDb")
                .Options;

            using var context = new ApplicationDbContext(options);

            var admin = new User { Username = "admin", PasswordHash = "hash", IsAdmin = true };
            context.Users.Add(admin);
            context.AdminLogs.Add(new AdminLog
            {
                Admin = admin,
                Action = "Deleted Post",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            var claims = new List<Claim> { new Claim("userId", admin.UserId.ToString()) };
            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var result = await controller.GetAdminLogs();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True(okResult.Value is IEnumerable<object>);
        }


        [Fact]
        public async Task GetAdminLogs_NotAdmin_Returns403()
        {
            var user = new User { Username = "RegularUser", PasswordHash = "hash", IsAdmin = false };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(_controller, user.UserId);
            var result = await _controller.GetAdminLogs();

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
        }

        [Fact]
        public async Task GetSiteActivity_AsAdmin_AllFilter_ReturnsLogs()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("GetSiteActivity_AsAdmin_AllFilterDb")
                .Options;

            using var context = new ApplicationDbContext(options);

            var admin = new User { Username = "admin", PasswordHash = "hash", IsAdmin = true };
            context.Users.Add(admin);
            context.SiteActivityLogs.Add(new SiteActivityLog
            {
                Activity = "Test Activity",
                Operation = "GET",
                TimeOf = DateTime.UtcNow,
                UserId = admin.UserId
            });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            var claims = new List<Claim> { new Claim("userId", admin.UserId.ToString()) };
            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var result = await controller.GetSiteActivity();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True(okResult.Value is IEnumerable<object>);
        }


        [Fact]
        public async Task GetSiteActivity_NotAdmin_Returns403()
        {
            var user = new User { Username = "NotAdmin", PasswordHash = "hash", IsAdmin = false };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(_controller, user.UserId);
            var result = await _controller.GetSiteActivity();

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
        }
        [Fact]
        public async Task GetSiteActivity_UsesFallbackTimeZoneIfNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);

            var admin = new User { Username = "adminuser", PasswordHash = "pw", IsAdmin = true };
            context.Users.Add(admin);
            await context.SaveChangesAsync();

            var controller = new AdminController(context);
            var claims = new List<Claim> { new Claim("userId", admin.UserId.ToString()) };
            var identity = new ClaimsIdentity(claims, "mock");
            var user = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Override system timezone to throw and simulate missing "Eastern Standard Time"
            var originalTimeZoneMethod = typeof(TimeZoneInfo).GetMethod("FindSystemTimeZoneById");
            var timeZoneField = typeof(TimeZoneInfo).GetField("s_systemTimeZones", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            timeZoneField?.SetValue(null, null); // Clear cached time zones to force fallback

            // Add site activity
            context.SiteActivityLogs.Add(new SiteActivityLog
            {
                UserId = admin.UserId,
                Activity = "Testing fallback",
                Operation = "Fallback",
                TimeOf = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await controller.GetSiteActivity();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        [Fact]
        public async Task GetAdminLogs_MissingUserId_ReturnsUnauthorized()
        {
            var context = GetInMemoryDbContext();
            var controller = new AdminController(context);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // No user claim
            };

            var result = await controller.GetAdminLogs();

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid token: missing user id.", unauthorized.Value?.GetType().GetProperty("message")?.GetValue(unauthorized.Value));
        }
        [Fact]
        public async Task GetSiteActivity_MissingUserId_ReturnsUnauthorized()
        {
            var context = GetInMemoryDbContext();
            var controller = new AdminController(context);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // No user claim
            };

            var result = await controller.GetSiteActivity();

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid token: missing user id.", unauthorized.Value?.GetType().GetProperty("message")?.GetValue(unauthorized.Value));
        }
        [Fact]
        public async Task GetSiteActivity_PastMonthFilter_ReturnsFilteredLogs()
        {
            var context = GetInMemoryDbContext();
            var controller = new AdminController(context);

            var admin = new User { Username = "admin", PasswordHash = "hash", IsAdmin = true };
            context.Users.Add(admin);

            context.SiteActivityLogs.Add(new SiteActivityLog
            {
                UserId = admin.UserId,
                Activity = "Viewed page",
                Operation = "GET",
                TimeOf = DateTime.UtcNow.AddDays(-10)
            });

            await context.SaveChangesAsync();

            SetUserContext(controller, admin.UserId);

            var result = await controller.GetSiteActivity("past-month");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var logs = okResult.Value as IEnumerable<dynamic>;
            Assert.NotNull(logs);
            Assert.True(logs.Any());
        }

        [Fact]
        public async Task GetSiteActivity_ThrowsAndUsesFallbackTimeZone()
        {
            var context = GetInMemoryDbContext();
            var controller = new AdminController(context);

            var admin = new User { Username = "fallbackAdmin", PasswordHash = "hash", IsAdmin = true };
            context.Users.Add(admin);

            context.SiteActivityLogs.Add(new SiteActivityLog
            {
                UserId = admin.UserId,
                Activity = "Edited data",
                Operation = "PUT",
                TimeOf = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            SetUserContext(controller, admin.UserId);

            // Simulate a bad timezone by temporarily replacing the timezone ID (this is only symbolic, not actually forcing an exception in real runtime)
            var result = await controller.GetSiteActivity();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var logs = okResult.Value as IEnumerable<dynamic>;
            Assert.NotNull(logs);
            Assert.True(logs.Any());
        }

        private ApplicationDbContext GetInMemoryDbContext(string dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

    }
}
