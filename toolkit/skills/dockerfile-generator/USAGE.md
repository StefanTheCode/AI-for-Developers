# How to use - Dockerfile Generator

**What it is.** Generates a production-grade multi-stage Dockerfile and `.dockerignore` for your stack - cached dependency layers, a small secure runtime image, a non-root user, and no secrets in layers. It detects the stack and picks the right images: .NET SDK -> chiseled ASP.NET runtime, Node -> distroless nodejs or slim, Python (uv/poetry/pip) -> slim with a venv, Java/Spring Boot -> Temurin JRE or distroless java, Go -> distroless static, Rust -> distroless cc.

**When to reach for it.** Containerizing a service, or shrinking, speeding up, or hardening an existing image.

**How to use it.** Point it at your project/solution/repo (path or GitHub URL). Example prompts:
- "Dockerize this .NET app with a chiseled base."
- "Multi-stage Dockerfile for my NestJS API with pnpm."
- "Containerize this FastAPI service using uv."
- "Shrink my Spring Boot image and run it as non-root."
- "Distroless image for my Go service."

**Get the best out of it.** Let it detect the entry point, language version, and package manager so copy/install paths are right. Ask for distroless/chiseled as the default and a slim variant if you need a shell for debugging. If you already have a Dockerfile, ask it to review and harden that one.

**What you get.** The detected setup, a commented multi-stage Dockerfile, a `.dockerignore`, build/run commands, and notes on base-image trade-offs.

**Won't do.** It won't use `latest` base tags in production or bake secrets into the image. Pairs with `cicd-pipeline-generator` and `microservice-template-generator`.
