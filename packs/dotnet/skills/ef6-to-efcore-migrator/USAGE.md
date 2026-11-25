# ef6-to-efcore-migrator - usage

```text
Get LegacyShop.Data onto .NET 10 with EF 6.5 first - no model changes.
```

```text
Port ShopContext to EF Core. Keep the existing database, add a baseline migration, and write query parity tests for every repository method.
```

```text
What breaks if we turn off lazy loading in this data layer?
```

Bridge first (EF 6.5 on .NET 10), EF Core per context second, with query parity tests against the same database.
