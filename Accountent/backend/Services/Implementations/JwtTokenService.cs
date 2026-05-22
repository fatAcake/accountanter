using backend.Abstractions.Services;
using backend.Configuration;
using backend.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace backend.Services.Implementations
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(IOptions<JwtSettings> options, ILogger<JwtTokenService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public (string token, DateTime expiresAt) CreateAccessToken(Users user)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.email),
                new Claim(ClaimTypes.Name, user.nickname),
                new Claim(ClaimTypes.Role, user.role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            _logger.LogDebug("Создан access token для пользователя {UserId}", user.id);
            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public string CreateRefreshToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        public ClaimsPrincipal? ValidateAccessToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
                ClockSkew = TimeSpan.Zero,
            };

            try
            {
                return handler.ValidateToken(token, parameters, out _);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка валидации access token");
                return null;
            }
        }
    }
}
