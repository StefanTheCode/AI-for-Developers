using System.Threading.Channels;

namespace JobRunner;

/// <summary>
/// An in-memory producer/consumer queue built on <see cref="Channel{T}"/>.
///
/// LESSON — Channels are the modern .NET in-process queue.
/// A <b>bounded</b> channel gives you back-pressure: if consumers fall behind and
/// the queue fills up, producers *await* instead of blowing up memory. One or more
/// producers write; one or more consumers read — all thread-safe, all async.
/// </summary>
public sealed class JobQueue
{
    private readonly Channel<Job> _channel;

    public JobQueue()
    {
        // Capacity 100; when full, writers wait (back-pressure) rather than drop.
        _channel = Channel.CreateBounded<Job>(new BoundedChannelOptions(capacity: 100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,   // one consumer (the JobProcessor)
            SingleWriter = false   // possibly many producers
        });
    }

    /// <summary>Adds a job. Awaits if the channel is full.</summary>
    public ValueTask EnqueueAsync(Job job, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(job, ct);

    /// <summary>Async stream of jobs; ends when the queue is completed and drained.</summary>
    public IAsyncEnumerable<Job> DequeueAllAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAllAsync(ct);

    /// <summary>Signals that no more jobs will be added, so consumers can finish.</summary>
    public void Complete() => _channel.Writer.Complete();
}
