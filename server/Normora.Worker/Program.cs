using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Normora.Infrastructure.Extensions;
using Normora.Infrastructure.BackgroundJobs;

// Dedicated Background Worker Host:
// Runs as a separate micro-process from the API host to isolate heavy CPU/IO background workloads:
// 1. Apache Tika document text extraction and OCR parsing.
// 2. High-dimensional vector embedding generation and pgvector indexing.
// 3. Nightly maintenance routines (purging soft-deleted documents past 30-day grace period).
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddConfigurationServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddNormoraSignalR(builder.Configuration);
// Enable Hangfire Server processing exclusively on the worker process:
builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment, enableHangfireServer: true);
builder.Services.AddExternalProviders(builder.Configuration, builder.Environment);
builder.Services.AddApplicationServices();
builder.Services.AddNormoraTelemetry(builder.Environment, "Normora.Worker");

using var host = builder.Build();

// Recurring Maintenance Job Registration:
// Runs daily at 03:00 UTC using Hangfire cron expressions.
// Scans for documents soft-deleted more than 30 days ago, permanently purging MinIO objects and pgvector chunks in batches.
// Uses IRecurringJobManager service directly to avoid static JobStorage.Current dependency.
var recurringJobs = host.Services.GetRequiredService<IRecurringJobManager>();

recurringJobs.AddOrUpdate<HangfirePurgeDeletedDocumentsJob>(
    "purge-deleted-documents",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily(3, 0));

recurringJobs.AddOrUpdate<HangfireDocumentProcessingOutboxDispatcher>(
    "dispatch-document-processing-outbox",
    job => job.DispatchPendingAsync(CancellationToken.None),
    Cron.Minutely());

await host.RunAsync();
