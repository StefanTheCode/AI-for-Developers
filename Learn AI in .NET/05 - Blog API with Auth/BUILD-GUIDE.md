# 05 · Blog API with Auth — Build Guide

> **Level:** Intermediate · **Time:** 6–10 hours · **You'll need:** .NET 10 SDK, an AI assistant
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it yourself with AI.

---

## The goal

Anyone can read posts. Logged-in users can write. **Only the author** can edit or delete. Covered by integration tests.

**The real goal:** auth is where AI code looks the most convincing and fails the most dangerously. You learn to test security, not eyeball it.

---

## Step-by-step

### Step 1 — Entities + DTOs

`User` (Username with a **unique index**, PasswordHash) and `BlogPost` (AuthorId FK). The DTOs carry validation: password min 8, username 3–50.

### Step 2 — Password hashing

> **Prompt:** "Implement a `PasswordHasher` with PBKDF2 via `Rfc2898DeriveBytes.Pbkdf2`: 16-byte random salt, 100k iterations, SHA256, output stored as `iterations.salt.key`. Verify with a fixed-time comparison. Explain what each of the three protects against."

**Check yourself:** Why store the iteration count inside the hash string?

### Step 3 — Issue JWTs

> **Prompt:** "Create `JwtOptions` bound from config and a `JwtTokenService` that issues HMAC-SHA256 tokens with `sub`, `unique_name`, `jti`, issuer, audience, and expiry. What must never go into a JWT, and why?"

### Step 4 — Validate JWTs

> **Prompt:** "Configure `AddJwtBearer` to validate issuer, audience, lifetime, and signing key. Set `MapInboundClaims = false`. Explain what happens to the `sub` claim if I don't."

**Check yourself:** Where does the signing key come from in production? (Not `appsettings.json` in git.)

### Step 5 — Endpoints with ownership

> **Prompt:** "Add register/login and posts endpoints. Reads are public, writes use `RequireAuthorization()`. On PUT/DELETE, compare the post's AuthorId with the `sub` claim. Give me a table: missing token / wrong user / missing post → status code."

Expected: **401** no token · **403** not your post · **404** doesn't exist · login returns the **same 401** for "no user" and "wrong password".

### Step 6 — Integration tests

> **Prompt:** "Create an xUnit project with `WebApplicationFactory<Program>`. Replace the DbContext with SQLite `:memory:` and keep the connection open for the fixture. Test: register+login, weak password 400, wrong password 401, create without token 401, create+read, and editing another user's post → 403."

Don't forget `public partial class Program;` at the bottom of `Program.cs`.

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| Hash with plain SHA256, or no salt | Rainbow tables crack it in seconds | PBKDF2 / Identity's `PasswordHasher` |
| Put the JWT key in `appsettings.json` | It ends up in git forever | user-secrets / env vars / a vault |
| Set `ValidateLifetime = false` or `ValidateIssuerSigningKey = false` "to fix a 401" | Now anyone can forge tokens | Fix the actual config mismatch |
| Check *authentication* but not *ownership* | Broken Object Level Authorization, #1 in the OWASP API Top 10 | Compare `AuthorId` with `sub` |
| Return "user not found" vs. "wrong password" | Username enumeration | Same 401 for both |
| Read `ClaimTypes.NameIdentifier` while mapping is off | Returns null → crash or wrong user | Be consistent: `sub` + `MapInboundClaims = false` |
| Test with the EF InMemory provider | It doesn't enforce unique indexes or FKs | SQLite in-memory |

### 🔍 Review moment: our own code

`/auth/register` runs `AnyAsync(username)` and *then* inserts. Two simultaneous registrations for the same name pass the check, and the unique index throws. What does the client get? Write a test for it and make it return 409.

---

## 🔨 Break it on purpose

1. Log in, take the token, Base64-decode the middle part, and change `sub` to another user's id. Re-encode it and call PUT. What happens, and **why**?
2. Set `ExpiryMinutes` to 1 and wait 2 minutes. (Note the 30-second `ClockSkew`.)
3. Comment out the ownership check and run the tests. Exactly one test should fail. Does it?

---

## ✅ Definition of done

- [ ] All tests pass with `dotnet test`
- [ ] 401 / 403 / 404 are used correctly, and a test proves each one
- [ ] No real signing key in any committed file
- [ ] A tampered token is rejected
- [ ] The duplicate-username race returns 409, not 500
- [ ] You can explain "signed, not encrypted" to a junior dev
- [ ] You wrote down **one thing the AI got wrong**

## 🚀 Stretch goal

Refresh tokens with rotation + revocation, and an `Admin` role enforced by a policy, both with tests.

---

**Doing the [October Build Challenge](../../CHALLENGE.md)?** Share your repo and the one thing AI got wrong in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. That's also where the Advanced & Expert levels come with walkthroughs, roadmaps, and someone to ask when you get stuck.
