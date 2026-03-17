using Lite.Procedures;
using Microsoft.AspNetCore.Mvc;

namespace Lite.Procedures.Playground.Controllers;

[ApiController]
[Route("[controller]")]
public class EchoController : ControllerBase
{
    private readonly IAsyncProcedure<string, string> _pipeline;

    public EchoController(IAsyncProcedure<string, string> pipeline)
    {
        _pipeline = pipeline;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, CancellationToken ct)
    {
        var result = await _pipeline.InvokeAsync(q ?? "", ct);
        return Ok(result);
    }
}
