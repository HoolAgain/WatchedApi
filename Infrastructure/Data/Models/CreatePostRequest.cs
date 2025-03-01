namespace WatchedApi.Infrastructure.Data.Models
{
    public class CreatePostRequest
    {
        public int MovieId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
