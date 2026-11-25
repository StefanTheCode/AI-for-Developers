#:property PublishAot=false
// render.cs - turns migration-assessment.json into a single, self-contained HTML report.
//
//   dotnet run render.cs -- <migration-assessment.json> [--out <file.html>]
//
// Recomputes all totals from the JSON (so edits to units / hours / targets are reflected),
// draws the diagrams with Mermaid (embedded, works offline) and writes migration-assessment.html
// next to the JSON. Needs the .NET 10 SDK. No NuGet packages, no network.

using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Usage: dotnet run render.cs -- <migration-assessment.json> [--out <file.html>]");
    return 1;
}

var jsonPath = Path.GetFullPath(args[0]);
var outIndex = Array.IndexOf(args, "--out");
var outPath = outIndex >= 0 && outIndex + 1 < args.Length ? Path.GetFullPath(args[outIndex + 1]) : Path.ChangeExtension(jsonPath, ".html");
var assets = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ThisFile())!, "..", "assets"));

var root = JsonNode.Parse(File.ReadAllText(jsonPath))!.AsObject();
var units = Arr(root["units"]);
var cross = Arr(root["crossCutting"]);
var blockers = Arr(root["blockers"]);
var projects = Arr(root["projects"]);
var risks = Arr(root["risks"]);
var questions = Arr(root["questions"]);
var nextSteps = Arr(root["nextSteps"]);
var model = root["effortModel"]?.AsObject() ?? new JsonObject();
var summary = root["summary"]?.AsObject() ?? new JsonObject();

double hoursPerDay = Num(model["hoursPerDay"], 6);
double review = Num(model["reviewOverhead"], 0.25);
var aiShare = new Dictionary<string, double> { ["mechanical"] = 0.9, ["translation"] = 0.7, ["verification"] = 0.8, ["decision"] = 0, ["environment"] = 0.3 };
if (model["aiShare"] is JsonObject shares)
    foreach (var kv in shares) aiShare[kv.Key] = Num(kv.Value, aiShare.GetValueOrDefault(kv.Key, 0.5));
double WithAi(string cat, double h) { var a = aiShare.GetValueOrDefault(cat, 0.5); return h * (1 - a) + h * a * review; }

// ---------- totals ----------
var items = units.Select(u => (Cat: Str(u["category"]), Lo: Num(u["hoursLow"]), Hi: Num(u["hoursHigh"]), Kind: Str(u["kind"]), Id: Str(u["id"])))
    .Concat(cross.Select(c => (Cat: Str(c["category"]), Lo: Num(c["hoursLow"]), Hi: Num(c["hoursHigh"]), Kind: "cross", Id: Str(c["id"]))))
    .ToList();
var catOrder = new[] { "decision", "mechanical", "translation", "verification", "environment" };
var byCat = catOrder.ToDictionary(c => c, c =>
{
    var rows = items.Where(i => i.Cat == c).ToList();
    return (Lo: rows.Sum(r => r.Lo), Hi: rows.Sum(r => r.Hi), LoAi: rows.Sum(r => WithAi(c, r.Lo)), HiAi: rows.Sum(r => WithAi(c, r.Hi)));
});
double lo = byCat.Values.Sum(v => v.Lo), hi = byCat.Values.Sum(v => v.Hi), loAi = byCat.Values.Sum(v => v.LoAi), hiAi = byCat.Values.Sum(v => v.HiAi);
var mid = (lo + hi) / 2;
var size = mid < 80 ? "S" : mid < 240 ? "M" : mid < 800 ? "L" : "XL";
string Days(double h) => (h / hoursPerDay).ToString("0");
var midAi = (loAi + hiAi) / 2;
// Share of the ORIGINAL work an agent can take over (midpoints)
var aiDoes = items.Sum(i => (i.Lo + i.Hi) / 2 * aiShare.GetValueOrDefault(i.Cat, 0.5));
var aiPct = mid > 0 ? aiDoes / mid * 100 : 0;
var hardCount = blockers.Count(b => Str(b["severity"]) == "hard");

// ---------- html ----------
var sb = new StringBuilder();
var solutionName = Str(root["solution"]?["name"], "Solution");
var verdict = Str(summary["verdict"], "pending");
var verdictText = verdict switch { "go" => "Go", "go-with-conditions" => "Go, with conditions", "not-now" => "Not now", _ => "Verdict pending review" };

sb.Append($"""
<header class="top">
  <div>
    <div class="eyebrow">Migration assessment &middot; .NET Framework &rarr; .NET 10</div>
    <h1>{E(solutionName)}</h1>
    <span class="verdict {E(verdict)}">{E(verdictText)}</span>
    <span class="muted small">&nbsp; Generated {E(Str(root["generatedAt"]))}</span>
  </div>
  <div class="toolbar"><button onclick="toggleTheme()">Light / dark</button> <button onclick="window.print()">Print / PDF</button></div>
</header>
""");
if (Str(summary["headline"]) != "") sb.Append($"<p style=\"font-size:18px;margin-top:14px\">{E(Str(summary["headline"]))}</p>");

sb.Append($"""
<div class="kpis">
  <div class="kpi"><div class="label">Size</div><div class="value">{size}</div><div class="sub">{units.Count} units of work, {projects.Count} projects</div></div>
  <div class="kpi"><div class="label">Without AI</div><div class="value">{Days(lo)}-{Days(hi)} days</div><div class="sub">{lo:0}-{hi:0} hours</div></div>
  <div class="kpi"><div class="label">With an AI agent</div><div class="value">{Days(loAi)}-{Days(hiAi)} days</div><div class="sub">{loAi:0}-{hiAi:0} hours, incl. review</div></div>
  <div class="kpi"><div class="label">AI can take over</div><div class="value">~{aiPct:0}%</div><div class="sub">of the hands-on work</div></div>
  <div class="kpi"><div class="label">Hard blockers</div><div class="value">{hardCount}</div><div class="sub">{blockers.Count} blocker signals in total</div></div>
</div>
""");

// Executive summary
sb.Append("<h2>Executive summary</h2><div class=\"card\">");
if (Str(summary["destination"]) != "") sb.Append($"<p><strong>Recommended path:</strong> {E(Str(summary["destination"]))}</p>");
if (Str(summary["verdictReason"]) != "") sb.Append($"<p>{E(Str(summary["verdictReason"]))}</p>");
else sb.Append("<p class=\"muted\">The summary hasn't been reviewed yet. Numbers below come straight from the scan.</p>");
var topRisks = risks.Take(5).ToList();
if (topRisks.Count > 0)
{
    sb.Append("<h3>Top risks</h3><ul class=\"clean\">");
    foreach (var r in topRisks) sb.Append($"<li><span class=\"chip {Sev(Str(r["impact"]))}\">{E(Str(r["impact"]))}</span> {E(Str(r["title"]))}</li>");
    sb.Append("</ul>");
}
sb.Append("</div>");

// Where the effort goes
sb.Append("<h2>Where the effort goes</h2><div class=\"two\"><div class=\"card\"><div class=\"bars\">");
var maxHi = Math.Max(1, byCat.Values.Max(v => v.Hi));
var catLabel = new Dictionary<string, string>
{
    ["decision"] = "Decisions", ["mechanical"] = "Mechanical changes", ["translation"] = "Translating code", ["verification"] = "Tests and proof", ["environment"] = "Hosting and rollout"
};
foreach (var c in catOrder)
{
    var v = byCat[c];
    if (v.Hi == 0) continue;
    var midNo = (v.Lo + v.Hi) / 2; var midWith = (v.LoAi + v.HiAi) / 2;
    sb.Append($"""
<div class="bar-row"><div class="head"><span>{catLabel[c]}</span><span class="muted">{v.Lo:0}-{v.Hi:0} h &rarr; {v.LoAi:0}-{v.HiAi:0} h with AI</span></div>
<div class="bar"><span class="noai" style="width:{midNo / maxHi * 100:0.0}%"></span><span class="ai" style="width:{midWith / maxHi * 100:0.0}%"></span></div></div>
""");
}
sb.Append("</div><div class=\"legend\"><span><i style=\"background:var(--muted);opacity:.45\"></i>without AI</span><span><i style=\"background:var(--accent)\"></i>with an AI agent</span></div></div>");

// AI vs human
sb.Append("<div class=\"card\"><h3 style=\"margin-top:0\">Who does what</h3>");
var aiVsHuman = new (string Cat, string AiDoes, string HumanDoes)[]
{
    ("mechanical", "Project files, packages, namespaces, config files, Program.cs wiring", "Reviews the diff"),
    ("translation", "Pages, controllers, endpoints, EF mappings, CoreWCF contracts - first full draft", "Reviews, fixes edge cases, UI polish"),
    ("verification", "Writes parity, characterization and contract tests", "Decides what \"the same behavior\" means, accepts differences"),
    ("decision", "Lays out options and trade-offs", "Decides: auth, session, hosting, UI targets, data"),
    ("environment", "Drafts pipelines, scripts and checklists", "Owns hosting, release, monitoring, sign-off"),
};
foreach (var row in aiVsHuman)
{
    var share = aiShare.GetValueOrDefault(row.Cat, 0) * 100;
    sb.Append($"""
<div style="margin:10px 0 14px"><div class="head" style="display:flex;justify-content:space-between"><strong>{catLabel[row.Cat]}</strong><span class="muted small">AI ~{share:0}% &middot; human ~{100 - share:0}%</span></div>
<div class="split"><div class="a" style="width:{share:0}%"></div><div class="h" style="width:{100 - share:0}%"></div></div>
<div class="small muted" style="margin-top:4px">AI: {E(row.AiDoes)}. Human: {E(row.HumanDoes)}.</div></div>
""");
}
sb.Append("<div class=\"legend\"><span><i style=\"background:var(--ai)\"></i>AI agent</span><span><i style=\"background:var(--human)\"></i>developer</span></div></div></div>");

// ---------- diagrams ----------
string Id(string s) => "n" + Regex.Replace(s, "[^A-Za-z0-9]", "");
string L(string s) => s.Replace("\"", "#quot;").Replace("<", "#lt;").Replace(">", "#gt;");
var kindLabel = new Dictionary<string, string> { ["web"] = "ASP.NET web app", ["wcf"] = "WCF service", ["library"] = "class library", ["desktop"] = "desktop app", ["console"] = "console app" };
var hasEf = units.Any(u => Str(u["kind"]) == "ef-context");
var hasWcf = units.Any(u => Str(u["kind"]) == "wcf-service");

// 1. current architecture
var cur = new StringBuilder("flowchart LR\n  browser([\"Browser / API clients\"])\n");
foreach (var p in projects)
{
    var name = Str(p["name"]); var kind = Str(p["kind"]);
    var fc = p["fileCounts"]?.AsObject();
    var detail = kind == "web" ? $"{Num(fc?["aspx"]):0} pages, {units.Count(u => Str(u["project"]) == name && Str(u["kind"]).Contains("controller"))} controllers" : $"{Num(p["loc"]):0} LOC";
    cur.Append($"  {Id(name)}[\"{L(name)}<br/><small>{L(kindLabel.GetValueOrDefault(kind, kind))} &middot; {L(Str(p["targetFramework"]))} &middot; {L(detail)}</small>\"]\n");
}
foreach (var p in projects.Where(p => Str(p["kind"]) is "web")) cur.Append($"  browser --> {Id(Str(p["name"]))}\n");
foreach (var p in projects) foreach (var r in Arr(p["projectReferences"])) cur.Append($"  {Id(Str(p["name"]))} --> {Id(r!.ToString())}\n");
if (hasEf)
{
    cur.Append("  db[(\"SQL database<br/><small>EF6</small>\")]\n");
    var efProject = Str(units.First(u => Str(u["kind"]) == "ef-context")["project"]);
    cur.Append($"  {Id(efProject)} --> db\n");
}
if (hasWcf && blockers.Any(b => Str(b["signal"]) == "WCF client config"))
{
    var wcfProject = Str(units.First(u => Str(u["kind"]) == "wcf-service")["project"]);
    foreach (var p in projects.Where(p => Str(p["kind"]) == "web" && Str(p["name"]) != wcfProject)) cur.Append($"  {Id(Str(p["name"]))} -. SOAP .-> {Id(wcfProject)}\n");
}
cur.Append("  classDef old fill:#eee,stroke:#999,color:#333;\n");

// 2. target architecture
var targets = units.Where(u => Str(u["kind"]) == "webforms-page").GroupBy(u => Str(u["target"])).ToDictionary(g => g.Key, g => g.Count());
var tgt = new StringBuilder("flowchart LR\n  browser([\"Browser / API clients\"])\n  subgraph host[\"ASP.NET Core on .NET 10\"]\n");
foreach (var t in targets) tgt.Append($"    {Id(t.Key)}[\"{L(t.Key)}<br/><small>{t.Value} pages</small>\"]\n");
var apiCount = units.Count(u => Str(u["kind"]) == "webapi-controller");
var mvcCount = units.Count(u => Str(u["kind"]) == "mvc-controller");
var endpointCount = units.Count(u => Str(u["kind"]) is "http-handler" or "http-module");
if (apiCount > 0) tgt.Append($"    api[\"API controllers<br/><small>{apiCount} from Web API 2</small>\"]\n");
if (mvcCount > 0) tgt.Append($"    mvc[\"MVC controllers + view components<br/><small>{mvcCount} from MVC 5</small>\"]\n");
if (endpointCount > 0) tgt.Append($"    mw[\"Middleware + endpoints<br/><small>{endpointCount} modules/handlers</small>\"]\n");
tgt.Append("  end\n");
if (hasWcf) tgt.Append("  corewcf[\"CoreWCF service<br/><small>same SOAP contract</small>\"]\n");
foreach (var lib in projects.Where(p => Str(p["kind"]) == "library")) tgt.Append($"  {Id(Str(lib["name"]))}[\"{L(Str(lib["name"]))}<br/><small>net10.0</small>\"]\n");
if (hasEf) tgt.Append("  db[(\"SQL database<br/><small>EF Core</small>\")]\n");
tgt.Append("  browser --> host\n");
foreach (var lib in projects.Where(p => Str(p["kind"]) == "library")) tgt.Append($"  host --> {Id(Str(lib["name"]))}\n");
if (hasWcf) tgt.Append("  host -. SOAP .-> corewcf\n");
if (hasEf) tgt.Append("  host --> db\n");

// 3. strangler fig
var webProject = projects.FirstOrDefault(p => Str(p["kind"]) == "web");
var str = new StringBuilder("flowchart LR\n  browser([\"Browser / API clients\"]) --> yarp\n  subgraph newapp[\"New .NET 10 app\"]\n    yarp{\"YARP<br/><small>route already migrated?</small>\"}\n    done[\"Migrated routes<br/><small>tested with parity tests</small>\"]\n  end\n");
str.Append($"  subgraph oldapp[\"Old app (.NET Framework)\"]\n    legacy[\"{L(webProject != null ? Str(webProject["name"]) : "Legacy app")}<br/><small>everything not migrated yet</small>\"]\n  end\n");
str.Append("  yarp -- yes --> done\n  yarp -- no / fallback --> legacy\n");
if (blockers.Any(b => Str(b["signal"]) is "Session state" or "Forms authentication"))
    str.Append("  done -. \"remote session + remote auth<br/>(System.Web adapters)\" .-> legacy\n");
str.Append("  style done fill:#e3f5ea,stroke:#2d7a52\n  style legacy fill:#f1f1f1,stroke:#999\n");

// 4. phases + gantt
var phaseDefs = new List<(string Name, Func<(string Cat, double Lo, double Hi, string Kind, string Id), bool> Match)>
{
    ("Decisions", i => i.Cat == "decision"),
    ("Safety net (tests first)", i => i.Cat == "verification"),
    ("Foundations - projects, packages, libraries", i => i.Cat == "mechanical"),
    ("New .NET 10 host + YARP", i => i.Id == "strangler-host"),
    ("Migrate slice by slice", i => i.Cat == "translation" && i.Kind != "cross"),
    ("Auth, session and cross-cutting code", i => i.Cat == "translation" && i.Kind == "cross"),
    ("Rollout and shut down the old app", i => i.Id is "rollout" or "decommission"),
};
var phases = phaseDefs.Select(p => (p.Name, Hours: items.Where(i => p.Match(i)).Sum(i => (WithAi(i.Cat, i.Lo) + WithAi(i.Cat, i.Hi)) / 2))).Where(p => p.Hours > 0).ToList();
var start = DateTime.Today;
while (start.DayOfWeek != DayOfWeek.Monday) start = start.AddDays(1);
var gantt = new StringBuilder($"gantt\n  dateFormat YYYY-MM-DD\n  axisFormat %d %b\n  excludes weekends\n  section With an AI agent (1 developer)\n");
for (var i = 0; i < phases.Count; i++)
{
    var d = Math.Max(1, (int)Math.Ceiling(phases[i].Hours / hoursPerDay));
    gantt.Append(i == 0 ? $"  {L(phases[i].Name)} :p{i}, {start:yyyy-MM-dd}, {d}d\n" : $"  {L(phases[i].Name)} :p{i}, after p{i - 1}, {d}d\n");
}

sb.Append("<h2>Architecture today and after the move</h2>");
Diagram(cur.ToString(), "Today: projects, references and what talks to what.");
Diagram(tgt.ToString(), "Target: one ASP.NET Core host on .NET 10. WebForms targets are chosen per page (see the table below).");
sb.Append("<h2>How the move works: strangler fig</h2><p>Nothing is rewritten in one go. A new .NET 10 app sits in front of the old one. Every request it can already handle stays in the new app; everything else falls through to the old app. One route at a time moves over - after parity tests prove the new route answers like the old one.</p>");
Diagram(str.ToString(), "Both apps run side by side until the last route has moved.");
sb.Append("<h2>Roadmap</h2>");
Diagram(gantt.ToString(), $"Calendar time with an AI agent and one developer (midpoint of the range, {hoursPerDay:0} focused hours a day). Decisions can run in parallel with the safety net.");

// ---------- WebForms targets ----------
var pages = units.Where(u => Str(u["kind"]) is "webforms-page" or "user-control" or "master-page").ToList();
if (pages.Count > 0)
{
    sb.Append("<h2>WebForms: target per page</h2><table><tr><th>Page</th><th>Size</th><th>Target</th><th>Why</th><th class=\"num\">Hours</th></tr>");
    foreach (var u in pages)
        sb.Append($"<tr><td><code>{E(Str(u["name"]))}</code></td><td><span class=\"chip size\">{E(Str(u["size"]))}</span></td><td><strong>{E(Str(u["target"]))}</strong></td><td class=\"small\">{E(Str(u["targetReason"]))}{Notes(u)}</td><td class=\"num\">{Num(u["hoursLow"]):0}-{Num(u["hoursHigh"]):0}</td></tr>");
    sb.Append("</table>");
}

// ---------- all units ----------
sb.Append("<h2>Every unit of work</h2><table><tr><th>Unit</th><th>Kind</th><th>Size</th><th>Target</th><th class=\"num\">Without AI</th><th class=\"num\">With AI</th></tr>");
foreach (var u in units.Where(u => !pages.Contains(u)))
{
    var cat = Str(u["category"]);
    sb.Append($"<tr><td><code>{E(Str(u["name"]))}</code><div class=\"small muted\">{E(Str(u["file"]))}</div>{Notes(u)}</td><td class=\"small\">{E(Str(u["kind"]))}</td><td><span class=\"chip size\">{E(Str(u["size"]))}</span></td><td class=\"small\">{E(Str(u["target"]))}</td><td class=\"num\">{Num(u["hoursLow"]):0}-{Num(u["hoursHigh"]):0} h</td><td class=\"num\">{WithAi(cat, Num(u["hoursLow"])):0}-{WithAi(cat, Num(u["hoursHigh"])):0} h</td></tr>");
}
foreach (var c in cross)
{
    var cat = Str(c["category"]);
    var tasks = Arr(c["humanTasks"]).Select(t => E(t!.ToString())).ToList();
    sb.Append($"<tr><td>{E(Str(c["name"]))}{(tasks.Count > 0 ? "<div class=\"small muted\">Human: " + string.Join("; ", tasks) + "</div>" : "")}</td><td class=\"small\">{E(catLabel.GetValueOrDefault(cat, cat))}</td><td></td><td></td><td class=\"num\">{Num(c["hoursLow"]):0}-{Num(c["hoursHigh"]):0} h</td><td class=\"num\">{WithAi(cat, Num(c["hoursLow"])):0}-{WithAi(cat, Num(c["hoursHigh"])):0} h</td></tr>");
}
sb.Append($"<tr><td><strong>Total</strong></td><td></td><td><span class=\"chip size\">{size}</span></td><td></td><td class=\"num\"><strong>{lo:0}-{hi:0} h</strong></td><td class=\"num\"><strong>{loAi:0}-{hiAi:0} h</strong></td></tr></table>");

// ---------- blockers ----------
sb.Append("<h2>Blockers found in the code</h2><table><tr><th>Severity</th><th>Signal</th><th class=\"num\">Hits</th><th>What it means / the fix</th><th>Where</th></tr>");
foreach (var b in blockers.OrderBy(b => Str(b["severity"]) switch { "hard" => 0, "medium" => 1, _ => 2 }))
{
    var files = Arr(b["files"]).Select(f => $"<code>{E(f!.ToString())}</code>").Take(4).ToList();
    sb.Append($"<tr><td><span class=\"chip {Sev(Str(b["severity"]))}\">{E(Str(b["severity"]))}</span></td><td><strong>{E(Str(b["signal"]))}</strong></td><td class=\"num\">{Num(b["count"]):0}</td><td class=\"small\">{E(Str(b["meaning"]))} <em>Fix:</em> {E(Str(b["fix"]))}</td><td class=\"small\">{string.Join("<br/>", files)}</td></tr>");
}
sb.Append("</table>");

// ---------- projects + packages ----------
sb.Append("<h2>Projects and packages</h2><table><tr><th>Project</th><th>Kind</th><th>Framework</th><th class=\"num\">LOC</th><th>Format</th><th>Packages</th></tr>");
foreach (var p in projects)
{
    var pk = Arr(p["packages"]).Select(x => $"<span title=\"{E(Str(x!["note"]))}\"><code>{E(Str(x["id"]))} {E(Str(x["version"]))}</code> <span class=\"chip {PkgChip(Str(x["status"]))}\">{E(Str(x["status"]))}</span></span>").ToList();
    sb.Append($"<tr><td><strong>{E(Str(p["name"]))}</strong></td><td class=\"small\">{E(Str(p["kind"]))}</td><td class=\"small\">{E(Str(p["targetFramework"]))}</td><td class=\"num\">{Num(p["loc"]):0}</td><td class=\"small\">{(Bool(p["sdkStyle"]) ? "SDK-style" : "old-style")}{(Bool(p["packagesConfig"]) ? ", packages.config" : "")}</td><td class=\"small\">{(pk.Count == 0 ? "-" : string.Join("<br/>", pk))}</td></tr>");
}
sb.Append("</table>");

// ---------- risks, questions, next steps ----------
if (risks.Count > 0)
{
    sb.Append("<h2>Risks and how to reduce them</h2><table><tr><th>Impact</th><th>Risk</th><th>Mitigation</th></tr>");
    foreach (var r in risks) sb.Append($"<tr><td><span class=\"chip {Sev(Str(r["impact"]))}\">{E(Str(r["impact"]))}</span></td><td>{E(Str(r["title"]))}</td><td class=\"small\">{E(Str(r["mitigation"]))}</td></tr>");
    sb.Append("</table>");
}
if (questions.Count > 0)
{
    sb.Append("<h2>Questions for the team</h2><div class=\"card\"><ul class=\"clean\">");
    foreach (var q in questions) sb.Append($"<li>{E(q!.ToString())}</li>");
    sb.Append("</ul></div>");
}
if (nextSteps.Count > 0)
{
    sb.Append("<h2>Next steps</h2><div class=\"card\"><ol class=\"clean\">");
    foreach (var s in nextSteps) sb.Append($"<li>{E(s!.ToString())}</li>");
    sb.Append("</ol></div>");
}

sb.Append("<h2>How these numbers were made</h2><div class=\"card small\"><ul class=\"clean\">");
foreach (var a in Arr(model["assumptions"])) sb.Append($"<li>{E(a!.ToString())}</li>");
sb.Append($"<li>AI share per kind of work: {string.Join(", ", catOrder.Select(c => $"{catLabel[c].ToLowerInvariant()} {aiShare.GetValueOrDefault(c, 0) * 100:0}%"))}.</li>");
sb.Append("<li>Counts (pages, controllers, operations, blockers) come from a deterministic scan of the source. Targets, risks and the verdict were reviewed by an AI agent and should be confirmed by the team.</li></ul></div>");

// ---------- write ----------
var template = File.ReadAllText(Path.Combine(assets, "report-template.html"));
var gzPath = Directory.GetFiles(assets, "mermaid-*.min.js.gz").OrderBy(f => f).LastOrDefault();
var mermaid = gzPath != null ? Convert.ToBase64String(File.ReadAllBytes(gzPath)) : "";
var footer = $"{E(Str(root["tool"], "Migration Pack"))} &middot; scan + review by an AI agent &middot; estimates are ranges, not quotes.";
var html = template
    .Replace("{{TITLE}}", E($"{solutionName} - migration assessment"))
    .Replace("{{BODY}}", sb.ToString())
    .Replace("{{FOOTER}}", footer)
    .Replace("{{MERMAID_GZ_BASE64}}", mermaid);
File.WriteAllText(outPath, html);
Console.WriteLine($"Wrote {outPath} ({new FileInfo(outPath).Length / 1024} KB). Size {size}, {Days(lo)}-{Days(hi)} days without AI, {Days(loAi)}-{Days(hiAi)} with AI, AI ~{aiPct:0}%.");
return 0;

// ================= helpers =================
void Diagram(string mermaidSource, string caption) =>
    sb.Append($"<div class=\"diagram\"><pre class=\"mermaid\">{E(mermaidSource)}</pre><div class=\"caption\">{E(caption)}</div></div>");

string Notes(JsonNode u)
{
    var notes = Arr(u["notes"]).Select(n => E(n!.ToString())).ToList();
    var human = Arr(u["humanTasks"]).Select(n => E(n!.ToString())).ToList();
    var html = "";
    if (notes.Count > 0) html += "<div class=\"small muted\">" + string.Join(" ", notes) + "</div>";
    if (human.Count > 0) html += "<div class=\"small\"><em>Human:</em> " + string.Join("; ", human) + "</div>";
    return html;
}

static List<JsonNode> Arr(JsonNode? n) => n is JsonArray a ? a.Where(x => x != null).Select(x => x!).ToList() : [];
static string Str(JsonNode? n, string fallback = "") => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : fallback;
static double Num(JsonNode? n, double fallback = 0) => n is JsonValue v ? (v.TryGetValue<double>(out var d) ? d : v.TryGetValue<int>(out var i) ? i : fallback) : fallback;
static bool Bool(JsonNode? n) => n is JsonValue v && v.TryGetValue<bool>(out var b) && b;
static string E(string s) => WebUtility.HtmlEncode(s);
static string Sev(string s) => s switch { "hard" or "high" => "hard", "medium" => "medium", _ => "low" };
static string PkgChip(string s) => s switch { "blocked" => "hard", "replace" or "bridge" or "check" => "medium", _ => "low" };
static string ThisFile([CallerFilePath] string path = "") => path;
