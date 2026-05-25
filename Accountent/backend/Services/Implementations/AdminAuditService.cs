using backend.Abstractions.Data;
using backend.Abstractions.Services;
using backend.Models;
using backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations
{
    public class AdminAuditService : ServiceBase<AdminAuditService>, IAdminAuditService
    {
        public AdminAuditService(IDatabaseContextFactory dbFactory, ILogger<AdminAuditService> logger)
            : base(dbFactory, logger)
        {
        }

        public async Task LogAsync(
            int adminUserId,
            string action,
            int? targetUserId = null,
            string? targetEmail = null,
            string? details = null)
        {
            await using var db = CreateContext();
            var admin = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.id == adminUserId && !u.deleted);
            if (admin is null)
                return;

            db.AdminAuditLogs.Add(new AdminAuditLog
            {
                admin_user_id = adminUserId,
                admin_nickname = admin.nickname,
                action = action,
                target_user_id = targetUserId,
                target_email = targetEmail,
                details = details,
                created_at = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        public async Task<List<AdminAuditLogResponse>> GetLogsAsync(AuditLogFilter filter)
        {
            var limit = Math.Clamp(filter.limit, 1, 500);

            await using var db = CreateContext();
            var query = db.AdminAuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.action))
                query = query.Where(l => l.action == filter.action.Trim());

            if (!string.IsNullOrWhiteSpace(filter.search))
            {
                var term = filter.search.Trim().ToLowerInvariant();
                query = query.Where(l =>
                    l.admin_nickname.ToLower().Contains(term) ||
                    (l.target_email != null && l.target_email.ToLower().Contains(term)) ||
                    (l.details != null && l.details.ToLower().Contains(term)));
            }

            return await query
                .OrderByDescending(l => l.created_at)
                .Take(limit)
                .Select(l => new AdminAuditLogResponse
                {
                    id = l.id,
                    admin_user_id = l.admin_user_id,
                    admin_nickname = l.admin_nickname,
                    action = l.action,
                    target_user_id = l.target_user_id,
                    target_email = l.target_email,
                    details = l.details,
                    created_at = l.created_at,
                })
                .ToListAsync();
        }
    }
}
