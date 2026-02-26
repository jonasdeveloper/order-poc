namespace OrderApi.Domain.Exceptions;

public class FraudException : DomainException
{
    public FraudException() : base("ORDER_FRAUD_DETECTED", "Transaction flagged as fraud.")
    {
    }
}
