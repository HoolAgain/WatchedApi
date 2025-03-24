namespace WatchedApi.Infrastructure.Data.Models
{
    public class PostDto
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Username { get; set; }
        public int LikeCount { get; set; }
        public bool HasLiked { get; set; }
    }
}
