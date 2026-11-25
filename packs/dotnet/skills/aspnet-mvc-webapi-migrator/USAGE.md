# aspnet-mvc-webapi-migrator - usage

```text
Migrate ProductsController (Web API 2) into LegacyShop.AspNetCore. Same routes, same JSON, parity tests green.
```

```text
Move the MVC 5 ReportsController and its child action - the TopProducts child action becomes a view component.
```

```text
Replace Global.asax, the RequestTimingModule and the ProductImage.ashx handler in the new host.
```

One controller, module or handler per commit. Behavior changes (401 instead of 302, no XML) go to `migration/accepted-differences.md` for a person to approve.
