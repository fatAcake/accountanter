using backend.Abstractions.Services;
using backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboard;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboard, ILogger<DashboardController> logger)
        {
            _dashboard = dashboard;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> Get(
            [FromQuery] DateTime start_date,
            [FromQuery] DateTime end_date)
        {
            _logger.LogInformation("Дашборд {Start} — {End}", start_date, end_date);

            var result = await _dashboard.GetDashboardAsync(start_date, end_date);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Ошибка дашборда: {Error}", result.Error!.Message);
                return result.ToActionResult(this);
            }

            return Ok(result.Data);
        }

        [HttpGet("kpi")]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> GetKpi(
            [FromQuery] DateTime start_date,
            [FromQuery] DateTime end_date)
        {
            var result = await _dashboard.GetKpiAsync(start_date, end_date);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            return Ok(result.Data);
        }
    }
}
