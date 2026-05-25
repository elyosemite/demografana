using Demografana.Core.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var builder = Host.CreateApplicationBuilder(args);

var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://alloy:4317";

builder.Services.AddSerilog((services, cfg) => cfg
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.OpenTelemetry(o =>
    {
        o.Endpoint = endpoint;
        o.Protocol = OtlpProtocol.Grpc;
        o.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "demografana-relay",
            ["service.namespace"] = "local",
            ["service.version"] = "1.0.0"
        };
    }));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("demografana-relay", "local", "1.0.0"))
    .WithTracing(t => t
        .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint)));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.MessageTopology.SetEntityNameFormatter(new SimpleNameEntityFormatter());

        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
    });
});

builder.Services.AddHostedService<OutboxRelay>();

var host = builder.Build();
host.Run();
