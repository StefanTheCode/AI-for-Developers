# wcf-service-migrator - usage

```text
Move LegacyShop.Inventory to CoreWCF on .NET 10. Keep the SOAP contract and prove it with contract tests.
```

```text
Our WCF clients use system.serviceModel config - move them to code-based bindings for .NET 10.
```

```text
We own every client of this WCF service. Plan a move to gRPC.
```

Default path is CoreWCF: same contract, same WSDL, existing clients untouched. gRPC/REST only when you own every client.
