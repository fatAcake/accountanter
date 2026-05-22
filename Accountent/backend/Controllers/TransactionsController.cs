using backend.Abstractions.Services;
using backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _service;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            ITransactionService service,
            ILogger<TransactionsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? start_date,
            [FromQuery] DateTime? end_date,
            [FromQuery] int? account_id,
            [FromQuery] int? counterparty_id)
        {
            _logger.LogDebug("GET transactions");
            var filter = new TransactionFilter
            {
                start_date = start_date,
                end_date = end_date,
                account_id = account_id,
                counterparty_id = counterparty_id,
            };
            return Ok(await _service.GetAllAsync(filter));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> GetById(int id)
        {
            var transaction = await _service.GetByIdAsync(id);
            if (transaction is null)
                return NotFound(new { message = "Проводка не найдена." });

            return Ok(transaction);
        }

        [HttpPost]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var (transaction, error) = await _service.CreateAsync(request);
            if (error is not null)
            {
                _logger.LogWarning("Ошибка создания проводки: {Error}", error);
                return BadRequest(new { message = error });
            }

            return CreatedAtAction(nameof(GetById), new { id = transaction!.id }, transaction);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var (transaction, error) = await _service.UpdateAsync(id, request);
            if (error is not null)
            {
                if (error == "Проводка не найдена.")
                    return NotFound(new { message = error });
                _logger.LogWarning("Ошибка обновления проводки {Id}: {Error}", id, error);
                return BadRequest(new { message = error });
            }

            return Ok(transaction);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, error) = await _service.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = error });

            _logger.LogInformation("Проводка {Id} удалена", id);
            return NoContent();
        }
    }
}
