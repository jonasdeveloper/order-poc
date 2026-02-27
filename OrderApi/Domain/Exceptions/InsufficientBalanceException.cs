namespace OrderApi.Domain.Exceptions;

public class InsufficientBalanceException : DomainException
{
    public InsufficientBalanceException()
        : base("BALANCE_INSUFFICIENT", "Insufficient balance.")
    {
    }
}
