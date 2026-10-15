# How to use - GC Pressure Auditor

**What it is.** Diagnoses garbage-collection and memory pressure on any managed runtime and recommends config, allocation, retention, and pooling fixes. It covers .NET (Server vs Workstation GC, LOH, Gen2 churn, DATAS, heap limits), the JVM (G1/ZGC, container heap sizing, humongous objects, off-heap memory), Go (GOGC, GOMEMLIMIT, escape analysis), Node/V8 (old space, scavenges, heap snapshots), and CPython (refcounting + cyclic gc, fragmentation, leaks).

**When to reach for it.** GC pauses, high CPU in GC, steady memory growth, OOM kills in containers, "JavaScript heap out of memory", or RSS that never goes down.

**How to use it.** Point it at the project (path or GitHub URL) and/or paste the data you have. Example prompts:
- "Why is the GC eating CPU in this ASP.NET Core service? Here are the dotnet-counters numbers."
- "Our Spring Boot pods get OOM-killed but the heap looks fine - what's wrong?"
- "G1 or ZGC for this latency-sensitive service?"
- "Tune GOGC and GOMEMLIMIT for a 512Mi pod."
- "Node keeps crashing with heap out of memory - help me find the leak."
- "Our gunicorn workers grow to 2 GB over a day."

**Get the best out of it.** Share real data: `dotnet-counters`, GC logs/JFR, `gctrace` output, heap snapshots, RSS graphs, and your container limits. It tunes to evidence. Pairs with `memory-allocation-analyzer` for the allocation sources behind the pressure.

**What you get.** The detected runtime and config, a diagnosis (allocation rate vs retention vs large objects vs heap sizing), numbered findings with fixes, recommended settings for your runtime, and what to measure next.

**Won't do.** It won't tune blind - without data it tells you exactly what to capture rather than guessing.
