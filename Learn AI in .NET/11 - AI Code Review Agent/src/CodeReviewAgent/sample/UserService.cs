using System.Data.SqlClient;

namespace Sample;

// NOTE: This file is intentionally flawed — it's the code the agent reviews.
// It is excluded from compilation (see the .csproj) and read only as text.
public class UserService
{
    public string? GetUser(int id)
    {
        // SQL injection: user input concatenated straight into the query.
        var sql = "SELECT * FROM Users WHERE Id = " + id;

        // New HttpClient per call -> socket exhaustion under load.
        var http = new HttpClient();

        try
        {
            return Query(sql);
        }
        catch (Exception)
        {
            // Swallowed exception: the error disappears with no logging.
        }

        // Returns null instead of a Result/empty -> NullReferenceException risk for callers.
        return null;
    }

    private string Query(string sql) => sql;
}
