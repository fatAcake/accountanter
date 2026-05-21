using backend.Abstractions.Services;
using backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/chart_of_accounts")]
    [Authorize]
    public class ChartOfAccountsController : ControllerBase
    {
        private readonly IChartOfAccountsService _service;
        private readonly ILogger<ChartOfAccountsController> _logger;

        public ChartOfAccountsController(
            IChartOfAccountsService service,
            ILogger<ChartOfAccountsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> GetAll([FromQuery] bool flat = false)
        {
            _logger.LogDebug("GET chart_of_accounts flat={Flat}", flat);
            return Ok(await _service.GetAllAsync(flat));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> GetById(int id)
        {
            var account = await _service.GetByIdAsync(id);
            if (account is null)
                return NotFound(new { message = "Счёт не найден." });

            return Ok(account);
        }

        [HttpPost]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var (account, error) = await _service.CreateAsync(request);
            if (error is not null)
            {
                _logger.LogWarning("Ошибка создания счёта: {Error}", error);
                return BadRequest(new { message = error });
            }

            return CreatedAtAction(nameof(GetById), new { id = account!.id }, account);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var (account, error) = await _service.UpdateAsync(id, request);
            if (error is not null)
            {
                if (error == "Счёт не найден.")
                    return NotFound(new { message = error });
                return BadRequest(new { message = error });
            }

            return Ok(account);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, error) = await _service.DeleteAsync(id);
            if (!success)
            {
                if (error == "Счёт не найден.")
                    return NotFound(new { message = error });
                return BadRequest(new { message = error });
            }

            _logger.LogInformation("Счёт {Id} удалён", id);
            return NoContent();
        }
    }
}
