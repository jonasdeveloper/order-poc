using OrderApi.Application.Interfaces;
using OrderApi.DTO;
using Serilog;
using Microsoft.AspNetCore.Mvc;

namespace OrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderRequestDTO request)
    {
        Log.Information("Requesting POST/orders with asset={Asset}", request.Asset);
        
        var token = Request.Headers.Authorization.ToString();
        Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey);

        Log.Information("Processing purchase order amount={Amount} idempotency_key={IdemKey}", request, idempotencyKey);
        
        try
        {
            var order = await _service.CreateAsync(request, idempotencyKey!, token);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _service.GetByIdAsync(id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }
}
