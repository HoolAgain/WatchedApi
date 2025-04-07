namespace WatchedApi.Infrastructure.Data.Models
{
    public class SiteActivityLogDto
    {
        public int Id { get; set; }
        public string Activity { get; set; }
        public string Operation { get; set; }
        public DateTime? TimeOf { get; set; }
        public int UserId { get; set; }
    }

}
