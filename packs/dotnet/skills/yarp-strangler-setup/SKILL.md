---
name: yarp-strangler-setup
description: (.NET) Put a new ASP.NET Core (.NET 10) app in front of an existing ASP.NET Framework app for an incremental migration - YARP fallback for every route that hasn't moved yet, Microsoft.AspNetCore.SystemWebAdapters for shared System.Web code, remote session and remote authentication so both apps share the signed-in user and the session while they run side by side. Use this skill whenever the user wants to "start the strangler fig", "add YARP in front of the old app", "set up incremental migration", "System.Web adapters", "share session / auth between .NET Framework and ASP.NET Core", or asks how to run the old and new app together during a migration. Always use this skill for the side-by-side setup instead of improvising; it applies Microsoft's documented setup and verifies it with the parity tests.
category: Legacy (.NET)
---

# YARP Strangler Setup (beta)

After this skill, users talk to the new .NET 10 app. It handles nothing yet - every request falls through to the old app via YARP - but from now on each migrated route simply takes its place in the new app. Nothing changes for users, and the parity tests prove it.

```text
Browser -> new ASP.NET Core app (.NET 10)
             |- migrated routes      (handled here)
             '- everything else  --> YARP fallback --> old ASP.NET app (.NET Framework)
                                      remote session + remote auth (System.Web adapters)
```

## Before you start

- The parity baseline exists (`migration-safety-net`). If not, build it first.
- Projects are SDK-style or at least on `PackageReference` (`legacy-package-migrator`) - adapters are NuGet packages.
- Read `Web.config` (auth mode, session mode, machineKey, `<location>` rules), `Global.asax.cs` and every `Session["..."]` use, including helpers that wrap session.

## Process

### 1. Create the new host

```bash
dotnet new web -n <App>.AspNetCore -f net10.0
dotnet sln add <App>.AspNetCore
dotnet add <App>.AspNetCore package Yarp.ReverseProxy
dotnet add <App>.AspNetCore package Microsoft.AspNetCore.SystemWebAdapters.CoreServices
```

Name it after the old web project (`LegacyShop.Web` -> `LegacyShop.AspNetCore`). Give it its own port in `launchSettings.json`.

### 2. Shared settings

Generate one GUID for both apps.

`appsettings.json` (new app):

```json
{
  "RemoteAppApiKey": "<the-guid>",
  "RemoteAppUri": "http://localhost:5100"
}
```

`Web.config` (old app), in `<appSettings>`:

```xml
<add key="RemoteAppApiKey" value="the-guid" />
```

Both apps must use the same virtual directory layout (both at `/`, or both under the same path).

### 3. Program.cs in the new app

```csharp
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSystemWebAdapters()
    .AddJsonSessionSerializer(options =>
    {
        // Every session key, with its type - same list in both apps (step 4).
        options.RegisterKey<List<CartLine>>("Cart");
        options.RegisterKey<DateTime>("CreatedAt");
    })
    .AddRemoteAppClient(options =>
    {
        options.RemoteAppUrl = new(builder.Configuration["RemoteAppUri"]!);
        options.ApiKey = builder.Configuration["RemoteAppApiKey"]!;
    })
    .AddSessionClient()
    .AddAuthenticationClient(true);   // remote app auth is the default scheme

builder.Services.AddReverseProxy();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSystemWebAdapters();

// Migrated endpoints go here. Mark the ones that use session:
//   app.MapRazorPages().RequireSystemWebAdapterSession();
//   [Session] on controllers that use System.Web session

app.MapForwarder("/{**catch-all}",
        app.Services.GetRequiredService<IOptions<RemoteAppClientOptions>>().Value.RemoteAppUrl.OriginalString)
    .WithOrder(int.MaxValue)   // lowest priority: only what nothing else handles
    .ShortCircuit();           // fallback requests skip the rest of the pipeline (incl. remote auth calls)

app.Run();
```

### 4. The old app

```bash
# in the old web project (PackageReference)
Microsoft.AspNetCore.SystemWebAdapters.FrameworkServices
```

`Global.asax.cs`, at the top of `Application_Start`:

```csharp
HttpApplicationHost.RegisterHost(builder =>
{
    builder.AddSystemWebAdapters()
        .AddProxySupport(options => options.UseForwardedHeaders = true)
        .AddJsonSessionSerializer(options =>
        {
            options.RegisterKey<List<CartLine>>("Cart");   // same keys + types as the new app
            options.RegisterKey<DateTime>("CreatedAt");
        })
        .AddRemoteAppServer(options => options.ApiKey = ConfigurationManager.AppSettings["RemoteAppApiKey"])
        .AddSessionServer()
        .AddAuthenticationServer();
});
```

`Web.config`:

```xml
<system.webServer>
  <modules>
    <remove name="SystemWebAdapterModule" />
    <add name="SystemWebAdapterModule" type="Microsoft.AspNetCore.SystemWebAdapters.SystemWebAdapterModule, Microsoft.AspNetCore.SystemWebAdapters.FrameworkServices" preCondition="managedHandler" />
  </modules>
</system.webServer>
```

Session values must be JSON-serializable on both sides (a plain `CartLine` class is fine; `[Serializable]` isn't needed). Every key needs `RegisterKey<T>` - a missing registration breaks session access.

### 5. Shared class libraries

Libraries that use `System.Web` (`HttpContext.Current`, `HttpRequest`, `HttpSessionState`):

1. Remove the `System.Web` reference, add `Microsoft.AspNetCore.SystemWebAdapters`.
2. Multi-target: `<TargetFrameworks>net48;net10.0</TargetFrameworks>` (or `netstandard2.0` if nothing else needs net48).
3. Build both targets. Code that the adapters don't cover goes behind an interface (`ICurrentUser`) - note each one for later cleanup.

The adapters are a bridge. The last phase of the migration removes them.

### 6. Optional: run both with Aspire

If the team uses Aspire: `Aspire.Hosting.IncrementalMigration` (AppHost) and `Aspire.Microsoft.AspNetCore.SystemWebAdapters` (ServiceDefaults) start both apps and wire the fallback. Both are preview packages - say so, and don't make them a requirement.

### 7. Verify - nothing changed for users

1. Start the old app, then the new one.
2. Run the parity tests against the NEW app's URL. All green with zero migrated routes = the proxy is transparent.
3. Sign in through the new app's URL (it's proxied to the old login page). Open a page, check you're still signed in; add something to the session, check it's there.
4. Check headers the old app relies on (`X-Forwarded-*`, host) - `AddProxySupport(... UseForwardedHeaders = true)` handles the usual ones.

Report: the new project, ports, the session keys registered, libraries switched to the adapters, and the parity result.

## Rules

- The fallback route is always last (`WithOrder(int.MaxValue)`) and short-circuited.
- Never point the old app at the new one - traffic only flows new -> old.
- Remote authentication can't do Windows auth, and sign-in / sign-out stay in the old app until the login page itself moves (`legacy-config-auth-migrator`).
- Keep public URLs identical. A migrated page answers on the same path the old one did.
