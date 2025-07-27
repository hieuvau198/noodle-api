using Microsoft.AspNetCore.Identity.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Application.Dtos;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services
{
    public interface IUsersService
    {
        Task<AuthResult> LoginAsync(Dtos.LoginRequest request);
        Task<AuthResult> RegisterAsync(Dtos.RegisterRequest request);
        Task<bool> ForgotPasswordAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> UpdateUserAsync(int id, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(int id);
    }

    public class UsersService : IUsersService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public UsersService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<AuthResult> LoginAsync(Dtos.LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null || !user.IsActive)
            {
                return new AuthResult { Success = false, Message = "Invalid email or password" };
            }

            // For Google users, they should use Google login
            if (user.IsGoogleUser)
            {
                return new AuthResult { Success = false, Message = "Please use Google login for this account" };
            }

            if (!UserRepository.VerifyPassword(request.Password, user.Password!))
            {
                return new AuthResult { Success = false, Message = "Invalid email or password" };
            }

            var token = GenerateJwtToken(user);

            return new AuthResult
            {
                Success = true,
                Token = token,
                User = new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    IsGoogleUser = user.IsGoogleUser
                }
            };
        }

        public async Task<AuthResult> RegisterAsync(Dtos.RegisterRequest request)
        {
            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                return new AuthResult { Success = false, Message = "Email already exists" };
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password, // Will be hashed in repository
                Role = 0, // Default role (customer)
                IsGoogleUser = false,
                IsActive = true
            };

            var createdUser = await _userRepository.CreateAsync(user);
            var token = GenerateJwtToken(createdUser);

            return new AuthResult
            {
                Success = true,
                Token = token,
                User = new UserDto
                {
                    UserId = createdUser.UserId,
                    FullName = createdUser.FullName,
                    Email = createdUser.Email,
                    Role = createdUser.Role,
                    IsGoogleUser = createdUser.IsGoogleUser
                }
            };
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null || !user.IsActive || user.IsGoogleUser)
            {
                // Return true even if user doesn't exist for security
                return true;
            }

            // Generate temporary password
            var tempPassword = GenerateTemporaryPassword();
            user.Password = tempPassword; // Will be hashed in repository

            await _userRepository.UpdateAsync(user);

            // TODO: Send email with temporary password
            // For now, just log it (in production, use proper email service)
            Console.WriteLine($"Temporary password for {email}: {tempPassword}");

            return true;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<User> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || !user.IsActive)
            {
                throw new ArgumentException("User not found");
            }

            user.FullName = request.FullName ?? user.FullName;
            user.Email = request.Email ?? user.Email;

            if (!string.IsNullOrEmpty(request.Password))
            {
                user.Password = request.Password; // Will be hashed in repository
            }

            return await _userRepository.UpdateAsync(user);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            if (!await _userRepository.ExistsAsync(id))
            {
                return false;
            }

            await _userRepository.DeleteAsync(id);
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "SpicyNoodleSecretKey12345");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
