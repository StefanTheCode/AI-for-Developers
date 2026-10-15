---
name: typed-config-generator
description: Generate strongly-typed, validated-at-startup configuration in any stack - the Options pattern generalized. Covers ASP.NET Core IOptions/IOptionsSnapshot/IOptionsMonitor with data annotations and ValidateOnStart, TypeScript zod/envalid and @nestjs/config, Python pydantic-settings, Spring Boot @ConfigurationProperties + @Validated, and Go envconfig/viper with validation. Use this skill whenever the user wants strongly-typed config, the options pattern, typed settings, env var validation, to replace magic configuration strings or scattered process.env/os.environ/getenv lookups, fail-fast on missing config, or asks "how do I bind config / validate settings / stop using IConfiguration or process.env everywhere". Always use this skill for typed-config requests; it generates the settings type, binding, and validation.
category: DevOps
version: 1.0.0
---

# Typed Config Generator

Replace scattered string-keyed config lookups (`IConfiguration["Key"]`, `process.env.X`, `os.environ["X"]`, `@Value("${x}")`, `os.Getenv("X")`) with one strongly-typed, validated settings object per concern that **fails fast at startup** if misconfigured.

## Input - point it at your project
Works on a target: a **project** or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`).

## Step 1 - detect the stack and find magic-string config

| Manifest | Idiomatic typed config | Find lookups to replace |
|---|---|---|
| `*.csproj` | Options pattern + `ValidateOnStart` | `grep -rn "Configuration\[\|GetValue<\|GetSection(" --include=*.cs` |
| `package.json` | zod / envalid / `@nestjs/config` | `grep -rn "process\.env\." --include=*.ts --include=*.js \| grep -v node_modules` |
| `pyproject.toml`, `requirements*.txt` | pydantic-settings `BaseSettings` | `grep -rn "os\.environ\|os\.getenv\|getenv(" --include=*.py` |
| `pom.xml`, `build.gradle(.kts)` | `@ConfigurationProperties` + `@Validated` | `grep -rn "@Value(\"\${" <target>/src` / `Environment.getProperty` |
| `go.mod` | envconfig / caarlos0/env / viper + validator | `grep -rn "os.Getenv\|os.LookupEnv\|viper.Get" --include=*.go` |

## Universal principles (every stack)
1. **One typed object per concern** (`JwtSettings`, `DatabaseSettings`), grouped under a section/prefix. No string keys outside the binding code.
2. **Validate at startup.** Required, ranges, formats, URLs, min lengths. A misconfigured app should fail to boot with a clear message, not throw at 3am on first use.
3. **Parse, don't read raw strings.** Numbers, booleans, durations, enums, and URLs are converted once, at the edge.
4. **Defaults only for safe values** (timeouts, page sizes). Never default a secret or a production URL.
5. **Inject the settings object**, don't reach for a global env reader inside business code. Makes it testable.
6. **Secrets bind from the secret store / env**, never from values in source (`secrets-config-auditor`).
7. **Immutable after startup** unless reload is a real requirement - then make reload explicit.
8. **Don't log the whole config object** - it contains secrets. Redact.

## C# / .NET (Options pattern)
```csharp
public class JwtOptions
{
    public const string Section = "Jwt";
    [Required] public string Issuer { get; init; } = default!;
    [Required] public string Audience { get; init; } = default!;
    [Required, MinLength(32)] public string Key { get; init; } = default!;
    [Range(1, 1440)] public int AccessTokenMinutes { get; init; } = 15;
}
```
```csharp
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();          // fail at startup, not at first use
```
- Rules annotations can't express: implement `IValidateOptions<JwtOptions>` (or use FluentValidation inside it); cross-field checks go there too.
- The options validation source generator (`[OptionsValidator]` on a `partial class` implementing `IValidateOptions<T>`) avoids reflection and is AOT-friendly.

Which interface to inject:
- **`IOptions<T>`** - singleton, value fixed at startup. Most config.
- **`IOptionsSnapshot<T>`** - scoped, re-read per request. For values that change and need per-request freshness.
- **`IOptionsMonitor<T>`** - singleton with change notifications. For singletons that must react to config reloads.
Don't inject `IOptionsSnapshot` into a singleton.

## TypeScript / Node
- **zod** (most common): parse `process.env` once in `config.ts`, export the typed result, import it everywhere.
```ts
import { z } from 'zod';
const Env = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']),
  PORT: z.coerce.number().int().default(3000),
  DATABASE_URL: z.string().url(),
  JWT_KEY: z.string().min(32),
});
export const config = Env.parse(process.env);   // throws at startup with every missing/invalid key
```
- **envalid:** `cleanEnv(process.env, { PORT: port({ default: 3000 }), DATABASE_URL: url() })` - same idea, purpose-built for env vars.
- **NestJS:** `ConfigModule.forRoot({ isGlobal: true, validate })` where `validate` runs a zod schema (or `validationSchema` with Joi); group with `registerAs('jwt', () => ({...}))` and inject via `ConfigType<typeof jwtConfig>`.
- Never ship server config to the browser: framework public prefixes (`NEXT_PUBLIC_*`, `VITE_*`) get bundled.

## Python (pydantic-settings)
```python
from pydantic import Field, PostgresDsn
from pydantic_settings import BaseSettings, SettingsConfigDict

class JwtSettings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="JWT_", env_file=".env", extra="ignore")
    issuer: str
    audience: str
    key: str = Field(min_length=32)
    access_token_minutes: int = Field(default=15, ge=1, le=1440)

settings = JwtSettings()   # raises ValidationError at import/startup if invalid
```
- Nested sections: `env_nested_delimiter="__"` (`DB__HOST`). Use `SecretStr` for secrets so they don't print in logs or reprs.
- FastAPI: expose via a cached dependency (`@lru_cache def get_settings()`) so tests can override it. Django: build `settings.py` values from a `BaseSettings` instance.

## Java / Kotlin (Spring Boot)
```java
@Validated
@ConfigurationProperties(prefix = "jwt")
public record JwtProperties(
    @NotBlank String issuer,
    @NotBlank String audience,
    @NotBlank @Size(min = 32) String key,
    @Min(1) @Max(1440) int accessTokenMinutes) {}
```
- Register with `@ConfigurationPropertiesScan` (or `@EnableConfigurationProperties(JwtProperties.class)`); needs `spring-boot-starter-validation` on the classpath for `@Validated` to fail startup.
- Use `Duration`/`DataSize` types for timeouts and sizes (`30s`, `10MB` bind natively). Add `spring-boot-configuration-processor` for IDE metadata.
- Replace `@Value("${...}")` scattered across beans with injected properties records. Kotlin: `data class` with `@ConfigurationProperties`.

## Go
```go
type Config struct {
    Port        int           `envconfig:"PORT" default:"8080"`
    DatabaseURL string        `envconfig:"DATABASE_URL" required:"true"`
    JWTKey      string        `envconfig:"JWT_KEY" required:"true"`
    Timeout     time.Duration `envconfig:"TIMEOUT" default:"5s"`
}

func Load() (Config, error) {
    var c Config
    if err := envconfig.Process("", &c); err != nil { return c, err }
    if len(c.JWTKey) < 32 { return c, errors.New("JWT_KEY must be at least 32 chars") }
    return c, nil
}
```
- Libraries: `github.com/kelseyhightower/envconfig` or `github.com/caarlos0/env` for env vars; `github.com/spf13/viper` for files + env + flags (unmarshal into a struct, then validate with `github.com/go-playground/validator/v10` struct tags).
- Call `Load()` in `main`, exit non-zero on error, pass `Config` (or sub-structs) into constructors.

## Output format
```
## Current state
<stack, magic-string lookups found (file:line), missing validation>

## Settings types
<typed classes/schemas per concern with validation rules>

## Binding + startup validation
<registration code, where it runs, what fails the boot>

## Call-site changes
<lookups replaced with the injected settings object>
```

## How to use it & best prompts
"Convert this config to the options pattern", "strongly-type and validate my env vars", "fail at startup if config is missing", "replace process.env everywhere with a zod config", "pydantic settings for my FastAPI app", "IOptions vs IOptionsSnapshot here". Pairs with `secrets-config-auditor`.
