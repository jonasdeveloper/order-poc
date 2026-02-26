namespace OrderApi.Domain.Exceptions;

public class FraudException : Exception
{
    public FraudException(string message) : base(message)
    {
    }
}
