---
name: webforms-page-migrator
description: (.NET) Migrate one ASP.NET WebForms page (with its code-behind, master page and user controls) to .NET 10 - Razor Pages, Blazor or an MVC view, whichever the assessment picked for that page - keeping the same URL and the same behavior, proven by parity tests. Use this skill whenever the user wants to "migrate this .aspx page", "convert WebForms to Razor Pages", "WebForms to Blazor", "replace this GridView / UpdatePanel / ViewState", "port the master page", or is moving pages one by one in a strangler-fig migration. Always use this skill for WebForms page migrations instead of rewriting from memory; it inventories the page's behavior first, maps every control and event, and only switches the route when the parity tests are green.
category: Legacy (.NET)
---

# WebForms Page Migrator (beta)

One page per run. The page keeps its URL, its behavior and its quirks - unless a person decided otherwise. The new page lives in the .NET 10 host (`yarp-strangler-setup`); because migrated endpoints win over the YARP fallback, the route switches the moment the page exists and builds.

## Input

- The page: `.aspx` + `.aspx.cs` + `.designer.cs`, its master page, its user controls.
- The target from `migration/migration-assessment.json` (`units[].target`). No assessment? Pick with the rules in the `migration-assessment-report` skill's `references/webforms-target-rules.md` and say which rule you used.
- The parity scenarios and flows that cover this page (`migration-safety-net`). If there are none, write them first.

## Process

### 1. Inventory the page's behavior (before writing anything)

Write a short list in your reply:

- **Inputs:** query string, form fields, route values, cookies, session keys, ViewState keys.
- **Events:** every handler in the code-behind (`Page_Load`, `X_Click`, `Grid_RowUpdating`, `List_SelectedIndexChanged`) and what each one changes.
- **Outputs:** what's shown, redirects, status codes (404s set in code), headers.
- **Data:** every database / service call (often straight from code-behind).
- **Rules and quirks:** validation ranges, defaults, "keeps the first price", culture-dependent parsing. Check `TRAPS.md` / the assessment risks.

### 2. Pull logic out of the code-behind first

If the code-behind talks to the database or holds business rules, move that code into a service class in a shared library (the one both apps can use through the adapters), and call it from the OLD page too. Run the parity tests. Now both pages can share the same logic, and the migration only translates UI.

### 3. Write the new page

Same URL. Razor Pages: `@page "/Product.aspx"` (add `"/Product"` too only if you want a clean alias). Blazor: `@page "/Admin/Products.aspx"`. Map with `references/control-mapping.md`:

- **Postback handlers -> page handlers** (`OnPostAddToCart`) or component methods.
- **ViewState -> explicit state.** Decide per key: reload from the database, hidden field, query string, TempData, or component field. ViewState that held "the price at page load" is a QUIRK - keep the behavior (hidden field) unless a person said otherwise.
- **Validators -> DataAnnotations** with the same ranges and messages.
- **Session -> the same session.** While both apps run, use `System.Web.HttpContext.Current.Session` through the adapters on endpoints marked `.RequireSystemWebAdapterSession()` / `[Session]`, so old and new pages share the cart. Switch to `ISession` when the last page using that key has moved.
- **Master page -> `_Layout.cshtml`**, master code-behind -> a view component. Keep the visible header text identical (parity compares text).
- **User controls -> view components** (Razor Pages) or **components** (Blazor).
- **Auth:** pages under a `<location>` rule get the same rule as a policy or `[Authorize(Roles = "...")]`.
- **Blazor pages:** `@rendermode InteractiveServer`. No `HttpContext`, no session, no cookies inside interactive components - load what you need in a service or pass it in from a Razor Page / SSR wrapper.

### 4. Test it

1. `dotnet build` the new host.
2. Run the parity scenarios for this page against the new host. Run the flow tests.
3. Every difference is either a bug to fix, or an entry in `migration/accepted-differences.md` that a person approves. Never edit the baseline to make it pass.

### 5. Finish the slice

- Commit: `migrate: Product.aspx -> Razor Pages`.
- Update `migration/MIGRATION.md` (if the orchestrator runs this): page moved, tests green, open human tasks.
- Leave the old `.aspx` in place - no traffic reaches it anymore. It's deleted in the decommission phase, after sign-off.

## Rules

- One page per commit. Never two pages "while we're at it".
- Same URL, same status codes, same redirects, same visible text - or a logged, approved difference.
- Don't fix quirks silently. Ask.
- Don't introduce a JavaScript framework. Razor Pages + a few lines of `fetch` replace an UpdatePanel; Blazor replaces heavy interactivity.

## Example: Default.aspx -> Razor Pages

Old: DropDownList with `AutoPostBack`, GridView with server paging and sorting, sort order in ViewState.
New: a GET form (`<select name="cat" onchange="this.form.submit()">`), paging and sorting in the query string (`?cat=2&page=2&sort=Price`), a plain `<table>` with links for sorting and paging. The quirk "clicking Name twice doesn't sort Z-A" stays until someone approves fixing it - the parity test for `?sort=Name DESC` would catch it.
