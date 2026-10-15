#!/usr/bin/env python3
"""Build the branded AI for Developers Toolkit - Prompt Pack (PDF). Requires reportlab.
Writes dist/TheCodeMan-AI-Toolkit-Prompt-Pack.pdf inside toolkit/."""
import os
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib.colors import HexColor
from reportlab.platypus import BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer, FrameBreak
from reportlab.lib.styles import ParagraphStyle

BG = HexColor('#150f29'); CARD = HexColor('#241c47'); LINE = HexColor('#3a2f6b')
INK = HexColor('#ECE9FF'); MUTED = HexColor('#A99FD6'); YELLOW = HexColor('#FFD23F')

# Prompts deliberately mix stacks: every core tool detects the stack itself.
DATA = [
 ("Architecture", [
   ("clean-architecture-scaffolder", "Scaffold a Clean Architecture solution with a CQRS sample feature."),
   ("cqrs-setup", "Add CQRS to this NestJS app with a validation + transaction pipeline."),
   ("result-pattern-scaffolder", "Add a Result pattern and convert these handlers off exceptions."),
   ("api-endpoint-scaffolder", "Add a create-order endpoint with validation, in this project's style."),
   ("ddd-aggregate-generator", "Model an Order aggregate with these invariants and extract value objects."),
   ("modular-monolith-generator", "Structure this as a modular monolith and add a guardrail test."),
   ("microservice-template-generator", "Scaffold a new service matching our setup: health, OTel, structured logs, Docker."),
   ("crud-vs-event-sourcing", "Should our Payments module be CRUD or event-sourced?"),
 ]),
 ("Database / ORM", [
   ("orm-query-optimizer", "Optimize the queries in src/orders - find N+1s and over-fetching."),
   ("db-migration-reviewer", "Review this migration before I deploy it - will it lose data?"),
   ("db-index-advisor", "What indexes does this project need? Design a composite for customer+status."),
   ("sql-orm-converter", "Convert this SQL to SQLAlchemy (or EF Core LINQ)."),
   ("orm-config-auditor", "Review my ORM and entity/model configuration."),
   ("temporal-tables-setup", "Add history tracking to my Order table."),
   ("specification-pattern-generator", "Dedupe these repeated queries into specifications."),
   ("db-resiliency-setup", "Add connection retry and make my transaction safe to retry."),
 ]),
 ("Performance", [
   ("async-await-auditor", "Audit this project for async/await problems and blocking calls."),
   ("benchmark-setup", "Benchmark these two implementations, with allocations."),
   ("memory-allocation-analyzer", "Reduce allocations in this hot handler."),
   ("caching-strategy-setup", "Cache this expensive query with an invalidation plan."),
   ("gc-pressure-auditor", "Why is the GC eating CPU in this service?"),
   ("hotpath-profiler-assistant", "What's slow in this endpoint and how do I capture a profile?"),
   ("serialization-optimizer", "Speed up my JSON serialization on the hot path."),
 ]),
 ("Observability", [
   ("opentelemetry-setup", "Add OpenTelemetry tracing and metrics, exporting to OTLP."),
   ("structured-logging-setup", "Set up JSON logs with request logging and trace correlation."),
   ("healthchecks-setup", "Add liveness and readiness checks with DB and Redis."),
   ("distributed-tracing-diagnostics", "My traces don't connect across services - find the break."),
   ("metrics-dashboard-generator", "Generate a Grafana RED dashboard for this service."),
   ("correlation-id-middleware", "Add a correlation id flowing through logs and downstream calls."),
 ]),
 ("Testing", [
   ("unit-test-generator", "Write unit tests for OrderService."),
   ("integration-test-setup", "Set up integration tests with Testcontainers against Postgres."),
   ("test-coverage-gap-finder", "Find untested business logic and tell me what to write."),
   ("mutation-testing-setup", "Set up mutation testing and explain the surviving mutants."),
   ("test-data-builder-generator", "Generate test data builders for my domain types."),
   ("architecture-test-generator", "Add architecture tests to enforce my layers."),
 ]),
 ("Security", [
   ("jwt-auth-setup", "Add JWT auth with revocable refresh tokens and policy authorization."),
   ("dependency-vuln-scanner", "Scan my project for vulnerable dependencies."),
   ("secrets-config-auditor", "Do I have any hardcoded secrets or unsafe config?"),
   ("authorization-setup", "Design authorization policies and stop IDOR on these endpoints."),
 ]),
 ("DevOps", [
   ("dockerfile-generator", "Dockerize this app with a small multi-stage, non-root build."),
   ("cicd-pipeline-generator", "Set up GitHub Actions with tests, coverage and a vuln scan."),
   ("dependency-analyzer", "Fix version drift and centralize dependency versions."),
   ("typed-config-generator", "Convert this config to typed, validated settings that fail fast at startup."),
 ]),
 ("Agents (whole-repo)", [
   ("code-reviewer", "Review my changes on this branch before I merge."),
   ("architecture-reviewer", "Review the architecture of this solution."),
   ("api-security-auditor", "Run an OWASP security audit on this repo."),
   ("db-performance-auditor", "Audit this repo's database performance."),
   ("observability-gap-finder", "Can I debug this service in production? Find the gaps."),
   ("legacy-modernization-assistant", "Assess this legacy app and give me a modernization roadmap."),
   ("framework-upgrade-agent", "Upgrade this solution to the latest LTS of its framework."),
 ]),
 ("AI Tooling", [
   ("codebase-health-dashboard", "Run a health check on this repo and give me a score."),
   ("toolkit-dashboard", "Open the dashboard."),
 ]),
 (".NET pack", [
   ("span-memory-refactor", "Make this parser allocation-free with Span."),
   ("legacy-package-migrator", "Migrate these legacy NuGet packages to supported replacements."),
   ("adonet-data-access-auditor", "Audit this raw ADO.NET data access for leaks and injection."),
   ("winforms-maintainer", "Help me safely fix and modernize this WinForms screen."),
 ]),
]

st_h1 = ParagraphStyle('h1', textColor=INK, fontName='Helvetica-Bold', fontSize=30, leading=34)
st_sub = ParagraphStyle('sub', textColor=MUTED, fontName='Helvetica', fontSize=12.5, leading=18)
st_eyebrow = ParagraphStyle('eb', textColor=YELLOW, fontName='Helvetica-Bold', fontSize=11, leading=14)
st_cat = ParagraphStyle('cat', textColor=YELLOW, fontName='Helvetica-Bold', fontSize=14, leading=18, spaceBefore=10, spaceAfter=2)
st_tool = ParagraphStyle('tool', textColor=INK, fontName='Courier-Bold', fontSize=9.5, leading=12, spaceBefore=6)
st_prompt = ParagraphStyle('pr', textColor=MUTED, fontName='Helvetica-Oblique', fontSize=9.5, leading=12.5, leftIndent=10)
st_foot = ParagraphStyle('ft', textColor=MUTED, fontName='Helvetica', fontSize=9, leading=12)


def paint(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(BG); canvas.rect(0, 0, A4[0], A4[1], stroke=0, fill=1)
    # footer
    canvas.setFillColor(MUTED); canvas.setFont('Helvetica', 8)
    canvas.drawString(18*mm, 12*mm, "AI for Developers Toolkit  -  thecodeman.net/ai-toolkit")
    canvas.drawRightString(A4[0]-18*mm, 12*mm, "Start your 7-day free trial")
    canvas.restoreState()


def build(out_path):
    doc = BaseDocTemplate(out_path, pagesize=A4,
                          leftMargin=18*mm, rightMargin=18*mm, topMargin=18*mm, bottomMargin=20*mm)
    fw = A4[0]-36*mm; gap = 8*mm; cw = (fw-gap)/2
    fh = A4[1]-38*mm
    # cover frame (full width) + content frames (two columns) on later pages
    cover = PageTemplate(id='cover', frames=[Frame(18*mm, 20*mm, fw, fh, id='c')], onPage=paint)
    twocol = PageTemplate(id='twocol', frames=[
        Frame(18*mm, 20*mm, cw, fh, id='L', leftPadding=0, rightPadding=0),
        Frame(18*mm+cw+gap, 20*mm, cw, fh, id='R', leftPadding=0, rightPadding=0),
    ], onPage=paint)
    doc.addPageTemplates([cover, twocol])

    story = []
    # ---- cover ----
    story.append(Spacer(1, 60*mm))
    story.append(Paragraph("THE PROMPT PACK", st_eyebrow))
    story.append(Spacer(1, 6))
    story.append(Paragraph("AI for Developers Toolkit", st_h1))
    story.append(Spacer(1, 10))
    story.append(Paragraph("The exact prompts that get the best out of every AI skill and agent - "
                           "49 skills plus whole-repo agents for any stack (.NET, Node.js, Python, Java, Go), "
                           "each one pointed at your real code. Copy, paste, ship.", st_sub))
    story.append(Spacer(1, 18))
    story.append(Paragraph("How to use: install a tool in Claude (Claude Code or Cowork), then paste the "
                           "prompt and point it at a file, a folder, a project, or a GitHub link.", st_sub))
    from reportlab.platypus import NextPageTemplate, PageBreak
    story.append(NextPageTemplate('twocol'))
    story.append(PageBreak())

    # ---- content (two columns) ----
    for cat, tools in DATA:
        story.append(Paragraph(cat, st_cat))
        for name, prompt in tools:
            story.append(Paragraph(name, st_tool))
            story.append(Paragraph('&ldquo;' + prompt + '&rdquo;', st_prompt))

    doc.build(story)


if __name__ == '__main__':
    here = os.path.dirname(os.path.abspath(__file__))
    root = here
    for _ in range(6):
        if os.path.isdir(os.path.join(root, 'toolkit', 'skills')):
            root = os.path.join(root, 'toolkit')
            break
        if os.path.isdir(os.path.join(root, 'skills')):
            break
        root = os.path.dirname(root)
    out_dir = os.path.join(root, 'dist'); os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(out_dir, 'TheCodeMan-AI-Toolkit-Prompt-Pack.pdf')
    build(out)
    print('wrote', out)
