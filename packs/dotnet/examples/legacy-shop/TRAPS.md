# LegacyShop - planted traps (ground truth)

This is the answer key for the Migration Pack. The assessment report should find these, and a migration to .NET 10 has to deal with every one of them. Search the code for `TRAP T##` or `QUIRK` to jump to each spot.

## Migration traps

| # | Trap | Where | Why it hurts on .NET 10 | Who handles it |
|---|---|---|---|---|
| T01 | ViewState used as data storage (sort order, product id/name/price at load) | `Default.aspx.cs`, `Product.aspx.cs` | No ViewState in Razor Pages / Blazor - state has to be explicit | `webforms-page-migrator` |
| T02 | UpdatePanel partial postback for "Add to cart" | `Product.aspx` | No UpdatePanel - needs a handler + fetch, or Blazor interactivity | `webforms-page-migrator` |
| T03 | AutoPostBack dropdown + GridView server-side paging/sorting events | `Default.aspx(.cs)` | Becomes query-string paging/sorting or component state | `webforms-page-migrator` |
| T04 | Cart = `List<CartLine>` object in InProc Session | `Infrastructure/CartSession.cs` | ASP.NET Core session stores bytes/strings only; old and new app don't share session without remote session | `legacy-config-auth-migrator` + `yarp-strangler-setup` |
| T05 | Master page with logic in code-behind | `Site.Master.cs` | Becomes a layout + a view component / partial | `webforms-page-migrator` |
| T06 | User control reading Session, outside the UpdatePanel (stale after partial postback) | `Controls/MiniCart.ascx(.cs)` | Becomes a view component / Blazor component | `webforms-page-migrator` |
| T07 | GridView inline editing with validators and ValidationGroups | `Admin/Products.aspx` | Rebuilt as a form + DataAnnotations | `webforms-page-migrator` |
| T08 | DbContext used directly from code-behind (no service layer) | `Admin/Products.aspx.cs` | Logic has to move into a service first, or it gets copied twice | `webforms-page-migrator` |
| T09 | Global.asax events + `Application[...]` state + `Session_End` | `Global.asax.cs` | No Global.asax, no Application state, no Session_End | `aspnet-mvc-webapi-migrator` + `legacy-config-auth-migrator` |
| T10 | `IHttpModule` registered in Web.config | `Modules/RequestTimingModule.cs` | Becomes middleware | `aspnet-mvc-webapi-migrator` |
| T11 | `.ashx` handler drawing PNGs with System.Drawing (GDI+) | `Handlers/ProductImage.ashx.cs` | System.Drawing.Common is Windows-only on .NET 6+ - blocks Linux containers | `aspnet-mvc-webapi-migrator` (endpoint) + image library decision (human) |
| T12 | `HttpContext.Current` inside a class library | `LegacyShop.Core/CurrentUser.cs` | Library can't target .NET 10 without System.Web adapters or an abstraction | `yarp-strangler-setup` (adapters) |
| T13 | `ConfigurationManager.AppSettings` inside a library | `LegacyShop.Core/AppSettings.cs` | No Web.config; use IConfiguration / IOptions | `legacy-config-auth-migrator` |
| T14 | `BinaryFormatter` disk cache | `Infrastructure/DiskCache.cs` | Removed in .NET 9+ (throws) and insecure | `legacy-config-auth-migrator` (replace) |
| T15 | System.Web.Optimization bundling | `App_Start/BundleConfig.cs`, `Site.Master`, `_Layout.cshtml` | Doesn't exist in ASP.NET Core | `aspnet-mvc-webapi-migrator` |
| T16 | Web.config transforms for Release | `Web.Release.config` | Becomes appsettings.{Environment}.json + environment variables | `legacy-config-auth-migrator` |
| T17 | Forms auth with a hand-made ticket (role + customer id in userData), roles via `Application_PostAuthenticateRequest`, `<authorization>` in `<location path="Admin">` | `Account/Login.aspx.cs`, `Global.asax.cs`, `Web.config` | ASP.NET Core can't read the Forms cookie; authorization moves to policies | `legacy-config-auth-migrator` (remote auth during the move) |
| T18 | Fixed `machineKey` (fake demo keys) | `Web.config` | Encrypts the auth cookie and ViewState - Data Protection replaces it | `legacy-config-auth-migrator` |
| T19 | Web API 2: `ApiController`, `IHttpActionResult`, `[FromUri]`, `RoutePrefix`, `GlobalConfiguration` | `Api/ProductsController.cs`, `App_Start/WebApiConfig.cs` | ASP.NET Core controllers / minimal APIs, different binding rules | `aspnet-mvc-webapi-migrator` |
| T19b | `[Authorize]` API returns **302 to Login.aspx** instead of 401 (Forms auth redirect) | `Api/OrdersController.cs` | Behavior changes on .NET 10 (401) - API clients that relied on 302 break | human decision, parity test flags it |
| T20 | Web API reads `HttpContext.Current.User` through a static helper | `Api/OrdersController.cs` -> `CurrentUser` | Needs `User` / `IHttpContextAccessor` | `aspnet-mvc-webapi-migrator` |
| T21 | MVC 5 child action (`[ChildActionOnly]` + `Html.Action`), `OutputCache`, `ViewBag`, a second copy of the layout | `Controllers/ReportsController.cs`, `Views/` | No child actions in ASP.NET Core (view components); OutputCache attribute is different | `aspnet-mvc-webapi-migrator` |
| T22 | Newtonsoft.Json camelCase formatter + XML content negotiation still on | `App_Start/WebApiConfig.cs` | .NET 10 uses System.Text.Json and JSON only - `Accept: application/xml` stops working | `aspnet-mvc-webapi-migrator` (decide: keep XML or not) |
| T23 | WCF service with `FaultContract`, `DataContract` namespaces, config-based basicHttpBinding + mex | `LegacyShop.Inventory/` | No WCF server on .NET 10 - CoreWCF (same contract) or gRPC/REST (new contract) | `wcf-service-migrator` |
| T24 | WCF client: static `ChannelFactory` from a config endpoint | `Services/InventoryClient.cs`, `Web.config` | No `system.serviceModel` config on .NET 10 - build the binding in code | `wcf-service-migrator` |
| T25 | EF6 lazy loading + a context that is never disposed -> N+1 queries | `ShopContext.cs`, `OrderRepository.ForCustomer`, `Api/OrdersController.cs` | EF Core doesn't lazy load by default - the code returns empty navigations or throws | `ef6-to-efcore-migrator` |
| T26 | EF6 database initializer + `Seed` | `ShopInitializer.cs`, `Global.asax.cs` | No initializers in EF Core - migrations + seeding | `ef6-to-efcore-migrator` |
| T26b | Deleting a product cascades into order lines (required FK convention) - order history disappears | `Admin/Products.aspx.cs` | Must be a conscious decision in the new model (Restrict vs Cascade) | `ef6-to-efcore-migrator` + human |
| T27 | EF6-only APIs: `DbModelBuilder` fluent config (`HasRequired`), `DbFunctions.TruncateTime`, string `Include` | `ShopContext.cs`, `OrderRepository.cs`, `ProductRepository.cs` | Rewritten for EF Core (`HasOne`, `.Date`, lambda Include) | `ef6-to-efcore-migrator` |
| T28 | `packages.config`, old-style `.csproj`, `EnsureNuGetPackageBuildImports` target | all projects | SDK-style + PackageReference (+ CPM) first | `legacy-package-migrator` |
| T29 | Newtonsoft.Json 12.0.3 has a known high-severity advisory (GHSA-5crp-9r3c-p9vr) | `LegacyShop.Web/packages.config` | Flag it, upgrade or drop it during the move | `dependency-vuln-scanner` |

## Behavior quirks (keep them or change them on purpose)

Parity tests must catch every one of these. A migration that "fixes" them silently changes what customers see.

| # | Quirk | Where |
|---|---|---|
| Q1 | Clicking the Name column twice does NOT sort Z-A ("Name DESC" isn't handled) | `Default.aspx.cs`, `ProductRepository.GetActive` |
| Q2 | The cart keeps the FIRST price it saw for a product | `CartSession.Add` |
| Q3 | "Add to cart" uses the price from ViewState (price at page load), not the current price | `Product.aspx.cs` |
| Q4 | The stored order total ignores coupon, shipping and VAT - it differs from the total on the cart page | `Cart.aspx.cs`, `OrderRepository.Create` |
| Q5 | "3 for 2" on stickers is counted per cart line, not per cart | `PriceCalculator` |
| Q6 | The LEGACY10 coupon is case-insensitive, applied after the sticker discount and rounded DOWN to cents | `PriceCalculator` |
| Q7 | VAT is on goods only (shipping is VAT-free) and uses banker's rounding | `PriceCalculator` |
| Q8 | Admin price input is parsed with the server's current culture | `Admin/Products.aspx.cs` |
| Q9 | `Release` on the WCF service does nothing | `InventoryService.svc.cs` |

## Counts (for checking the assessment inventory)

| Item | Count |
|---|---|
| Projects | 4 (Web, Inventory, Data, Core) |
| WebForms pages (.aspx) | 6 (Default, Product, Cart, Account/Login, Admin/Products, Error) |
| Master pages / user controls / handlers | 1 / 1 / 1 |
| Web API 2 controllers / actions | 2 / 5 |
| MVC 5 controllers / actions | 1 / 2 (1 child action) |
| WCF services / operations | 1 / 3 |
| EF6 DbContexts / entities | 1 / 5 |
| HTTP modules | 1 |
| packages.config files | 3 |
