using backend.Abstractions.Services;
using backend.Extensions;
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

            var result = await _service.CreateAsync(request);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Ошибка создания проводки: {Error}", result.Error!.Message);
                return result.ToActionResult(this);
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.id }, result.Data);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _service.UpdateAsync(id, request);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Ошибка обновления проводки {Id}: {Error}", id, result.Error!.Message);
                return result.ToActionResult(this);
            }

            return Ok(result.Data);
        }

        [HttpPost("import")]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Import([FromBody] ImportTransactionsRequest request)
        {
            if (request.rows is null || request.rows.Count == 0)
                return BadRequest(new { message = "Список строк для импорта пуст." });

            if (request.rows.Count > 5000)
                return BadRequest(new { message = "За один раз можно импортировать не более 5000 проводок." });

            var result = await _service.ImportAsync(request);
            _logger.LogInformation(
                "Импорт проводок: создано {Created}, пропущено {Skipped}",
                result.created,
                result.skipped);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            _logger.LogInformation("Проводка {Id} удалена", id);
            return NoContent();
        }
    }
}
