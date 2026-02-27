namespace OrderApi.Application.Interfaces;

public interface IOutboxWriter
{
    Task EnqueueAsync(string type, object payload);
}
