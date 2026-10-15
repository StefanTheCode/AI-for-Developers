# How to use - API Endpoint Scaffolder

**What it is.** Generates HTTP endpoints in your project's framework - ASP.NET Core Minimal APIs or controllers, Express, Fastify, NestJS, FastAPI, Django REST Framework, Spring Boot, Go net/http, chi, or Gin - one feature per endpoint, with request validation, explicit response types, route grouping, auth, and OpenAPI metadata, matching your existing endpoint style.

**When to reach for it.** Adding endpoints, converting controllers to minimal APIs, standardizing route structure, or bringing a sprawling controller/router into one-feature-per-endpoint shape.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Add a minimal API endpoint to create an order." (.NET)
- "Convert OrdersController to minimal APIs."
- "Add a Fastify route for POST /invoices with schema validation."
- "Scaffold a FastAPI router for refunds with Pydantic models."
- "Add a Spring Boot controller for cancelling an order with Bean Validation."
- "Add a chi route group under /orders with auth middleware."

**Get the best out of it.** Let it read an existing endpoint first so the new one fits (Carter vs MapGroup, Express vs NestJS, chi vs Gin, how errors are returned). Tell it the validation rules and who may call the endpoint.

**What you get.** The detected stack and style, full code for request/response types, handler and route registration, the wiring change, and notes on assumptions.

**Won't do.** It won't put business logic or data access in the endpoint - rules go to the handler/service/domain.
