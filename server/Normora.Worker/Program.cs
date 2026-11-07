using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Normora.Api.Extensions;
using Normora.Modules.Documents.Application.Jobs;

// Dedicated Background Worker Host:
// Runs as a separate micro-process from the API host to isolate heavy CPU/IO background workloads:
// 1. Apache Tika document text extraction and OCR parsing.
// 2. High-dimensional vector embedding generation and pgvector indexing.
// 3. Nightly maintenance routines (purging soft-deleted documents past 30-day grace period).
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddConfigurationServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
// Enable Hangfire Server processing exclusively on the worker process:
builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment, enableHangfireServer: true);
builder.Services.AddApplicationServices();
builder.Services.AddNormoraTelemetry(builder.Environment, "Normora.Worker");

using var host = builder.Build();

// Recurring Maintenance Job Registration:
// Runs daily at 03:00 UTC using Hangfire cron expressions.
// Scans for documents soft-deleted more than 30 days ago, permanently purging MinIO objects and pgvector chunks in batches.
RecurringJob.AddOrUpdate<PurgeDeletedDocumentsJob>(
    "purge-deleted-documents",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily(3, 0));

await host.RunAsync();
