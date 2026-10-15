#!/usr/bin/env python3
"""
Build a branded, screenshot-ready case-study showcase from the run records.
Groups results by category, with verdict badges and findings.
Scans toolkit/skills, toolkit/agents and packs/*/skills (and packs/*/agents) for
categories; reads runs/ and writes showcase/ inside toolkit/.
Usage: python3 build_showcase.py [--root <repo root or toolkit dir>] [--target "Project Atlas"] [--stack ".NET 10"]
"""
import argparse, glob, json, os, re, collections

def read(p):
    with open(p, encoding="utf-8") as f: return f.read()

def parse_fm(text):
    m = re.match(r'^---\s*\n(.*?)\n---\s*\n', text, re.S); fm = {}
    if m:
        for line in m.group(1).split('\n'):
            mm = re.match(r'^([A-Za-z_]+):\s*(.*)$', line)
            if mm:
                v = mm.group(2).strip()
                if len(v) >= 2 and v[0] in '"\'' and v[-1] == v[0]: v = v[1:-1]
                fm[mm.group(1)] = v
    return fm

def resolve_layout(root):
    """Return (home, skill_dirs, agent_dirs). home holds runs/, catalog-meta.json, showcase/.
    Accepts the repo root (toolkit/skills) or the toolkit dir; legacy flat layout still works."""
    root = os.path.abspath(root)
    if os.path.isdir(os.path.join(root, "toolkit", "skills")):
        repo, home = root, os.path.join(root, "toolkit")
    elif os.path.isdir(os.path.join(root, "skills")):
        home = root
        parent = os.path.dirname(root)
        repo = parent if os.path.isdir(os.path.join(parent, "packs")) else root
    else:
        return None, [], []
    skill_dirs = [os.path.join(home, "skills")] + sorted(glob.glob(os.path.join(repo, "packs", "*", "skills")))
    agent_dirs = [os.path.join(home, "agents")] + sorted(glob.glob(os.path.join(repo, "packs", "*", "agents")))
    return home, skill_dirs, agent_dirs

def catalog(home, skill_dirs, agent_dirs):
    meta = {}
    mp = os.path.join(home, "catalog-meta.json")
    if os.path.isfile(mp):
        try: meta = json.loads(read(mp))
        except Exception: pass
    cat = {}
    for sdir in skill_dirs:
        if not os.path.isdir(sdir): continue
        for n in os.listdir(sdir):
            p = os.path.join(sdir, n, "SKILL.md")
            if os.path.isfile(p):
                fm = parse_fm(read(p))
                cat[fm.get("name", n)] = fm.get("category") or meta.get(n, {}).get("category", "Other")
    for adir in agent_dirs:
        if not os.path.isdir(adir): continue
        for f in os.listdir(adir):
            if f.endswith(".md") and not f.endswith(".USAGE.md"):
                fm = parse_fm(read(os.path.join(adir, f)))
                cat[fm.get("name", f[:-3])] = fm.get("category") or meta.get(f[:-3], {}).get("category", "Other")
    return cat

def load_runs(root):
    out = []
    rdir = os.path.join(root, "runs")
    if os.path.isdir(rdir):
        for f in sorted(os.listdir(rdir)):
            if not f.endswith(".json"): continue
            try:
                r = json.loads(read(os.path.join(rdir, f)))
                if r.get("_skip") or not r.get("tool") or not r.get("verdict"): continue
                out.append(r)
            except Exception: pass
    return out

VORDER = {"critical": 0, "issues": 1, "pass": 2, "generated": 3, "clean": 4}
CORDER = ["Architecture", "Database / ORM", "Database", "EF Core / Database", "Performance", "Observability",
          "Testing", "Quality", "Code Quality", "Security", "DevOps", "AI Tooling", "Meta / Tooling", "Other"]

def build(root, target, stack):
    home, skill_dirs, agent_dirs = resolve_layout(root)
    if home is None:
        raise SystemExit(f"error: no toolkit/skills/ or skills/ folder under {root}")
    cat = catalog(home, skill_dirs, agent_dirs)
    runs = load_runs(home)
    for r in runs:
        r["_cat"] = cat.get(r["tool"], "Other")
    order = CORDER + sorted({r["_cat"] for r in runs} - set(CORDER))
    groups = collections.OrderedDict()
    for c in order:
        g = [r for r in runs if r["_cat"] == c]
        if g:
            g.sort(key=lambda r: (VORDER.get(r["verdict"], 9), r["tool"]))
            groups[c] = g
    vc = collections.Counter(r["verdict"] for r in runs)
    payload = {
        "target": target,
        "stack": stack,
        "groups": [{"category": c, "runs": g} for c, g in groups.items()],
        "stats": {"total": len(runs), "critical": vc.get("critical", 0),
                  "issues": vc.get("issues", 0), "clean_pass": vc.get("clean", 0) + vc.get("pass", 0),
                  "generated": vc.get("generated", 0)},
        "skills": sum(1 for r in runs if r.get("type") == "skill"),
        "agents": sum(1 for r in runs if r.get("type") == "agent"),
    }
    html = TEMPLATE.replace("/*__PAYLOAD__*/", json.dumps(payload, ensure_ascii=False))
    out_dir = os.path.join(home, "showcase"); os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(out_dir, "index.html")
    with open(out, "w", encoding="utf-8") as f: f.write(html)
    print(f"Showcase built: {payload['stats']} -> {out}")

TEMPLATE = r"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>TheCodeMan Toolkit - Case Study</title>
<style>
:root{--bg:#1a1530;--card:#2a2150;--card2:#322764;--ink:#ECE9FF;--muted:#A99FD6;--line:#3d3270;
--yellow:#FFD23F;--green:#46d39a;--red:#ff6b81;--orange:#ffa552;--blue:#7aa2ff;}
*{box-sizing:border-box}
body{margin:0;background:radial-gradient(1200px 700px at 75% -15%,#2c2358 0%,var(--bg) 55%);
color:var(--ink);font:15px/1.55 -apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,Helvetica,Arial,sans-serif;padding:0 0 70px}
.wrap{max-width:1140px;margin:0 auto;padding:0 24px}
.hero{padding:54px 0 14px}
.kic{display:inline-flex;align-items:center;gap:9px;background:var(--card);border:1px solid var(--line);
border-radius:999px;padding:6px 14px;font-size:12.5px;color:var(--muted);font-weight:600}
.dot{width:8px;height:8px;border-radius:50%;background:var(--yellow)}
h1{font-size:33px;margin:18px 0 8px;letter-spacing:.2px;line-height:1.15}
h1 .hl{color:var(--yellow)}
.lede{color:var(--muted);font-size:16px;max-width:760px}
.anon{margin-top:12px;font-size:12.5px;color:#8d83b8;border-left:2px solid var(--line);padding-left:12px}
.band{display:flex;gap:14px;flex-wrap:wrap;margin:30px 0 8px}
.kpi{flex:1;min-width:150px;background:linear-gradient(180deg,var(--card2),var(--card));
border:1px solid var(--line);border-radius:16px;padding:18px 20px}
.kpi .n{font-size:30px;font-weight:800}
.kpi .l{font-size:12px;color:var(--muted);text-transform:uppercase;letter-spacing:.6px;margin-top:3px}
.n.red{color:var(--red)}.n.orange{color:var(--orange)}.n.green{color:var(--green)}.n.blue{color:var(--blue)}.n.yellow{color:var(--yellow)}
.catsec{margin-top:38px}
.cathead{display:flex;align-items:baseline;gap:10px;border-bottom:1px solid var(--line);padding-bottom:8px;margin-bottom:16px}
.cathead h2{font-size:19px;margin:0}.cathead .c{color:var(--muted);font-size:13px}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(330px,1fr));gap:14px}
.card{background:linear-gradient(180deg,var(--card2),var(--card));border:1px solid var(--line);
border-radius:15px;padding:15px 16px;display:flex;flex-direction:column;gap:9px}
.card .top{display:flex;justify-content:space-between;align-items:flex-start;gap:8px}
.card h3{margin:0;font-size:14.5px;font-family:ui-monospace,SFMono-Regular,Menlo,monospace}
.v{font-size:10.5px;font-weight:800;padding:3px 9px;border-radius:999px;text-transform:uppercase;letter-spacing:.5px;white-space:nowrap}
.v-clean,.v-pass{background:rgba(70,211,154,.16);color:var(--green);border:1px solid rgba(70,211,154,.4)}
.v-issues{background:rgba(255,165,82,.16);color:var(--orange);border:1px solid rgba(255,165,82,.4)}
.v-critical{background:rgba(255,107,129,.16);color:var(--red);border:1px solid rgba(255,107,129,.4)}
.v-generated{background:rgba(122,162,255,.16);color:var(--blue);border:1px solid rgba(122,162,255,.4)}
.sum{color:var(--muted);font-size:13px;margin:0}
.kind{font-size:10.5px;color:#8d83b8;border:1px solid var(--line);border-radius:5px;padding:1px 6px}
.findings{list-style:none;margin:3px 0 0;padding:0;display:flex;flex-direction:column;gap:4px}
.findings li{font-size:11.5px;display:flex;gap:7px;align-items:baseline;color:var(--ink)}
.sev{font-size:9.5px;font-weight:800;padding:1px 6px;border-radius:4px;text-transform:uppercase}
.sev-low{background:rgba(122,162,255,.15);color:var(--blue)}
.sev-medium{background:rgba(255,165,82,.15);color:var(--orange)}
.sev-high{background:rgba(255,107,129,.15);color:var(--red)}
.sev-critical{background:var(--red);color:#1a1530}
.loc{color:#8d83b8;font-family:ui-monospace,monospace}
footer{margin-top:44px;text-align:center;color:#7d73aa;font-size:12.5px}
.brandline{margin-top:6px;color:var(--muted)}
</style></head><body><div class="wrap">
<div class="hero">
  <span class="kic"><span class="dot"></span> TheCodeMan Toolkit &middot; Case Study</span>
  <h1>We pointed <span class="hl">all the skills &amp; agents</span><br>at one real production codebase.</h1>
  <p class="lede" id="lede"></p>
  <p class="anon">Project name, vendor packages and domain details are withheld - the codebase isn't ours to disclose. Findings are real; identifiers are anonymized.</p>
</div>
<div class="band" id="band"></div>
<div id="cats"></div>
<footer><div>Generated from real tool runs &middot; verdicts: <b style="color:var(--green)">clean/pass</b> &middot; <b style="color:var(--orange)">issues</b> &middot; <b style="color:var(--red)">critical</b> &middot; <b style="color:var(--blue)">generated</b></div>
<div class="brandline">TheCodeMan.net - AI for Developers Toolkit</div></footer>
</div>
<script id="data" type="application/json">/*__PAYLOAD__*/</script>
<script>
const D=JSON.parse(document.getElementById('data').textContent);
const esc=s=>(s||'').replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
document.getElementById('lede').innerHTML=`Run against <b style="color:var(--ink)">${esc(D.target)}</b> - a real ${D.stack?esc(D.stack)+' ':''}production platform. ${D.stats.total} tools (${D.skills} skills + ${D.agents} agents), one pass, here's what they surfaced.`;
document.getElementById('band').innerHTML=[
 ['n',D.stats.total,'Tools run'],['red',D.stats.critical,'Critical'],
 ['orange',D.stats.issues,'Issues found'],['green',D.stats.clean_pass,'Clean / healthy'],
 ['blue',D.stats.generated,'Artifacts generated']
].map(([c,n,l])=>`<div class="kpi"><div class="n ${c}">${n}</div><div class="l">${l}</div></div>`).join('');
document.getElementById('cats').innerHTML=D.groups.map(g=>`
 <div class="catsec"><div class="cathead"><h2>${esc(g.category)}</h2><span class="c">${g.runs.length} ${g.runs.length===1?'tool':'tools'}</span></div>
 <div class="grid">${g.runs.map(r=>{
   const v=(r.verdict||'pass').toLowerCase();
   const f=(r.findings||[]).map(x=>`<li><span class="sev sev-${(x.severity||'low')}">${esc(x.severity)}</span><span>${esc(x.title)}${x.location?` <span class="loc">${esc(x.location)}</span>`:''}</span></li>`).join('');
   return `<div class="card"><div class="top"><h3>${esc(r.tool)}</h3><span class="v v-${v}">${esc(v)}</span></div>
   <div><span class="kind">${esc(r.type)}</span></div>
   <p class="sum">${esc(r.summary)}</p>${f?`<ul class="findings">${f}</ul>`:''}</div>`;
 }).join('')}</div></div>`).join('');
</script></body></html>"""

def _default_root():
    d = os.path.dirname(os.path.abspath(__file__))
    for _ in range(6):
        if os.path.isdir(os.path.join(d, "skills")):
            return d
        d = os.path.dirname(d)
    return os.getcwd()


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=None, help="Repo root or toolkit dir (auto-detected if omitted)")
    ap.add_argument("--target", default="Project Atlas")
    ap.add_argument("--stack", default=".NET 10", help='Stack shown in the headline, e.g. ".NET 10", "Node.js", "" to omit')
    a = ap.parse_args()
    root = os.path.abspath(a.root) if a.root else _default_root()
    build(root, a.target, a.stack)
