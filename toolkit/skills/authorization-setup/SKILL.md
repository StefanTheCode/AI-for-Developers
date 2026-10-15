---
name: authorization-setup
description: Design API authorization in any stack - policy-based, role-based, claims/permission-based, and resource-based (ownership) checks. Covers ASP.NET Core policies and AuthorizationHandler, NestJS guards, Express middleware and CASL, FastAPI dependencies/scopes, Django and DRF permission classes, Spring Security (@PreAuthorize, SecurityFilterChain), Go middleware, and Casbin/OPA. Use this skill whenever the user wants authorization policies, role/claim/permission checks, RBAC/ABAC, custom authorization rules, resource-based authorization, IDOR protection, "deny by default", or asks "how do I authorize this / policy vs role / restrict this endpoint to X / stop users seeing other users' data". Always use this skill for authorization requests; it generates policies, handlers, and endpoint wiring.
category: Security
version: 1.0.0
---

# Authorization Setup

Design authorization the right way in any framework: named policies/permissions over scattered role strings, dedicated handlers for real business rules, resource-based checks for "can this user act on this object", and deny-by-default wiring. Authentication (who you are) is `jwt-auth-setup`; this is the authorization half (what you may do).

## Input - point it at your project
Works on a target: a **project**, **repo**, or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). Inspect the current setup before generating anything.

## Step 1 - detect the stack

| Manifest | Look for | Authorization mechanism |
|---|---|---|
| `*.csproj` | `Microsoft.AspNetCore.*` | Policies, `IAuthorizationHandler`, `IAuthorizationService` |
| `package.json` | `@nestjs/core`, `express`, `fastify`, `@casl/ability` | Guards + decorators, middleware, CASL abilities |
| `pyproject.toml`, `requirements*.txt` | `fastapi`, `django`, `djangorestframework` | `Depends`/`Security` scopes, DRF permission classes, Django perms |
| `pom.xml`, `build.gradle(.kts)` | `spring-boot-starter-security` | `SecurityFilterChain`, `@PreAuthorize`, `PermissionEvaluator` |
| `go.mod` | `net/http`, `gin`, `echo`, `chi`, `casbin` | Middleware, Casbin enforcer |

Find existing checks:
```
grep -rn "AddAuthorization\|RequireRole\|RequireClaim\|\[Authorize\|RequireAuthorization\|IAuthorizationHandler" --include=*.cs <target>
grep -rn "@UseGuards\|CanActivate\|@Roles\|defineAbility\|req.user" --include=*.ts <target> | grep -v node_modules
grep -rn "Depends(\|Security(\|permission_classes\|has_perm\|@permission_required" --include=*.py <target>
grep -rn "@PreAuthorize\|authorizeHttpRequests\|hasRole\|hasAuthority" <target>/src
grep -rn "casbin\|Enforce(\|func.*Middleware" --include=*.go <target>
```

## Universal principles (every stack)
1. **Deny by default.** New endpoints require authentication unless explicitly marked public (fallback policy, global guard, `anyRequest().authenticated()`).
2. **Named policies/permissions, not role strings everywhere.** `orders:manage` checked in one place, mapped to roles centrally. Changing who can do what is a config change, not a grep.
3. **Business rules in dedicated handlers**, not `if (user.role == "Admin" || ...)` blocks inside controllers.
4. **Resource-based checks stop IDOR.** Load the resource, then check ownership/tenant against the current user. A role check alone lets user A read user B's order by changing the ID.
5. **Enforce on the server, at the edge of every operation.** Hiding a button is not authorization. Check reads as well as writes.
6. **Tenant isolation** in multi-tenant apps: filter every query by tenant, not just the endpoint.
7. **Return 401 vs 403 correctly** (unauthenticated vs forbidden); for sensitive resources, 404 avoids leaking existence.
8. **Test it.** One allowed and one denied test per policy, including the "other user's resource" case.

## C# / .NET (ASP.NET Core)

Named policies:
```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanManageOrders", p => p.RequireClaim("permission", "orders:manage"))
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("Over18", p => p.AddRequirements(new MinimumAgeRequirement(18)))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
```

Custom requirement + handler (real business rules):
```csharp
public record MinimumAgeRequirement(int Age) : IAuthorizationRequirement;

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext ctx, MinimumAgeRequirement req)
    {
        var dob = ctx.User.FindFirst("dob")?.Value;
        if (DateTime.TryParse(dob, out var d) && d.AddYears(req.Age) <= DateTime.Today)
            ctx.Succeed(req);
        return Task.CompletedTask;
    }
}
builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
```

Resource-based (can THIS user act on THIS object) - implement `AuthorizationHandler<TRequirement, Order>` and call:
```csharp
var auth = await _authService.AuthorizeAsync(User, order, "OrderOwnerPolicy");
if (!auth.Succeeded) return Results.Forbid();
```

Wiring endpoints:
```csharp
app.MapPost("/orders", ...).RequireAuthorization("CanManageOrders");
group.RequireAuthorization("AdminOnly");
app.MapGet("/health", ...).AllowAnonymous();   // explicit opt-out from the fallback policy
```

## TypeScript / Node
- **NestJS:** a global guard (`APP_GUARD`) for authentication, a `@Roles()` / `@Permissions()` decorator read via `Reflector` in a `RolesGuard`, and a `@Public()` decorator for opt-outs.
```ts
@Injectable()
export class PermissionsGuard implements CanActivate {
  constructor(private reflector: Reflector) {}
  canActivate(ctx: ExecutionContext): boolean {
    const required = this.reflector.getAllAndOverride<string[]>('permissions', [ctx.getHandler(), ctx.getClass()]);
    if (!required) return true;
    const { user } = ctx.switchToHttp().getRequest();
    return required.every(p => user?.permissions?.includes(p));
  }
}
```
- **Express / Fastify:** a `requirePermission('orders:manage')` middleware / `preHandler` hook; mount auth globally and whitelist public routes.
- **Resource-based / ABAC:** CASL (`defineAbility`, `ability.can('update', subject('Order', order))`) keeps rules in one place.

## Python
- **FastAPI:** dependencies are the policy layer. `Security(get_current_user, scopes=["orders:manage"])` with `SecurityScopes`, or a `require_permission("orders:manage")` dependency factory; apply router-wide via `APIRouter(dependencies=[...])`.
- **Django / DRF:** `permission_classes = [IsAuthenticated, IsOrderOwner]` with `has_permission` (endpoint) and `has_object_permission` (resource). Set `DEFAULT_PERMISSION_CLASSES` to `IsAuthenticated` for deny-by-default. Object-level perms in plain Django: django-guardian or rules.
```python
class IsOrderOwner(BasePermission):
    def has_object_permission(self, request, view, obj):
        return obj.owner_id == request.user.id
```
Note: DRF calls `has_object_permission` only via `get_object()` - custom list queries must filter by owner themselves.

## Java / Kotlin (Spring Security)
```java
@Bean
SecurityFilterChain api(HttpSecurity http) throws Exception {
    http.authorizeHttpRequests(a -> a
        .requestMatchers("/health").permitAll()
        .requestMatchers("/admin/**").hasRole("ADMIN")
        .anyRequest().authenticated());          // deny by default
    return http.build();
}
```
- Method security: `@EnableMethodSecurity` + `@PreAuthorize("hasAuthority('orders:manage')")`.
- Resource-based: `@PreAuthorize("@orderSecurity.isOwner(#id, authentication)")` with a bean, or a custom `PermissionEvaluator` for `hasPermission(#order, 'write')`. `@PostAuthorize` checks the returned object.

## Go
- Middleware per route group: `RequirePermission("orders:manage")(handler)` reading claims from `context.Context` (set by the auth middleware). Same pattern in gin (`r.Group(...).Use(...)`), echo, and chi.
- Resource checks belong in the service layer after loading the entity (`if order.OwnerID != userID { return ErrForbidden }`).
- Policy engines: Casbin (RBAC/ABAC models, also available for Node, Python, Java, .NET) or OPA (Rego) when policies must live outside the code.

## External policy engines (any stack)
Casbin, OPA, Cedar, OpenFGA / SpiceDB (relationship-based, Zanzibar-style). Recommend them only when rules are shared across services or change without deploys; otherwise in-code policies are simpler.

## Output format
```
## Current state
<stack, what exists, gaps: anonymous endpoints, inline role strings, missing ownership checks>

## Policy design
<permissions/roles table, which endpoint needs which>

## Code
<policies/guards/permission classes/handlers + endpoint wiring, ready to paste>

## Tests to add
<allowed + denied + other-user's-resource per policy>
```

## How to use it & best prompts
"Design authorization policies for my API", "restrict this endpoint to managers", "add a custom authorization rule", "stop users from accessing others' records" (resource-based), "add deny-by-default to my NestJS/Spring/FastAPI app". Pairs with `jwt-auth-setup` and the `api-security-auditor` agent.
