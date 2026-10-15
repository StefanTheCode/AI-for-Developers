---
name: dockerfile-generator
description: Generate an optimized multi-stage Dockerfile and .dockerignore for an app in any stack - .NET, Node/TypeScript, Python, Java/Kotlin (Spring Boot), Go, Rust - with layer caching, a small final image (chiseled/distroless/slim), a non-root user, and production best practices. Use this skill whenever the user wants a Dockerfile, to containerize or dockerize an app, a multi-stage build, smaller or more secure images, chiseled or distroless images, faster Docker builds, or asks "how do I dockerize this / Dockerfile for my API / shrink my image / run as non-root". Always use this skill for Dockerfile requests; it produces a production-grade multi-stage build.
category: DevOps
version: 1.0.0
---

# Dockerfile Generator

Produce a production-grade multi-stage Dockerfile for any app: fast cached builds, a small secure runtime image, and a non-root user.

## Input - point it at your project
Works on a target: a **project/solution/repo** or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). If a Dockerfile exists, review and harden it instead of replacing it blindly.

## Step 1 - detect the stack and entry point

| Manifest | Read | Build stage | Runtime stage (smallest sensible) |
|---|---|---|---|
| `*.csproj` | `<TargetFramework>`, entry project, solution layout | `mcr.microsoft.com/dotnet/sdk` | `aspnet:<ver>-noble-chiseled` / `runtime-deps` (AOT) |
| `package.json` + lock | `engines.node`, `scripts.build`/`start`, framework (Nest, Next, Express) | `node:<ver>-slim` | `gcr.io/distroless/nodejs<ver>-debian12` or `node:<ver>-slim` + `USER node` |
| `pyproject.toml` / `requirements*.txt` | Python version, uv/poetry/pip, ASGI/WSGI server | `python:<ver>-slim` | `python:<ver>-slim` + non-root user |
| `pom.xml` / `build.gradle(.kts)` | Java version, wrapper (`mvnw`/`gradlew`), Spring Boot | `eclipse-temurin:<ver>-jdk` | `eclipse-temurin:<ver>-jre` or `gcr.io/distroless/java<ver>-debian12` |
| `go.mod` | Go version, `main` package path, cgo usage | `golang:<ver>` | `gcr.io/distroless/static-debian12:nonroot` (or `scratch`) |
| `Cargo.toml` | binary name, native deps | `rust:<ver>` | `gcr.io/distroless/cc-debian12:nonroot` |

Use the runtime/language version the project already targets; don't invent tags - check the image exists for that version.

## Universal rules (every stack)
1. **Multi-stage:** SDK/compiler only in the build stage; the final image gets just the artifact and runtime.
2. **Copy dependency manifests first, install, then copy source** - the dependency layer stays cached until manifests change. The single biggest build-time win.
3. **BuildKit cache mounts** for package caches (`RUN --mount=type=cache,target=...`) speed rebuilds further.
4. **Non-root user** in the final stage (chiseled/distroless `nonroot` do this; otherwise create one and `USER` it).
5. **Smallest base that runs the app** - chiseled/distroless reduce attack surface (no shell, no package manager). Alpine uses musl - watch native deps.
6. **Pin base images** to a version tag (ideally a digest); never `latest` in production.
7. **No secrets in layers** - no `ENV`/`ARG` secrets; use `RUN --mount=type=secret` for build-time and runtime injection for run-time.
8. **Lean context** with `.dockerignore`; **exec-form `ENTRYPOINT`** so the process gets signals (PID 1) and shuts down gracefully.
9. **Expose an unprivileged port** (8080, not 80) and add a `HEALTHCHECK` only if the image has a tool to run it (distroless doesn't - use orchestrator probes, `healthchecks-setup`).

## C# / .NET
```dockerfile
# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MyApp/MyApp.csproj", "MyApp/"]
RUN dotnet restore "MyApp/MyApp.csproj"          # restore as its own layer = cached unless csproj changes
COPY . .
RUN dotnet publish "MyApp/MyApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# runtime (small + non-root)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID                                     # non-root (chiseled images set this)
EXPOSE 8080
ENTRYPOINT ["dotnet", "MyApp.dll"]
```
- Multi-project solutions: copy every referenced `.csproj` (plus `Directory.Build.props`, `Directory.Packages.props`, `nuget.config`) before `restore`.
- Image options (smallest -> most compatible): **`-chiseled`** (no shell, non-root, best default for services), **`-alpine`** (musl), **`-noble`**/standard. **Native AOT + `runtime-deps`** chiseled for tiny self-contained images when the app is AOT-compatible.
- Alternative: `dotnet publish /t:PublishContainer` builds an image without a Dockerfile.

## TypeScript / Node
```dockerfile
FROM node:22-slim AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN --mount=type=cache,target=/root/.npm npm ci
COPY . .
RUN npm run build && npm prune --omit=dev

FROM gcr.io/distroless/nodejs22-debian12:nonroot
WORKDIR /app
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/dist ./dist
COPY --from=build /app/package.json ./
ENV NODE_ENV=production
EXPOSE 8080
CMD ["dist/main.js"]                              # distroless node image's entrypoint is node
```
- pnpm: `corepack enable`, copy `pnpm-lock.yaml`, `pnpm install --frozen-lockfile`. yarn: `yarn install --immutable`.
- Next.js: `output: 'standalone'` and copy `.next/standalone` + `.next/static` + `public`.
- Need a shell for debugging? Use `node:22-slim` as the runtime with `USER node`. Native modules must be built on the same libc as the runtime.

## Python
```dockerfile
FROM python:3.12-slim AS build
COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv   # pin this tag in production
WORKDIR /app
ENV UV_COMPILE_BYTECODE=1 UV_LINK_MODE=copy
COPY pyproject.toml uv.lock ./
RUN --mount=type=cache,target=/root/.cache/uv uv sync --frozen --no-dev --no-install-project
COPY . .
RUN --mount=type=cache,target=/root/.cache/uv uv sync --frozen --no-dev

FROM python:3.12-slim
RUN useradd --create-home --uid 10001 app
WORKDIR /app
COPY --from=build --chown=app:app /app /app
ENV PATH="/app/.venv/bin:$PATH" PYTHONUNBUFFERED=1
USER app
EXPOSE 8080
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8080"]
```
- pip: `python -m venv /opt/venv`, `pip install --no-cache-dir -r requirements.txt` in the build stage, copy `/opt/venv`. poetry: export to requirements or install into a venv with `--only main`.
- Same Python minor version and libc in both stages (compiled wheels). Django: gunicorn (+ uvicorn workers for ASGI), `collectstatic` in the build stage.

## Java / Kotlin (Spring Boot)
```dockerfile
FROM eclipse-temurin:21-jdk AS build
WORKDIR /src
COPY mvnw pom.xml ./
COPY .mvn .mvn
RUN --mount=type=cache,target=/root/.m2 ./mvnw -B dependency:go-offline
COPY src src
RUN --mount=type=cache,target=/root/.m2 ./mvnw -B package -DskipTests

FROM eclipse-temurin:21-jre
RUN useradd --uid 10001 app
WORKDIR /app
COPY --from=build /src/target/*.jar app.jar
USER app
EXPOSE 8080
ENTRYPOINT ["java", "-XX:MaxRAMPercentage=75", "-jar", "app.jar"]
```
- Gradle: copy `gradlew`, `gradle/`, `settings.gradle(.kts)`, `build.gradle(.kts)` first; cache `/root/.gradle`; `./gradlew bootJar`.
- Spring Boot layered jars (extract with the jar's layer tooling) split dependencies from app classes for better caching. Distroless `java<ver>-debian12:nonroot` is a smaller runtime option. Alternatives: Jib or Cloud Native Buildpacks (`spring-boot:build-image`) without a Dockerfile.
- Container-aware heap: set `MaxRAMPercentage`, not a fixed `-Xmx` sized for a laptop.

## Go
```dockerfile
FROM golang:1.23 AS build
WORKDIR /src
COPY go.mod go.sum ./
RUN --mount=type=cache,target=/go/pkg/mod go mod download
COPY . .
RUN --mount=type=cache,target=/go/pkg/mod --mount=type=cache,target=/root/.cache/go-build \
    CGO_ENABLED=0 go build -trimpath -ldflags="-s -w" -o /out/app ./cmd/api

FROM gcr.io/distroless/static-debian12:nonroot
COPY --from=build /out/app /app
EXPOSE 8080
ENTRYPOINT ["/app"]
```
- cgo needed: build without `CGO_ENABLED=0` and use `distroless/base-debian12:nonroot`. `scratch` works for static binaries but needs CA certs and tzdata copied in.

## Rust
Build with `cargo build --release` (cache `/usr/local/cargo/registry` and `target`; cargo-chef for dependency-layer caching), copy the binary to `gcr.io/distroless/cc-debian12:nonroot`.

## .dockerignore (adapt per stack)
```
**/.git
**/bin/
**/obj/
**/node_modules
**/.venv
**/__pycache__
**/target/
**/build/
**/.vs/
**/.idea/
**/*.user
**/.env*
Dockerfile*
```

## Output format
```
## Detected
<stack, version, entry point, package manager>

## Dockerfile
<multi-stage, commented where non-obvious>

## .dockerignore

## Build & run
docker build -t <app>:<tag> .
docker run -p 8080:8080 <app>:<tag>

## Notes
<base image choice, size/security trade-offs, anything to verify>
```

## How to use it & best prompts
"Dockerize this .NET app", "give me a multi-stage Dockerfile for my NestJS API", "containerize this FastAPI service with uv", "shrink my Spring Boot image", "distroless image for my Go service", "make my container run as non-root". Pairs with `cicd-pipeline-generator`, `dependency-vuln-scanner` (scan the image), and `microservice-template-generator`.
