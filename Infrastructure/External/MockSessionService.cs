namespace OrderApi.Infrastructure.External;

public class MockSessionService : ISessionService
{
    public Task<Guid> GetUserIdAsync(string bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            throw new UnauthorizedAccessException("Missing token");
        return Task.FromResult(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }
}
