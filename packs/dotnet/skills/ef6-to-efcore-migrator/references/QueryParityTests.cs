// Starting point: run the SAME query through EF6 (old) and EF Core (new) against the same test database
// and compare the results. One test per repository method.
// The test project targets net10.0 and references both data projects (EF 6.5 runs on net10.0).
using System.Text.Json;
using Xunit;

namespace QueryParityTests;

public class OrderQueriesParity
{
    private static readonly string Conn = Environment.GetEnvironmentVariable("PARITY_DB")
        ?? @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=LegacyShop_Parity;Integrated Security=True;TrustServerCertificate=True";

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(365)]
    public void SalesByDay_returns_the_same_rows(int days)
    {
        var old = new Ef6.OrderRepository(Conn).SalesByDay(days)
            .Select(r => new { Day = r.Day.Date, r.Orders, r.Revenue });
        var @new = new EfCore.OrderRepository(Conn).SalesByDay(days)
            .Select(r => new { Day = r.Day.Date, r.Orders, r.Revenue });

        Assert.Equal(Json(old), Json(@new));
    }

    [Fact]
    public void Customer_orders_include_lines_and_product_names()
    {
        // EF6 got lines + product names through lazy loading; EF Core must Include them.
        var old = new Ef6.OrderRepository(Conn).ForCustomer(2)
            .Select(o => new { o.Id, o.Total, Lines = o.Lines.Select(l => new { l.Product.Name, l.Quantity, l.UnitPrice }).OrderBy(l => l.Name) });
        var @new = new EfCore.OrderRepository(Conn).ForCustomer(2)
            .Select(o => new { o.Id, o.Total, Lines = o.Lines.Select(l => new { l.Product.Name, l.Quantity, l.UnitPrice }).OrderBy(l => l.Name) });

        Assert.Equal(Json(old), Json(@new));
    }

    private static string Json<T>(IEnumerable<T> rows) =>
        JsonSerializer.Serialize(rows.ToList(), new JsonSerializerOptions { WriteIndented = true });
}

// Ef6.* and EfCore.* are aliases for the two data-layer implementations, e.g. with extern alias
// or by giving the EF Core project a different root namespace during the move.
