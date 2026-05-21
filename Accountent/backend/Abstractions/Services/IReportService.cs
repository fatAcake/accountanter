using backend.Models.DTOs;

namespace backend.Abstractions.Services
{
    public interface IReportService
    {
        Task<(OsvReportResponse? report, string? error)> GetOsvAsync(
            DateTime startDate,
            DateTime endDate,
            int? accountId = null);
    }
}
