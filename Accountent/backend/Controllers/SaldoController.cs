using backend.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/saldo")]
    [Authorize]
    public class SaldoController : ControllerBase
    {
        private readonly IReportService _reports;
        private readonly ILogger<SaldoController> _logger;

        public SaldoController(IReportService reports, ILogger<SaldoController> logger)
        {
            _reports = reports;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> GetSaldo(
            [FromQuery] DateTime start_date,
            [FromQuery] DateTime end_date,
            [FromQuery] int? account_id = null)
        {
            _logger.LogDebug("GET saldo {Start} — {End}", start_date, end_date);

            var (report, error) = await _reports.GetOsvAsync(start_date, end_date, account_id);
            if (error is not null)
                return BadRequest(new { message = error });

            return Ok(report);
        }
    }
}
