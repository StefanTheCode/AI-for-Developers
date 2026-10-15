---
name: caching-strategy-setup
description: Add the right caching to an app in any stack - in-process, distributed (Redis/Valkey/Memcached), two-level, or HTTP/output caching - with correct keys, expiration, stampede protection, and invalidation. Covers HybridCache / IMemoryCache / IDistributedCache / output caching in .NET, lru-cache / keyv / cache-manager / ioredis in Node, functools / cachetools / redis-py / Django cache in Python, Spring Cache with Caffeine or Redis in Java/Kotlin, and ristretto / groupcache / singleflight / go-redis in Go. Use this skill whenever the user wants caching, to cache a query/response, Redis caching, @Cacheable, lru_cache, HybridCache, cache-aside, output caching, or asks "how do I cache this / add caching / avoid cache stampede / invalidate this cache". Always use this skill for caching requests; it picks the right layer and wires expiration + invalidation correctly.
category: Performance
version: 1.0.0
---

# Caching Strategy Setup

Add caching at the right layer with the parts people forget: sane keys, expiration policy, stampede protection, and an invalidation plan.

## Input - point it at your project
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL**. Find hot reads worth caching (repeated queries, expensive computations, slow downstream calls) and any existing cache usage. Report what you found before adding anything.

## Step 1 - detect the stack and existing cache

| Manifest | Look for | Grep |
|---|---|---|
| `*.csproj` | `Microsoft.Extensions.Caching.*`, `StackExchange.Redis`, `ZiggyCreatures.FusionCache` | `IMemoryCache\|IDistributedCache\|HybridCache\|OutputCache` |
| `package.json` | `lru-cache`, `keyv`, `cache-manager`, `ioredis`, `redis` | `new LRUCache\|createClient\|new Redis\|cache.get` |
| `pyproject.toml`, `requirements*.txt` | `cachetools`, `redis`, `aiocache`, `django` | `lru_cache\|@cache\|TTLCache\|redis\.\|cache\.get` |
| `pom.xml`, `build.gradle(.kts)` | `spring-boot-starter-cache`, `caffeine`, `spring-boot-starter-data-redis` | `@Cacheable\|@CacheEvict\|Caffeine\.newBuilder` |
| `go.mod` | `ristretto`, `groupcache`, `golang.org/x/sync/singleflight`, `go-redis` | `ristretto\|singleflight\|redis.NewClient` |

Also establish: **single instance or many** (drives in-process vs distributed), **how stale data may be**, and **what writes change the cached data**.

## Step 2 - pick the layer (universal)
- **In-process (L1)** - fastest, per instance, lost on restart, not shared. Good for hot, small, rarely-changing data or a single instance.
- **Distributed (L2: Redis / Valkey / Memcached)** - shared across instances, survives app restarts, costs a network hop and serialization.
- **Two-level (L1 + L2)** - L1 for hot keys, L2 for sharing; needs a way to invalidate L1 on other nodes (pub/sub or short L1 TTL).
- **HTTP / output caching** - cache whole responses at the edge/reverse proxy/framework with `Cache-Control` + `ETag`. Often the biggest win for public GETs.

## The parts people forget (universal checklist)
1. **Stampede / thundering herd** - on expiry, many requests recompute at once. Coalesce concurrent misses per key (single-flight), add TTL jitter, or refresh ahead of expiry.
2. **Keys** - stable, namespaced, versioned, and include every input that changes the result (`v2:product:{id}:{culture}`). Missing an input = serving the wrong data.
3. **Expiration** - every entry has an absolute TTL; sliding only where access-based retention is intended. Don't cache forever.
4. **Invalidation** - the hard part. On write, evict or refresh affected keys; group with tags or a version prefix for bulk eviction. Evict after the DB commit, not before.
5. **Scope and security** - never put user-specific or secret data in a shared cache without the user/tenant in the key.
6. **Negative caching** - cache "not found" briefly so missing keys don't hammer the DB.
7. **Size bounds** - in-process caches need a max size/eviction policy, or they become a memory leak.
8. **Failure mode** - if Redis is down, fall through to the source with a short timeout; the cache must not become a hard dependency.
9. **Serialization cost** - distributed caches serialize; cache DTOs, not ORM entities with lazy relations (see `serialization-optimizer`).

## C# / .NET
- **`HybridCache` (.NET 9+)** - L1 memory + optional L2 `IDistributedCache`, **built-in stampede protection**, tag-based eviction (`RemoveByTagAsync`). Prefer it where available:
```csharp
builder.Services.AddHybridCache();
// optional L2: builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);

public class ProductService(HybridCache cache, IProductRepo repo)
{
    public ValueTask<Product> GetAsync(int id, CancellationToken ct) =>
        cache.GetOrCreateAsync($"product:{id}",
            factory: async c => await repo.GetAsync(id, c),
            options: new() { Expiration = TimeSpan.FromMinutes(10), LocalCacheExpiration = TimeSpan.FromMinutes(2) },
            tags: ["products"],
            cancellationToken: ct);

    public ValueTask InvalidateAsync(int id, CancellationToken ct) => cache.RemoveAsync($"product:{id}", ct);
}
```
- **`IMemoryCache`** - single instance; `GetOrCreateAsync` does **not** prevent stampedes - add a per-key `SemaphoreSlim` or use LazyCache / FusionCache. Set `SizeLimit` + entry `Size` to bound memory.
- **`IDistributedCache` (Redis)** - shared, but byte-array API with no stampede protection; wrap it or use HybridCache on top.
- **Output caching** - `AddOutputCache()` + `.CacheOutput(...)` on endpoints, with tags and `EvictByTagAsync` on writes.

## TypeScript / Node
- In-process: `lru-cache` with `max` and `ttl` (and `fetchMethod` for async loading with built-in in-flight dedup). Avoid an unbounded `Map`.
- Distributed: `ioredis` or `redis` (node-redis); `keyv` or `cache-manager` for a store-agnostic API. Use `SET key value EX ttl`; batch with `MGET`/pipelines.
- Stampede: keep a `Map<string, Promise<T>>` of in-flight loads so concurrent misses await the same promise; delete it in `finally`.
- HTTP: `Cache-Control`/`ETag` headers (Express/Fastify), or a CDN in front.

## Python
- `functools.lru_cache` / `functools.cache` - pure, sync functions only, unbounded with `cache`, no TTL; **never** decorate an `async def` (it caches the coroutine object, which can be awaited only once).
- `cachetools.TTLCache(maxsize, ttl)` with `@cached(cache, lock=threading.Lock())` for TTL + bounds; `cachetools` has no async support - use `aiocache` or a small dict-of-futures for asyncio.
- Distributed: `redis-py` (`redis.asyncio` for async) with `set(key, value, ex=ttl)`.
- Django: cache framework (`django.core.cache` with Redis backend), `cache.get_or_set`, `@cache_page` for views; delete keys in `post_save`/`post_delete` handlers or after the write.

## Java / Kotlin (Spring)
- Spring Cache abstraction: `@EnableCaching`, `@Cacheable("products")`, `@CachePut`, `@CacheEvict` (on the write path), backed by **Caffeine** (in-process: `maximumSize`, `expireAfterWrite`, `refreshAfterWrite`) or **Redis** (`RedisCacheManager` with per-cache TTLs).
- Stampede: Caffeine's `cache.get(key, loader)` computes once per key per node; `@Cacheable(sync = true)` gets the same behavior through the abstraction.
- Watch for self-invocation: `@Cacheable` on a method called from the same class bypasses the proxy and never caches.
- Key with SpEL (`key = "#id + ':' + #locale"`) when the default key misses an input.

## Go
- In-process: `ristretto` (bounded by cost, high hit ratio, TTL via `SetWithTTL`); `hashicorp/golang-lru` for simple LRU.
- Stampede: `golang.org/x/sync/singleflight` (`group.Do(key, fn)`) around the loader.
- Distributed: `go-redis` with `Set(ctx, key, val, ttl)`; `groupcache` for a peer-to-peer, fill-once cache (no TTL or explicit invalidation by design - fits immutable data).

## Output
```
## Summary
<stack + current cache usage; what is worth caching and at which layer>

## Cache plan
| Data | Layer | Key | TTL | Invalidation | Stampede protection |
|---|---|---|---|---|---|

## Code
<setup + cached read path + invalidation on write, in the project's idiom>

## Verify on your side
- <hit-rate metric, memory bound, Redis-down behavior, staleness acceptance>
```

## Principles
- Cache to cut **expensive, repeated, tolerably-stale** reads - not everything.
- Every cache entry needs an expiration and an invalidation story before it ships.
- Coalesce concurrent misses; a cache without stampede protection can amplify an outage.
- Measure hit rate; a low-hit cache is just overhead and another failure mode.

## How to use it & best prompts
"Cache this expensive query", "add Redis caching to this Node service", "use HybridCache here", "add @Cacheable with Caffeine and evict on update", "I'm getting a cache stampede on expiry - fix it", "how do I invalidate this cache on update". Pairs with `serialization-optimizer` and `orm-query-optimizer`.
