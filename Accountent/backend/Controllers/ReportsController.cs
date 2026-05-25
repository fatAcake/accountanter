using backend.Abstractions.Services;
using backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reports;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportService reports, ILogger<ReportsController> logger)
        {
            _reports = reports;
            _logger = logger;
        }

        [HttpGet("OSV")]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> GetOsv(
            [FromQuery] DateTime start_date,
            [FromQuery] DateTime end_date,
            [FromQuery] int? account_id = null)
        {
            _logger.LogInformation("Запрос ОСВ {Start} — {End}", start_date, end_date);

            var result = await _reports.GetOsvAsync(start_date, end_date, account_id);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Ошибка ОСВ: {Error}", result.Error!.Message);
                return result.ToActionResult(this);
            }

            return Ok(result.Data);
        }
    }
}
