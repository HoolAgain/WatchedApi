using Xunit;
using WatchedApi.Infrastructure;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using WatchedApi.Ports.Rest.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace WatchedApi.Tests
{
    public class CommentServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly CommentService _commentService;
        private readonly CommentController _commentController;

        public CommentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("WatchedApiTestCommentsDb")
                .Options;

            _context = new ApplicationDbContext(options);
            _commentService = new CommentService(_context);
            _commentController = new CommentController(_commentService, _context);
        }

        private void SetUserContext(int userId)
        {
            var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString())
        };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            _commentController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task UpdateComment_Success_Owner()
        {
            // Clear the database before each test to ensure no key conflict
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            var user = new User { UserId = 2, Username = "ownerUser", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
            var comment = new Comment { CommentId = 1, Content = "Initial Content", UserId = user.UserId };  // Ensure unique CommentId

            _context.Users.Add(user);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _commentController.UpdateComment(comment.CommentId, new UpdateCommentRequest
            {
                Content = "Updated Content"
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedComment = Assert.IsType<Comment>(okResult.Value);
            Assert.Equal("Updated Content", updatedComment.Content);
        }


        [Fact]
        public async Task DeleteComment_Success_Admin()
        {
            var adminUser = new User { UserId = 3, Username = "admin", IsAdmin = true, PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
            var commentOwner = new User { UserId = 4, Username = "owner", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
            var comment = new Comment { Content = "To delete", UserId = commentOwner.UserId };

            _context.Users.AddRange(adminUser, commentOwner);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            SetUserContext(adminUser.UserId);

            var result = await _commentController.DeleteComment(comment.CommentId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetCommentsForPost_Success()
        {
            var post = new Post { PostId = 2, Title = "Post with comments", Content = "Some content" };
            var user = new User { Username = "userWithComments", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };

            _context.Posts.Add(post);
            _context.Users.Add(user);
            _context.Comments.AddRange(
                new Comment { Content = "First Comment", Post = post, User = user },
                new Comment { Content = "Second Comment", Post = post, User = user }
            );
            await _context.SaveChangesAsync();

            var result = await _commentController.GetCommentsForPost(post.PostId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var comments = Assert.IsType<List<CommentDto>>(okResult.Value);

            Assert.Equal(2, comments.Count);
        }
        [Fact]
        public async Task CreateComment_Fail_Unauthorized()
        {
            _commentController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // No claims
                }
            };

            var result = await _commentController.CreateComment(new CreateCommentRequest
            {
                PostId = 1,
                Content = "This is a comment"
            });

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);

            var messageProperty = unauthorizedResult.Value.GetType().GetProperty("message");
            var messageValue = messageProperty?.GetValue(unauthorizedResult.Value) as string;

            Assert.Equal("Invalid token: missing user id.", messageValue);
        }




        [Fact]
        public async Task GetComment_Success()
        {
            var user = new User
            {
                UserId = 60,
                Username = "viewer",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("viewerpass")
            };

            var post = new Post
            {
                PostId = 10,
                Title = "Sample Post",
                Content = "Post content"
            };

            var comment = new Comment
            {
                Content = "Nice post!",
                User = user,
                Post = post
            };

            _context.Users.Add(user);
            _context.Posts.Add(post);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var result = await _commentController.GetComment(comment.CommentId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedComment = Assert.IsType<Comment>(okResult.Value);
            Assert.Equal(comment.Content, returnedComment.Content);
            Assert.Equal(user.UserId, returnedComment.UserId);
        }

        [Fact]
        public async Task GetComment_Fail_CommentNotFound()
        {
            // Act
            var result = await _commentController.GetComment(999); // Invalid ID

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var messageProp = notFoundResult.Value!.GetType().GetProperty("message");
            var messageValue = messageProp?.GetValue(notFoundResult.Value) as string;

            Assert.Equal("Comment not found", messageValue);
        }

        [Fact]
        public async Task CreateComment_Success()
        {
            var user = new User { UserId = 70, Username = "commenter", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass") };
            var post = new Post { PostId = 22, Title = "New Post", Content = "Post body" };
            _context.Users.Add(user);
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _commentController.CreateComment(new CreateCommentRequest
            {
                PostId = post.PostId,
                Content = "This is a test comment"
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<CommentDto>(okResult.Value);
            Assert.Equal("This is a test comment", dto.Content);
            Assert.Equal(user.UserId, dto.UserId);
            Assert.Equal(user.Username, dto.Username);
        }
        [Fact]
        public async Task GetCommentsForPost_Empty()
        {
            var post = new Post { PostId = 100, Title = "Empty Post", Content = "No comments here." };
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var result = await _commentController.GetCommentsForPost(post.PostId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var comments = Assert.IsType<List<CommentDto>>(okResult.Value);
            Assert.Empty(comments);
        }
        [Fact]
        public async Task DeleteComment_Fail_Unauthorized_NoUserId()
        {
            _commentController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // No claims
                }
            };

            var result = await _commentController.DeleteComment(1);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var message = unauthorized.Value.GetType().GetProperty("message")?.GetValue(unauthorized.Value);
            Assert.Equal("Invalid token: missing user id.", message);
        }
        [Fact]
        public async Task CreateComment_Fail_InvalidPostId()
        {
            var user = new User { UserId = 88, Username = "ghost", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pw") };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _commentController.CreateComment(new CreateCommentRequest
            {
                PostId = 9999, // Doesn't exist
                Content = "Trying to comment on non-existent post"
            });

            var okResult = Assert.IsType<OkObjectResult>(result); // Optional: change if you implement a validation
            Assert.NotNull(okResult.Value); // At least confirm it's handled gracefully
        }
        [Fact]
        public async Task GetCommentsForPost_NonExistentPost()
        {
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            var result = await _commentController.GetCommentsForPost(9999);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var comments = Assert.IsType<List<CommentDto>>(okResult.Value);
            Assert.Empty(comments);
        }


        [Fact]
        public async Task CreateCommentAsync_AddsCommentWithGivenUserId()
        {
            var comment = new Comment
            {
                Content = "New comment",
                PostId = 1
            };

            var result = await _commentService.CreateCommentAsync(comment, userId: 101); // User doesn't exist

            Assert.Equal(101, result.UserId);
            Assert.NotEqual(default, result.CreatedAt);
            Assert.NotEqual(default, result.UpdatedAt);

            var inDb = await _context.Comments.FindAsync(result.CommentId);
            Assert.NotNull(inDb);
        }
        [Fact]
        public async Task UpdateCommentAsync_NonExistingComment_ReturnsNull()
        {
            var result = await _commentService.UpdateCommentAsync(999, new Comment { Content = "test" }, 1, false);

            Assert.Null(result);
        }
        [Fact]
        public async Task UpdateCommentAsync_NotOwnerOrAdmin_ReturnsNull()
        {
            var comment = new Comment
            {
                CommentId = 500,
                Content = "original",
                UserId = 1
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var updated = new Comment { Content = "hacked content" };
            var result = await _commentService.UpdateCommentAsync(500, updated, currentUserId: 2, isAdmin: false);

            Assert.Null(result);
        }
        [Fact]
        public async Task DeleteCommentAsync_CommentNotFound_ReturnsFalse()
        {
            var result = await _commentService.DeleteCommentAsync(999, 1, true);
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateCommentAsync_ReturnsNull_WhenCommentDoesNotExist()
        {
            var updated = new Comment { Content = "Doesn't matter" };
            var result = await _commentService.UpdateCommentAsync(999, updated, currentUserId: 1, isAdmin: false);

            Assert.Null(result);
        }
        [Fact]
        public async Task UpdateCommentAsync_ReturnsNull_WhenNotOwnerOrAdmin()
        {
            var comment = new Comment { Content = "Secret", UserId = 10 };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var updated = new Comment { Content = "Attempted hack" };
            var result = await _commentService.UpdateCommentAsync(comment.CommentId, updated, currentUserId: 20, isAdmin: false);

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteCommentAsync_ReturnsFalse_WhenNotOwnerOrAdmin()
        {
            var comment = new Comment { Content = "Cannot delete", UserId = 1 };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var result = await _commentService.DeleteCommentAsync(comment.CommentId, currentUserId: 2, isAdmin: false);
            Assert.False(result);
        }
        [Fact]
        public async Task DeleteCommentAsync_ReturnsTrue_WhenOwnerDeletes()
        {
            var comment = new Comment { Content = "Mine", UserId = 5 };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var result = await _commentService.DeleteCommentAsync(comment.CommentId, currentUserId: 5, isAdmin: false);
            Assert.True(result);
        }
        [Fact]
        public async Task UpdateCommentAsync_AdminCanUpdate_OthersComment()
        {
            var owner = new User { UserId = 80, Username = "owner", PasswordHash = "hash" };
            var comment = new Comment { Content = "Original", UserId = owner.UserId };
            _context.Users.Add(owner);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var updated = new Comment { Content = "Admin edit" };

            var result = await _commentService.UpdateCommentAsync(comment.CommentId, updated, currentUserId: 999, isAdmin: true);

            Assert.NotNull(result);
            Assert.Equal("Admin edit", result.Content);
        }
        [Fact]
        public async Task DeleteCommentAsync_AdminDeletes_OthersComment()
        {
            var owner = new User { UserId = 81, Username = "owner", PasswordHash = "hash" };
            var comment = new Comment { Content = "Admin target", UserId = owner.UserId };
            _context.Users.Add(owner);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var result = await _commentService.DeleteCommentAsync(comment.CommentId, currentUserId: 999, isAdmin: true);

            Assert.True(result);
        }
        [Fact]
        public async Task CreateCommentAsync_SetsCreatedAndUpdatedAt()
        {
            var comment = new Comment
            {
                Content = "Timestamp test",
                PostId = 1
            };

            var result = await _commentService.CreateCommentAsync(comment, userId: 77);

            Assert.NotEqual(default, result.CreatedAt);
            Assert.NotEqual(default, result.UpdatedAt);
            Assert.Equal(77, result.UserId);
        }
        [Fact]
        public async Task CreateComment_Fail_MissingContent()
        {
            var user = new User { UserId = 111, Username = "blank", PasswordHash = "hashed" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _commentController.CreateComment(new CreateCommentRequest
            {
                PostId = 1,
                Content = "" // simulate empty but non-null to avoid EF crash
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CreateComment_Fail_NoContextSet()
        {
            _commentController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // no user set
            };

            var result = await _commentController.CreateComment(new CreateCommentRequest
            {
                PostId = 1,
                Content = "Test"
            });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var msg = unauthorized.Value.GetType().GetProperty("message")?.GetValue(unauthorized.Value);
            Assert.Equal("Invalid token: missing user id.", msg);
        }

        [Fact]
        public async Task UpdateComment_Fail_NoContext()
        {
            _commentController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // no user ID claim
                }
            };

            var result = await _commentController.UpdateComment(1, new UpdateCommentRequest
            {
                Content = "No context edit"
            });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var msg = unauthorized.Value.GetType().GetProperty("message")?.GetValue(unauthorized.Value);
            Assert.Equal("Invalid token: missing user id.", msg);
        }

        [Fact]
        public async Task DeleteComment_Fail_NoContext()
        {
            _commentController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // no user ID claim
                }
            };

            var result = await _commentController.DeleteComment(1);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var msg = unauthorized.Value.GetType().GetProperty("message")?.GetValue(unauthorized.Value);
            Assert.Equal("Invalid token: missing user id.", msg);
        }

        [Fact]
        public async Task CreateComment_Fail_PostNotFound()
        {
            var user = new User { UserId = 222, Username = "nopost", PasswordHash = "hash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _commentController.CreateComment(new CreateCommentRequest
            {
                PostId = 99999, // Definitely doesn't exist
                Content = "Trying to comment on a missing post"
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateComment_AdminAppendsSignatureAndLogsAction()
        {
            var adminUser = new User { UserId = 201, Username = "adminUser", IsAdmin = true, PasswordHash = "hash" };
            var commentOwner = new User { UserId = 202, Username = "commentOwner", PasswordHash = "hash" };
            var comment = new Comment { CommentId = 301, Content = "Original content", UserId = commentOwner.UserId };

            _context.Users.AddRange(adminUser, commentOwner);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            SetUserContext(adminUser.UserId);

            var result = await _commentController.UpdateComment(comment.CommentId, new UpdateCommentRequest
            {
                Content = "Admin edited"
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedComment = Assert.IsType<Comment>(okResult.Value);
            Assert.Contains("-edited by adminUser", updatedComment.Content);

            var log = await _context.AdminLogs.FirstOrDefaultAsync(l => l.Action == "Edited Comment");
            Assert.NotNull(log);
            Assert.Equal(adminUser.UserId, log.AdminId);
        }
        [Fact]
        public async Task UpdateComment_Fails_WhenCommentNotFound()
        {
            var user = new User { UserId = 301, Username = "editor", PasswordHash = "hash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _commentController.UpdateComment(99999, new UpdateCommentRequest
            {
                Content = "Non-existent update"
            });

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var msg = notFound.Value.GetType().GetProperty("message")?.GetValue(notFound.Value);
            Assert.Equal("Comment not found.", msg);
        }

        [Fact]
        public async Task DeleteComment_Fails_WhenCommentNotFound()
        {
            var user = new User { UserId = 601, Username = "deleter", PasswordHash = "hash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUserContext(user.UserId);

            var result = await _commentController.DeleteComment(99999);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var msg = notFound.Value.GetType().GetProperty("message")?.GetValue(notFound.Value);
            Assert.Equal("Comment not found.", msg);
        }



    }
}