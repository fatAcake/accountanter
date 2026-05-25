using backend.Abstractions.Services;
using backend.Extensions;
using backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/counterparties")]
    [Authorize]
    public class CounterpartiesController : ControllerBase
    {
        private readonly ICounterpartyService _service;
        private readonly ILogger<CounterpartiesController> _logger;

        public CounterpartiesController(
            ICounterpartyService service,
            ILogger<CounterpartiesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            _logger.LogDebug("GET counterparties search={Search}", search);
            return Ok(await _service.GetAllAsync(new CounterpartyFilter { search = search }));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "admin,accountant,observer")]
        public async Task<IActionResult> GetById(int id)
        {
            var counterparty = await _service.GetByIdAsync(id);
            if (counterparty is null)
                return NotFound(new { message = "Контрагент не найден." });

            return Ok(counterparty);
        }

        [HttpPost]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Create([FromBody] CreateCounterpartyRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _service.CreateAsync(request);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.id }, result.Data);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCounterpartyRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _service.UpdateAsync(id, request);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            return Ok(result.Data);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin,accountant")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            _logger.LogInformation("Контрагент {Id} удалён", id);
            return NoContent();
        }
    }
}
