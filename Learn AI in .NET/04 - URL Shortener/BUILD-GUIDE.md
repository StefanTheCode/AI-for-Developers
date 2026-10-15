# 04 · URL Shortener — Build Guide

> **Level:** Intermediate · **Time:** 5–8 hours · **You'll need:** .NET 10 SDK, an AI assistant, curl or a `.http` file
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it yourself with AI.

---

## The goal

`POST /shorten` → short code. `GET /{code}` → 302 redirect, served from cache. `/shorten` is rate-limited per IP.

**The real goal:** this is the first project where "it works on my machine" isn't enough. You're designing for traffic, abuse, and races.

---

## Step-by-step

### Step 1 — Entity + unique index

> **Prompt:** "Create a `ShortUrl` entity (Id, Code, OriginalUrl, CreatedAt, Hits) and a DbContext with a UNIQUE index on Code. Explain what the index gives me beyond speed."

**Check yourself:** Two requests insert the same code at the same millisecond. Who stops the duplicate: your C# code or the database?

### Step 2 — Code generator

> **Prompt:** "Write a Base62 short-code generator with length 7. It must be unpredictable. Explain your choice of random source."

The right answer is `RandomNumberGenerator.GetInt32`. If you get `new Random()` or `Guid.NewGuid().ToString()[..7]`, push back.

### Step 3 — Create with URL safety

> **Prompt:** "Implement `CreateAsync(url)`. Only accept absolute http/https URLs, retry on code collision up to 5 times, then save. What attacks does the scheme check prevent?"

### Step 4 — Cache-aside resolve

> **Prompt:** "Implement `ResolveAsync(code)` using `IMemoryCache` with the cache-aside pattern and a 10-minute expiry. Explain when cached data could be stale, and whether that matters here."

### Step 5 — Rate limiting

> **Prompt:** "Add ASP.NET Core's built-in rate limiter: a named fixed-window policy, 5 requests per 10 seconds, partitioned by client IP, returning 429. Apply it ONLY to POST /shorten. Show where `UseRateLimiter` goes in the pipeline."

### Step 6 — Endpoints + stats

Redirect with `Results.Redirect`, and bump the counter with `ExecuteUpdateAsync` (one SQL UPDATE, no tracking).

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| `new Random()` or a GUID substring for codes | Predictable, so someone can enumerate everyone's links | `RandomNumberGenerator` |
| Accept any string as the URL | `javascript:` URLs turn your shortener into an attack vector | `Uri.TryCreate` + scheme allow-list |
| "Check if exists, then insert" and call it safe | Race: both requests pass the check, and the unique index throws → **500** | Catch `DbUpdateException` and retry |
| Partition the limiter by `RemoteIpAddress` behind a proxy | Every user shares the proxy's IP, so one bucket for everyone | Forwarded Headers middleware |
| Cache with no expiry or size limit | Memory grows forever | Expiry + `SizeLimit` |
| Forget `app.UseRateLimiter()` | The policy exists but never runs | Test for the 429 |

### 🔍 Review moment: our own code

In `Program.cs`, every redirect still runs `await service.RecordHitAsync(...)`: **one DB write per click.** The cache saved the read, but not the write.

Is that okay at 10 requests/sec? At 10,000? Ask your AI for two alternatives: in-memory counters flushed by a background service (hello, project 06), or a separate analytics pipeline. Pick one and justify it.

---

## 🔨 Break it on purpose

1. Send 6 `POST /shorten` requests in under 10 seconds. The 6th should get **429**.
2. Shorten `javascript:alert(1)`. You should get 400.
3. Temporarily make the generator return `"aaaaaaa"` every time, then create two links. Does the retry loop kick in? What happens under a real race?
4. Call the same `/{code}` twice with EF logging on. How many SQL queries does each call run?

---

## ✅ Definition of done

- [ ] Create, redirect, and stats all work
- [ ] 429 after the limit, and redirects are never limited
- [ ] Unsafe URL schemes are rejected
- [ ] A duplicate-code race returns a clean response, not a 500
- [ ] You can explain cache-aside and when it serves stale data
- [ ] You made a written decision about the per-redirect DB write
- [ ] You wrote down **one thing the AI got wrong**

## 🚀 Stretch goal

Replace `IMemoryCache` with `HybridCache` (in-memory + Redis) so multiple instances share the cache, and return a `Retry-After` header on 429.

---

**Doing the [October Build Challenge](../../CHALLENGE.md)?** Share your repo and the one thing AI got wrong in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. That's also where the Advanced & Expert levels come with walkthroughs, roadmaps, and someone to ask when you get stuck.
