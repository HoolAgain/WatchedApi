using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;

namespace WatchedApi.Infrastructure
{
    public class CommentService
    {
        private readonly ApplicationDbContext _context;

        public CommentService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create a new comment, must be logged in
        public async Task<Comment> CreateCommentAsync(Comment comment, int userId)
        {
            comment.UserId = userId;
            comment.CreatedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }


        public async Task<Comment> GetCommentByIdAsync(int commentId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);
        }


        public async Task<List<Comment>> GetCommentsByPostIdAsync(int postId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostId == postId)
                .ToListAsync();
        }

        // only the comment owner or an admin may update.
        public async Task<Comment> UpdateCommentAsync(int commentId, Comment updatedComment, int currentUserId, bool isAdmin)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return null;

            if (comment.UserId != currentUserId && !isAdmin)
                return null;

            comment.Content = updatedComment.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return comment;
        }

        // only the comment owner or an admin may delete.
        public async Task<bool> DeleteCommentAsync(int commentId, int currentUserId, bool isAdmin)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return false;

            if (comment.UserId != currentUserId && !isAdmin)
                return false;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
