using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobRunner;

/// <summary>
/// The consumer: a long-running <see cref="BackgroundService"/> that pulls jobs
/// off the queue and processes each one, retrying transient failures.
///
/// LESSON — a hosted service is your always-on worker.
/// <see cref="BackgroundService.ExecuteAsync"/> starts with the app and runs
/// until shutdown. The <paramref name="stoppingToken"/> is your cue to stop
/// gracefully when the host is shutting down — always honour it.
/// </summary>
public sealed class JobProcessor(
    JobQueue queue,
    IHostApplicationLifetime lifetime,
    ILogger<JobProcessor> logger) : BackgroundService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("JobProcessor started, waiting for jobs...");

        // Drains the channel until the producer completes it and it's empty.
        await foreach (var job in queue.DequeueAllAsync(stoppingToken))
        {
            await ProcessWithRetryAsync(job, stoppingToken);
        }

        logger.LogInformation("All jobs drained. Shutting down.");
        lifetime.StopApplication(); // clean exit once the demo work is done
    }

    /// <summary>
    /// Runs a job with up to <see cref="MaxAttempts"/> tries and exponential backoff.
    ///
    /// LESSON — retries need a limit and a delay.
    /// * A cap (3) stops a permanently-broken job from looping forever.
    /// * Exponential backoff (200ms, 400ms, ...) avoids hammering a struggling
    ///   downstream service. Real systems add "jitter" (randomness) too.
    /// * After the last failure the job goes to a "dead letter" log for a human.
    /// </summary>
    private async Task ProcessWithRetryAsync(Job job, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await DoWorkAsync(job, ct);
                logger.LogInformation("✅ Job {Id} '{Name}' succeeded on attempt {Attempt}",
                    job.Id, job.Name, attempt);
                return;
            }
            catch (OperationCanceledException)
            {
                throw; // shutdown requested — let it bubble up, don't "retry" a cancel
            }
            catch (Exception ex)
            {
                if (attempt == MaxAttempts)
                {
                    logger.LogError("💀 Job {Id} '{Name}' failed after {Max} attempts: {Message}. Dead-lettered.",
                        job.Id, job.Name, MaxAttempts, ex.Message);
                    return;
                }

                var delay = BaseDelay * Math.Pow(2, attempt - 1);
                logger.LogWarning("⚠️  Job {Id} attempt {Attempt} failed: {Message}. Retrying in {Delay}ms",
                    job.Id, attempt, ex.Message, delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>
    /// Simulated work: pretends to do I/O and fails ~40% of the time so you can
    /// watch the retry logic kick in. Replace this with a real handler.
    /// </summary>
    private static async Task DoWorkAsync(Job job, CancellationToken ct)
    {
        await Task.Delay(200, ct);

        if (Random.Shared.NextDouble() < 0.4)
            throw new InvalidOperationException("transient downstream error (simulated)");
    }
}
