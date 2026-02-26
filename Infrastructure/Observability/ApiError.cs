namespace OrderApi.Infrastructure.Observability;

public class ApiError
{
    public string ErrorCode { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string? TraceId { get; init; }
    public string? CorrelationId { get; init; }
}
