using System.Text.Json.Serialization;

namespace WatchedApi.Infrastructure.Data.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; }

        //used so when you execute a json it ignored the required value for this
        [JsonIgnore]
        public User User { get; set; }
    }
}
