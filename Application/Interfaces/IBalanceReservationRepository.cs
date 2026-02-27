using OrderApi.Domain.Entities;

namespace OrderApi.Application.Interfaces;

public interface IBalanceReservationRepository
{
    Task AddAsync(BalanceReservation r);
    Task<BalanceReservation?> GetByOrderIdForUpdateAsync(Guid orderId);
}
