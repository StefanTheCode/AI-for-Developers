# AI for Developers Toolkit - Dashboard

A single self-contained page (`index.html`) that catalogs every skill and agent in the toolkit and shows the latest run results. No dependencies, no build step - the data is embedded.

## View it locally
- Double-click `index.html`, **or**
- `cd` into this folder and run `python -m http.server 8080`, then open `http://localhost:8080`.

## Host it (stable link for the weekly update)

**Option A - GitHub Pages (recommended for a permanent link):**
1. Put `index.html` in a GitHub repo (at the repo root, or in a `/docs` folder).
2. Repo **Settings → Pages → Build from a branch →** pick the branch and `/ (root)` or `/docs`.
3. Your link: `https://<your-username>.github.io/<repo>/`
4. Each week: regenerate `index.html`, commit, push. The same link updates automatically.

**Option B - Netlify Drop (fastest):**
Drag this whole folder onto https://app.netlify.com/drop. You get an instant public URL.

**Option C - Zip:**
Use `dist/dashboard.zip` and upload/share it directly.

## Regenerate (after adding skills/agents or new runs)

**With .NET (recommended - no extra tools, you already have the SDK):**
```
dotnet run --project dashboard-generator
```

**Or with Python (fallback):**
```
python toolkit/skills/toolkit-dashboard/scripts/build_dashboard.py --root .
```

Both auto-detect the toolkit root and write `dashboard/index.html`. They produce the exact same output - use whichever you prefer.

## Extras in this folder
- `linkedin-4x5.html` - a 1080x1350 (4:5) results card sized for a LinkedIn post. Open it and screenshot the card.
