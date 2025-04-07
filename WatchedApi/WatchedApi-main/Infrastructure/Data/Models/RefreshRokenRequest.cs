using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WatchedApi.Infrastructure.Data.Models
{
    public class RefreshTokenRequest
    {
        public int UserId { get; set; }
        public string Token { get; set; }
    }
}