using OrderApi.Application.Interfaces;
using OrderApi.DTO;

using Microsoft.AspNetCore.Mvc;
using OrderApi.Domain.Entities;
using OrderApi.Infrastructure.Persistence;

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
        var order = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
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
