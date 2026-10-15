namespace JobRunner;

/// <summary>
/// A unit of background work.
///
/// LESSON — keep queued messages small and serializable.
/// A job should carry an ID and just enough data to do the work later. In a real
/// system this crosses a process/network boundary (e.g. Azure Service Bus), so
/// you pass identifiers and payloads, not live objects or open connections.
/// </summary>
public sealed record Job(int Id, string Name);
