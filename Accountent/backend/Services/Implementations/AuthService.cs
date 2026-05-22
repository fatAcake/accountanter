using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Configuration;
using backend.Models;
using backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.Implementations
{
    public class AuthService : ServiceBase<AuthService>, IAuthService
    {
        private readonly IJwtTokenService _jwt;
        private readonly IEntityMappingService _mapper;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IDatabaseContextFactory dbFactory,
            IJwtTokenService jwt,
            IEntityMappingService mapper,
            IOptions<JwtSettings> jwtOptions,
            ILogger<AuthService> logger)
            : base(dbFactory, logger)
        {
            _jwt = jwt;
            _mapper = mapper;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<(AuthResponse? response, string? error)> RegisterAsync(RegisterRequest request)
        {
            Logger.LogInformation("Регистрация пользователя {Email}", request.email);
            var email = request.email.Trim().ToLowerInvariant();

            await using var db = CreateContext();

            if (await db.Users.AnyAsync(u => u.email == email && !u.deleted))
            {
                Logger.LogWarning("Email уже занят: {Email}", email);
                return (null, "Пользователь с таким email уже существует.");
            }

            if (request.role is Roles.admin or Roles.accountant)
                return (null, "Роль admin/accountant может назначить только администратор.");

            var user = new Users
            {
                nickname = request.nickname.Trim(),
                email = email,
                password = BCrypt.Net.BCrypt.HashPassword(request.password),
                role = request.role == Roles.None ? Roles.observer : request.role,
                registration_date = DateTime.UtcNow,
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var tokens = ApplyTokens(user);
            await db.SaveChangesAsync();

            Logger.LogInformation("Пользователь {UserId} зарегистрирован", user.id);
            return (_mapper.MapAuthResponse(user, tokens.accessToken, tokens.accessExpires), null);
        }

        public async Task<(AuthResponse? response, string? error)> LoginAsync(LoginRequest request)
        {
            Logger.LogInformation("Вход пользователя {Email}", request.email);
            var email = request.email.Trim().ToLowerInvariant();

            await using var db = CreateContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.email == email && !u.deleted);

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.password, user.password))
            {
                Logger.LogWarning("Неудачный вход: {Email}", email);
                return (null, "Неверный email или пароль.");
            }

            var tokens = ApplyTokens(user);
            user.edited_at = DateTime.UtcNow;
            await db.SaveChangesAsync();

            Logger.LogInformation("Пользователь {UserId} вошёл в систему", user.id);
            return (_mapper.MapAuthResponse(user, tokens.accessToken, tokens.accessExpires), null);
        }

        public async Task<(AuthResponse? response, string? error)> RefreshAsync(RefreshTokenRequest request)
        {
            Logger.LogDebug("Обновление access token");

            await using var db = CreateContext();
            var user = await db.Users.FirstOrDefaultAsync(u =>
                u.refresh_token == request.refresh_token && !u.deleted);

            if (user is null ||
                user.refresh_token_expires_at is null ||
                user.refresh_token_expires_at <= DateTime.UtcNow)
            {
                Logger.LogWarning("Недействительный refresh token");
                return (null, "Недействительный или просроченный refresh token.");
            }

            var tokens = ApplyTokens(user);
            user.edited_at = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return (_mapper.MapAuthResponse(user, tokens.accessToken, tokens.accessExpires), null);
        }

        public async Task LogoutAsync(int userId)
        {
            Logger.LogInformation("Выход пользователя {UserId}", userId);

            await using var db = CreateContext();
            var user = await db.Users.FindAsync(userId);
            if (user is null || user.deleted)
                return;

            user.refresh_token = null;
            user.refresh_token_expires_at = null;
            user.edited_at = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        private (string accessToken, DateTime accessExpires) ApplyTokens(Users user)
        {
            var (accessToken, accessExpires) = _jwt.CreateAccessToken(user);
            user.refresh_token = _jwt.CreateRefreshToken();
            user.refresh_token_expires_at = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);
            return (accessToken, accessExpires);
        }
    }
}
