using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using UserService.Domain.Context;
using UserService.Domain.Entities;

namespace UserService.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> EmailExistsAsync(string email);
    }

    public class UserRepository : IUserRepository
    {
        private readonly SpicyNoodleDbContext _context;

        public UserRepository(SpicyNoodleDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;

            // Hash password if it's provided
            if (!string.IsNullOrEmpty(user.Password))
            {
                user.Password = HashPassword(user.Password);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = false; // Soft delete
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Users.AnyAsync(u => u.UserId == id && u.IsActive);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email && u.IsActive);
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password + "SpicyNoodleSalt"));
                return Convert.ToBase64String(bytes);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            string hashOfInput = HashPassword(password);
            return hashOfInput == hashedPassword;
        }
    }
}
