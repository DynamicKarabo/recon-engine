using Microsoft.AspNetCore.Mvc;

namespace ReconciliationEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        throw new InvalidOperationException("Test exception for middleware testing");
    }

    [HttpPost]
    public IActionResult Post([FromBody] object request)
    {
        return Ok();
    }
}
