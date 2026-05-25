using backend.Abstractions.Services;
using backend.Extensions;
using backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService auth, ILogger<AuthController> logger)
        {
            _auth = auth;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _auth.RegisterAsync(request);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Ошибка регистрации: {Error}", result.Error!.Message);
                return result.ToActionResult(this);
            }

            return Ok(result.Data);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _auth.LoginAsync(request);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Ошибка входа для {Email}", request.email);
                return result.ToActionResult(this);
            }

            return Ok(result.Data);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _auth.RefreshAsync(request);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            return Ok(result.Data);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(sub, out var userId))
                return Unauthorized();

            _logger.LogInformation("Выход пользователя {UserId}", userId);
            await _auth.LogoutAsync(userId);
            return Ok(new { message = "Выход выполнен." });
        }
    }
}
