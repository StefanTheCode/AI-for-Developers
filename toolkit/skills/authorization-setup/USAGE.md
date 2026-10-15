# How to use - Authorization Setup

**What it is.** Designs API authorization for your stack - named policies/permissions, role and claim checks, custom handlers for business rules, resource-based (ownership) checks that block IDOR, and deny-by-default wiring. It detects the framework first: ASP.NET Core policies and handlers, NestJS guards, Express/Fastify middleware or CASL, FastAPI dependencies, Django/DRF permission classes, Spring Security, or Go middleware / Casbin.

**When to reach for it.** Restricting endpoints, replacing scattered role strings, adding RBAC/permissions, or enforcing "can this user act on this object".

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Design authorization policies for my ASP.NET Core API."
- "Add a permissions guard to my NestJS app, deny by default."
- "Stop users from accessing other users' orders in this DRF API."
- "Restrict /admin to ADMIN in my Spring Boot service and add method security."
- "Add role-based middleware to my Go chi router."

**Get the best out of it.**
- Ask for **resource-based checks** where ownership or tenancy matters - that's what blocks IDOR.
- Ask for **deny-by-default** (fallback policy, global guard, `anyRequest().authenticated()`) so new endpoints aren't accidentally public.
- Ask for the **tests** - one allowed, one denied, one "other user's resource" per policy.

**What you get.** A gap list of the current setup, a permissions/roles table, ready-to-paste policy/guard/handler code with endpoint wiring, and the tests to add.

**Won't do.** It won't issue or validate tokens - that's `jwt-auth-setup`; this is the authorization half. Pairs with `api-security-auditor`.
