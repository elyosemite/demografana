using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.OpenTelemetry;

internal static class OpenTelemetryExtensions
{
    internal static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://alloy:4317";
        Console.WriteLine($"OTLP Endpoint: {endpoint}");
        SelfLog.Enable(msg => Console.Error.WriteLine($"[Serilog] {msg}"));

        builder.Host.UseSerilog((ctx, services, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithMachineName()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.OpenTelemetry(o =>
            {
                o.Endpoint = endpoint;
                o.Protocol = OtlpProtocol.Grpc;
                o.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"]      = "demografana-api",
                    ["service.namespace"] = "local",
                    ["service.version"]   = "1.0.0",
                };
            }));

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: "demografana-api",
                serviceNamespace: "local",
                serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint)));

        return builder;
    }
}
