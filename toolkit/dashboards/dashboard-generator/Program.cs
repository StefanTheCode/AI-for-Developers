// AI for Developers Toolkit - dashboard generator (.NET)
// Scans skills/, agents/ and runs/ and writes a single self-contained dashboard/index.html.
//
// Run it (from anywhere inside the toolkit repo):
//     dotnet run --project dashboard-generator
// or, if you are inside this folder:
//     dotnet run
//
// No NuGet packages. Just the .NET SDK. The toolkit root is auto-detected
// (the nearest folder that contains a "skills" directory); override with --root <path>.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

const string CommunityUrl = "https://www.skool.com/ai-for-dotnet-developers/about";
const string BrandName = "AI for Developers Toolkit";
const string BrandSub = "AI for .NET developers";

// The HTML/CSS/JS template. /*__PAYLOAD__*/ is replaced with the embedded JSON.
const string Template = """"
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>AI for Developers Toolkit - Dashboard</title>
<style>
:root{
  --bg:#150f29; --bg2:#1a1530; --card:#241c47; --card2:#2c2358; --line:#3a2f6b;
  --ink:#ECE9FF; --muted:#A99FD6; --yellow:#FFD23F; --green:#46d39a; --red:#ff6b81;
  --orange:#ffa552; --blue:#7aa2ff; --teal:#3fd6c9; --pink:#ff8ad1; --violet:#b69bff; --cyan:#5fd0ff;
}
*{box-sizing:border-box;margin:0;padding:0}
body{background:radial-gradient(1100px 560px at 80% -12%,#2c2358 0%,var(--bg) 60%);
  color:var(--ink);font:15px/1.55 -apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,Helvetica,Arial,sans-serif;
  min-height:100vh;padding:0 0 64px}
a{color:var(--yellow);text-decoration:none}
.wrap{max-width:1180px;margin:0 auto;padding:0 22px}
header{padding:26px 0 8px}
.htop{display:flex;align-items:center;justify-content:space-between;gap:16px;flex-wrap:wrap}
.brand{display:flex;align-items:center;gap:13px}
.logo{width:44px;height:44px;border-radius:12px;background:linear-gradient(135deg,var(--yellow),#ff9f43);
  display:flex;align-items:center;justify-content:center;font-weight:900;color:#150f29;font-size:19px}
.brand .n{font-size:21px;font-weight:800;letter-spacing:.2px}
.brand .s{font-size:13px;color:var(--muted)}
.hright{display:flex;align-items:center;gap:14px;flex-wrap:wrap}
.updated{font-size:12.5px;color:var(--muted)}
.updated b{color:var(--ink)}
.cta{background:var(--yellow);color:#150f29;font-weight:800;font-size:13.5px;border-radius:999px;
  padding:10px 18px;white-space:nowrap;transition:.15s}
.cta:hover{transform:translateY(-1px);box-shadow:0 8px 22px rgba(255,210,63,.3)}
.stats{display:flex;gap:14px;flex-wrap:wrap;margin:22px 0 6px}
.stat{background:var(--card);border:1px solid var(--line);border-radius:14px;padding:14px 20px;min-width:108px}
.stat .v{font-size:26px;font-weight:800;color:var(--yellow)}
.stat .l{font-size:11.5px;color:var(--muted);text-transform:uppercase;letter-spacing:.6px;margin-top:2px}
.tabs{display:flex;gap:8px;margin:24px 0 14px;flex-wrap:wrap}
.tab{background:var(--card);border:1px solid var(--line);color:var(--muted);border-radius:999px;
  padding:8px 17px;cursor:pointer;font-weight:600;font-size:13px}
.tab.active{background:var(--yellow);color:#150f29;border-color:var(--yellow)}
.controls{display:flex;gap:10px;flex-wrap:wrap;margin-bottom:18px}
input[type=search],select{background:var(--bg2);border:1px solid var(--line);color:var(--ink);
  border-radius:10px;padding:10px 12px;font-size:14px;outline:none}
input[type=search]{flex:1;min-width:220px}
input::placeholder{color:#7d73aa}
.hidden{display:none}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(330px,1fr));gap:14px}
.card{background:linear-gradient(180deg,var(--card2),var(--card));border:1px solid var(--line);
  border-radius:16px;padding:16px 17px;display:flex;flex-direction:column;gap:9px;transition:.16s;cursor:pointer}
.card:hover{border-color:var(--yellow);transform:translateY(-2px)}
.card .top{display:flex;justify-content:space-between;align-items:flex-start;gap:10px}
.card h3{margin:0;font-size:15.5px;font-family:ui-monospace,SFMono-Regular,Menlo,monospace}
.badge{font-size:11px;font-weight:700;padding:3px 9px;border-radius:999px;white-space:nowrap}
.b-kind{background:rgba(255,210,63,.14);color:var(--yellow);border:1px solid rgba(255,210,63,.32)}
.card p{margin:0;color:var(--muted);font-size:13.5px}
.meta{display:flex;gap:8px;flex-wrap:wrap;margin-top:2px;font-size:11.5px;color:#8d83b8}
.pill{border:1px solid var(--line);border-radius:6px;padding:2px 7px}
.full{display:none;color:var(--ink);font-size:13px;border-top:1px dashed var(--line);padding-top:10px;margin-top:2px}
.card.open .full{display:block}.card.open .lead{display:none}
.legend{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin-bottom:16px;
  background:var(--card);border:1px solid var(--line);border-radius:14px;padding:14px 16px}
.legend .t{font-size:13px;color:var(--muted);margin-right:6px}
.vsum{display:inline-flex;align-items:center;gap:7px;font-size:13px;font-weight:700}
.vdot{width:10px;height:10px;border-radius:50%}
.note{color:var(--muted);font-size:12.5px;margin:0 0 16px}
.run{background:linear-gradient(180deg,var(--card2),var(--card));border:1px solid var(--line);
  border-radius:14px;padding:14px 16px;margin-bottom:12px}
.run .rtop{display:flex;justify-content:space-between;gap:12px;flex-wrap:wrap;align-items:center}
.run h3{margin:0;font-size:15px;font-family:ui-monospace,monospace}
.v{font-size:11px;font-weight:800;padding:3px 11px;border-radius:999px;text-transform:uppercase;letter-spacing:.5px}
.v-clean,.v-pass{background:rgba(70,211,154,.16);color:var(--green);border:1px solid rgba(70,211,154,.4)}
.v-issues{background:rgba(255,165,82,.16);color:var(--orange);border:1px solid rgba(255,165,82,.4)}
.v-critical{background:rgba(255,107,129,.16);color:var(--red);border:1px solid rgba(255,107,129,.4)}
.v-generated{background:rgba(122,162,255,.16);color:var(--blue);border:1px solid rgba(122,162,255,.4)}
.run .sum{color:var(--muted);margin:8px 0 0;font-size:13.5px}
.findings{list-style:none;margin:10px 0 0;padding:0;display:flex;flex-direction:column;gap:5px}
.findings li{font-size:12.5px;display:flex;gap:8px;align-items:baseline}
.sev{font-size:10px;font-weight:800;padding:2px 7px;border-radius:5px;text-transform:uppercase}
.sev-low{background:rgba(122,162,255,.15);color:var(--blue)}
.sev-medium{background:rgba(255,165,82,.15);color:var(--orange)}
.sev-high{background:rgba(255,107,129,.15);color:var(--red)}
.sev-critical{background:var(--red);color:#150f29}
.loc{color:#8d83b8;font-family:ui-monospace,monospace}
.empty{color:var(--muted);text-align:center;padding:50px;border:1px dashed var(--line);border-radius:14px}
footer{color:#8d83b8;font-size:12.5px;margin-top:34px;text-align:center;border-top:1px solid var(--line);padding-top:22px}
footer a{font-weight:700}
@media(max-width:640px){.brand .s{display:none}}
</style>
</head>
<body>
<div class="wrap">
  <header>
    <div class="htop">
      <div class="brand">
        <div class="logo">&lt;/&gt;</div>
        <div><div class="n" id="b-name"></div><div class="s" id="b-sub"></div></div>
      </div>
      <div class="hright">
        <div class="updated">Updated <b id="b-upd"></b></div>
        <a class="cta" id="b-cta" target="_blank" rel="noopener">Start free trial</a>
      </div>
    </div>
    <div class="stats" id="stats"></div>
  </header>

  <div class="tabs">
    <div class="tab active" data-view="runs">Run results</div>
    <div class="tab" data-view="skills">Skills</div>
    <div class="tab" data-view="agents">Agents</div>
  </div>

  <div class="controls" id="controls">
    <input type="search" id="q" placeholder="Search by name or description...">
    <select id="cat"><option value="">All categories</option></select>
  </div>

  <div id="view"></div>

  <footer id="foot"></footer>
</div>

<script id="data" type="application/json">/*__PAYLOAD__*/</script>
<script>
const DATA = JSON.parse(document.getElementById('data').textContent);
let view = 'runs';

const CAT_COLORS = {
  'Architecture':'#7aa2ff','EF Core / Database':'#46d39a','Performance':'#ffa552',
  'Observability':'#3fd6c9','Testing':'#b69bff','Security':'#ff6b81','DevOps':'#5fd0ff',
  'Quality':'#ff8ad1','Code Quality':'#ff8ad1','Meta / Tooling':'#FFD23F','AI Tooling':'#FFD23F'
};
const VCOL = {critical:'#ff6b81',issues:'#ffa552',pass:'#46d39a',clean:'#46d39a',generated:'#7aa2ff'};
const VLABEL = {critical:'Critical',issues:'Issues',pass:'Healthy',clean:'Healthy',generated:'Generated'};
function catColor(c){return CAT_COLORS[c]||'#A99FD6';}
function esc(s){return (s||'').replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));}

function head(){
  document.getElementById('b-name').textContent = DATA.brand;
  document.getElementById('b-sub').textContent = DATA.sub;
  document.getElementById('b-upd').textContent = DATA.generated;
  document.getElementById('b-cta').href = DATA.community;
  const c=DATA.counts;
  document.getElementById('stats').innerHTML=[
    ['Skills',c.skills],['Agents',c.agents],['Categories',c.categories],['Tools run',c.runs]
  ].map(([l,n])=>`<div class="stat"><div class="v">${n}</div><div class="l">${l}</div></div>`).join('');
  document.getElementById('foot').innerHTML =
    `${esc(DATA.brand)} &middot; <a href="${esc(DATA.community)}" target="_blank" rel="noopener">Join the community</a> &middot; updated ${esc(DATA.generated)}`;
}
function fillCats(){
  document.getElementById('cat').innerHTML='<option value="">All categories</option>'+
    DATA.categories.map(c=>`<option value="${esc(c)}">${esc(c)}</option>`).join('');
}
function cardHTML(it){
  const docs=[]; if(it.has_usage)docs.push('USAGE'); if(it.has_refs)docs.push('references');
  const ver=it.version?`<span class="pill">v${esc(it.version)}</span>`:'';
  const col=catColor(it.category);
  const catBadge=`<span class="badge" style="background:${col}22;color:${col};border:1px solid ${col}55">${esc(it.category||'-')}</span>`;
  return `<div class="card" data-name="${esc((it.name+' '+it.description).toLowerCase())}" data-cat="${esc(it.category)}">
    <div class="top"><h3>${esc(it.name)}</h3>${catBadge}</div>
    <p class="lead">${esc(it.lead||it.description)}</p>
    <div class="full">${esc(it.description)}</div>
    <div class="meta"><span class="badge b-kind">${esc(it.kind)}</span>${ver}${docs.map(d=>`<span class="pill">${d}</span>`).join('')}</div>
  </div>`;
}
function runHTML(r){
  const v=(r.verdict||'pass').toLowerCase();
  const findings=(r.findings||[]).map(f=>{
    const sev=(f.severity||'low').toLowerCase();
    const loc=f.location?` <span class="loc">${esc(f.location)}</span>`:'';
    return `<li><span class="sev sev-${sev}">${esc(sev)}</span><span>${esc(f.title)}${loc}</span></li>`;
  }).join('');
  return `<div class="run" data-name="${esc((r.tool+' '+r.target+' '+(r.summary||'')).toLowerCase())}">
    <div class="rtop"><h3>${esc(r.tool)} <span style="color:#8d83b8">&rarr; ${esc(r.target)}</span></h3>
      <span class="v v-${v}">${esc(v)}</span></div>
    <div class="meta"><span class="badge b-kind">${esc(r.type||'')}</span><span class="pill">${esc(r.date||'')}</span></div>
    <p class="sum">${esc(r.summary||'')}</p>${findings?`<ul class="findings">${findings}</ul>`:''}</div>`;
}
function verdictSummary(runs){
  const c={}; runs.forEach(r=>{const v=(r.verdict||'').toLowerCase(); c[v]=(c[v]||0)+1;});
  const order=['critical','issues','pass','clean','generated'];
  const seen=new Set();
  const items=order.filter(v=>c[v]).map(v=>{
    const label=VLABEL[v]; if(seen.has(label))return ''; seen.add(label);
    let n=c[v]; if(v==='pass')n=(c['pass']||0)+(c['clean']||0);
    return `<span class="vsum"><span class="vdot" style="background:${VCOL[v]}"></span>${n} ${label}</span>`;
  }).filter(Boolean).join('');
  return `<div class="legend"><span class="t">Results:</span>${items}</div>`;
}
function render(){
  const host=document.getElementById('view');
  const q=document.getElementById('q').value.toLowerCase().trim();
  const cat=document.getElementById('cat').value;
  document.getElementById('cat').classList.toggle('hidden', view==='runs');
  let html='';
  if(view==='runs'){
    let runs=DATA.runs.filter(r=>!q||(r.tool+' '+r.target+' '+(r.summary||'')).toLowerCase().includes(q));
    const note=`<p class="note">Latest results from running the toolkit against <b style="color:var(--ink)">Project Atlas</b> - an anonymized real-world .NET 10 codebase.</p>`;
    html = runs.length ? verdictSummary(runs)+note+runs.map(runHTML).join('')
      : '<div class="empty">No runs recorded yet.</div>';
  } else {
    let items=(view==='skills'?DATA.skills:DATA.agents)
      .filter(it=>(!q||(it.name+' '+it.description).toLowerCase().includes(q))&&(!cat||it.category===cat));
    html = items.length ? `<div class="grid">${items.map(cardHTML).join('')}</div>`
      : '<div class="empty">Nothing matches your filter.</div>';
  }
  host.innerHTML=html;
  host.querySelectorAll('.card').forEach(c=>c.addEventListener('click',()=>c.classList.toggle('open')));
}
document.querySelectorAll('.tab').forEach(t=>t.addEventListener('click',()=>{
  document.querySelectorAll('.tab').forEach(x=>x.classList.remove('active'));
  t.classList.add('active'); view=t.dataset.view; render();
}));
document.getElementById('q').addEventListener('input',render);
document.getElementById('cat').addEventListener('change',render);
head(); fillCats(); render();
</script>
</body>
</html>
"""";

// ---------------- main ----------------

string? argRoot = null;
for (int i = 0; i < args.Length; i++)
    if (args[i] == "--root" && i + 1 < args.Length) argRoot = args[i + 1];

string? root = FindRoot(argRoot);
if (root is null)
{
    Console.Error.WriteLine("error: could not find the toolkit root (a folder containing 'skills').");
    Console.Error.WriteLine("       run this from inside the toolkit, or pass --root <path>.");
    Environment.Exit(1);
    return;
}

JsonObject? meta = LoadMeta(root);
List<JsonObject> skills = LoadSkills(root, meta);
List<JsonObject> agents = LoadAgents(root, meta);
List<JsonObject> runs = LoadRuns(root);

List<string> cats = skills.Concat(agents)
    .Select(o => (string?)o["category"])
    .Where(c => !string.IsNullOrEmpty(c))
    .Select(c => c!)
    .Distinct()
    .OrderBy(c => c, StringComparer.Ordinal)
    .ToList();

var payload = new JsonObject
{
    ["brand"] = BrandName,
    ["sub"] = BrandSub,
    ["community"] = CommunityUrl,
    ["skills"] = new JsonArray(skills.Cast<JsonNode>().ToArray()),
    ["agents"] = new JsonArray(agents.Cast<JsonNode>().ToArray()),
    ["runs"] = new JsonArray(runs.Cast<JsonNode>().ToArray()),
    ["categories"] = new JsonArray(cats.Select(c => (JsonNode)JsonValue.Create(c)!).ToArray()),
    ["generated"] = DateTime.UtcNow.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture),
    ["counts"] = new JsonObject
    {
        ["skills"] = skills.Count,
        ["agents"] = agents.Count,
        ["categories"] = cats.Count,
        ["runs"] = runs.Count,
    },
};

string html = Template.Replace("/*__PAYLOAD__*/", payload.ToJsonString());

string outDir = Path.Combine(root, "dashboard");
Directory.CreateDirectory(outDir);
string outFile = Path.Combine(outDir, "index.html");
File.WriteAllText(outFile, html, new UTF8Encoding(false));

Console.WriteLine($"{BrandName} dashboard built.");
Console.WriteLine($"  skills: {skills.Count} | agents: {agents.Count} | categories: {cats.Count} | runs: {runs.Count}");
Console.WriteLine($"  -> {outFile}");
Console.WriteLine("  serve:  cd dashboard && python -m http.server 8080   (or just open index.html)");
return;

// ---------------- helpers ----------------

static string ReadText(string path) => File.ReadAllText(path).Replace("\r\n", "\n");

static string? FindRoot(string? argRoot)
{
    foreach (var start in new[] { argRoot, Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        if (string.IsNullOrEmpty(start)) continue;
        string? d = Path.GetFullPath(start);
        for (int i = 0; i < 8 && d is not null; i++)
        {
            if (Directory.Exists(Path.Combine(d, "skills"))) return d;
            d = Path.GetDirectoryName(d);
        }
    }
    return null;
}

static Dictionary<string, string> ParseFrontmatter(string text)
{
    var fm = new Dictionary<string, string>();
    var m = Regex.Match(text, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
    if (!m.Success) return fm;
    foreach (var line in m.Groups[1].Value.Split('\n'))
    {
        var mm = Regex.Match(line, @"^([A-Za-z_][A-Za-z0-9_]*):\s*(.*)$");
        if (!mm.Success) continue;
        var k = mm.Groups[1].Value.Trim();
        var v = mm.Groups[2].Value.Trim();
        if (v.Length >= 2 && (v[0] == '"' || v[0] == '\'') && v[^1] == v[0]) v = v[1..^1];
        fm[k] = v;
    }
    return fm;
}

static string LeadSentence(string desc)
{
    if (string.IsNullOrEmpty(desc)) return "";
    foreach (var marker in new[] { ". Use this", ". Always use", ". Trigger" })
    {
        int idx = desc.IndexOf(marker, StringComparison.Ordinal);
        if (idx != -1) return desc[..(idx + 1)].Trim();
    }
    int p = desc.IndexOf(". ", StringComparison.Ordinal);
    return (p != -1 ? desc[..(p + 1)] : desc).Trim();
}

static JsonObject? LoadMeta(string root)
{
    var p = Path.Combine(root, "catalog-meta.json");
    if (!File.Exists(p)) return null;
    try { return JsonNode.Parse(ReadText(p)) as JsonObject; }
    catch (Exception e) { Console.WriteLine($"  warn: could not parse catalog-meta.json: {e.Message}"); return null; }
}

static void ApplyMeta(JsonObject item, JsonObject? meta)
{
    JsonObject? m = null;
    if (meta is not null)
    {
        var name = (string?)item["name"];
        var folder = (string?)item["folder"];
        if (name is not null && meta[name] is JsonObject mo) m = mo;
        else if (folder is not null && meta[folder] is JsonObject mo2) m = mo2;
    }
    if (string.IsNullOrEmpty((string?)item["category"]))
        item["category"] = m?["category"]?.GetValue<string>() ?? "Uncategorized";
    if (string.IsNullOrEmpty((string?)item["version"]))
        item["version"] = m?["version"]?.GetValue<string>() ?? "";
    var tags = new JsonArray();
    if (m?["tags"] is JsonArray ta)
        foreach (var t in ta) tags.Add(JsonValue.Create((string?)t ?? ""));
    item["tags"] = tags;
}

static List<JsonObject> LoadSkills(string root, JsonObject? meta)
{
    var list = new List<JsonObject>();
    var sdir = Path.Combine(root, "skills");
    if (!Directory.Exists(sdir)) return list;
    foreach (var dir in Directory.GetDirectories(sdir).OrderBy(d => Path.GetFileName(d), StringComparer.Ordinal))
    {
        var name = Path.GetFileName(dir);
        var skillMd = Path.Combine(dir, "SKILL.md");
        if (!File.Exists(skillMd)) continue;
        var fm = ParseFrontmatter(ReadText(skillMd));
        var desc = fm.GetValueOrDefault("description", "");
        var item = new JsonObject
        {
            ["name"] = fm.GetValueOrDefault("name", name),
            ["folder"] = name,
            ["kind"] = "skill",
            ["description"] = desc,
            ["lead"] = LeadSentence(desc),
            ["category"] = fm.GetValueOrDefault("category", ""),
            ["version"] = fm.GetValueOrDefault("version", ""),
            ["has_usage"] = File.Exists(Path.Combine(dir, "USAGE.md")),
            ["has_refs"] = Directory.Exists(Path.Combine(dir, "references")),
        };
        ApplyMeta(item, meta);
        list.Add(item);
    }
    return list;
}

static List<JsonObject> LoadAgents(string root, JsonObject? meta)
{
    var list = new List<JsonObject>();
    var adir = Path.Combine(root, "agents");
    if (!Directory.Exists(adir)) return list;
    foreach (var file in Directory.GetFiles(adir, "*.md").OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal))
    {
        var fname = Path.GetFileName(file);
        if (fname.EndsWith(".USAGE.md", StringComparison.Ordinal)) continue;
        var fm = ParseFrontmatter(ReadText(file));
        var @base = fname[..^3];
        var desc = fm.GetValueOrDefault("description", "");
        var item = new JsonObject
        {
            ["name"] = fm.GetValueOrDefault("name", @base),
            ["folder"] = fname,
            ["kind"] = "agent",
            ["description"] = desc,
            ["lead"] = LeadSentence(desc),
            ["category"] = fm.GetValueOrDefault("category", ""),
            ["version"] = fm.GetValueOrDefault("version", ""),
            ["tools"] = fm.GetValueOrDefault("tools", ""),
            ["has_usage"] = File.Exists(Path.Combine(adir, @base + ".USAGE.md")),
            ["has_refs"] = false,
        };
        ApplyMeta(item, meta);
        list.Add(item);
    }
    return list;
}

static List<JsonObject> LoadRuns(string root)
{
    var runs = new List<JsonObject>();
    var rdir = Path.Combine(root, "runs");
    if (!Directory.Exists(rdir)) return runs;
    foreach (var file in Directory.GetFiles(rdir, "*.json").OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal))
    {
        try
        {
            if (JsonNode.Parse(ReadText(file)) is not JsonObject rec) continue;
            bool skip = rec["_skip"] is JsonNode s && s.GetValue<bool>();
            if (skip || rec["tool"] is null || rec["verdict"] is null) continue;
            runs.Add(rec);
        }
        catch (Exception e) { Console.WriteLine($"  warn: could not parse runs/{Path.GetFileName(file)}: {e.Message}"); }
    }
    return runs.OrderByDescending(r => (string?)r["date"] ?? "", StringComparer.Ordinal).ToList();
}
