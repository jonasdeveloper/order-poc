namespace OrderApi.Domain.Exceptions;

public class BalanceException : Exception
{
    public BalanceException(string message) : base(message)
    {
    }
}
