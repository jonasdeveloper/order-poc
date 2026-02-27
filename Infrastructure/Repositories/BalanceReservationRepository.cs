using Microsoft.EntityFrameworkCore;
using OrderApi.Application.Interfaces;
using OrderApi.Domain.Entities;
using OrderApi.Infrastructure.Persistence;

namespace OrderApi.Infrastructure.Repositories;

public class BalanceReservationRepository : IBalanceReservationRepository
{
    private readonly OrderDbContext _context;
    public BalanceReservationRepository(OrderDbContext context) => _context = context;

    public Task AddAsync(BalanceReservation r) => _context.BalanceReservations.AddAsync(r).AsTask();

    public async Task<BalanceReservation?> GetByOrderIdForUpdateAsync(Guid orderId)
    {
        return await _context.BalanceReservations.FirstOrDefaultAsync(x => x.OrderId == orderId);
    }
}
