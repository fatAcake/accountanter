using backend.Models.DTOs;
using backend.Models.Results;

namespace backend.Abstractions.Services
{
    public interface IDashboardService
    {
        Task<ServiceResult<DashboardResponse>> GetDashboardAsync(DateTime startDate, DateTime endDate);
        Task<ServiceResult<DashboardKpiResponse>> GetKpiAsync(DateTime startDate, DateTime endDate);
    }
}
