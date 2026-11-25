// Starting point for the parity-test normalizer. Adapt the namespace; keep the rules.
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;

namespace ParityTests;

public static class Normalizer
{
    private static readonly string[] HiddenFields =
    [
        "__VIEWSTATE", "__VIEWSTATEGENERATOR", "__EVENTVALIDATION", "__EVENTTARGET",
        "__EVENTARGUMENT", "__LASTFOCUS", "__RequestVerificationToken"
    ];

    public static string Html(string html, string select, IEnumerable<string> ignore)
    {
        var doc = new HtmlParser().ParseDocument(html);
        foreach (var el in doc.QuerySelectorAll("script, style, noscript").ToList()) el.Remove();
        foreach (var name in HiddenFields)
            foreach (var el in doc.QuerySelectorAll($"input[name='{name}']").ToList()) el.Remove();
        foreach (var selector in ignore)
            foreach (var el in doc.QuerySelectorAll(selector).ToList()) el.Remove();

        var root = doc.QuerySelector(select) ?? doc.Body;
        if (root is null) return "";

        var sb = new StringBuilder();
        // Visible text in document order
        sb.AppendLine("TEXT: " + Collapse(root.TextContent));
        // Links and images (path + query only), in order
        foreach (var a in root.QuerySelectorAll("a[href]"))
            sb.AppendLine("LINK: " + PathOnly(a.GetAttribute("href")!) + " | " + Collapse(a.TextContent));
        foreach (var img in root.QuerySelectorAll("img[src]"))
            sb.AppendLine("IMG: " + PathOnly(img.GetAttribute("src")!));
        // Form fields a user can fill in (by label text or name without WebForms prefixes)
        foreach (var input in root.QuerySelectorAll("input:not([type=hidden]), select, textarea"))
            sb.AppendLine("FIELD: " + StripWebFormsPrefix(input.GetAttribute("name") ?? input.GetAttribute("id") ?? "?"));
        return sb.ToString().TrimEnd();
    }

    public static string Json(string json)
    {
        var node = JsonNode.Parse(json);
        return Canonical(node)?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null";
    }

    public static string Location(string? location, IReadOnlyDictionary<string, string> loginUrlMap)
    {
        if (string.IsNullOrEmpty(location)) return "";
        var path = PathOnly(location);
        foreach (var (oldUrl, newUrl) in loginUrlMap)
            path = path.Replace(newUrl, oldUrl, StringComparison.OrdinalIgnoreCase);
        return path;
    }

    private static JsonNode? Canonical(JsonNode? node) => node switch
    {
        JsonObject o => new JsonObject(o.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => KeyValuePair.Create(p.Key.ToLowerInvariant(), Canonical(p.Value?.DeepClone())))),
        JsonArray a => new JsonArray(a.Select(x => Canonical(x?.DeepClone())).ToArray()),
        JsonValue v when v.TryGetValue<decimal>(out var d) => JsonValue.Create(d / 1.0000000000000000000000000000m),
        _ => node?.DeepClone()
    };

    private static string PathOnly(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var abs)) return abs.PathAndQuery;
        return url.StartsWith("~/") ? url[1..] : url;
    }

    private static string StripWebFormsPrefix(string name) =>
        Regex.Replace(name, @"^(ctl\d+\$)?(\w+\$)*", "");

    private static string Collapse(string text) =>
        Regex.Replace(text.Replace(' ', ' '), @"\s+", " ").Trim();
}
