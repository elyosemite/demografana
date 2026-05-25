using Microsoft.EntityFrameworkCore;

sealed class GetOrdersUseCase(AppDbContext db)
{
    public async Task<List<Order>> ExecuteAsync() =>
        await db.Orders.ToListAsync();
}
