using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddObservability();

builder.Services.AddSingleton<OrderStore>();
builder.Services.AddScoped<PlaceOrderUseCase>();
builder.Services.AddScoped<GetOrdersUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/orders", (GetOrdersUseCase useCase) =>
    Results.Ok(useCase.Execute()))
    .WithName("GetOrders");

app.MapPost("/orders", async (PlaceOrderRequest request, PlaceOrderUseCase useCase) =>
{
    var order = await useCase.ExecuteAsync(request);
    return Results.Created($"/orders/{order.Id}", order);
}).WithName("PlaceOrder");

app.Run();
