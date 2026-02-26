public interface ISessionService
{
    Task<Guid> GetUserIdAsync(string bearerToken);
}
