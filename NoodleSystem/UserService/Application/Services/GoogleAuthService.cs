using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Application.Dtos;

namespace UserService.Application.Services
{
    public interface IGoogleAuthService
    {
        Task<AuthResult> AuthenticateGoogleUserAsync(string idToken);
        Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken);
    }

    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GoogleAuthService(IUserRepository userRepository, IConfiguration configuration, HttpClient httpClient)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<AuthResult> AuthenticateGoogleUserAsync(string idToken)
        {
            try
            {
                Console.WriteLine($"Starting Google authentication for token length: {idToken.Length}");
                
                // Verify the ID token with Google
                var googleUserInfo = await VerifyGoogleIdTokenAsync(idToken);
                if (googleUserInfo == null)
                {
                    Console.WriteLine("Google ID token verification failed");
                    return new AuthResult { Success = false, Message = "Invalid Google ID token" };
                }

                // Check if user exists
                var existingUser = await _userRepository.GetByGoogleIdAsync(googleUserInfo.Sub);
                
                if (existingUser != null)
                {
                    // User exists, generate JWT token and token ID
                    var (token, tokenId) = GenerateJwtTokenWithId(existingUser);
                    
                    // Debug logging
                    Console.WriteLine($"Generated token for existing user: {existingUser.Email}");
                    Console.WriteLine($"Token length: {token?.Length ?? 0}");
                    Console.WriteLine($"TokenId: {tokenId}");
                    
                    return new AuthResult
                    {
                        Success = true,
                        Token = token,
                        TokenId = tokenId,
                        User = new UserDto
                        {
                            UserId = existingUser.UserId,
                            FullName = existingUser.FullName,
                            Email = existingUser.Email,
                            Role = existingUser.Role,
                            IsGoogleUser = existingUser.IsGoogleUser
                        }
                    };
                }
                else
                {
                    // Create new user
                    var newUser = new User
                    {
                        FullName = googleUserInfo.Name,
                        Email = googleUserInfo.Email,
                        GoogleId = googleUserInfo.Sub,
                        IsGoogleUser = true,
                        Role = 2, // Customer role
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdUser = await _userRepository.CreateAsync(newUser);
                    var (token, tokenId) = GenerateJwtTokenWithId(createdUser);

                    // Debug logging
                    Console.WriteLine($"Generated token for new user: {createdUser.Email}");
                    Console.WriteLine($"Token length: {token?.Length ?? 0}");
                    Console.WriteLine($"TokenId: {tokenId}");

                    return new AuthResult
                    {
                        Success = true,
                        Token = token,
                        TokenId = tokenId,
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
            }
            catch (Exception ex)
            {
                return new AuthResult { Success = false, Message = $"Google authentication failed: {ex.Message}" };
            }
        }

        public async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}");
                response.EnsureSuccessStatusCode();
                
                var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
                return userInfo ?? throw new Exception("Failed to parse Google user info");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get Google user info: {ex.Message}");
            }
        }

        private async Task<GoogleUserInfo?> VerifyGoogleIdTokenAsync(string idToken)
        {
            try
            {
                Console.WriteLine($"Verifying Google ID token, length: {idToken.Length}");
                
                var response = await _httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
                
                Console.WriteLine($"Google tokeninfo response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Google tokeninfo error: {errorContent}");
                    return null;
                }

                var tokenInfo = await response.Content.ReadFromJsonAsync<GoogleTokenInfo>();
                if (tokenInfo == null)
                {
                    Console.WriteLine("Failed to parse Google token info");
                    return null;
                }

                Console.WriteLine($"Google token info - Aud: {tokenInfo.Aud}, Sub: {tokenInfo.Sub}, Email: {tokenInfo.Email}");

                // Verify the token is for our application
                var clientId = _configuration["Authentication:Google:ClientId"];
                Console.WriteLine($"Expected client ID: {clientId}");
                Console.WriteLine($"Token audience: {tokenInfo.Aud}");
                
                if (tokenInfo.Aud != clientId)
                {
                    Console.WriteLine("Client ID mismatch");
                    return null;
                }

                Console.WriteLine("Google ID token verification successful");
                return new GoogleUserInfo
                {
                    Sub = tokenInfo.Sub,
                    Name = tokenInfo.Name,
                    Email = tokenInfo.Email,
                    Picture = tokenInfo.Picture
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying Google ID token: {ex.Message}");
                return null;
            }
        }

        private (string token, string tokenId) GenerateJwtTokenWithId(User user)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("Jwt");
                var secretKey = jwtSettings["Secret"] ?? "your-secret-key-here";
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Generate a unique token ID
                var tokenId = Guid.NewGuid().ToString();

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("IsGoogleUser", user.IsGoogleUser.ToString()),
                    new Claim("TokenId", tokenId), // Add token ID to claims
                    new Claim("jti", tokenId) // JWT ID claim
                };

                var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(Convert.ToInt32(jwtSettings["ExpirationInDays"] ?? "7")),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                
                // Debug logging
                Console.WriteLine($"JWT Settings - Issuer: {jwtSettings["Issuer"]}, Audience: {jwtSettings["Audience"]}");
                Console.WriteLine($"Generated token string length: {tokenString?.Length ?? 0}");
                Console.WriteLine($"TokenId: {tokenId}");
                
                return (tokenString, tokenId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating JWT token: {ex.Message}");
                throw;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var (token, _) = GenerateJwtTokenWithId(user);
            return token;
        }
    }

    public class GoogleUserInfo
    {
        public string Sub { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
    }

    public class GoogleTokenInfo
    {
        public string Aud { get; set; } = string.Empty;
        public string Sub { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
    }
} 