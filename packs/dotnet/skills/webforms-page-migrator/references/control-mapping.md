# WebForms control and API mapping

| WebForms | Razor Pages (.NET 10) | Blazor (.NET 10) |
|---|---|---|
| `<asp:Label>`, `<asp:Literal>` | `@Model.X` (encoded by default) | `@x` |
| `<asp:HyperLink NavigateUrl="~/X.aspx">` | `<a asp-page="/X">` or `href="/X.aspx"` | `<a href="/X.aspx">` |
| `<asp:Button OnClick="Save_Click">` | `<button type="submit" asp-page-handler="Save">` + `OnPostSave()` | `<button @onclick="Save">` |
| `<asp:LinkButton CommandName="Remove" CommandArgument="5">` | `<form method="post" asp-page-handler="Remove" asp-route-id="5">` | `<button @onclick="() => Remove(5)">` |
| `<asp:TextBox>` | `<input asp-for="X">` + `[BindProperty]` | `<InputText @bind-Value="model.X">` |
| `<asp:DropDownList AutoPostBack="true">` | `<select asp-for="Cat" asp-items="..." onchange="this.form.submit()">` in a GET form | `<InputSelect @bind-Value>` + `@bind:after` |
| `<asp:CheckBox>` | `<input asp-for="X" type="checkbox">` | `<InputCheckbox>` |
| `<asp:HiddenField>` | `<input type="hidden" asp-for>` | component field |
| `<asp:Panel Visible="false">` | `@if (...) { }` | `@if (...) { }` |
| `<asp:Repeater>` / `<asp:ListView>` | `@foreach` | `@foreach` |
| `<asp:GridView AllowPaging AllowSorting>` | `<table>` + `?page=&sort=` + `Skip/Take` | `<QuickGrid>` + `Pagination` |
| GridView `EditItemTemplate`, `RowUpdating` | separate edit page or per-row form | `<QuickGrid>` + `<EditForm>` |
| `<asp:FormView>` / `<asp:DetailsView>` | a form bound to a model | `<EditForm>` |
| `RequiredFieldValidator` | `[Required]` + `<span asp-validation-for>` | `[Required]` + `<DataAnnotationsValidator>` |
| `RangeValidator Type="Integer"` | `[Range(1, 20)]` | `[Range(1, 20)]` |
| `RegularExpressionValidator` | `[RegularExpression]` | `[RegularExpression]` |
| `CustomValidator` (server) | `IValidatableObject` or a check in the handler + `ModelState.AddModelError` | same |
| `ValidationGroup` | separate forms / handlers | separate `EditForm`s |
| `<asp:UpdatePanel>` | `fetch` to a handler that returns a partial (`Partial("_X", model)`) | not needed (interactive render mode) |
| `<asp:ScriptManager>` | remove | remove |
| `<asp:Image ImageUrl>` | `<img src>` | `<img src>` |
| `<asp:ContentPlaceHolder>` / `<asp:Content>` | `@RenderBody()` / `@RenderSection` | `@Body` / `SectionContent` |
| Master page | `_Layout.cshtml` | `MainLayout.razor` |
| User control (`.ascx`) | view component or partial | component |
| `Page_Load` + `if (!IsPostBack)` | `OnGet()` | `OnInitializedAsync()` |
| `ViewState["X"]` | reload, hidden field, query string, TempData | component field |
| `Session["X"]` | adapters' session while both apps run, then `HttpContext.Session` | pass data in; no session in interactive components |
| `Response.Redirect("~/X.aspx")` | `return RedirectToPage("/X")` (or `Redirect("/X.aspx")` to keep the URL) | `Navigation.NavigateTo("/X.aspx")` |
| `Response.StatusCode = 404` | `return NotFound()` | `NavigationManager.NotFound()` (.NET 10) or render a not-found block |
| `Server.HtmlEncode` | not needed (Razor encodes) | not needed |
| `Request.QueryString["id"]` | `[BindProperty(SupportsGet = true)] int Id` or `Request.Query["id"]` | `[SupplyParameterFromQuery] int Id` |
| `Page.IsValid` | `ModelState.IsValid` | `OnValidSubmit` |
| `Title = "..."` | `ViewData["Title"]` | `<PageTitle>` |
| `<%# Eval("X", "{0:0.00}") %>` | `@item.X.ToString("0.00")` | `@item.X.ToString("0.00")` |
| `<%#: Eval("X") %>` | `@item.X` | `@item.X` |
