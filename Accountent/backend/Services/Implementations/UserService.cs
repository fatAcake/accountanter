using backend.Abstractions.Common;
using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Models;
using backend.Models.DTOs;
using backend.Models.Results;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class UserService : ServiceBase<UserService>, IUserService
    {
        private readonly IEntityMappingService _mapper;
        private readonly IAdminAuditService _audit;

        public UserService(
            IDatabaseContextFactory dbFactory,
            IEntityMappingService mapper,
            IAdminAuditService audit,
            ILogger<UserService> logger)
            : base(dbFactory, logger)
        {
            _mapper = mapper;
            _audit = audit;
        }

        public async Task<List<UserResponse>> GetAllAsync(UserFilter filter)
        {
            Logger.LogDebug("Список пользователей, search={Search}", filter.search);

            await using var db = CreateContext();
            var query = db.Users.AsNoTracking().Where(u => !u.deleted);

            if (!string.IsNullOrWhiteSpace(filter.search))
            {
                var term = filter.search.Trim().ToLowerInvariant();
                query = query.Where(u =>
                    u.nickname.ToLower().Contains(term) ||
                    u.email.Contains(term));
            }

            var items = await query.OrderBy(u => u.nickname).ToListAsync();
            return items.Select(_mapper.MapUserResponse).ToList();
        }

        public async Task<UserResponse?> GetByIdAsync(int id)
        {
            await using var db = CreateContext();
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.id == id && !u.deleted);

            return user is null ? null : _mapper.MapUserResponse(user);
        }

        public async Task<ServiceResult<UserResponse>> CreateAsync(
            CreateUserRequest request,
            int adminUserId)
        {
            Logger.LogInformation("Создание пользователя {Email}", request.email);

            if (request.role is Roles.None)
                return ServiceResult<UserResponse>.Fail(
                    ServiceErrorCode.Validation,
                    "Укажите допустимую роль.");

            var email = request.email.Trim().ToLowerInvariant();

            await using var db = CreateContext();
            if (await db.Users.AnyAsync(u => u.email == email && !u.deleted))
                return ServiceResult<UserResponse>.Fail(
                    ServiceErrorCode.Conflict,
                    "Пользователь с таким email уже существует.");

            var entity = new Users
            {
                nickname = request.nickname.Trim(),
                email = email,
                password = BCrypt.Net.BCrypt.HashPassword(request.password),
                role = request.role,
                registration_date = DateTime.UtcNow,
            };

            db.Users.Add(entity);
            await db.SaveChangesAsync();

            await _audit.LogAsync(
                adminUserId,
                AdminAuditActions.UserCreate,
                entity.id,
                entity.email,
                $"роль: {entity.role}");

            Logger.LogInformation("Пользователь {UserId} создан", entity.id);
            return ServiceResult<UserResponse>.Ok(_mapper.MapUserResponse(entity));
        }

        public async Task<ServiceResult<UserResponse>> UpdateAsync(
            int id,
            UpdateUserRequest request,
            int currentUserId)
        {
            Logger.LogInformation("Обновление пользователя {UserId}", id);

            if (request.role is Roles.None)
                return ServiceResult<UserResponse>.Fail(
                    ServiceErrorCode.Validation,
                    "Укажите допустимую роль.");

            var email = request.email.Trim().ToLowerInvariant();

            await using var db = CreateContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.id == id && !u.deleted);
            if (user is null)
                return ServiceResult<UserResponse>.Fail(
                    ServiceErrorCode.NotFound,
                    "Пользователь не найден.");

            if (await db.Users.AnyAsync(u => u.email == email && u.id != id && !u.deleted))
                return ServiceResult<UserResponse>.Fail(
                    ServiceErrorCode.Conflict,
                    "Пользователь с таким email уже существует.");

            if (id == currentUserId && request.role != Roles.admin)
                return ServiceResult<UserResponse>.Fail(
                    ServiceErrorCode.Forbidden,
                    "Нельзя снять с себя роль администратора.");

            if (user.role == Roles.admin && request.role != Roles.admin)
            {
                var otherAdmins = await db.Users.CountAsync(u =>
                    !u.deleted && u.role == Roles.admin && u.id != id);
                if (otherAdmins == 0)
                    return ServiceResult<UserResponse>.Fail(
                        ServiceErrorCode.Validation,
                        "В системе должен остаться хотя бы один администратор.");
            }

            var passwordChanged = !string.IsNullOrWhiteSpace(request.password);
            user.nickname = request.nickname.Trim();
            user.email = email;
            user.role = request.role;

            if (passwordChanged)
            {
                user.password = BCrypt.Net.BCrypt.HashPassword(request.password!);
                user.refresh_token = null;
                user.refresh_token_expires_at = null;
            }

            user.edited_at = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var details = passwordChanged
                ? $"роль: {user.role}, пароль изменён"
                : $"роль: {user.role}";

            await _audit.LogAsync(
                currentUserId,
                AdminAuditActions.UserUpdate,
                user.id,
                user.email,
                details);

            return ServiceResult<UserResponse>.Ok(_mapper.MapUserResponse(user));
        }

        public async Task<ServiceResult> DeleteAsync(int id, int currentUserId)
        {
            Logger.LogInformation("Удаление пользователя {UserId}", id);

            if (id == currentUserId)
                return ServiceResult.Fail(
                    ServiceErrorCode.Validation,
                    "Нельзя удалить свою учётную запись.");

            await using var db = CreateContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.id == id && !u.deleted);
            if (user is null)
                return ServiceResult.Fail(ServiceErrorCode.NotFound, "Пользователь не найден.");

            if (user.role == Roles.admin)
            {
                var otherAdmins = await db.Users.CountAsync(u =>
                    !u.deleted && u.role == Roles.admin && u.id != id);
                if (otherAdmins == 0)
                    return ServiceResult.Fail(
                        ServiceErrorCode.Validation,
                        "Нельзя удалить последнего администратора.");
            }

            user.deleted = true;
            user.deleted_at = DateTime.UtcNow;
            user.refresh_token = null;
            user.refresh_token_expires_at = null;
            user.edited_at = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await _audit.LogAsync(
                currentUserId,
                AdminAuditActions.UserDelete,
                user.id,
                user.email);

            return ServiceResult.Ok();
        }

        public async Task<List<UserSessionResponse>> GetActiveSessionsAsync()
        {
            var now = DateTime.UtcNow;

            await using var db = CreateContext();
            return await db.Users
                .AsNoTracking()
                .Where(u =>
                    !u.deleted &&
                    u.refresh_token != null &&
                    u.refresh_token_expires_at != null &&
                    u.refresh_token_expires_at > now)
                .OrderBy(u => u.nickname)
                .Select(u => new UserSessionResponse
                {
                    user_id = u.id,
                    nickname = u.nickname,
                    email = u.email,
                    role = u.role,
                    expires_at = u.refresh_token_expires_at!.Value,
                    last_seen_at = u.edited_at,
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> RevokeSessionAsync(int userId, int adminUserId)
        {
            await using var db = CreateContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.id == userId && !u.deleted);
            if (user is null)
                return ServiceResult.Fail(ServiceErrorCode.NotFound, "Пользователь не найден.");

            if (user.refresh_token is null)
                return ServiceResult.Fail(ServiceErrorCode.NotFound, "Активная сессия не найдена.");

            user.refresh_token = null;
            user.refresh_token_expires_at = null;
            user.edited_at = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await _audit.LogAsync(
                adminUserId,
                AdminAuditActions.SessionRevoke,
                user.id,
                user.email);

            return ServiceResult.Ok();
        }

        public async Task<ImportUsersResult> ImportFromCsvAsync(string csvContent, int adminUserId)
        {
            var result = new ImportUsersResult();
            var rows = ParseCsvRows(csvContent);
            if (rows.Count == 0)
            {
                result.errors.Add(new ImportUserRowError
                {
                    line = 0,
                    message = "Файл пуст или не содержит данных.",
                });
                return result;
            }

            await using var db = CreateContext();

            foreach (var (lineNumber, nickname, email, password, roleStr) in rows)
            {
                if (!TryParseRole(roleStr, out var role))
                {
                    result.skipped++;
                    result.errors.Add(new ImportUserRowError
                    {
                        line = lineNumber,
                        email = email,
                        message = $"Недопустимая роль: {roleStr}",
                    });
                    continue;
                }

                if (role is Roles.None)
                {
                    result.skipped++;
                    result.errors.Add(new ImportUserRowError
                    {
                        line = lineNumber,
                        email = email,
                        message = "Укажите роль: admin, accountant или observer.",
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(nickname) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    result.skipped++;
                    result.errors.Add(new ImportUserRowError
                    {
                        line = lineNumber,
                        email = email,
                        message = "Заполните nickname, email и password.",
                    });
                    continue;
                }

                var normalizedEmail = email.Trim().ToLowerInvariant();
                if (await db.Users.AnyAsync(u => u.email == normalizedEmail && !u.deleted))
                {
                    result.skipped++;
                    result.errors.Add(new ImportUserRowError
                    {
                        line = lineNumber,
                        email = normalizedEmail,
                        message = "Email уже зарегистрирован.",
                    });
                    continue;
                }

                if (password.Length < 5)
                {
                    result.skipped++;
                    result.errors.Add(new ImportUserRowError
                    {
                        line = lineNumber,
                        email = normalizedEmail,
                        message = "Пароль должен быть не короче 5 символов.",
                    });
                    continue;
                }

                var entity = new Users
                {
                    nickname = nickname.Trim(),
                    email = normalizedEmail,
                    password = BCrypt.Net.BCrypt.HashPassword(password),
                    role = role,
                    registration_date = DateTime.UtcNow,
                };
                db.Users.Add(entity);
                result.created++;
            }

            if (result.created > 0)
                await db.SaveChangesAsync();

            await _audit.LogAsync(
                adminUserId,
                AdminAuditActions.UserImport,
                details: $"создано: {result.created}, пропущено: {result.skipped}, ошибок: {result.errors.Count}");

            return result;
        }

        private static List<(int line, string nickname, string email, string password, string role)> ParseCsvRows(
            string csvContent)
        {
            var rows = new List<(int, string, string, string, string)>();
            var lines = csvContent
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var startIndex = 0;
            if (lines.Length > 0 && LooksLikeHeader(lines[0]))
                startIndex = 1;

            for (var i = startIndex; i < lines.Length; i++)
            {
                var parts = SplitCsvLine(lines[i]);
                if (parts.Count < 4)
                    continue;

                rows.Add((i + 1, parts[0], parts[1], parts[2], parts[3]));
            }

            return rows;
        }

        private static bool LooksLikeHeader(string line)
        {
            var lower = line.ToLowerInvariant();
            return lower.Contains("nickname") &&
                   lower.Contains("email") &&
                   (lower.Contains("password") || lower.Contains("пароль"));
        }

        private static List<string> SplitCsvLine(string line)
        {
            var delimiter = line.Contains(';') && !line.Contains(',') ? ';' : ',';
            return line.Split(delimiter, StringSplitOptions.TrimEntries)
                .Select(p => p.Trim().Trim('"'))
                .ToList();
        }

        private static bool TryParseRole(string value, out Roles role)
        {
            var normalized = value.Trim().ToLowerInvariant();
            return normalized switch
            {
                "admin" or "администратор" => Assign(Roles.admin, out role),
                "accountant" or "бухгалтер" => Assign(Roles.accountant, out role),
                "observer" or "наблюдатель" => Assign(Roles.observer, out role),
                _ => Assign(Roles.None, out role),
            };

            static bool Assign(Roles r, out Roles role)
            {
                role = r;
                return true;
            }
        }
    }
}
