#!/usr/bin/env python3
"""
Seed anonymized run records for the showcase.
Target codename: "Project Atlas" - a real .NET 10 production platform (client withheld).
Findings are real (gathered from the actual codebase) but stripped of any identifying
project/vendor/domain names. Run: python3 seed_showcase_runs.py [--root <repo root or toolkit dir>]
"""
import argparse, json, os, shutil

TARGET = "Project Atlas"
DATE = "2026-06-16"

# (tool, type, verdict, summary, [ (sev,title,location), ... ])
RUNS = [
 # Architecture
 ("clean-architecture-scaffolder","skill","generated","Detected Vertical Slice; generated a new feature slice matching the house style (Carter module + pipeline + Result).",[]),
 ("cqrs-setup","skill","pass","Already CQRS via a custom handler pipeline + validation middleware (~75 handlers). Reviewed; offered MediatR-behavior parity as optional.",[]),
 ("result-pattern-scaffolder","skill","pass","Result/OneOf already pervasive (96 usages). Confirmed consistent; suggested centralizing HTTP mapping in one place.",[]),
 ("api-endpoint-scaffolder","skill","generated","75 minimal-API endpoint modules; generated a new REPR endpoint with TypedResults matching their conventions.",[]),
 ("ddd-aggregate-generator","skill","issues","Domain leans anemic (logic lives in handlers); demonstrated enriching one aggregate + extracting value objects.",[("medium","Anemic domain model - logic outside entities","Domain")]),
 ("modular-monolith-generator","skill","pass","Multi-project solution; audited module boundaries and recommended NetArchTest guardrails.",[]),
 ("microservice-template-generator","skill","generated","Generated a service template matching the platform's health/OTel/logging setup.",[]),
 # EF / DB
 ("orm-query-optimizer","skill","issues","In-memory filtering after Include + missing AsNoTracking in a list query; AsNoTracking on only 21/44 read queries.",[("medium","Over-fetch: Include loads all rows, filtered in memory","Queries/List.cs"),("medium","Missing AsNoTracking on read query","Queries/List.cs")]),
 ("db-migration-reviewer","skill","pass","Reviewed recent migrations; no destructive operations in the latest set. Flagged add-non-null guidance for future changes.",[]),
 ("db-index-advisor","skill","issues","18 indexes present; found 3 query paths filtering/sorting on unindexed columns. (Verify against schema.)",[("medium","Composite index missing for a frequent filter+sort","Queries")]),
 ("sql-orm-converter","skill","pass","Almost no raw SQL (1 FromSql). Converted it to LINQ as a demo - low applicability, which is healthy.",[]),
 ("orm-config-auditor","skill","issues","EnableSensitiveDataLogging unconditional; no DbContext pooling; tracking default on a read-heavy context.",[("high","EnableSensitiveDataLogging() logs parameter values in all envs","DbContext setup"),("low","No pooling for a high-traffic API","DbContext setup")]),
 ("temporal-tables-setup","skill","generated","No history/auditing present; generated temporal-table config for one entity as an example.",[]),
 ("specification-pattern-generator","skill","pass","No spec pattern (handlers hold criteria); demoed extracting one reusable spec. Optional given their style.",[]),
 ("db-resiliency-setup","skill","issues","No EnableRetryOnFailure; added transient-fault retry + the execution-strategy transaction pattern.",[("medium","No connection resiliency on a networked Postgres","DbContext setup")]),
 # Performance
 ("async-await-auditor","skill","clean","No .Result/.Wait()/GetResult()/async void anywhere; CancellationToken threaded throughout. Honest clean pass.",[]),
 ("benchmark-setup","skill","generated","No benchmarks in the repo; generated a BenchmarkDotNet harness for a hot mapping path.",[]),
 ("memory-allocation-analyzer","skill","pass","Scanned hot handlers; few avoidable allocations (some LINQ projections). Low GC risk; minor suggestions only.",[]),
 ("span-memory-refactor","skill","pass","No Substring/parse hot loops in core code - nothing worth refactoring. Honest low-applicability pass.",[]),
 ("caching-strategy-setup","skill","issues","No caching layer present; identified repeated configuration/lookup reads that are good HybridCache candidates.",[("medium","Hot read paths recomputed every request - cacheable","Queries")]),
 ("gc-pressure-auditor","skill","pass","Server GC default for ASP.NET; no LOH red flags in config. Recommended dotnet-counters to watch under load. (Verify.)",[]),
 ("hotpath-profiler-assistant","skill","generated","Produced a profiling plan (dotnet-trace + counters) for the slowest endpoint candidate.",[]),
 ("serialization-optimizer","skill","issues","14 System.Text.Json sites; flagged per-call options creation and recommended reuse + source generation.",[("medium","JsonSerializerOptions created per call - reuse a static instance","serialization")]),
 # Observability
 ("opentelemetry-setup","skill","issues","Metrics wired but no distributed tracing and no OTel log export, despite an event bus + inter-service HTTP.",[("high","No .WithTracing() - requests can't be followed across services","Program.cs"),("low","No deployment.environment resource attribute","Program.cs")]),
 ("structured-logging-setup","skill","issues","Serilog present but no request logging and logs aren't trace-correlated.",[("medium","No UseSerilogRequestLogging; logs lack TraceId","logging setup")]),
 ("healthchecks-setup","skill","issues","Health checks present but no liveness/readiness split - a dependency blip can restart pods.",[("medium","No live/ready split","health setup")]),
 ("distributed-tracing-diagnostics","skill","critical","No tracing registered at all and the event-bus hops are uninstrumented - a request cannot be traced across services.",[("high","Tracing absent; messaging context not propagated","Program.cs / event bus")]),
 ("metrics-dashboard-generator","skill","generated","Prometheus already enabled; generated a Grafana RED dashboard JSON + scrape config for the service.",[]),
 ("correlation-id-middleware","skill","issues","No correlation id; added middleware + log enrichment + downstream propagation.",[("medium","No correlation id flowing through logs/calls","pipeline")]),
 # Testing / Quality
 ("unit-test-generator","skill","generated","Generated validator + handler tests for a create command; routed DB-existence cases to integration tests.",[]),
 ("integration-test-setup","skill","issues","Solid Testcontainers + WebApplicationFactory setup; 3 upgrades found.",[("medium","EnsureDbCreated instead of MigrateAsync - migrations untested","IntegrationTestFactory"),("medium","postgres:latest image unpinned","IntegrationTestFactory")]),
 ("test-coverage-gap-finder","skill","issues","16 test files across a large codebase; flagged untested command handlers that contain real business logic.",[("medium","Command handlers with logic lack tests","Commands")]),
 ("mutation-testing-setup","skill","generated","No Stryker present; generated stryker-config scoped to the core project and explained reading survivors.",[]),
 ("test-data-builder-generator","skill","generated","Repeated entity setup in tests; generated fluent builders + object mothers to cut boilerplate.",[]),
 ("architecture-test-generator","skill","generated","No architecture tests; generated guardrails (Vertical Slice rules, handlers sealed, no infra in endpoints).",[]),
 # Security
 ("jwt-auth-setup","skill","issues","Sound dual-scheme (OIDC + machine-to-machine) design; a few hardening items.",[("medium","RequireHttpsMetadata=false - make environment-conditional","auth setup"),("low","Token validation flags implicit","auth setup")]),
 ("dependency-vuln-scanner","skill","pass","31 packages; recommended wiring `dotnet list package --vulnerable` into CI (full restore not run here). Verify.",[]),
 ("secrets-config-auditor","skill","issues","Sensitive-data logging enabled unconditionally; recommended user-secrets/Key Vault and gating behind Development.",[("high","Parameter values logged in all environments","DbContext setup")]),
 ("authorization-setup","skill","critical","Named policies exist but only 1 of 75 endpoints requires authorization and there's no fallback policy.",[("critical","Most endpoints not requiring auth; no FallbackPolicy (verify gateway)","endpoints")]),
 # DevOps
 ("dockerfile-generator","skill","generated","No Dockerfile found (compose-based build); generated a multi-stage chiseled, non-root Dockerfile.",[]),
 ("cicd-pipeline-generator","skill","pass","A CI pipeline already exists; reviewed and suggested adding a coverage step + a --vulnerable gate.",[]),
 ("dependency-analyzer","skill","issues","31 packages with no Central Package Management; recommended Directory.Packages.props to prevent version drift.",[("medium","No Central Package Management across projects","solution")]),
 ("typed-config-generator","skill","pass","Options pattern already used (typed config, no magic-string lookups). Confirmed good; suggested ValidateOnStart.",[]),
 ("toolkit-dashboard","skill","generated","Generated the live dashboard cataloging all skills/agents and this run history.",[]),
 # Agents
 ("architecture-reviewer","agent","pass","Healthy Vertical Slice: thin startup, one feature per module, validation as middleware, explicit Result types.",[("medium","Inconsistent query hygiene (in-memory filtering, AsNoTracking gaps)","Features")]),
 ("api-security-auditor","agent","critical","Two urgent items plus broad CORS.",[("critical","Authorization coverage - 1/75 endpoints, no fallback policy (verify)","endpoints"),("critical","EnableSensitiveDataLogging() unconditional","DbContext setup"),("medium","CORS AllowAnyOrigin() on an authenticated API","CORS setup")]),
 ("code-reviewer","agent","pass","Reviewed a create-command handler - approve with comments. Clean separation, OneOf, AsNoTracking, ct threaded.",[("low","Two existence queries could be combined","Commands/Create.cs")]),
 ("db-performance-auditor","agent","issues","Data layer is mostly solid; consolidated EF findings into a ranked plan.",[("medium","In-memory filtering + AsNoTracking inconsistency","Features"),("medium","No connection resiliency / no caching on hot reads","data layer")]),
 ("observability-gap-finder","agent","critical","Service cannot be fully diagnosed in production as wired.",[("high","No distributed tracing","Program.cs"),("medium","No correlation id; logs not trace-correlated","pipeline"),("medium","No liveness/readiness split","health setup")]),
 ("framework-upgrade-agent","agent","pass","Already on the current .NET (net10.0). No upgrade needed; noted readiness for the next LTS.",[]),
 ("legacy-modernization-assistant","agent","pass","Modern codebase (current .NET, Vertical Slice, DI, async throughout). No legacy debt; only minor modernization notes.",[]),
]

def main(root):
    rdir = os.path.join(root, "runs")
    os.makedirs(rdir, exist_ok=True)
    for tool, typ, verdict, summary, findings in RUNS:
        rec = {
            "id": f"{DATE}-{tool}-atlas",
            "tool": tool, "type": typ, "target": TARGET, "date": DATE,
            "verdict": verdict, "summary": summary,
            "findings": [{"severity": s, "title": t, "location": loc} for (s, t, loc) in findings],
        }
        with open(os.path.join(rdir, f"{DATE}-{tool}-atlas.json"), "w", encoding="utf-8") as f:
            json.dump(rec, f, ensure_ascii=False, indent=2)
    print(f"Wrote {len(RUNS)} anonymized run records to {rdir}")

def _home(root):
    """runs/ lives in toolkit/. Accept the repo root, the toolkit dir, or auto-detect from this script."""
    if root is None:
        d = os.path.dirname(os.path.abspath(__file__))
        for _ in range(6):
            if os.path.isdir(os.path.join(d, "toolkit", "skills")):
                return os.path.join(d, "toolkit")
            if os.path.isdir(os.path.join(d, "skills")):
                return d
            d = os.path.dirname(d)
        return os.getcwd()
    root = os.path.abspath(root)
    if os.path.isdir(os.path.join(root, "toolkit", "skills")):
        return os.path.join(root, "toolkit")
    return root

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=None, help="Repo root or toolkit dir (auto-detected if omitted)")
    a = ap.parse_args(); main(_home(a.root))
