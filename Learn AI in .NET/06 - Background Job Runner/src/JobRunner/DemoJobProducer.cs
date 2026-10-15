using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobRunner;

/// <summary>
/// The producer: enqueues a batch of demo jobs, then completes the queue so the
/// processor knows no more work is coming and the app can exit cleanly.
///
/// In a real app the producer is usually an API endpoint, a timer, or a message
/// from a broker — anything that hands work off to be done "later".
/// </summary>
public sealed class DemoJobProducer(
    JobQueue queue,
    ILogger<DemoJobProducer> logger) : BackgroundService
{
    private const int JobCount = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var i = 1; i <= JobCount; i++)
        {
            var job = new Job(i, $"send-email-{i}");
            await queue.EnqueueAsync(job, stoppingToken);
            logger.LogInformation("📥 Enqueued job {Id} '{Name}'", job.Id, job.Name);

            await Task.Delay(100, stoppingToken);
        }

        // Tell the consumer the party's over — it will drain, then stop the app.
        queue.Complete();
        logger.LogInformation("Producer finished; queue completed.");
    }
}
