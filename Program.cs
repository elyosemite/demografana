using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddObservability();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PlaceOrderUseCase>();
builder.Services.AddScoped<GetOrdersUseCase>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/orders", async (GetOrdersUseCase useCase) =>
    Results.Ok(await useCase.ExecuteAsync()))
    .WithName("GetOrders");

app.MapPost("/orders", async (PlaceOrderRequest request, PlaceOrderUseCase useCase) =>
{
    var order = await useCase.ExecuteAsync(request);
    return Results.Created($"/orders/{order.Id}", order);
}).WithName("PlaceOrder");

app.Run();
