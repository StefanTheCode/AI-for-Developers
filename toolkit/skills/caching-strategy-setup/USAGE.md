# How to use - Caching Strategy Setup

**What it is.** Adds caching at the right layer (in-process, distributed Redis/Valkey/Memcached, two-level, or HTTP/output caching) with the parts people forget: keys, expiration, stampede protection, invalidation, size bounds, and Redis-down behavior. It detects your stack first: HybridCache / IMemoryCache / IDistributedCache / output caching (.NET), lru-cache / keyv / cache-manager / ioredis (Node), functools / cachetools / redis-py / Django cache (Python), Spring Cache with Caffeine or Redis (Java/Kotlin), ristretto / singleflight / groupcache / go-redis (Go).

**When to reach for it.** Expensive repeated reads, scaling reads across instances, slow downstream calls, or fixing a cache stampede.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Use HybridCache here with a 10-minute expiry."
- "Add Redis caching to this Express product endpoint with invalidation on update."
- "My `lru_cache` on an async function behaves weirdly - fix it."
- "Add `@Cacheable` with Caffeine and evict on save."
- "I get a cache stampede on expiry in my Go service - fix it."

**What you get.** A cache plan table (data, layer, key, TTL, invalidation, stampede protection), the code for setup, cached reads, and invalidation on writes in your project's idiom, and a list of things to verify (hit rate, memory bound, failure mode).

**Get the best out of it.** Tell it single-instance vs multi-instance and how stale data can be. Always ask for the invalidation plan - that's the hard part. Verify hit rate after shipping. Pairs with `serialization-optimizer` (distributed cache payloads) and `orm-query-optimizer` (fix the query before caching it).

**Won't do.** It won't cache user-specific or secret data in a shared cache without scoped keys, and it will push back on caching things that shouldn't be cached.
