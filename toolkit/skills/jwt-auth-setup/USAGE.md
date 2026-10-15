# How to use - JWT Auth Setup

**What it is.** A skill that wires up JWT authentication in your API with secure defaults - token validation (algorithm pinning, issuer/audience, tight clock skew), a token-issuing service, revocable rotating refresh tokens, and protected endpoints. It detects the stack first: ASP.NET Core JwtBearer, Node (jose, passport-jwt, NestJS), Python (PyJWT, FastAPI, DRF SimpleJWT), Spring Security Resource Server, or Go (golang-jwt).

**When to reach for it.** Securing an API, adding login, setting up refresh tokens, validating tokens from an identity provider (Entra ID, Auth0, Keycloak, Cognito), or reviewing auth you already have.

**How to use it.** Point it at your project (a path or GitHub URL) to review/harden existing auth, or for a new setup say whether you're **issuing tokens yourself** or **validating tokens from an external IdP**. Example prompts:
- "Add JWT auth to my ASP.NET Core API - I'll handle login myself, with refresh tokens."
- "Validate Auth0 tokens in my NestJS API with JWKS."
- "Set up JWT login with refresh rotation in FastAPI."
- "Make my Spring Boot service a resource server for Keycloak."
- "Review the JWT middleware in this Go service."

**How to get the best out of it.**
- **Be clear about issuing vs. validating** - it changes which half of the setup you need.
- **Ask for the refresh-token flow done properly** - short access tokens plus hashed, rotating, revocable refresh tokens. This is where most homegrown auth goes wrong.
- **Let it set the secure defaults** rather than disabling validation to "make it work".
- **Ask where the signing key actually goes** - it steers you to a secret store instead of committed config.

**What you get.** The setup summary, packages and wiring code, token issuing/refresh code, and the config shape with where the secrets live.

**What it won't do.** It won't store your real secrets or set up your IdP tenant. It's not a full user-management system (it points you to ASP.NET Core Identity, Spring Authorization Server, Keycloak, or similar). For permission design, use `authorization-setup`.
