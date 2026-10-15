# How to use - Typed Config Generator

**What it is.** The Options pattern, generalized: replaces scattered string-keyed config lookups with strongly-typed settings objects that are validated at startup, so a misconfigured app fails to boot instead of failing on first use. It detects your stack and uses the idiomatic tool - ASP.NET Core `IOptions` + `ValidateOnStart`, zod / envalid / `@nestjs/config`, pydantic-settings, Spring `@ConfigurationProperties` + `@Validated`, or Go envconfig / viper with validation.

**When to reach for it.** Replacing magic `IConfiguration["..."]`, `process.env.X`, `os.environ[...]`, `@Value`, or `os.Getenv` lookups, or whenever you want config that fails fast.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Convert this config to the options pattern with validation." (.NET)
- "IOptions vs IOptionsSnapshot for this - which do I use?"
- "Replace process.env everywhere with a validated zod config."
- "Add pydantic-settings to my FastAPI app."
- "Turn these @Value fields into a validated @ConfigurationProperties record."
- "Load and validate env config for my Go service."

**Get the best out of it.** Ask for startup validation explicitly so misconfig stops boot, not a 3am request. Group settings per concern. Keep secret values in a store, not in source - pair with `secrets-config-auditor`.

**What you get.** A list of the lookups it found, typed settings classes/schemas with validation rules, the binding + startup-validation code, and the call-site replacements.

**Won't do.** It won't embed secrets in the settings types or defaults - those bind from env/configuration/the secret store.
