using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/saldo")]
    [Authorize]
    [Obsolete("Используйте GET /api/reports/OSV")]
    public class SaldoController : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "admin,accountant,observer")]
        public IActionResult GetSaldo()
        {
            return RedirectPermanent($"/api/reports/OSV{Request.QueryString}");
        }
    }
}
