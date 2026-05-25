using backend.Models.DTOs;

namespace backend.Abstractions.Services
{
    public interface IAdminAuditService
    {
        Task LogAsync(
            int adminUserId,
            string action,
            int? targetUserId = null,
            string? targetEmail = null,
            string? details = null);

        Task<List<AdminAuditLogResponse>> GetLogsAsync(AuditLogFilter filter);
    }
}
