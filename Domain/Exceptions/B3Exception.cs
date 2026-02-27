namespace OrderApi.Domain.Exceptions;

public class B3Exception : DomainException
{
    public B3Exception() : base("B3_INTEGRATION_ERROR", "An error occurred while integrating with B3.")
    {
    }
}
