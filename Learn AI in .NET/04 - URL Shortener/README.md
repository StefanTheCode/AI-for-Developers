# 04 · URL Shortener

> **Intermediate project** · Focus: **Caching** + **Rate limiting**.

A tiny URL shortener: `POST` a long URL, get back a short code, then follow
`/{code}` to be redirected. Redirects are served from an **in-memory cache**, and
the create endpoint is protected by a per-IP **rate limiter**.

---

## What you'll learn

| Concept | Where to look |
|---|---|
| Cache-aside pattern with `IMemoryCache` | [`Services/UrlShortenerService.cs`](src/UrlShortener/Services/UrlShortenerService.cs) |
| Built-in ASP.NET Core **rate limiting** (fixed window, per IP) | [`Program.cs`](src/UrlShortener/Program.cs) |
| Cryptographically-random short codes | [`Services/ShortCodeGenerator.cs`](src/UrlShortener/Services/ShortCodeGenerator.cs) |
| Unique index for fast, safe lookups | [`Data/UrlShortenerDbContext.cs`](src/UrlShortener/Data/UrlShortenerDbContext.cs) |
| Blocking unsafe redirect schemes (`javascript:` etc.) | [`Services/UrlShortenerService.cs`](src/UrlShortener/Services/UrlShortenerService.cs) |
| `ExecuteUpdateAsync` for a cheap counter bump | [`Services/UrlShortenerService.cs`](src/UrlShortener/Services/UrlShortenerService.cs) |

---

## Run it

```bash
cd "04 - URL Shortener"
dotnet run --project src/UrlShortener
```

Starts on `http://localhost:5090`. Use [`src/UrlShortener/UrlShortener.http`](src/UrlShortener/UrlShortener.http)
to try it, or curl:

```bash
# create
curl -X POST http://localhost:5090/shorten -H "Content-Type: application/json" -d '{"url":"https://thecodeman.net"}'
# -> { "code":"aB3xK9q", "shortUrl":"http://localhost:5090/aB3xK9q", ... }

# follow (‑L follows the 302 redirect)
curl -L http://localhost:5090/aB3xK9q

# stats
curl http://localhost:5090/api/stats/aB3xK9q
```

---

## The two lessons, in one paragraph each

**Caching.** A short link is written once but read many times. Reading the
database on every redirect is wasteful, so `UrlShortenerService.ResolveAsync`
checks `IMemoryCache` first and only falls back to the DB on a miss (then caches
the result). This "cache-aside" pattern is the workhorse of read-heavy APIs. The
trade-off to understand: cached data can be **stale** — here that's fine because
a code's target never changes.

**Rate limiting.** The `/shorten` endpoint is a write and an easy spam target, so
`AddRateLimiter` installs a **fixed-window** limiter — 5 requests per 10 seconds,
**partitioned by client IP** so one abuser can't block everyone. The 6th request
in a window gets **HTTP 429**. Redirects are deliberately left unlimited.

> 🔒 **Security note:** the service only accepts absolute `http`/`https` URLs.
> That blocks `javascript:` and other schemes that turn a "shortener" into an
> attack vector (open-redirect / stored-XSS style abuse).

---

## 🤖 Try these prompts with your AI assistant

**Swap the cache for Redis (distributed):**
> "Replace `IMemoryCache` with a distributed cache (`IDistributedCache` + Redis)
> so multiple instances share it. Keep the cache-aside logic in
> `UrlShortenerService` and explain the serialization + expiry choices."

**Tighten the limits:**
> "Add a second, stricter global rate limiter (100 req/min per IP across ALL
> endpoints) on top of the per-endpoint one. Show me how the two compose and how
> to return a `Retry-After` header on 429."

**Practise the review skill:**
> "Review `ResolveAsync` for a cache stampede: if 1,000 requests hit an uncached
> code at once, what happens? Propose a fix and explain the trade-offs before code."

**Add a feature:**
> "Let clients request a custom alias (`POST /shorten { url, alias }`). Validate
> it, enforce uniqueness via the existing index, and 409 on conflict. Plan first."

> 💡 **The mindset:** ask for a *plan before edits*, give *outcomes + constraints*,
> and make sure you understand what the cache can return that's stale.

---

## Next

Move on to **[05 · Blog API with Auth](../05%20-%20Blog%20API%20with%20Auth)** — JWT auth, validation + tests.
