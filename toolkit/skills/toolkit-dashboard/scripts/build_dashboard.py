#!/usr/bin/env python3
"""
Build the AI for Developers Toolkit dashboard - a single self-contained index.html
cataloging every skill and agent plus the run history.

Repo layout it scans (AI-for-Developers repo):
    toolkit/skills/<name>/SKILL.md      core, stack-neutral skills
    toolkit/agents/<name>.md            core agents
    packs/<pack>/skills/<name>/SKILL.md stack-specific packs (e.g. packs/dotnet)
    packs/<pack>/agents/<name>.md       (optional)
Run records, catalog-meta.json and the generated dashboard/ live in toolkit/.
The legacy flat layout (skills/ and agents/ at the root) still works.

Usage:
    python3 build_dashboard.py [--root <repo root or toolkit dir>]
(root auto-detected if omitted)
"""
import argparse
import json
import os
import glob
import re
import sys
from datetime import datetime, timezone

# ---- brand / links (edit these) ----
COMMUNITY_URL = "https://www.skool.com/ai-for-dotnet-developers/about"
BRAND_NAME = "AI for Developers Toolkit"
BRAND_SUB = "AI for Developers - skills & agents for any stack"


def parse_frontmatter(text):
    m = re.match(r'^---\s*\n(.*?)\n---\s*\n', text, re.S)
    fm = {}
    if not m:
        return fm
    for line in m.group(1).split('\n'):
        mm = re.match(r'^([A-Za-z_][A-Za-z0-9_]*):\s*(.*)$', line)
        if mm:
            key = mm.group(1).strip()
            val = mm.group(2).strip()
            if len(val) >= 2 and val[0] in '"\'' and val[-1] == val[0]:
                val = val[1:-1]
            fm[key] = val
    return fm


def lead_sentence(desc):
    if not desc:
        return ""
    for marker in ['. Use this', '. Always use', '. Trigger']:
        idx = desc.find(marker)
        if idx != -1:
            return desc[:idx + 1].strip()
    idx = desc.find('. ')
    return (desc[:idx + 1] if idx != -1 else desc).strip()


def read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def load_meta(root):
    p = os.path.join(root, 'catalog-meta.json')
    if os.path.isfile(p):
        try:
            return json.loads(read(p))
        except Exception as e:
            print(f"  warn: could not parse catalog-meta.json: {e}")
    return {}


def apply_meta(item, meta):
    m = meta.get(item['name']) or meta.get(item.get('folder', '')) or {}
    if not item.get('category'):
        item['category'] = m.get('category', 'Uncategorized')
    if not item.get('version'):
        item['version'] = m.get('version', '')
    item['tags'] = m.get('tags', [])
    return item


def resolve_layout(root):
    """Return (home, skill_sources, agent_sources).

    home           - folder holding runs/, catalog-meta.json and dashboard/ (toolkit/)
    skill_sources  - list of (pack, skills_dir)
    agent_sources  - list of (pack, agents_dir)
    Accepts the repo root (contains toolkit/skills) or the toolkit dir itself,
    and falls back to the legacy flat layout (skills/ at the root).
    """
    root = os.path.abspath(root)
    if os.path.isdir(os.path.join(root, 'toolkit', 'skills')):
        repo, home = root, os.path.join(root, 'toolkit')
    elif os.path.isdir(os.path.join(root, 'skills')):
        home = root
        parent = os.path.dirname(root)
        repo = parent if os.path.isdir(os.path.join(parent, 'packs')) else root
    else:
        return None, [], []
    skill_sources = [('core', os.path.join(home, 'skills'))]
    agent_sources = [('core', os.path.join(home, 'agents'))]
    for pack_dir in sorted(glob.glob(os.path.join(repo, 'packs', '*'))):
        if not os.path.isdir(pack_dir):
            continue
        pack = os.path.basename(pack_dir)
        if os.path.isdir(os.path.join(pack_dir, 'skills')):
            skill_sources.append((pack, os.path.join(pack_dir, 'skills')))
        if os.path.isdir(os.path.join(pack_dir, 'agents')):
            agent_sources.append((pack, os.path.join(pack_dir, 'agents')))
    return home, skill_sources, agent_sources


def load_skills(sources, meta):
    items = []
    for pack, sdir in sources:
        if not os.path.isdir(sdir):
            continue
        for name in sorted(os.listdir(sdir)):
            skill_md = os.path.join(sdir, name, 'SKILL.md')
            if not os.path.isfile(skill_md):
                continue
            fm = parse_frontmatter(read(skill_md))
            desc = fm.get('description', '')
            item = {
                'name': fm.get('name', name),
                'folder': name,
                'kind': 'skill',
                'pack': pack,
                'description': desc,
                'lead': lead_sentence(desc),
                'category': fm.get('category', ''),
                'version': fm.get('version', ''),
                'has_usage': os.path.isfile(os.path.join(sdir, name, 'USAGE.md')),
                'has_refs': os.path.isdir(os.path.join(sdir, name, 'references')),
            }
            items.append(apply_meta(item, meta))
    return items


def load_agents(sources, meta):
    items = []
    for pack, adir in sources:
        if not os.path.isdir(adir):
            continue
        for f in sorted(os.listdir(adir)):
            if not f.endswith('.md') or f.endswith('.USAGE.md'):
                continue
            fm = parse_frontmatter(read(os.path.join(adir, f)))
            base = f[:-3]
            desc = fm.get('description', '')
            item = {
                'name': fm.get('name', base),
                'folder': f,
                'kind': 'agent',
                'pack': pack,
                'description': desc,
                'lead': lead_sentence(desc),
                'category': fm.get('category', ''),
                'version': fm.get('version', ''),
                'tools': fm.get('tools', ''),
                'has_usage': os.path.isfile(os.path.join(adir, base + '.USAGE.md')),
                'has_refs': False,
            }
            items.append(apply_meta(item, meta))
    return items


def load_runs(root):
    runs = []
    rdir = os.path.join(root, 'runs')
    if not os.path.isdir(rdir):
        return runs
    for f in sorted(os.listdir(rdir)):
        if not f.endswith('.json'):
            continue
        try:
            rec = json.loads(read(os.path.join(rdir, f)))
            if rec.get('_skip') or not rec.get('tool') or not rec.get('verdict'):
                continue
            runs.append(rec)
        except Exception as e:
            print(f"  warn: could not parse runs/{f}: {e}")
    runs.sort(key=lambda r: r.get('date', ''), reverse=True)
    return runs


def build(root):
    home, skill_sources, agent_sources = resolve_layout(root)
    meta = load_meta(home)
    skills = load_skills(skill_sources, meta)
    agents = load_agents(agent_sources, meta)
    runs = load_runs(home)
    packs = sorted({i['pack'] for i in (skills + agents) if i.get('pack') and i['pack'] != 'core'})

    cats = sorted({i['category'] for i in (skills + agents) if i.get('category')})
    payload = {
        'brand': BRAND_NAME,
        'sub': BRAND_SUB,
        'community': COMMUNITY_URL,
        'skills': skills,
        'agents': agents,
        'runs': runs,
        'categories': cats,
        'packs': packs,
        'generated': datetime.now(timezone.utc).strftime('%B %d, %Y'),
        'counts': {
            'skills': len(skills),
            'agents': len(agents),
            'categories': len(cats),
            'runs': len(runs),
        },
    }

    html = TEMPLATE.replace('/*__PAYLOAD__*/', json.dumps(payload, ensure_ascii=False))
    out_dir = os.path.join(home, 'dashboard')
    os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(out_dir, 'index.html')
    with open(out, 'w', encoding='utf-8') as f:
        f.write(html)

    print(f"{BRAND_NAME} dashboard built.")
    print(f"  skills: {len(skills)} | agents: {len(agents)} | categories: {len(cats)} | runs: {len(runs)}")
    for pack, d in skill_sources + agent_sources:
        if os.path.isdir(d):
            print(f"  scanned [{pack}] {os.path.relpath(d, os.path.dirname(home))}")
    print(f"  -> {out}")
    print(f"  serve:  cd {out_dir} && python3 -m http.server 8080   then open http://localhost:8080")
    return out


TEMPLATE = r"""<!DOCTYPE html>
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
/* header */
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
/* tabs + controls */
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
/* cards */
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
/* runs */
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
  'Architecture':'#7aa2ff','EF Core / Database':'#46d39a','Database':'#46d39a','Database / ORM':'#46d39a','Performance':'#ffa552',
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
  const pack=(it.pack&&it.pack!=='core')?`<span class="pill">${esc(it.pack)} pack</span>`:'';
  const col=catColor(it.category);
  const catBadge=`<span class="badge" style="background:${col}22;color:${col};border:1px solid ${col}55">${esc(it.category||'-')}</span>`;
  return `<div class="card" data-name="${esc((it.name+' '+it.description).toLowerCase())}" data-cat="${esc(it.category)}">
    <div class="top"><h3>${esc(it.name)}</h3>${catBadge}</div>
    <p class="lead">${esc(it.lead||it.description)}</p>
    <div class="full">${esc(it.description)}</div>
    <div class="meta"><span class="badge b-kind">${esc(it.kind)}</span>${pack}${ver}${docs.map(d=>`<span class="pill">${d}</span>`).join('')}</div>
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
    const targets=[...new Set(runs.map(r=>r.target).filter(Boolean))];
    const note=targets.length?`<p class="note">Latest results from running the toolkit against <b style="color:var(--ink)">${targets.map(esc).join(', ')}</b>.</p>`:'';
    html = runs.length ? verdictSummary(runs)+note+runs.map(runHTML).join('')
      : '<div class="empty">No runs recorded yet.</div>';
  } else {
    let items=(view==='skills'?DATA.skills:DATA.agents)
      .filter(it=>(!q||(it.name+' '+it.description+' '+(it.pack||'')).toLowerCase().includes(q))&&(!cat||it.category===cat));
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
</html>"""


def _default_root():
    d = os.path.dirname(os.path.abspath(__file__))
    for _ in range(6):
        if os.path.isdir(os.path.join(d, 'toolkit', 'skills')):
            return d
        if os.path.isdir(os.path.join(d, 'skills')) and os.path.basename(d) != 'toolkit-dashboard':
            return d
        d = os.path.dirname(d)
    return os.getcwd()


if __name__ == '__main__':
    ap = argparse.ArgumentParser()
    ap.add_argument('--root', default=None, help='Repo root or toolkit dir (auto-detected if omitted)')
    args = ap.parse_args()
    root = os.path.abspath(args.root) if args.root else _default_root()
    if resolve_layout(root)[0] is None:
        print(f"error: no toolkit/skills/ or skills/ folder under {root}", file=sys.stderr)
        sys.exit(1)
    build(root)
