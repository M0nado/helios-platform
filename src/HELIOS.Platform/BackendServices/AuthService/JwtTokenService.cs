using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HELIOS.Platform.BackendServices.AuthService
{
    /// <summary>
    /// JWT token service for authentication
    /// Generates and validates JWT tokens
    /// </summary>
    public interface IJwtTokenService
    {
        string GenerateAccessToken(Guid userId, string username, IEnumerable<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        bool ValidateToken(string token);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(
            string secretKey,
            string issuer,
            string audience,
            int expirationMinutes,
            ILogger<JwtTokenService> logger)
        {
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
            _audience = audience ?? throw new ArgumentNullException(nameof(audience));
            _expirationMinutes = expirationMinutes > 0 ? expirationMinutes : 60;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateAccessToken(Guid userId, string username, IEnumerable<string> roles)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, username),
                    new Claim("token_type", "access")
                };

                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
                    Issuer = _issuer,
                    Audience = _audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation($"Generated access token for user {userId}");
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token");
                throw;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = false // We allow expired tokens for refresh
                }, out SecurityToken securityToken);

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating expired token");
                return null;
            }
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true
                }, out SecurityToken securityToken);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// User authentication service
    /// Handles login, registration, and user validation
    /// </summary>
    public interface IAuthenticationService
    {
        Task<(bool Success, string Message, Guid? UserId)> LoginAsync(string username, string password);
        Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password);
        Task<(bool Success, string Message)> LogoutAsync(Guid userId);
        Task<bool> ValidateUserAsync(Guid userId);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(ILogger<AuthenticationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(bool Success, string Message, Guid? UserId)> LoginAsync(string username, string password)
        {
            try
            {
                // TODO: Implement actual user validation against database
                // This is a placeholder for the actual implementation
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning($"Login attempt with missing credentials");
                    return (false, "Invalid credentials", null);
                }

                var userId = Guid.NewGuid(); // Placeholder
                _logger.LogInformation($"User {username} logged in successfully");
                return (true, "Login successful", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return (false, "Login failed", null);
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Registration attempt with missing fields");
                    return (false, "Missing required fields");
                }

                // TODO: Implement actual user registration with database
                _logger.LogInformation($"User {username} registered successfully");
                return (true, "Registration successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return (false, "Registration failed");
            }
        }

        public async Task<(bool Success, string Message)> LogoutAsync(Guid userId)
        {
            try
            {
                // TODO: Implement logout logic (invalidate tokens, etc.)
                _logger.LogInformation($"User {userId} logged out");
                return (true, "Logout successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return (false, "Logout failed");
            }
        }

        public async Task<bool> ValidateUserAsync(Guid userId)
        {
            try
            {
                // TODO: Implement actual user validation
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user");
                return false;
            }
        }
    }
}
