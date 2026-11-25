// Starting point for WCF contract tests: raw SOAP in, normalized SOAP out.
// Run against the old .svc first (baseline), then against the CoreWCF endpoint.
//   WCF_BASE_URL=http://localhost:5101 dotnet test
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace ContractTests;

public class InventoryServiceContract
{
    private const string Ns = "http://legacyshop.local/inventory/2013";
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("WCF_BASE_URL") ?? "http://localhost:5101";
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(BaseUrl) };

    [Fact]
    public async Task GetStock_returns_the_same_shape()
    {
        var body = await Call("GetStock", $"<GetStock xmlns=\"{Ns}\"><productId>3</productId></GetStock>");
        var result = body.Descendants(XName.Get("GetStockResult", Ns)).Single();
        Assert.NotNull(result.Element(XName.Get("ProductId", Ns)));
        Assert.NotNull(result.Element(XName.Get("Available", Ns)));
        Assert.NotNull(result.Element(XName.Get("LowStock", Ns)));
    }

    [Fact]
    public async Task Reserve_with_zero_quantity_returns_the_same_fault()
    {
        var body = await Call("Reserve", $"<Reserve xmlns=\"{Ns}\"><productId>3</productId><quantity>0</quantity></Reserve>", expectFault: true);
        var detail = body.Descendants(XName.Get("InventoryFault", Ns)).Single();
        Assert.Equal("INVALID_QUANTITY", detail.Element(XName.Get("Code", Ns))!.Value);
        Assert.Equal("Invalid quantity", body.Descendants().First(e => e.Name.LocalName == "faultstring").Value);
    }

    [Fact]
    public async Task Wsdl_keeps_the_contract()
    {
        var wsdl = await Http.GetStringAsync("/InventoryService.svc?wsdl");
        Assert.Contains(Ns, wsdl);
        foreach (var op in new[] { "GetStock", "Reserve", "Release" })
            Assert.Matches(new Regex($"operation name=\"{op}\""), wsdl);
    }

    private static async Task<XDocument> Call(string operation, string payload, bool expectFault = false)
    {
        var envelope = $"<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body>{payload}</s:Body></s:Envelope>";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/InventoryService.svc")
        {
            Content = new StringContent(envelope, Encoding.UTF8, "text/xml")
        };
        request.Headers.Add("SOAPAction", $"\"{Ns}/IInventoryService/{operation}\"");
        using var response = await Http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectFault ? 500 : 200, (int)response.StatusCode);
        return XDocument.Parse(text);
    }
}
