#:property PublishAot=false
// scan.cs - deterministic inventory of a .NET Framework solution for the Migration Pack.
//
//   dotnet run scan.cs -- <path-to.sln-or-folder> [--out <folder>]
//
// Writes <out>/migration-assessment.json (default: <solution folder>/migration).
// Everything here is counted from the files - no AI. The agent reviews and enriches the JSON
// (targets, risks, questions, verdict), then render.cs turns it into the HTML report.
// Needs the .NET 10 SDK. No NuGet packages, no network.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

var argList = args.ToList();
if (argList.Count == 0 || argList[0] is "-h" or "--help")
{
    Console.WriteLine("Usage: dotnet run scan.cs -- <path-to.sln|.slnx|folder> [--out <folder>]");
    return 1;
}

var input = Path.GetFullPath(argList[0]);
string? outArg = null;
var outIndex = argList.IndexOf("--out");
if (outIndex >= 0 && outIndex + 1 < argList.Count) outArg = argList[outIndex + 1];

var solutionFile = File.Exists(input) ? input : FindSolution(input);
var solutionDir = solutionFile != null ? Path.GetDirectoryName(solutionFile)! : input;
var outDir = Path.GetFullPath(outArg ?? Path.Combine(solutionDir, "migration"));
Directory.CreateDirectory(outDir);

var projectFiles = solutionFile != null ? ReadSolution(solutionFile) : Directory.GetFiles(input, "*.csproj", SearchOption.AllDirectories).ToList();
projectFiles = projectFiles.Where(File.Exists).Distinct().OrderBy(p => p).ToList();

var report = new Assessment
{
    GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
    Solution = new SolutionInfo { Name = Path.GetFileNameWithoutExtension(solutionFile ?? input), Path = Path.GetFileName(solutionFile ?? input) },
};

var allCodeFiles = new List<(string Project, string File, string Text)>();

foreach (var projectFile in projectFiles)
{
    var project = ScanProject(projectFile);
    report.Projects.Add(project);
}

// ---------- units ----------
foreach (var project in report.Projects)
{
    var dir = Path.GetDirectoryName(project.FullPath)!;
    var files = SourceFiles(dir);
    foreach (var f in files.Where(f => f.EndsWith(".cs") || f.EndsWith(".config") || f.EndsWith(".aspx") || f.EndsWith(".ascx") || f.EndsWith(".master") || f.EndsWith(".cshtml") || f.EndsWith(".asax") || f.EndsWith(".svc") || f.EndsWith(".ashx")))
    {
        allCodeFiles.Add((project.Name, f, File.ReadAllText(f)));
    }

    // WebForms pages, user controls, master pages
    foreach (var markup in files.Where(f => f.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".master", StringComparison.OrdinalIgnoreCase)))
    {
        report.Units.Add(WebFormsUnit(project, dir, markup));
    }

    // .ashx handlers
    foreach (var h in files.Where(f => f.EndsWith(".ashx", StringComparison.OrdinalIgnoreCase)))
    {
        report.Units.Add(MakeUnit("http-handler", Rel(dir, h), project.Name, Rel(solutionDir, h), "S", "translation", 1, 3,
            "Minimal API endpoint or middleware", "One handler = one endpoint. Keep the URL so links and caches keep working."));
    }

    // Global.asax
    foreach (var g in files.Where(f => Path.GetFileName(f).Equals("Global.asax.cs", StringComparison.OrdinalIgnoreCase)))
    {
        var text = File.ReadAllText(g);
        var events = Regex.Matches(text, @"protected\s+void\s+(Application_\w+|Session_\w+)\s*\(").Select(m => m.Groups[1].Value).ToList();
        var u = MakeUnit("global-asax", "Global.asax", project.Name, Rel(solutionDir, g), events.Count > 4 ? "M" : "S", "translation", events.Count > 4 ? 3 : 2, events.Count > 4 ? 6 : 4,
            "Program.cs + middleware", "Lifecycle events move to Program.cs, middleware and hosted services.");
        u.Metrics["events"] = events.Count;
        u.Notes.Add("Events: " + string.Join(", ", events) + ".");
        if (events.Contains("Session_End")) u.Notes.Add("Session_End has no ASP.NET Core equivalent.");
        report.Units.Add(u);
    }

    foreach (var cs in files.Where(f => f.EndsWith(".cs") && !f.EndsWith(".designer.cs")))
    {
        var text = File.ReadAllText(cs);
        var rel = Rel(solutionDir, cs);

        // IHttpModule
        foreach (Match m in Regex.Matches(text, @"class\s+(\w+)\s*:\s*[^{]*\bIHttpModule\b"))
        {
            report.Units.Add(MakeUnit("http-module", m.Groups[1].Value, project.Name, rel, "S", "translation", 1, 3, "Middleware", "Module events become a middleware component."));
        }

        // Web API 2 controllers
        foreach (Match m in Regex.Matches(text, @"class\s+(\w+)\s*:\s*ApiController\b"))
        {
            var body = ClassBody(text, m.Index);
            var actions = Regex.Matches(body, @"public\s+(?:async\s+)?(?:IHttpActionResult|HttpResponseMessage|Task<[^>]+>|[\w<>\[\],\s]+?)\s+\w+\s*\([^)]*\)\s*\{").Count;
            var size = actions <= 3 ? "S" : actions <= 8 ? "M" : "L";
            var (lo, hi) = size == "S" ? (1, 3) : size == "M" ? (3, 6) : (8, 14);
            var u = MakeUnit("webapi-controller", m.Groups[1].Value, project.Name, rel, size, "translation", lo, hi, "ASP.NET Core controller", "Same routes and status codes; IHttpActionResult -> IActionResult.");
            u.Metrics["actions"] = actions;
            if (body.Contains("HttpContext.Current") || Regex.IsMatch(text, @"\bCurrentUser\.")) u.Notes.Add("Uses HttpContext.Current (directly or through a helper).");
            if (Regex.IsMatch(body, @"\[Authorize")) u.Notes.Add("[Authorize] with Forms auth returns 302 today - .NET 10 returns 401.");
            report.Units.Add(u);
        }

        // MVC controllers
        foreach (Match m in Regex.Matches(text, @"class\s+(\w+)\s*:\s*Controller\b"))
        {
            var body = ClassBody(text, m.Index);
            var actions = Regex.Matches(body, @"public\s+(?:async\s+)?(?:ActionResult|ViewResult|PartialViewResult|JsonResult|Task<ActionResult>)\s+\w+\s*\(").Count;
            var childActions = Regex.Matches(body, @"\[ChildActionOnly\]").Count;
            var size = actions <= 3 ? "S" : actions <= 8 ? "M" : "L";
            var (lo, hi) = size == "S" ? (2, 4) : size == "M" ? (4, 8) : (10, 16);
            var u = MakeUnit("mvc-controller", m.Groups[1].Value, project.Name, rel, size, "translation", lo + childActions, hi + childActions * 2, "ASP.NET Core MVC controller + views", "Views mostly carry over; HTML helpers -> tag helpers where it helps.");
            u.Metrics["actions"] = actions;
            u.Metrics["childActions"] = childActions;
            if (childActions > 0) u.Notes.Add("Child actions become view components.");
            if (body.Contains("[OutputCache")) u.Notes.Add("[OutputCache] -> ASP.NET Core output caching middleware.");
            report.Units.Add(u);
        }

        // WCF service contracts (server side = there is an implementing class in the same project)
        foreach (Match m in Regex.Matches(text, @"\[ServiceContract[^\]]*\]\s*public\s+interface\s+(\w+)"))
        {
            var name = m.Groups[1].Value;
            var body = ClassBody(text, m.Index);
            var ops = Regex.Matches(body, @"\[OperationContract").Count;
            var faults = Regex.Matches(body, @"\[FaultContract").Count;
            var isServer = files.Any(f => f.EndsWith(".svc", StringComparison.OrdinalIgnoreCase)) ||
                           files.Where(f => f.EndsWith(".cs")).Any(f => Regex.IsMatch(File.ReadAllText(f), @"class\s+\w+\s*:\s*[^{]*\b" + name + @"\b"));
            if (!isServer) continue;
            var size = ops <= 5 ? "S" : ops <= 15 ? "M" : "L";
            var u = MakeUnit("wcf-service", name, project.Name, rel, size, "translation", 2 + ops * 0.5, 4 + ops * 1.5, "CoreWCF (same contract)",
                "CoreWCF keeps the SOAP contract, so existing clients keep working. gRPC/REST only if you also own every client.");
            u.Metrics["operations"] = ops;
            u.Metrics["faultContracts"] = faults;
            report.Units.Add(u);
        }

        // EF6 DbContext
        foreach (Match m in Regex.Matches(text, @"class\s+(\w+)\s*:\s*DbContext\b"))
        {
            var isEf6 = text.Contains("using System.Data.Entity");
            var dbsets = Regex.Matches(text, @"DbSet<\w+>").Count;
            var ef6Only = CountAcross(files, @"DbFunctions\.|SqlFunctions\.|DbModelBuilder|HasRequired\(|WillCascadeOnDelete|\.Include\(""");
            var size = dbsets <= 10 ? "S" : dbsets <= 40 ? "M" : "L";
            var u = MakeUnit("ef-context", m.Groups[1].Value, project.Name, rel, size, "translation",
                2 + dbsets * 0.5 + ef6Only * 0.25, 4 + dbsets * 1.25 + ef6Only * 0.75,
                isEf6 ? "EF 6.5 on .NET 10 first, then EF Core" : "EF Core",
                "EF 6.5 runs on .NET 10, so the data layer can move without a rewrite. Port to EF Core per context, behind query parity tests.");
            u.Metrics["dbSets"] = dbsets;
            u.Metrics["ef6OnlyApiHits"] = ef6Only;
            if (text.Contains("LazyLoadingEnabled = true") || CountAcross(files, @"public\s+virtual\s+(ICollection|\w+)\s") > 0) u.Notes.Add("Lazy loading via virtual navigations - EF Core needs Include or proxies.");
            if (CountAcross(files, @"SetInitializer|DropCreateDatabase|CreateDatabaseIfNotExists") > 0 || allCodeFiles.Any(f => f.Text.Contains("SetInitializer"))) u.Notes.Add("Database initializer + Seed - no EF Core equivalent.");
            report.Units.Add(u);
        }
    }

    // Libraries: non-web projects get a "port library" unit sized by LOC
    if (project.Kind is "library")
    {
        var size = project.Loc < 500 ? "S" : project.Loc < 2000 ? "M" : "L";
        var (lo, hi) = size == "S" ? (2, 4) : size == "M" ? (6, 12) : (16, 40);
        var u = MakeUnit("library", project.Name, project.Name, Rel(solutionDir, project.FullPath), size, "mechanical", lo, hi, "netstandard2.0 or net10.0 (multi-target during the move)",
            "Move libraries first (bottom-up), so both the old and the new app can use them.");
        u.Metrics["loc"] = project.Loc;
        if (project.UsesSystemWeb) u.Notes.Add("References System.Web - needs Microsoft.AspNetCore.SystemWebAdapters or an abstraction.");
        report.Units.Add(u);
    }
}

// ---------- blockers ----------
var signals = new (string Signal, string Pattern, string Severity, string Meaning, string Fix, string[] Ext)[]
{
    ("HttpContext.Current", @"HttpContext\.Current\b", "hard", "Static access to the request - not available in ASP.NET Core.", "IHttpContextAccessor / pass the user in; System.Web adapters while both apps run.", [".cs"]),
    ("System.Web in a library", @"^using\s+System\.Web\b", "hard", "Class library compiled against System.Web.", "Microsoft.AspNetCore.SystemWebAdapters, then remove.", [".cs"]),
    ("BinaryFormatter", @"\bBinaryFormatter\b", "hard", "Removed in .NET 9+ and insecure.", "System.Text.Json / MessagePack / protobuf.", [".cs"]),
    (".NET Remoting", @"System\.Runtime\.Remoting", "hard", "Not supported on .NET Core / .NET 5+.", "gRPC or HTTP APIs.", [".cs"]),
    ("AppDomain.CreateDomain", @"AppDomain\.CreateDomain", "hard", "Secondary AppDomains are not supported.", "AssemblyLoadContext or separate processes.", [".cs"]),
    ("WCF service (server)", @"\[ServiceContract", "hard", "No WCF server in .NET 10.", "CoreWCF (same contract) or gRPC/REST.", [".cs"]),
    ("ViewState", @"ViewState\[", "medium", "WebForms page state.", "Explicit state: hidden fields, query string, component state.", [".cs"]),
    ("UpdatePanel", @"<asp:UpdatePanel", "medium", "Partial postbacks.", "Blazor interactivity or a small fetch + partial.", [".aspx", ".ascx", ".master"]),
    ("Session state", @"\bSession\[|\.Session\b", "medium", "InProc session with objects.", "ASP.NET Core session (serialized) or a store; remote session while both apps run.", [".cs"]),
    ("Application state", @"\bApplication\[", "low", "Global mutable state.", "Singleton service or cache.", [".cs"]),
    ("Forms authentication", @"FormsAuthentication|<authentication\s+mode=""Forms""", "medium", "Forms auth cookie and ticket.", "ASP.NET Core cookie auth / Identity; remote authentication during the move.", [".cs", ".config"]),
    ("Membership / Roles API", @"\bMembership\.|\bRoles\.", "medium", "ASP.NET Membership providers.", "ASP.NET Core Identity.", [".cs"]),
    ("machineKey", @"<machineKey", "medium", "Shared keys for cookies and ViewState.", "Data Protection; shared key ring if cookies must work in both apps.", [".config"]),
    ("IHttpModule", @"\bIHttpModule\b", "medium", "HTTP module in the IIS pipeline.", "Middleware.", [".cs"]),
    ("IHttpHandler / .ashx", @"\bIHttpHandler\b", "medium", "Custom HTTP handler.", "Endpoint / middleware.", [".cs"]),
    ("System.Drawing", @"^using\s+System\.Drawing\b", "medium", "GDI+ - Windows-only on .NET 6+.", "ImageSharp / SkiaSharp, or stay on Windows hosting.", [".cs"]),
    ("ConfigurationManager", @"\bConfigurationManager\.", "medium", "Reads Web.config / App.config.", "IConfiguration + IOptions.", [".cs"]),
    ("WCF client config", @"<client>|ChannelFactory<|ClientBase<", "medium", "WCF client built from system.serviceModel config.", "System.ServiceModel.* NuGet packages; endpoint and binding in code.", [".cs", ".config"]),
    ("Global.asax", @"class\s+\w+\s*:\s*(System\.Web\.)?HttpApplication\b", "medium", "Application lifecycle events.", "Program.cs, middleware, hosted services.", [".cs"]),
    ("Bundling (System.Web.Optimization)", @"System\.Web\.Optimization|BundleTable|ScriptBundle|StyleBundle", "low", "Runtime bundling and minification.", "Static files + a build-time bundler, or plain tags.", [".cs", ".master", ".cshtml", ".config"]),
    ("Web.config transforms", @"xdt:Transform", "low", "Per-environment config via XDT.", "appsettings.{Environment}.json + environment variables.", [".config"]),
    ("MVC child actions", @"\[ChildActionOnly\]|Html\.Action\(|Html\.RenderAction\(", "low", "Child actions.", "View components.", [".cs", ".cshtml"]),
    ("OutputCache attribute", @"\[OutputCache", "low", "MVC 5 output caching.", "Output caching middleware.", [".cs"]),
    ("Newtonsoft.Json formatter", @"JsonFormatter\.SerializerSettings|CamelCasePropertyNamesContractResolver", "low", "Web API 2 JSON settings (and XML negotiation).", "System.Text.Json options (or AddNewtonsoftJson).", [".cs"]),
    ("EF6-only APIs", @"DbFunctions\.|SqlFunctions\.|DbModelBuilder|HasRequired\(|WillCascadeOnDelete", "medium", "APIs that don't exist in EF Core.", "EF Core equivalents (HasOne, .Date, ...).", [".cs"]),
    ("EF6 database initializer", @"SetInitializer|DropCreateDatabase|CreateDatabaseIfNotExists<", "low", "Initializers + Seed.", "EF Core migrations + seeding.", [".cs"]),
};

foreach (var s in signals)
{
    var hits = allCodeFiles
        .Where(f => s.Ext.Any(e => f.File.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
        .Where(f => s.Signal != "System.Web in a library" || report.Projects.First(p => p.Name == f.Project).Kind == "library")
        .Where(f => s.Signal != "WCF service (server)" || f.Project == report.Units.FirstOrDefault(u => u.Kind == "wcf-service")?.Project)
        .Select(f => (f.File, Count: Regex.Matches(f.Text, s.Pattern, RegexOptions.Multiline).Count))
        .Where(x => x.Count > 0)
        .ToList();
    if (hits.Count == 0) continue;
    report.Blockers.Add(new Blocker
    {
        Signal = s.Signal,
        Severity = s.Severity,
        Count = hits.Sum(h => h.Count),
        Files = hits.Select(h => Rel(solutionDir, h.File)).Take(12).ToList(),
        Meaning = s.Meaning,
        Fix = s.Fix
    });
}

// packages.config + old csproj as blockers too
var oldProjects = report.Projects.Where(p => !p.SdkStyle).ToList();
if (oldProjects.Count > 0)
    report.Blockers.Add(new Blocker { Signal = "Old-style .csproj", Severity = "low", Count = oldProjects.Count, Files = oldProjects.Select(p => p.Path).ToList(), Meaning = "Non-SDK project format.", Fix = "Convert to SDK-style (legacy-package-migrator / upgrade tooling)." });
var pc = report.Projects.Where(p => p.PackagesConfig).ToList();
if (pc.Count > 0)
    report.Blockers.Add(new Blocker { Signal = "packages.config", Severity = "low", Count = pc.Count, Files = pc.Select(p => p.Path).ToList(), Meaning = "Old NuGet management.", Fix = "PackageReference + Central Package Management (legacy-package-migrator)." });

// ---------- cross-cutting work ----------
var totalLoc = report.Projects.Sum(p => p.Loc);
report.EffortModel.FixedCostScale = totalLoc < 3000 ? 0.5 : totalLoc < 30000 ? 1.0 : 1.5;
report.EffortModel.TotalLoc = totalLoc;
var k = report.EffortModel.FixedCostScale;
bool Has(string signal) => report.Blockers.Any(b => b.Signal == signal);
var webProjects = report.Projects.Count(p => p.Kind is "web" or "wcf");
report.CrossCutting.Add(Cross("safety-net", "Safety net: parity + characterization tests", "verification",
    report.Units.Count(u => u.Kind is "webforms-page" or "webapi-controller" or "mvc-controller" or "http-handler") * 1.0 + report.Units.Count(u => u.Kind == "library") * 4,
    report.Units.Count(u => u.Kind is "webforms-page" or "webapi-controller" or "mvc-controller" or "http-handler") * 2.5 + report.Units.Count(u => u.Kind == "library") * 10,
    ["Decide what \"the same behavior\" means (which differences are OK)", "Review the recorded requests and the normalization rules"]));
if (webProjects > 0)
    report.CrossCutting.Add(Cross("strangler-host", "New .NET 10 host with YARP + System.Web adapters", "environment", 16 * k, 32 * k,
        ["Hosting decision (IIS, containers, cloud)", "Routing and TLS in front of both apps"]));
if (Has("Forms authentication") || Has("Membership / Roles API"))
{
    report.CrossCutting.Add(Cross("auth-decision", "Authentication & authorization design", "decision", 8 * k, 16 * k, ["Cookie auth vs Identity vs external IdP", "Shared sign-in while both apps run (remote auth)", "Role and policy model"]));
    report.CrossCutting.Add(Cross("auth-build", "Authentication & authorization implementation", "translation", 16 * k, 32 * k, ["Test sign-in, sign-out and expiry in both apps"]));
}
if (Has("Session state"))
{
    report.CrossCutting.Add(Cross("session-decision", "Session state strategy", "decision", 4 * k, 8 * k, ["Keep session (remote/distributed) or remove it", "What lives in session today and why"]));
    report.CrossCutting.Add(Cross("session-build", "Session state implementation", "translation", 8 * k, 16 * k, []));
}
if (Has("ConfigurationManager") || Has("Web.config transforms"))
    report.CrossCutting.Add(Cross("config", "Configuration: web.config -> appsettings + Options", "mechanical", 4 * k, 10 * k, ["Where secrets live in production"]));
if (Has("machineKey"))
    report.CrossCutting.Add(Cross("data-protection", "machineKey -> Data Protection key ring", "decision", 2, 6, ["Key storage and rotation"]));
if (Has("BinaryFormatter"))
    report.CrossCutting.Add(Cross("binaryformatter", "Replace BinaryFormatter", "translation", 2 * Count("BinaryFormatter"), 6 * Count("BinaryFormatter"), ["Check that existing serialized data can be read or safely dropped"]));
if (Has("System.Drawing"))
{
    report.CrossCutting.Add(Cross("drawing-decision", "Image processing without System.Drawing", "decision", 2, 4, ["Windows hosting vs ImageSharp/SkiaSharp (licenses!)"]));
    report.CrossCutting.Add(Cross("drawing-build", "Port image code", "translation", 4, 8, []));
}
if (Has("WCF client config"))
    report.CrossCutting.Add(Cross("wcf-clients", "WCF clients: config -> code", "translation", 2, 4, []));
if (Has("Bundling (System.Web.Optimization)"))
    report.CrossCutting.Add(Cross("bundling", "Static assets and bundling", "mechanical", 2, 4, []));
if (oldProjects.Count > 0 || pc.Count > 0)
    report.CrossCutting.Add(Cross("packaging", "SDK-style projects + PackageReference + CPM", "mechanical", 1.0 * Math.Max(oldProjects.Count, pc.Count), 2.0 * Math.Max(oldProjects.Count, pc.Count), []));
report.CrossCutting.Add(Cross("rollout", "Deployment, CI/CD and rollout", "environment", 16 * k, 40 * k, ["Release plan, monitoring, rollback", "Who signs off each switched route"]));
report.CrossCutting.Add(Cross("decommission", "Remove YARP fallback + adapters, shut down the old app", "environment", 4 * k, 8 * k, ["Final go/no-go"]));

int Count(string signal) => report.Blockers.FirstOrDefault(b => b.Signal == signal)?.Files.Count ?? 0;

report.Totals = Totals.From(report);

var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
});
var outFile = Path.Combine(outDir, "migration-assessment.json");
File.WriteAllText(outFile, json);

Console.WriteLine($"Scanned {report.Projects.Count} projects, {report.Units.Count} units, {report.Blockers.Count} blocker signals.");
foreach (var g in report.Units.GroupBy(u => u.Kind).OrderBy(g => g.Key)) Console.WriteLine($"  {g.Key,-18} {g.Count()}");
Console.WriteLine($"Estimate without AI: {report.Totals.HoursLowWithoutAi:0}-{report.Totals.HoursHighWithoutAi:0} h, with AI: {report.Totals.HoursLowWithAi:0}-{report.Totals.HoursHighWithAi:0} h ({report.Totals.Size})");
Console.WriteLine($"Wrote {outFile}");
return 0;

// ================= helpers =================

static string? FindSolution(string folder)
{
    var candidates = Directory.GetFiles(folder, "*.sln").Concat(Directory.GetFiles(folder, "*.slnx")).ToList();
    return candidates.Count == 1 ? candidates[0] : candidates.FirstOrDefault();
}

static List<string> ReadSolution(string sln)
{
    var dir = Path.GetDirectoryName(sln)!;
    if (sln.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
    {
        return XDocument.Load(sln).Descendants("Project").Select(p => (string?)p.Attribute("Path")).Where(p => p != null && p.EndsWith("proj"))
            .Select(p => Path.GetFullPath(Path.Combine(dir, p!.Replace('\\', Path.DirectorySeparatorChar)))).ToList();
    }
    return Regex.Matches(File.ReadAllText(sln), @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""")
        .Select(m => Path.GetFullPath(Path.Combine(dir, m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar)))).ToList();
}

static List<string> SourceFiles(string dir) =>
    Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
        .Where(f => !Regex.IsMatch(f.Replace('\\', '/'), @"/(bin|obj|packages|node_modules|\.vs|migration)/"))
        .ToList();

string Rel(string baseDir, string path) => Path.GetRelativePath(baseDir, path).Replace('\\', '/');

ProjectInfo ScanProject(string projectFile)
{
    var dir = Path.GetDirectoryName(projectFile)!;
    var xml = XDocument.Load(projectFile);
    var ns = xml.Root!.Name.Namespace;
    var sdk = xml.Root.Attribute("Sdk") != null;
    string Val(string name) => xml.Descendants(ns + name).Select(e => e.Value.Trim()).FirstOrDefault(v => v.Length > 0) ?? "";
    var tfm = Val("TargetFramework");
    if (tfm == "") tfm = Val("TargetFrameworks");
    if (tfm == "") tfm = Val("TargetFrameworkVersion");
    var typeGuids = Val("ProjectTypeGuids").ToLowerInvariant();
    var files = SourceFiles(dir);
    var csFiles = files.Where(f => f.EndsWith(".cs")).ToList();
    var loc = csFiles.Where(f => !f.EndsWith(".designer.cs")).Sum(f => File.ReadAllLines(f).Count(l => l.Trim().Length > 0 && !l.Trim().StartsWith("//")));

    var info = new ProjectInfo
    {
        Name = Path.GetFileNameWithoutExtension(projectFile),
        Path = Rel(solutionDir, projectFile),
        FullPath = projectFile,
        SdkStyle = sdk,
        TargetFramework = tfm,
        Loc = loc,
        PackagesConfig = File.Exists(Path.Combine(dir, "packages.config")),
        ProjectReferences = xml.Descendants(ns + "ProjectReference").Select(e => Path.GetFileNameWithoutExtension(((string?)e.Attribute("Include") ?? "").Replace('\\', '/'))).ToList(),
        UsesSystemWeb = xml.Descendants(ns + "Reference").Any(e => ((string?)e.Attribute("Include") ?? "").StartsWith("System.Web")) || csFiles.Any(f => Regex.IsMatch(File.ReadAllText(f), @"^using\s+System\.Web\b", RegexOptions.Multiline)),
    };

    info.FileCounts["aspx"] = files.Count(f => f.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase));
    info.FileCounts["ascx"] = files.Count(f => f.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase));
    info.FileCounts["master"] = files.Count(f => f.EndsWith(".master", StringComparison.OrdinalIgnoreCase));
    info.FileCounts["ashx"] = files.Count(f => f.EndsWith(".ashx", StringComparison.OrdinalIgnoreCase));
    info.FileCounts["svc"] = files.Count(f => f.EndsWith(".svc", StringComparison.OrdinalIgnoreCase));
    info.FileCounts["cshtml"] = files.Count(f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
    info.FileCounts["edmx"] = files.Count(f => f.EndsWith(".edmx", StringComparison.OrdinalIgnoreCase));
    info.FileCounts["cs"] = csFiles.Count;

    var isWeb = typeGuids.Contains("349c5851-65df-11da-9384-00065b846f21") || files.Any(f => Path.GetFileName(f).Equals("Web.config", StringComparison.OrdinalIgnoreCase) && Path.GetDirectoryName(f) == dir);
    info.Kind = info.FileCounts["svc"] > 0 && info.FileCounts["aspx"] == 0 ? "wcf"
              : isWeb ? "web"
              : Val("OutputType").Equals("WinExe", StringComparison.OrdinalIgnoreCase) ? "desktop"
              : Val("OutputType").Equals("Exe", StringComparison.OrdinalIgnoreCase) ? "console"
              : "library";

    // packages
    var packages = new List<PackageInfo>();
    var pcPath = Path.Combine(dir, "packages.config");
    if (File.Exists(pcPath))
        packages.AddRange(XDocument.Load(pcPath).Descendants("package").Select(p => new PackageInfo { Id = (string?)p.Attribute("id") ?? "", Version = (string?)p.Attribute("version") ?? "" }));
    packages.AddRange(xml.Descendants(ns + "PackageReference").Select(p => new PackageInfo { Id = (string?)p.Attribute("Include") ?? "", Version = (string?)p.Attribute("Version") ?? p.Element(ns + "Version")?.Value ?? "" }));
    foreach (var p in packages) Classify(p);
    info.Packages = packages;
    return info;
}

static void Classify(PackageInfo p)
{
    var id = p.Id;
    (p.Status, p.Note) = id switch
    {
        "Microsoft.AspNet.Mvc" or "Microsoft.AspNet.Razor" or "Microsoft.AspNet.WebPages" => ("replace", "Built into ASP.NET Core."),
        "Microsoft.AspNet.WebApi" or "Microsoft.AspNet.WebApi.Core" or "Microsoft.AspNet.WebApi.WebHost" or "Microsoft.AspNet.WebApi.Client" => ("replace", "ASP.NET Core controllers / minimal APIs (WebApi.Client has a .NET Standard build if clients need it)."),
        "Microsoft.AspNet.Web.Optimization" or "WebGrease" or "Antlr" => ("remove", "Bundling - not used in ASP.NET Core."),
        "Microsoft.Web.Infrastructure" or "Microsoft.CodeDom.Providers.DotNetCompilerPlatform" or "Microsoft.Net.Compilers" => ("remove", "System.Web plumbing."),
        "EntityFramework" => ("bridge", "EF 6.5 runs on .NET 10; EF Core is the long-term target."),
        "Newtonsoft.Json" => ("ok", "Has .NET Standard builds. Check the version for known advisories; consider System.Text.Json."),
        "Microsoft.Owin" or "Owin" or "Microsoft.Owin.Host.SystemWeb" or "Microsoft.Owin.Security" or "Microsoft.Owin.Security.Cookies" or "Microsoft.Owin.Security.OAuth" => ("replace", "ASP.NET Core middleware and authentication."),
        "Microsoft.AspNet.Identity.Core" or "Microsoft.AspNet.Identity.EntityFramework" or "Microsoft.AspNet.Identity.Owin" => ("replace", "ASP.NET Core Identity."),
        "Microsoft.AspNet.SignalR" or "Microsoft.AspNet.SignalR.Core" => ("replace", "ASP.NET Core SignalR (clients change too)."),
        "Dapper" or "AutoMapper" or "Serilog" or "NLog" or "log4net" or "FluentValidation" or "Polly" or "StackExchange.Redis" => ("ok", "Supports modern .NET - check the major version."),
        _ when id.StartsWith("System.") => ("check", "Often part of the .NET 10 runtime now - may be removable."),
        _ => ("check", "Check nuget.org for a netstandard2.0 / net8.0+ build."),
    };
}

Unit WebFormsUnit(ProjectInfo project, string dir, string markupPath)
{
    var markup = File.ReadAllText(markupPath);
    var codeBehindPath = markupPath + ".cs";
    var code = File.Exists(codeBehindPath) ? File.ReadAllText(codeBehindPath) : "";
    var ext = Path.GetExtension(markupPath).ToLowerInvariant();
    var kind = ext == ".aspx" ? "webforms-page" : ext == ".ascx" ? "user-control" : "master-page";

    var m = new Dictionary<string, double>
    {
        ["serverControls"] = Regex.Matches(markup, @"<asp:\w+", RegexOptions.IgnoreCase).Count,
        ["updatePanels"] = Regex.Matches(markup, @"<asp:UpdatePanel", RegexOptions.IgnoreCase).Count,
        ["dataControls"] = Regex.Matches(markup, @"<asp:(GridView|Repeater|ListView|FormView|DetailsView|DataList|DropDownList)\b", RegexOptions.IgnoreCase).Count,
        ["validators"] = Regex.Matches(markup, @"<asp:\w*Validator\b", RegexOptions.IgnoreCase).Count,
        ["postbackHandlers"] = Regex.Matches(code, @"protected\s+void\s+(?!Page_Load|Page_Init|Page_PreRender)\w+\s*\(\s*object\s+\w+\s*,\s*\w*EventArgs\s+\w+\s*\)").Count,
        ["viewStateUses"] = Regex.Matches(code, @"ViewState\[").Count,
        ["sessionUses"] = Regex.Matches(code, @"\bSession\b|\w*Session\.\w+").Count,
        ["inlineDataAccess"] = Regex.Matches(code, @"new\s+\w*(Context|DbContext)\s*\(|new\s+SqlConnection\s*\(|new\s+SqlCommand\s*\(").Count,
        ["authCalls"] = Regex.Matches(code, @"FormsAuthentication\.|Response\.Cookies|Membership\.").Count,
        ["inlineEditGrid"] = Regex.IsMatch(markup, @"OnRowEditing|OnRowUpdating|<EditItemTemplate", RegexOptions.IgnoreCase) ? 1 : 0,
        ["userControls"] = Regex.Matches(markup, @"<%@\s*Register\s+Src=", RegexOptions.IgnoreCase).Count,
        ["codeBehindLoc"] = code.Split('\n').Count(l => l.Trim().Length > 0),
    };

    var points = m["serverControls"] / 5 + m["updatePanels"] * 3 + m["dataControls"] * 2 + m["validators"] * 0.5 + m["postbackHandlers"]
               + m["viewStateUses"] + m["sessionUses"] * 0.5 + m["inlineDataAccess"] * 2 + m["inlineEditGrid"] * 4 + m["userControls"] + m["codeBehindLoc"] / 40;
    var size = points < 8 ? "S" : points < 20 ? "M" : "L";
    var (lo, hi) = size == "S" ? (2, 4) : size == "M" ? (4, 8) : (10, 18);
    if (kind != "webforms-page") (lo, hi) = (Math.Max(1, lo / 2), Math.Max(2, hi / 2));

    // Default target rules (references/webforms-target-rules.md). The agent reviews every one.
    string target, reason;
    if (kind == "master-page")
    {
        target = "Layout (+ view component)";
        reason = "Master pages become a shared layout; code-behind logic moves into a view component or partial.";
    }
    else if (kind == "user-control")
    {
        target = "View component (Razor Pages) or Blazor component";
        reason = "Pick the same technology as the pages that host it.";
    }
    else if (m["authCalls"] > 0)
    {
        target = "Razor Pages";
        reason = "Signs users in/out or writes cookies - needs a real HTTP request, which interactive Blazor doesn't have.";
    }
    else if (m["sessionUses"] > 0 && m["inlineEditGrid"] == 0)
    {
        target = "Razor Pages";
        reason = m["updatePanels"] > 0
            ? "Uses session state, and interactive Blazor has no HttpContext or session. Keep it a page; replace the UpdatePanel with a small fetch to a page handler."
            : "Uses session state, and interactive Blazor has no HttpContext or session. Postback handlers map to Razor Pages handlers.";
    }
    else if (m["updatePanels"] > 0 || m["inlineEditGrid"] > 0 || m["postbackHandlers"] >= 6)
    {
        target = "Blazor (Interactive Server)";
        reason = $"Heavy in-page interactivity ({m["updatePanels"]} UpdatePanel, inline grid editing: {(m["inlineEditGrid"] > 0 ? "yes" : "no")}, {m["postbackHandlers"]} postback handlers) maps to components better than to page handlers.";
    }
    else
    {
        target = "Razor Pages";
        reason = "List/form page: postback handlers map to Razor Pages handlers, GET pages stay linkable and cacheable.";
    }

    var u = MakeUnit(kind, Rel(dir, markupPath), project.Name, Rel(solutionDir, markupPath), size, "translation", lo, hi, target, reason);
    foreach (var kv in m) u.Metrics[kv.Key] = kv.Value;
    u.Metrics["complexityPoints"] = Math.Round(points, 1);
    if (m["viewStateUses"] > 0) u.Notes.Add($"Keeps data in ViewState ({m["viewStateUses"]} uses) - make that state explicit.");
    if (m["sessionUses"] > 0) u.Notes.Add("Uses session (directly or through a helper).");
    if (m["inlineDataAccess"] > 0) u.Notes.Add("Talks to the database from code-behind - extract a service first.");
    if (m["updatePanels"] > 0) u.Notes.Add("UpdatePanel partial postbacks.");
    return u;
}

Unit MakeUnit(string kind, string name, string project, string file, string size, string category, double lo, double hi, string target, string reason) =>
    new()
    {
        Id = (kind + "-" + Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-")).Trim('-'),
        Kind = kind, Name = name, Project = project, File = file, Size = size, Category = category,
        HoursLow = Math.Round(lo, 1), HoursHigh = Math.Round(hi, 1), Target = target, TargetReason = reason,
    };

static CrossItem Cross(string id, string name, string category, double lo, double hi, List<string> humanTasks) =>
    new() { Id = id, Name = name, Category = category, HoursLow = Math.Round(lo, 1), HoursHigh = Math.Round(hi, 1), HumanTasks = humanTasks };

static string ClassBody(string text, int start)
{
    var open = text.IndexOf('{', start);
    if (open < 0) return "";
    var depth = 0;
    for (var i = open; i < text.Length; i++)
    {
        if (text[i] == '{') depth++;
        else if (text[i] == '}' && --depth == 0) return text.Substring(open, i - open + 1);
    }
    return text[open..];
}

static int CountAcross(IEnumerable<string> files, string pattern) =>
    files.Where(f => f.EndsWith(".cs")).Sum(f => Regex.Matches(File.ReadAllText(f), pattern).Count);

// ================= model =================

class Assessment
{
    public int SchemaVersion { get; set; } = 1;
    public string GeneratedAt { get; set; } = "";
    public string Tool { get; set; } = "AI for Developers Toolkit - Migration Pack (beta)";
    public SolutionInfo Solution { get; set; } = new();
    public Summary Summary { get; set; } = new();
    public List<ProjectInfo> Projects { get; set; } = [];
    public List<Unit> Units { get; set; } = [];
    public List<CrossItem> CrossCutting { get; set; } = [];
    public List<Blocker> Blockers { get; set; } = [];
    public List<Risk> Risks { get; set; } = [];
    public List<string> Questions { get; set; } = [];
    public List<string> NextSteps { get; set; } = [];
    public EffortModel EffortModel { get; set; } = new();
    public Totals Totals { get; set; } = new();
}

class SolutionInfo { public string Name { get; set; } = ""; public string Path { get; set; } = ""; }

class Summary
{
    // Filled in by the agent after reviewing the scan.
    public string? Verdict { get; set; }          // "go" | "go-with-conditions" | "not-now"
    public string? Headline { get; set; }
    public string? VerdictReason { get; set; }
    public string? Destination { get; set; }      // e.g. "Port to .NET 10 incrementally (strangler fig)"
}

class ProjectInfo
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    [JsonIgnore] public string FullPath { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool SdkStyle { get; set; }
    public string TargetFramework { get; set; } = "";
    public int Loc { get; set; }
    public bool PackagesConfig { get; set; }
    public bool UsesSystemWeb { get; set; }
    public List<string> ProjectReferences { get; set; } = [];
    public Dictionary<string, int> FileCounts { get; set; } = new();
    public List<PackageInfo> Packages { get; set; } = [];
}

class PackageInfo
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public string Status { get; set; } = "";   // ok | bridge | replace | remove | check | blocked
    public string Note { get; set; } = "";
}

class Unit
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public string Project { get; set; } = "";
    public string File { get; set; } = "";
    public string Size { get; set; } = "";
    public string Category { get; set; } = "";
    public double HoursLow { get; set; }
    public double HoursHigh { get; set; }
    public string Target { get; set; } = "";
    public string TargetReason { get; set; } = "";
    public Dictionary<string, double> Metrics { get; set; } = new();
    public List<string> Notes { get; set; } = [];
    public List<string> HumanTasks { get; set; } = [];
}

class CrossItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public double HoursLow { get; set; }
    public double HoursHigh { get; set; }
    public List<string> HumanTasks { get; set; } = [];
}

class Blocker
{
    public string Signal { get; set; } = "";
    public string Severity { get; set; } = "";
    public int Count { get; set; }
    public List<string> Files { get; set; } = [];
    public string Meaning { get; set; } = "";
    public string Fix { get; set; } = "";
}

class Risk
{
    public string Title { get; set; } = "";
    public string Impact { get; set; } = "";     // high | medium | low
    public string Mitigation { get; set; } = "";
}

class EffortModel
{
    public double HoursPerDay { get; set; } = 6;
    public double ReviewOverhead { get; set; } = 0.25;
    public double FixedCostScale { get; set; } = 1.0;
    public int TotalLoc { get; set; }
    public Dictionary<string, double> AiShare { get; set; } = new()
    {
        ["mechanical"] = 0.9,
        ["translation"] = 0.7,
        ["verification"] = 0.8,
        ["decision"] = 0.0,
        ["environment"] = 0.3,
    };
    public List<string> Assumptions { get; set; } =
    [
        "Hours are for one developer who knows the codebase, at 6 focused hours a day.",
        "\"With AI\" = an agent does its share of the work and a developer reviews it (25% of the agent's share goes back into review).",
        "Decisions (auth, session, hosting, UI targets) take the same time with or without AI.",
        "Fixed costs (hosting, auth, session, config, rollout) are scaled by solution size: x0.5 under 3k LOC, x1 up to 30k, x1.5 above.",
        "Ranges, not promises: re-estimate after the first 3 migrated slices.",
    ];

    public double WithAi(string category, double hours)
    {
        var ai = AiShare.TryGetValue(category, out var s) ? s : 0.5;
        return hours * (1 - ai) + hours * ai * ReviewOverhead;
    }
}

class Totals
{
    public double HoursLowWithoutAi { get; set; }
    public double HoursHighWithoutAi { get; set; }
    public double HoursLowWithAi { get; set; }
    public double HoursHighWithAi { get; set; }
    public double DaysLowWithoutAi { get; set; }
    public double DaysHighWithoutAi { get; set; }
    public double DaysLowWithAi { get; set; }
    public double DaysHighWithAi { get; set; }
    public string Size { get; set; } = "";
    public Dictionary<string, double[]> ByCategory { get; set; } = new();   // category -> [lowNoAi, highNoAi, lowAi, highAi]

    public static Totals From(Assessment a)
    {
        var t = new Totals();
        var items = a.Units.Select(u => (u.Category, u.HoursLow, u.HoursHigh))
            .Concat(a.CrossCutting.Select(c => (c.Category, c.HoursLow, c.HoursHigh)));
        foreach (var (cat, lo, hi) in items)
        {
            if (!t.ByCategory.TryGetValue(cat, out var arr)) t.ByCategory[cat] = arr = new double[4];
            arr[0] += lo; arr[1] += hi; arr[2] += a.EffortModel.WithAi(cat, lo); arr[3] += a.EffortModel.WithAi(cat, hi);
        }
        foreach (var arr in t.ByCategory.Values) for (var i = 0; i < 4; i++) arr[i] = Math.Round(arr[i], 1);
        t.HoursLowWithoutAi = Math.Round(t.ByCategory.Values.Sum(v => v[0]));
        t.HoursHighWithoutAi = Math.Round(t.ByCategory.Values.Sum(v => v[1]));
        t.HoursLowWithAi = Math.Round(t.ByCategory.Values.Sum(v => v[2]));
        t.HoursHighWithAi = Math.Round(t.ByCategory.Values.Sum(v => v[3]));
        var d = a.EffortModel.HoursPerDay;
        t.DaysLowWithoutAi = Math.Round(t.HoursLowWithoutAi / d, 1);
        t.DaysHighWithoutAi = Math.Round(t.HoursHighWithoutAi / d, 1);
        t.DaysLowWithAi = Math.Round(t.HoursLowWithAi / d, 1);
        t.DaysHighWithAi = Math.Round(t.HoursHighWithAi / d, 1);
        var mid = (t.HoursLowWithoutAi + t.HoursHighWithoutAi) / 2;
        t.Size = mid < 80 ? "S" : mid < 240 ? "M" : mid < 800 ? "L" : "XL";
        return t;
    }
}
