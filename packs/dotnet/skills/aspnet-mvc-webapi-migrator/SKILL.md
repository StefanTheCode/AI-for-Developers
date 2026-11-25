---
name: aspnet-mvc-webapi-migrator
description: (.NET) Migrate ASP.NET MVC 5 and Web API 2 code to ASP.NET Core on .NET 10, one controller at a time - actions, routes, filters, model binding, IHttpActionResult, child actions, OutputCache, bundling - plus the app plumbing around them - Global.asax, IHttpModule, IHttpHandler/.ashx, HttpContext.Current, Newtonsoft.Json formatters and XML content negotiation. Use this skill whenever the user wants to "migrate this controller", "Web API 2 to ASP.NET Core", "MVC 5 to .NET 10", "replace Global.asax", "convert this HTTP module to middleware", "port this .ashx handler", or is moving endpoints one by one in a strangler-fig migration. Always use this skill for MVC/Web API migrations instead of answering from memory; it keeps routes, status codes and payloads identical and proves it with parity tests.
category: Legacy (.NET)
---

# MVC 5 + Web API 2 Migrator (beta)

One controller (or one module / handler) per run, into the .NET 10 host from `yarp-strangler-setup`. Same routes, same status codes, same JSON - checked by the parity tests before the commit.

## Input

The controller (or module, handler, `Global.asax.cs`), its routes (attribute routes, `RouteConfig`, `WebApiConfig`), its filters, the views it renders, and the parity scenarios that cover it.

## Process

### 1. Inventory

For every action: HTTP method, route, parameters and where they bind from (`[FromUri]`, `[FromBody]`, query, form), return type, status codes it can return, filters / `[Authorize]`, and anything static it touches (`HttpContext.Current`, `CurrentUser`, `ConfigurationManager`).

### 2. Translate - Web API 2

| Web API 2 | ASP.NET Core |
|---|---|
| `ApiController` | `ControllerBase` + `[ApiController]` |
| `IHttpActionResult`, `Ok()`, `NotFound()`, `BadRequest(ModelState)`, `Created(uri, x)` | `IActionResult` / `ActionResult<T>`, same helper names; `Created(uri, x)` stays |
| `HttpResponseMessage` | `IActionResult` (or `Results.*` in minimal APIs) |
| `[RoutePrefix("api/products")]` + `[Route("{id:int}")]` | `[Route("api/products")]` on the class + `[HttpGet("{id:int}")]` |
| `[FromUri] T x` | `[FromQuery] T x` |
| `config.MapHttpAttributeRoutes()` + `GlobalConfiguration.Configure` | `builder.Services.AddControllers()` + `app.MapControllers()` |
| `ModelState.IsValid` checks | automatic 400 with `[ApiController]` - keep the same response body shape if clients parse it, or set `SuppressModelStateInvalidFilter` |
| `HttpContext.Current.User`, static `CurrentUser` | `User` in the controller; `IHttpContextAccessor` / an `ICurrentUser` service elsewhere |
| JSON camelCase via `CamelCasePropertyNamesContractResolver` | default in System.Text.Json - nothing to do. Newtonsoft-specific settings -> `AddNewtonsoftJson()` only if needed |
| XML via content negotiation | gone by default. If any client sends `Accept: application/xml`, add `AddXmlSerializerFormatters()` - or log it as an accepted difference |
| `[Authorize]` -> 302 to login (Forms auth) | 401. **Behavior change** - check API clients, log it |

Minimal APIs are fine for new endpoints; for a 1:1 move, controllers keep the diff small and the parity easy.

### 3. Translate - MVC 5

| MVC 5 | ASP.NET Core MVC |
|---|---|
| `Controller` (System.Web.Mvc) | `Controller` (Microsoft.AspNetCore.Mvc) |
| `ActionResult`, `View()`, `PartialView()`, `RedirectToAction()` | same names |
| `[ChildActionOnly]` + `Html.Action("TopProducts", new { take = 5 })` | a view component + `@await Component.InvokeAsync("TopProducts", new { take = 5 })` |
| `[OutputCache(Duration = 60, VaryByParam = "days")]` | `builder.Services.AddOutputCache()` + `app.UseOutputCache()` + `[OutputCache(Duration = 60, VaryByQueryKeys = ["days"])]` |
| `ViewBag` / `ViewData` | same |
| `Views/Web.config` namespaces | `Views/_ViewImports.cshtml` |
| `@Styles.Render` / `@Scripts.Render` (bundling) | `<link>` / `<script>` tags to files in `wwwroot` (+ `asp-append-version="true"`), or a build-time bundler |
| `HandleErrorAttribute`, `customErrors` | `app.UseExceptionHandler("/Error")` + `UseStatusCodePages` |
| `RouteConfig.MapRoute(...)` | `app.MapControllerRoute(...)` with the same templates |
| `AntiForgeryToken` | built in for forms (tag helpers) |

Views mostly carry over. Change HTML helpers to tag helpers only where it simplifies the view - not as a goal.

### 4. App plumbing

| Old | New |
|---|---|
| `Global.asax` `Application_Start` | `Program.cs` (services + pipeline) |
| `Application_Error` | `UseExceptionHandler` + `ILogger` |
| `Application_PostAuthenticateRequest` (roles from a ticket) | claims transformation (`IClaimsTransformation`) or the cookie's claims - see `legacy-config-auth-migrator` |
| `Session_Start` / `Session_End` | no equivalent - remove or move the logic (counters -> metrics) |
| `Application["X"]` | a singleton service or `IMemoryCache` |
| `IHttpModule` (`BeginRequest`, `PreSendRequestHeaders`) | middleware; for response headers use `context.Response.OnStarting(...)` |
| `IHttpHandler` / `.ashx` | an endpoint on the SAME path: `app.MapGet("/Handlers/ProductImage.ashx", ...)` |
| `HttpContext.Current` in libraries | adapters while both apps run (`yarp-strangler-setup`), then an injected abstraction |
| `System.Drawing` in handlers | Windows-only on .NET 6+. Stay on Windows hosting, or move to ImageSharp / SkiaSharp (check licenses) - a human decision |

### 5. Verify and finish

1. Build the new host. Run the parity scenarios for every action of this controller against the new host.
2. Differences: fix, or log in `migration/accepted-differences.md` for a person to approve (401 vs 302 and XML are the usual ones).
3. Commit: `migrate: ProductsController -> ASP.NET Core`. Update `migration/MIGRATION.md`.

## Rules

- Same paths, same verbs, same status codes, same payload shape.
- One controller / module / handler per commit.
- Don't upgrade the API design while migrating (no renamed routes, no new envelopes). Do that after the move, with versioning.
