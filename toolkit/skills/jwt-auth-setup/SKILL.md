---
name: jwt-auth-setup
description: Set up JWT authentication in any stack - token issuing, validation, refresh tokens, and wiring to authorization - with secure defaults. Covers ASP.NET Core (AddJwtBearer), Node (jose, passport-jwt, @nestjs/jwt), Python (PyJWT, FastAPI OAuth2PasswordBearer, djangorestframework-simplejwt), Spring Security OAuth2 Resource Server, and Go (golang-jwt). Use this skill whenever the user wants to add login/auth, issue or validate JWT tokens, set up bearer authentication, refresh tokens, validate tokens from an IdP (Entra ID/Azure AD, Auth0, Keycloak, Cognito), JWKS, claims, or asks "how do I secure my API / add JWT auth / protect my endpoints". Always use this skill for API auth requests; it produces wired-up token issuing, validation, and authorization with secure defaults rather than a snippet.
category: Security
---

# JWT Auth Setup

Wire up JWT bearer authentication in an API with secure defaults: token issuing, validation, refresh tokens, and protected endpoints. For detailed policy/permission design, hand off to `authorization-setup`.

## Input - point it at your project
If they point at a project or repo, inspect the existing auth setup before changing anything:
- **A project or repo root** - find the auth wiring and review it.
- **A GitHub URL** - clone first: `git clone --depth 1 <url> /tmp/repo && cd /tmp/repo`

If auth already exists, **review and harden it** (validation flags, algorithm pinning, clock skew, refresh-token handling, where the signing key lives) rather than re-adding from scratch, and flag access-control gaps (unprotected endpoints, no deny-by-default).

## Step 1 - detect the stack

| Manifest | Look for | Library |
|---|---|---|
| `*.csproj` | `Microsoft.AspNetCore.Authentication.JwtBearer` | JwtBearer handler |
| `package.json` | `jose`, `jsonwebtoken`, `passport-jwt`, `@nestjs/jwt` | jose / passport-jwt / Nest JwtModule |
| `pyproject.toml`, `requirements*.txt` | `pyjwt`, `fastapi`, `djangorestframework-simplejwt`, `authlib` | PyJWT / SimpleJWT |
| `pom.xml`, `build.gradle(.kts)` | `spring-boot-starter-oauth2-resource-server` | Spring Security Resource Server |
| `go.mod` | `github.com/golang-jwt/jwt/v5`, `github.com/lestrrat-go/jwx` | golang-jwt / jwx |

```
grep -rn "AddJwtBearer\|TokenValidationParameters\|RequireHttpsMetadata" --include=*.cs <target>
grep -rn "jwtVerify\|jwt.verify\|JwtModule\|passport-jwt" --include=*.ts --include=*.js <target> | grep -v node_modules
grep -rn "jwt.decode\|OAuth2PasswordBearer\|SIMPLE_JWT" --include=*.py <target>
grep -rn "oauth2ResourceServer\|JwtDecoder\|issuer-uri" <target>/src
grep -rn "jwt.Parse\|jwt.WithValidMethods" --include=*.go <target>
```

## When this runs
Confirm whether they're **issuing** tokens themselves (own login) or **validating** tokens from an external IdP (Entra ID / Auth0 / Keycloak / Cognito). For an external IdP, validate against its issuer and JWKS endpoint and delegate issuing - this is the preferred path for most production systems. Self-issued is covered below for apps that own their login.

## Universal secure defaults (every stack)
- **Validate everything:** signature, `iss`, `aud`, `exp`/`nbf`. Never disable validation to "make it work".
- **Pin the algorithm(s).** Pass an explicit allow-list (e.g. `HS256` or `RS256`); never accept `alg: none` and never let the token header pick the algorithm.
- **Short-lived access tokens (~5-15 min) + revocable refresh tokens.** A JWT can't be un-issued.
- **Tight clock skew** (~30 s). Some libraries default to minutes.
- **Signing key never in source or committed config.** Dev: local secret store; prod: vault/secret manager/env. HS256 keys >= 32 random bytes.
- **Asymmetric keys (RS256/ES256) with a JWKS endpoint** when several services validate tokens from one issuer - they only need the public key, and key rotation uses `kid`.
- **HTTPS only.** Bearer tokens over HTTP are stolen tokens.
- **Minimal claims.** `sub`, `jti`, roles/permissions; no PII or secrets - JWTs are encoded, not encrypted.
- **Deny by default** for endpoints; see `authorization-setup`.

## Refresh tokens (do this properly, any stack)
- Refresh token: **opaque random value, long-lived, persisted hashed** with expiry, user, and `revoked` flag - not a JWT you can't revoke.
- On refresh: validate the stored token, **rotate** it (issue new, invalidate old), issue a new access token. Reuse of a rotated token = theft signal -> revoke the whole family.
- On logout / suspected theft: revoke the row.
- Browser clients: refresh token in an `HttpOnly; Secure; SameSite` cookie, not `localStorage`.

## C# / .NET (ASP.NET Core)
```
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
```
```csharp
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // External IdP instead: options.Authority = "<issuer>"; options.Audience = "<api>";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"], ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30) // tighten the default 5 min
        };
    });
builder.Services.AddAuthorization(o =>
    o.AddPolicy("CanManageOrders", p => p.RequireClaim("permission", "orders:manage")));
app.UseAuthentication();
app.UseAuthorization();
```
Token issuing:
```csharp
public class TokenService(IConfiguration config)
{
    public string CreateAccessToken(User user)
    {
        var jwt = config.GetSection("Jwt");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```
Endpoints: `.RequireAuthorization()` / `.RequireAuthorization("CanManageOrders")`; current user via `ClaimsPrincipal` (`user.FindFirstValue(JwtRegisteredClaimNames.Sub)`). Set `RequireHttpsMetadata = true` in production. Bind the `Jwt` section through validated options (`typed-config-generator`). For full user management, point to ASP.NET Core Identity.

## TypeScript / Node
- Prefer **jose** (maintained, Web Crypto, JWKS support): `jwtVerify(token, key, { issuer, audience, algorithms: ['RS256'], clockTolerance: 30 })`; for an IdP use `createRemoteJWKSet(new URL(jwksUri))`. Issue with `new SignJWT(claims).setProtectedHeader({ alg }).setExpirationTime('15m')`.
- `jsonwebtoken`: always pass `algorithms` to `jwt.verify`.
- **NestJS:** `@nestjs/jwt` `JwtModule.registerAsync` (secret from `ConfigService`) + `@nestjs/passport` `JwtStrategy`, registered as a global guard with a `@Public()` opt-out.
- **Express:** `passport-jwt` or a small middleware around `jwtVerify` that sets `req.user`.

## Python
- **PyJWT:** `jwt.decode(token, key, algorithms=["RS256"], audience=..., issuer=..., leeway=30)` - `algorithms` is mandatory, keep it explicit. `jwt.PyJWKClient(jwks_url)` for IdP keys.
- **FastAPI:** `OAuth2PasswordBearer(tokenUrl="token")` + a `get_current_user` dependency that decodes and loads the user; apply via `Depends` / router dependencies.
- **Django REST Framework:** `djangorestframework-simplejwt` (`SIMPLE_JWT` settings: short `ACCESS_TOKEN_LIFETIME`, `ROTATE_REFRESH_TOKENS = True`, `BLACKLIST_AFTER_ROTATION = True` with the blacklist app).

## Java / Kotlin (Spring Boot)
- Validate with the Resource Server starter: `spring.security.oauth2.resourceserver.jwt.issuer-uri=<issuer>` (discovers JWKS, validates `iss`/`exp`); add an audience validator on the `JwtDecoder`.
```java
http.oauth2ResourceServer(o -> o.jwt(Customizer.withDefaults()))
    .authorizeHttpRequests(a -> a.anyRequest().authenticated());
```
- Map claims to authorities with a `JwtAuthenticationConverter` (e.g. `permissions` claim -> `hasAuthority`).
- Self-issued: `NimbusJwtEncoder` / `NimbusJwtDecoder` with an RSA key pair; or delegate to Spring Authorization Server / Keycloak.

## Go
- `github.com/golang-jwt/jwt/v5`: `jwt.Parse(token, keyFunc, jwt.WithValidMethods([]string{"RS256"}), jwt.WithIssuer(iss), jwt.WithAudience(aud), jwt.WithLeeway(30*time.Second))`.
- JWKS: `github.com/MicahParks/keyfunc` or `lestrrat-go/jwx` for remote key sets.
- Middleware parses the bearer header, puts claims into `context.Context`, and returns 401 on failure.

## Output format
```
## What I'm setting up
<stack, self-issued vs external IdP, what's protected>

## Packages + wiring
...

## Token issuing / refresh
...

## Config & secrets
<config shape + where the key actually goes>
```
