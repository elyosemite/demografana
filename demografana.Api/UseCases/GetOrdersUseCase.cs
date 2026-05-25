using Microsoft.EntityFrameworkCore;

sealed class GetOrdersUseCase(AppDbContext db)
{
    public async Task<List<OrderProjection>> ExecuteAsync(CancellationToken ct = default) =>
        await db.Orders.ToListAsync(ct);
}
