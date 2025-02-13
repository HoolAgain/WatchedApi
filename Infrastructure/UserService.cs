using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;

namespace WatchedApi.Infrastructure
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterUserAsync(string username, string password)
        {
            //checks if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                return null;//which in the controller will display user already exists
            }

            //hash the password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var createdUser = new User
            {
                Username = username,
                PasswordHash = hashedPassword
            };

            //add to database
            _context.Users.Add(createdUser);
            await _context.SaveChangesAsync();

            return createdUser;
        }
    }
}