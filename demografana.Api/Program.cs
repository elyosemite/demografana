using MassTransit;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddObservability();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<EventStore>();
builder.Services.AddScoped<PlaceOrderUseCase>();
builder.Services.AddScoped<GetOrdersUseCase>();
builder.Services.AddScoped<ConfirmOrderUseCase>();
builder.Services.AddScoped<ShipOrderUseCase>();
builder.Services.AddScoped<DeliverOrderUseCase>();
builder.Services.AddScoped<CancelOrderUseCase>();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
    });
});

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

app.MapGet("/orders", async (GetOrdersUseCase uc, CancellationToken ct) =>
    Results.Ok(await uc.ExecuteAsync(ct)))
    .WithName("GetOrders");

app.MapPost("/orders", async (PlaceOrderRequest request, PlaceOrderUseCase uc, CancellationToken ct) =>
{
    var projection = await uc.ExecuteAsync(request, ct);
    return Results.Created($"/orders/{projection!.Id}", projection);
}).WithName("PlaceOrder");

app.MapPost("/orders/{id:guid}/confirm", async (Guid id, ConfirmOrderUseCase uc, CancellationToken ct) =>
{
    await uc.ExecuteAsync(id, ct);
    return Results.NoContent();
}).WithName("ConfirmOrder");

app.MapPost("/orders/{id:guid}/ship", async (Guid id, ShipOrderUseCase uc, CancellationToken ct) =>
{
    await uc.ExecuteAsync(id, ct);
    return Results.NoContent();
}).WithName("ShipOrder");

app.MapPost("/orders/{id:guid}/deliver", async (Guid id, DeliverOrderUseCase uc, CancellationToken ct) =>
{
    await uc.ExecuteAsync(id, ct);
    return Results.NoContent();
}).WithName("DeliverOrder");

app.MapPost("/orders/{id:guid}/cancel", async (Guid id, CancelOrderRequest request, CancelOrderUseCase uc, CancellationToken ct) =>
{
    await uc.ExecuteAsync(id, request.Reason, ct);
    return Results.NoContent();
}).WithName("CancelOrder");

app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    if (ex is InvalidOrderTransitionException transition)
    {
        ctx.Response.StatusCode = 409;
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = "InvalidTransition",
            from = transition.From.ToString(),
            to = transition.To.ToString()
        });
        return;
    }
    if (ex is OrderNotFoundException)
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        return;
    }
    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
}));

app.Run();
