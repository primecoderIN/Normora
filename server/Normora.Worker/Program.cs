using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Normora.Api.Extensions;
using Normora.Modules.Documents.Application.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddConfigurationServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment, enableHangfireServer: true);
builder.Services.AddApplicationServices();
builder.Services.AddNormoraTelemetry(builder.Environment, "Normora.Worker");

using var host = builder.Build();

RecurringJob.AddOrUpdate<PurgeDeletedDocumentsJob>(
    "purge-deleted-documents",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily(3, 0));

await host.RunAsync();
