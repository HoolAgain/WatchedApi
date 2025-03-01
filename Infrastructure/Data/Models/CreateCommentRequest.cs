namespace WatchedApi.Infrastructure.Data.Models
{
    public class CreateCommentRequest
    {
        public int PostId { get; set; }
        public string Content { get; set; }
    }
}
