# Choosing a .NET 10 target for each WebForms page

The scanner suggests a target per page. Check every suggestion against these rules, in order - the first rule that matches wins. Write the reason in one concrete sentence ("signs the user in with FormsAuthentication", not "it's simpler").

| # | If the page... | Target | Why |
|---|---|---|---|
| 1 | Signs users in or out, writes cookies, or redirects with auth | **Razor Pages** | Needs a real HTTP request and response. Interactive Blazor has no `HttpContext` and can't set cookies. |
| 2 | Reads or writes session (directly or through a helper) and has no inline-edit grid | **Razor Pages** | Interactive Blazor has no session. Razor Pages + `ISession` (or the System.Web adapters' remote session while both apps run) keeps it simple. |
| 3 | Is mostly read-only: lists, details, search, reports | **Razor Pages** (GET) | Linkable, cacheable, SEO-friendly URLs. Paging and sorting move to the query string. |
| 4 | Is a form with a few postback handlers (save, delete, search) | **Razor Pages** | Each postback handler becomes a page handler (`OnPostSave`, `OnPostDelete`). Validators become DataAnnotations. |
| 5 | Has an inline-edit grid, master-detail editing, several UpdatePanels or 6+ handlers that update parts of the page | **Blazor (Interactive Server)** | Component state replaces ViewState; QuickGrid + EditForm map cleanly to GridView editing. Admin / back-office screens are a good fit. |
| 6 | The rest of the app is already MVC and the page is simple | **MVC view + controller action** | Don't introduce a third UI model for one page. |
| 7 | Is static (error page, about, terms) | **Razor Pages** (or a static file) | Nothing to migrate but markup. |

## Things that change the call

- **Public URLs:** keep them. Razor Pages can serve the old URL (`@page "/Product.aspx"`), so links, bookmarks and YARP routing keep working. Clean URLs can come later with redirects.
- **Third-party control suites** (Telerik, DevExpress, Infragistics): check whether the vendor has Blazor or Core equivalents before picking a target. Note it as a human task.
- **Heavy client-side JavaScript already on the page:** Razor Pages + the existing JS is usually less work than rewriting it into Blazor.
- **Team skills:** if nobody on the team knows Blazor, say so in `humanTasks` and prefer Razor Pages unless rule 5 clearly applies.
- **User controls and master pages** follow the pages that use them: master page -> `_Layout.cshtml` (+ view component for logic), user control -> view component (Razor Pages) or component (Blazor).

## Common WebForms features and where they go

| WebForms | Razor Pages | Blazor |
|---|---|---|
| ViewState | hidden fields, query string, TempData, or reload from the database | component fields |
| Postback event handler | page handler (`OnPostX`) | `@onclick` / `OnValidSubmit` |
| UpdatePanel | `fetch` to a page handler returning a partial | built in (interactive render mode) |
| GridView paging / sorting | query string + `Skip/Take` | QuickGrid `Pagination` / sortable columns |
| GridView inline edit | separate edit page or row form | QuickGrid + EditForm |
| Validators | DataAnnotations + `asp-validation-for` | DataAnnotations + `DataAnnotationsValidator` |
| Repeater / ListView | `@foreach` | `@foreach` / QuickGrid |
| Master page | `_Layout.cshtml` | `MainLayout.razor` |
| User control | view component / partial | component |
| `Response.Redirect` | `RedirectToPage` | `NavigationManager.NavigateTo` |
| `Server.HtmlEncode` | Razor encodes by default | Razor encodes by default |
