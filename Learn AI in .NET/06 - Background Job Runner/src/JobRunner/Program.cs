using JobRunner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// =============================================================================
// Background Job Runner — Intermediate project #6
// Focus: Hosted services + System.Threading.Channels + retries.
//
// One producer enqueues jobs onto a Channel-backed queue; one hosted consumer
// processes them with retry + exponential backoff. The app exits once every job
// has been handled (or dead-lettered).
// =============================================================================

var builder = Host.CreateApplicationBuilder(args);

// The queue is shared state -> a singleton. Producer and consumer both get it.
builder.Services.AddSingleton<JobQueue>();

// Two hosted services running side by side: one produces, one consumes.
builder.Services.AddHostedService<DemoJobProducer>();
builder.Services.AddHostedService<JobProcessor>();

var host = builder.Build();
await host.RunAsync();
