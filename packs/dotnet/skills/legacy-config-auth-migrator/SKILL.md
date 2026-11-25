---
name: legacy-config-auth-migrator
description: (.NET) Migrate the cross-cutting parts of an ASP.NET Framework app to .NET 10 - web.config and transforms to appsettings + Options, machineKey to Data Protection, Forms authentication / Membership / OWIN cookies to ASP.NET Core cookie auth or Identity (with remote authentication while both apps run), <location> authorization to policies, session and Application state, BinaryFormatter, customErrors and Application_Error. Use this skill whenever the user asks about "web.config in .NET 10", "ConfigurationManager replacement", "machineKey", "Forms authentication to ASP.NET Core", "Membership to Identity", "share login between old and new app", "session in ASP.NET Core", "replace BinaryFormatter", or plans how auth and session work during a migration. Always use this skill for these cross-cutting pieces instead of answering from memory; they are where migrations break for users.
category: Legacy (.NET)
---

# Config, Auth & Cross-Cutting Migrator (beta)

The pages and controllers are the visible part. Sign-in, session, configuration and secrets are where users notice a broken migration. Each piece below has a "while both apps run" step and a "final" step - do them in that order.

## Input

`Web.config` (+ transforms), `Global.asax.cs`, every `ConfigurationManager` use, the login / logout code, the role setup, `<location>` rules, every session key and what writes it, `Application[...]` uses, `BinaryFormatter` uses, error handling.

## 1. Configuration

| Old | New |
|---|---|
| `<appSettings>` keys used through `ConfigurationManager.AppSettings` | `appsettings.json` sections + `IOptions<T>` classes (group keys by prefix: `Shop.*` -> `ShopOptions`) |
| `<connectionStrings>` | `ConnectionStrings` section + `GetConnectionString` |
| `Web.Release.config` (XDT transforms) | `appsettings.Production.json` + environment variables |
| Secrets in Web.config | user secrets (dev), environment variables / Key Vault (prod) - never in appsettings.json |
| Static `AppSettings` helper in a library | inject `IOptions<T>`; while the library still serves the old app, keep the helper reading `ConfigurationManager` on net48 and an `IOptions` path on net10 (`#if NET`) |

Write a key map in `migration/config-map.md`: old key -> new path -> where it's set per environment.

## 2. machineKey -> Data Protection

- While both apps run: the old app keeps its `machineKey` (it encrypts ViewState and the Forms cookie). The new app doesn't read that cookie directly - remote authentication handles it (next section).
- Final: `builder.Services.AddDataProtection().SetApplicationName("<App>").PersistKeysToFileSystem(...)` (or Redis / Blob Storage) so keys survive restarts and are shared across instances. Old cookies stop working after the switch - users sign in once again. Say this in the release notes.

## 3. Authentication

**While both apps run - remote authentication** (set up by `yarp-strangler-setup`):

- New app: `.AddAuthenticationClient(true)`. Every request in the new app asks the old app who the user is (one HTTP call per request - fine for the migration period).
- Sign-in and sign-out stay on the OLD login page until it moves.
- Windows authentication is not supported by remote authentication - if the app uses it, use `AddNegotiate()` in the new app instead and plan it with the user.

**Final - when the login page moves:**

| Old | New |
|---|---|
| Forms auth (`FormsAuthentication.SetAuthCookie`, a custom ticket with roles in `UserData`) | cookie authentication: `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => { o.LoginPath = "/Account/Login.aspx"; o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = true; })`, sign in with `HttpContext.SignInAsync(principal)` and roles as `ClaimTypes.Role` claims |
| `Application_PostAuthenticateRequest` building a `GenericPrincipal` | claims at sign-in (or `IClaimsTransformation`) |
| ASP.NET Membership / `SqlMembershipProvider` | ASP.NET Core Identity. Passwords can't be converted - keep the old hash format behind a custom `IPasswordHasher<T>` that verifies the old hash and re-hashes on the next successful login |
| Custom password table (salt + hash) | keep it, verify with the same algorithm in the new login page; plan a move to Identity's hasher |
| OWIN cookie auth | shared cookie auth is possible (same cookie name, shared Data Protection keys) - otherwise remote auth |
| `<location path="Admin"><authorization><allow roles="Admin"/>` | a policy: `AddAuthorizationBuilder().AddPolicy("AdminOnly", p => p.RequireRole("Admin"))` + `options.Conventions.AuthorizeFolder("/Admin", "AdminOnly")` (Razor Pages) or `[Authorize(Policy = "AdminOnly")]` |
| `[Authorize]` API -> 302 to login | 401 for APIs: in `AddCookie`, set `o.Events.OnRedirectToLogin` to return 401 for `/api` paths - or keep 302 if clients depend on it (log the decision) |

Test sign-in, sign-out, expiry, "remember me", role-protected pages and API calls without a cookie - all in the parity / flow tests.

## 4. Session and Application state

- **While both apps run:** remote session through the adapters (every key registered with `RegisterKey<T>` in both apps). Pages that use session in the new app are marked `.RequireSystemWebAdapterSession()` / `[Session]`.
- **Final:** `builder.Services.AddDistributedMemoryCache()` (or Redis) + `AddSession()` + `app.UseSession()`; values as JSON (`HttpContext.Session.SetString(key, JsonSerializer.Serialize(value))`). Ask whether the data belongs in session at all (a cart often belongs in the database).
- `Session_End` has no equivalent - move its logic (usually counters) to metrics or drop it.
- `Application["X"]` -> a singleton service or `IMemoryCache`.

## 5. BinaryFormatter

Removed in .NET 9+ (it throws), and unsafe before that. Replace with `System.Text.Json` (or MessagePack / protobuf for binary needs). Existing serialized data (files, cache, database blobs, session state server) can't be read by the new serializer - plan it: drop it (caches), convert it once with a small net48 tool (persistent data), or read old + write new during a transition window.

## 6. Errors and logging

| Old | New |
|---|---|
| `customErrors` + `defaultRedirect` | `app.UseExceptionHandler("/Error")` + `UseStatusCodePagesWithReExecute` |
| `Application_Error` writing a log file | `ILogger` + a real sink (Serilog, OpenTelemetry) |
| `Trace.TraceError` | `ILogger` |
| ELMAH / log4net / NLog | their .NET 10 packages or Serilog - keep the same log destinations during the move |

## Rules

- Never move secrets into `appsettings.json`.
- Auth changes ship alone, in their own release, with a rollback plan.
- Every behavior change users notice (signing in again after the switch, 401 vs 302) is logged in `migration/accepted-differences.md` and approved by a person.
