# legacy-config-auth-migrator - usage

```text
Map every Web.config setting in LegacyShop.Web to appsettings + Options classes and write the config map.
```

```text
The login page is next. Move Forms auth to ASP.NET Core cookie auth - keep the roles and the 8-hour sliding expiration.
```

```text
Replace the BinaryFormatter disk cache. What happens to the files that are already there?
```

Two steps for each piece: first "while both apps run" (remote auth, remote session), then "final". Auth changes ship alone, with a rollback plan.
