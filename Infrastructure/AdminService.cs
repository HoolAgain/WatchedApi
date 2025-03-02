using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using System;
using System.Threading.Tasks;

namespace WatchedApi.Infrastructure
{
    public class AdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        //hard coded strings
        private readonly string adminUsername = "admin";
        private readonly string adminPassword = "adminpass";

        public async Task<bool> DeletePostById(int postId)
        {
            try
            {
                //find postid
                var post = await _context.Posts.FindAsync(postId);
                if (post == null)
                    return false;

                //remove post given id
                _context.Posts.Remove(post);
                //save changes
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("Error deleting post");
                return false;
            }
        }

        public async Task<bool> ValidateAdminLogin(string username, string password)
        {
            //return true if input equals those strings
            return await Task.FromResult(username == adminUsername && password == adminPassword);
        }
    }
}
