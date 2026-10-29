using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Normora.Api.Extensions;

/// <summary>Registers application tracing and metrics with environment-specific exporters.</summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddNormoraTelemetry(
        this IServiceCollection services,
        IHostEnvironment environment,
        string serviceName = "Normora.Api")
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment.EnvironmentName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Filter out swagger/openapi requests
                        options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/swagger") &&
                                                !ctx.Request.Path.StartsWithSegments("/scalar") &&
                                                !ctx.Request.Path.StartsWithSegments("/openapi");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    // Add our custom activity source
                    .AddSource("Normora.Conversations");

                if (environment.IsDevelopment())
                {
                    tracing.AddConsoleExporter();
                }
                else
                {
                    tracing.AddOtlpExporter(); // Configured via OTEL_EXPORTER_OTLP_ENDPOINT
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    // Add our custom meter
                    .AddMeter("Normora.Conversations");

                if (environment.IsDevelopment())
                {
                    metrics.AddConsoleExporter();
                }
                else
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
