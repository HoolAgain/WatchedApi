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

        public async Task<User> RegisterUserAsync(string username, string password, string fullName, string email, string phoneNumber, string address)
        {
            //check
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("All fields are required.");
            }

            //Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                return null; // Will return null if user exists
            }

            //Hash the password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            //Create new user
            var createdUser = new User
            {
                Username = username,
                PasswordHash = hashedPassword,
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                Address = address,
                CreatedAt = DateTime.UtcNow
            };

            //Save to database
            _context.Users.Add(createdUser);
            await _context.SaveChangesAsync();

            return createdUser;
        }
    }
}