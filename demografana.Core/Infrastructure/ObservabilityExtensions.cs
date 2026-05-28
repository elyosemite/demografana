using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

public static class ObservabilityExtensions
{
    public static IHostApplicationBuilder AddObservability(
        this IHostApplicationBuilder builder, string serviceName)
    {
        builder.Logging.ClearProviders();

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
                    ["service.name"]      = serviceName,
                    ["service.namespace"] = "local",
                    ["service.version"]   = "1.0.0",
                };
            }));

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName, "local", "1.0.0"))
            .WithTracing(t => t
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint)))
            .WithMetrics(m => m
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint)));

        return builder;
    }
}
