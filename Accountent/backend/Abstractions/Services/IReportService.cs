using backend.Models.DTOs;
using backend.Models.Results;

namespace backend.Abstractions.Services
{
    public interface IReportService
    {
        Task<ServiceResult<OsvReportResponse>> GetOsvAsync(
            DateTime startDate,
            DateTime endDate,
            int? accountId = null);
    }
}
