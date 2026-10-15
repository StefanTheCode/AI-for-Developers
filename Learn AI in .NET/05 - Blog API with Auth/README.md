# 05 · Blog API with Auth

> **Intermediate project** · Focus: **JWT authentication**, **validation**, and **integration tests**.

A blog REST API where anyone can read posts, but you must be logged in to write —
and you can only edit your **own** posts. Passwords are hashed with PBKDF2, auth is
via JWT bearer tokens, and the whole thing is covered by end-to-end tests.

---

## What you'll learn

| Concept | Where to look |
|---|---|
| Hashing passwords with PBKDF2 (salt, iterations, fixed-time compare) | [`Auth/PasswordHasher.cs`](src/BlogApi/Auth/PasswordHasher.cs) |
| Issuing signed **JWT** access tokens | [`Auth/JwtTokenService.cs`](src/BlogApi/Auth/JwtTokenService.cs) |
| Validating JWTs + `RequireAuthorization()` | [`Program.cs`](src/BlogApi/Program.cs) |
| Ownership checks (403 vs 401 vs 404) | [`Program.cs`](src/BlogApi/Program.cs) |
| Boundary validation with DataAnnotations | [`RequestValidator.cs`](src/BlogApi/RequestValidator.cs) |
| **Integration tests** with `WebApplicationFactory` + in-memory SQLite | [`tests/BlogApi.Tests`](tests/BlogApi.Tests) |

---

## Run it

```bash
cd "05 - Blog API with Auth"
dotnet run --project src/BlogApi
```

Starts on `http://localhost:5100`. Use [`src/BlogApi/BlogApi.http`](src/BlogApi/BlogApi.http)
(it captures the login token and reuses it automatically), or curl:

```bash
# register
curl -X POST http://localhost:5100/auth/register -H "Content-Type: application/json" -d '{"username":"stefan","password":"supersecret1"}'

# login -> returns { "token": "..." }
TOKEN=$(curl -s -X POST http://localhost:5100/auth/login -H "Content-Type: application/json" -d '{"username":"stefan","password":"supersecret1"}' | jq -r .token)

# create a post (authenticated)
curl -X POST http://localhost:5100/posts -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"title":"Hi","content":"First post"}'
```

## Run the tests

```bash
dotnet test
```

The tests boot the real app in memory and cover: register/login, 400 on weak
passwords, 401 on wrong password, public reads, 401 without a token, the full
create-then-read flow, and a 403 when editing someone else's post.

---

## The security details that matter

- **Passwords** are never stored. We keep a PBKDF2 hash with a per-user salt and
  100k iterations, and verify with a fixed-time comparison ([`PasswordHasher.cs`](src/BlogApi/Auth/PasswordHasher.cs)).
- **JWTs are signed, not secret.** Anyone can read the claims; the signature just
  proves we issued the token and it wasn't tampered with. So we never put secrets
  in a token and always validate the signature ([`Program.cs`](src/BlogApi/Program.cs)).
- **Status codes tell the truth:** `401` = not authenticated, `403` = authenticated
  but not allowed (editing another user's post), `404` = doesn't exist.
- Login returns the **same 401** for "no such user" and "wrong password" — never
  reveal which, or you help attackers enumerate accounts.

> 🔒 **Never ship the dev key.** `appsettings.json` has an empty `Jwt:Key`, so the
> app falls back to a hard-coded DEV key just so it runs. In production, set a
> strong `Jwt:Key` via user-secrets, environment variables, or a secret store:
> ```bash
> dotnet user-secrets init --project src/BlogApi
> dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/BlogApi
> ```

---

## 🤖 Try these prompts with your AI assistant

**Add refresh tokens:**
> "Add refresh-token support: short-lived access tokens + long-lived refresh
> tokens stored server-side, with a `POST /auth/refresh` endpoint. Explain the
> rotation and revocation strategy before writing code."

**Add roles:**
> "Introduce an `Admin` role that can delete ANY post. Add the claim to the JWT,
> enforce it with a policy, and add tests for both allowed and forbidden cases."

**Practise the review skill:**
> "Audit this API against the OWASP API Top 10. Focus on broken auth, broken
> object-level authorization, and mass assignment. Give me a findings list first."

**Grow the test suite:**
> "What edge cases am I missing in the tests? Add tests for: expired token,
> tampered token, editing a non-existent post, and duplicate username on register."

> 💡 **The mindset:** ask for a *plan before edits*, give *outcomes + constraints*,
> and treat every auth change as something to test, not just eyeball.

---

## Next

Move on to **[06 · Background Job Runner](../06%20-%20Background%20Job%20Runner)** — hosted services, channels + retries.
