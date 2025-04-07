using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;

namespace WatchedApi.Infrastructure
{
    public class PostService
    {
        private readonly ApplicationDbContext _context;

        public PostService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Post> CreatePostAsync(Post post, int userId)
        {
            post.UserId = userId;
            post.CreatedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<Post> GetPostByIdAsync(int postId)
        {
            return await _context.Posts
                .Include(p => p.User)
                 .Include(p => p.Movie)
                .Include(p => p.Comments)
                .Include(p => p.PostLikes)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<List<Post>> GetAllPostsAsync()
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Movie)
                .Include(p => p.Comments)
                .Include(p => p.PostLikes)
                .ToListAsync();
        }

        // Must be owner or an admin.
        public async Task<Post> UpdatePostAsync(int postId, Post updatedPost, int currentUserId, bool isAdmin)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return null;

            // Only the owner or an admin may update the post.
            if (post.UserId != currentUserId && !isAdmin)
            {
                return null;
            }

            post.Title = updatedPost.Title;
            post.Content = updatedPost.Content;
            post.UpdatedAt = DateTime.UtcNow;

            _context.Posts.Update(post);
            await _context.SaveChangesAsync();

            return post;
        }

        // Must be owner or an admin.
        public async Task<bool> DeletePostAsync(int postId, int currentUserId, bool isAdmin)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return false;

            if (post.UserId != currentUserId && !isAdmin)
            {
                return false;
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task<PostLike> LikePostAsync(int postId, int userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return null;
            }

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(like => like.PostId == postId && like.UserId == userId);
            if (existingLike != null)
            {
                return null;
            }

            var newLike = new PostLike
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.PostLikes.Add(newLike);
            await _context.SaveChangesAsync();

            return newLike;
        }

        public async Task<bool> UnlikePostAsync(int postId, int userId)
        {
            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(like => like.PostId == postId && like.UserId == userId);
            if (existingLike == null)
            {
                return false;
            }
            _context.PostLikes.Remove(existingLike);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
