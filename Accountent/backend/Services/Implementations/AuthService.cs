using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Configuration;
using backend.Models;
using backend.Models.DTOs;
using backend.Models.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services.Implementations
{
    public class AuthService : ServiceBase<AuthService>, IAuthService
    {
        private readonly IJwtTokenService _jwt;
        private readonly IRefreshTokenService _refreshTokens;
        private readonly IEntityMappingService _mapper;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IDatabaseContextFactory dbFactory,
            IJwtTokenService jwt,
            IRefreshTokenService refreshTokens,
            IEntityMappingService mapper,
            IOptions<JwtSettings> jwtOptions,
            ILogger<AuthService> logger)
            : base(dbFactory, logger)
        {
            _jwt = jwt;
            _refreshTokens = refreshTokens;
            _mapper = mapper;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            Logger.LogInformation("Регистрация пользователя {Email}", request.email);
            var email = request.email.Trim().ToLowerInvariant();

            await using var db = CreateContext();

            if (await db.Users.AnyAsync(u => u.email == email && !u.deleted))
            {
                Logger.LogWarning("Email уже занят: {Email}", email);
                return ServiceResult<AuthResponse>.Fail(
                    ServiceErrorCode.Conflict,
                    "Пользователь с таким email уже существует.");
            }

            if (request.role is Roles.admin or Roles.accountant)
                return ServiceResult<AuthResponse>.Fail(
                    ServiceErrorCode.Forbidden,
                    "Роль admin/accountant может назначить только администратор.");

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
            return ServiceResult<AuthResponse>.Ok(MapAuth(user, tokens));
        }

        public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request)
        {
            Logger.LogInformation("Вход пользователя {Email}", request.email);
            var email = request.email.Trim().ToLowerInvariant();

            await using var db = CreateContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.email == email && !u.deleted);

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.password, user.password))
            {
                Logger.LogWarning("Неудачный вход: {Email}", email);
                return ServiceResult<AuthResponse>.Fail(
                    ServiceErrorCode.Unauthorized,
                    "Неверный email или пароль.");
            }

            var tokens = ApplyTokens(user);
            user.edited_at = DateTime.UtcNow;
            await db.SaveChangesAsync();

            Logger.LogInformation("Пользователь {UserId} вошёл в систему", user.id);
            return ServiceResult<AuthResponse>.Ok(MapAuth(user, tokens));
        }

        public async Task<ServiceResult<AuthResponse>> RefreshAsync(RefreshTokenRequest request)
        {
            Logger.LogDebug("Обновление access token");

            if (string.IsNullOrWhiteSpace(request.refresh_token))
            {
                return ServiceResult<AuthResponse>.Fail(
                    ServiceErrorCode.Unauthorized,
                    "Недействительный или просроченный refresh token.");
            }

            await using var db = CreateContext();
            var now = DateTime.UtcNow;

            var candidates = await db.Users
                .Where(u =>
                    !u.deleted &&
                    u.refresh_token != null &&
                    u.refresh_token_expires_at != null &&
                    u.refresh_token_expires_at > now)
                .ToListAsync();

            var user = candidates.FirstOrDefault(u =>
                _refreshTokens.VerifyToken(request.refresh_token, u.refresh_token!));

            if (user is null)
            {
                Logger.LogWarning("Недействительный refresh token");
                return ServiceResult<AuthResponse>.Fail(
                    ServiceErrorCode.Unauthorized,
                    "Недействительный или просроченный refresh token.");
            }

            var tokens = ApplyTokens(user);
            user.edited_at = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return ServiceResult<AuthResponse>.Ok(MapAuth(user, tokens));
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

        private AuthResponse MapAuth(Users user, TokenBundle tokens) =>
            _mapper.MapAuthResponse(
                user,
                tokens.AccessToken,
                tokens.AccessExpires,
                tokens.RefreshToken,
                tokens.RefreshExpires);

        private TokenBundle ApplyTokens(Users user)
        {
            var (accessToken, accessExpires) = _jwt.CreateAccessToken(user);
            var rawRefresh = _jwt.CreateRefreshToken();
            var refreshExpires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

            user.refresh_token = _refreshTokens.HashToken(rawRefresh);
            user.refresh_token_expires_at = refreshExpires;

            return new TokenBundle(accessToken, accessExpires, rawRefresh, refreshExpires);
        }

        private sealed record TokenBundle(
            string AccessToken,
            DateTime AccessExpires,
            string RefreshToken,
            DateTime RefreshExpires);
    }
}
