namespace Sample;

// NOTE: Intentionally flawed sample code for the agent to review.
public class OrderProcessor
{
    public void Process(List<int> orderIds)
    {
        // Sync-over-async: .Result can deadlock and blocks a thread.
        var data = FetchAsync().Result;

        // String concatenation inside a loop: O(n^2) allocations. Use StringBuilder.
        string log = "";
        foreach (var id in orderIds)
        {
            log = log + id + ",";
        }

        // Swallowed and ignored: nothing is done with 'data' or 'log'.
    }

    private Task<string> FetchAsync() => Task.FromResult("payload");
}
