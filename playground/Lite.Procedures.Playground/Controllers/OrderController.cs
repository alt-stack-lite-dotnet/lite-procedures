using Lite.Procedures;
using Microsoft.AspNetCore.Mvc;

namespace Lite.Procedures.Playground.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly IAsyncProcedure<CreateOrderRequest, CreateOrderResult> _pipeline;

    public OrderController(IAsyncProcedure<CreateOrderRequest, CreateOrderResult> pipeline)
    {
        _pipeline = pipeline;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await _pipeline.InvokeAsync(request, ct);
        return Ok(result);
    }
}
