using OrderApi.Application.Interfaces;
using OrderApi.Domain.Exceptions;
using Serilog;

namespace OrderApi.Infrastructure.External;

public class MockOrderProcessor : IOrderProcessor
{
    public async Task ProcessAsync(Guid orderId, CancellationToken ct)
    {
        Log.Information("Sending order {OrderId} to B3 (mock)", orderId);

        await Task.Delay(500, ct);

        if (Random.Shared.Next(0, 10) < 2)
            throw new B3Exception();
    }
}
