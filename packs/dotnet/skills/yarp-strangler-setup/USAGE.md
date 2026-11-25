# yarp-strangler-setup - usage

```text
Set up the strangler fig for examples/legacy-shop: a .NET 10 host with YARP in front of LegacyShop.Web, with remote session and remote auth.
```

```text
Move LegacyShop.Core to the System.Web adapters and multi-target it net48;net10.0.
```

You get a new `<App>.AspNetCore` project that proxies everything to the old app, both apps sharing sign-in and session, shared libraries on the adapters - and a green parity run against the new URL to prove nothing changed.
