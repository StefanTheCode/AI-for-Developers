# How to use - Hot-Path Profiler Assistant

**What it is.** Guides profiling your app to find the real hot paths before you optimize - which tool to use for your stack, how to capture under realistic load, and how to read the results. It covers .NET (dotnet-counters/trace/gcdump, PerfView, VS/Rider), Node.js (`--cpu-prof`, DevTools, clinic.js, 0x), Python (py-spy, cProfile, scalene, pyinstrument), JVM (async-profiler, JFR), Go (pprof, `go tool trace`), and perf flame graphs.

**When to reach for it.** Something's slow, CPU or memory is high, or latency spikes - and you don't yet know where the time goes.

**How to use it.** Point it at the project (path or GitHub URL) and describe the symptom, or give it a profile to interpret. Example prompts:
- "What's slow in my ASP.NET Core app - how do I find it?"
- "Profile this Express endpoint under load and tell me what to fix."
- "py-spy or cProfile for my Django app in production?"
- "Set up async-profiler for our Spring Boot pods."
- "Read this pprof CPU profile and tell me what to optimize."

**Get the best out of it.** Run it before any micro-optimization - it stops you tuning cold code. Capture under realistic load, after warm-up. Share the profile file or flame graph if you have one.

**What you get.** The detected stack and symptom class, an exact capture plan (commands, profile type, load, duration), an interpretation of your profile with the widest frames you own, and a hand-off to `benchmark-setup`, `memory-allocation-analyzer`, or `gc-pressure-auditor`.

**Won't do.** It guides profiling; it can't run a profiler on your machine for you - it tells you exactly what to run.
