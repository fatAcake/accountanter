using backend.Abstractions.Services;
using backend.Extensions;
using backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _users;
        private readonly IAdminAuditService _audit;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService users,
            IAdminAuditService audit,
            ILogger<UsersController> logger)
        {
            _users = users;
            _audit = audit;
            _logger = logger;
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue("sub");
            return int.TryParse(sub, out userId);
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? search,
            [FromQuery] string? action,
            [FromQuery] int limit = 100)
        {
            return Ok(await _audit.GetLogsAsync(new AuditLogFilter
            {
                search = search,
                action = action,
                limit = limit,
            }));
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            return Ok(await _users.GetActiveSessionsAsync());
        }

        [HttpPost("import")]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> ImportCsv(IFormFile? file)
        {
            if (!TryGetCurrentUserId(out var adminUserId))
                return Unauthorized();

            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Загрузите CSV-файл." });

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) &&
                file.ContentType is not ("text/csv" or "application/vnd.ms-excel"))
            {
                return BadRequest(new { message = "Допустим только формат CSV." });
            }

            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();

            var result = await _users.ImportFromCsvAsync(content, adminUserId);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            _logger.LogDebug("GET users search={Search}", search);
            return Ok(await _users.GetAllAsync(new UserFilter { search = search }));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _users.GetByIdAsync(id);
            if (user is null)
                return NotFound(new { message = "Пользователь не найден." });

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!TryGetCurrentUserId(out var adminUserId))
                return Unauthorized();

            var result = await _users.CreateAsync(request, adminUserId);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.id }, result.Data);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var result = await _users.UpdateAsync(id, request, currentUserId);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            return Ok(result.Data);
        }

        [HttpDelete("{id:int}/session")]
        public async Task<IActionResult> RevokeSession(int id)
        {
            if (!TryGetCurrentUserId(out var adminUserId))
                return Unauthorized();

            var result = await _users.RevokeSessionAsync(id, adminUserId);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var result = await _users.DeleteAsync(id, currentUserId);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            _logger.LogInformation("Пользователь {Id} удалён", id);
            return NoContent();
        }
    }
}
