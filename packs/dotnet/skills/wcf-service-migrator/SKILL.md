---
name: wcf-service-migrator
description: (.NET) Move WCF services and WCF clients from .NET Framework to .NET 10 - server side to CoreWCF (keeping the SOAP contract so existing clients keep working) or to gRPC/REST (when you own every client), and client side from system.serviceModel config to code with the WCF Client packages. Covers ServiceContract/DataContract/FaultContract, bindings, metadata (WSDL), and contract tests that prove old and new endpoints answer the same. Use this skill whenever the user mentions WCF, .svc files, CoreWCF, "WCF on .NET 8/10", "replace WCF", "move this SOAP service", system.serviceModel, ChannelFactory, ClientBase, or service references in a migration. Always use this skill for WCF migrations instead of answering from memory.
category: Legacy (.NET)
---

# WCF Service Migrator (beta)

.NET 10 has no WCF server. Two paths - pick with the user before writing code:

| | **A) CoreWCF** (default) | **B) gRPC or REST** |
|---|---|---|
| Contract | Same SOAP contract, same WSDL | New contract |
| Existing clients | Keep working, no change | All must change |
| Effort | Low-medium | Medium-high |
| Pick when | Anyone outside your control calls the service, or there are many clients | You own every client and want to modernize the API |

## Input

The service project: contracts (`[ServiceContract]`, `[OperationContract]`, `[DataContract]`, `[FaultContract]`), implementations, `.svc` files, `system.serviceModel` config (bindings, behaviors, endpoints, security), and every client in the solution (`ChannelFactory<T>`, `ClientBase<T>`, generated service references).

## Path A - CoreWCF

### 1. Lock the contract first

1. Save the current WSDL: `GET /InventoryService.svc?wsdl` (and `?xsd=xsd0` etc.) into `migration/wcf/<Service>.wsdl`.
2. Write contract tests: for each operation, a raw SOAP request (envelope + `SOAPAction` header) and the expected response, including one fault per `FaultContract`. Run them against the old service - that's the baseline. Template: `references/ContractTests.cs`.

### 2. Host it in ASP.NET Core

```bash
dotnet add <ServiceHost> package CoreWCF.Http
dotnet add <ServiceHost> package CoreWCF.Primitives
```

The host can be the new .NET 10 web app or a separate ASP.NET Core project - same path as before either way (`/InventoryService.svc`).

```csharp
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
builder.Services.AddTransient<InventoryService>();   // DI works in CoreWCF services

var app = builder.Build();

app.UseServiceModel(sb =>
{
    sb.AddService<InventoryService>(o => o.DebugBehavior.IncludeExceptionDetailInFaults = false);
    sb.AddServiceEndpoint<InventoryService, IInventoryService>(
        new BasicHttpBinding { MaxReceivedMessageSize = 1_048_576 },   // copy limits/timeouts from the old config
        "/InventoryService.svc");
});

var smb = app.Services.GetRequiredService<ServiceMetadataBehavior>();
smb.HttpGetEnabled = true;   // ?wsdl like before
```

### 3. Port the code

- `using System.ServiceModel;` -> `using CoreWCF;` for `ServiceContract`, `OperationContract`, `FaultContract`, `FaultException<T>`, `FaultReason`. `DataContract` / `DataMember` stay in `System.Runtime.Serialization`.
- Keep every `Namespace`, `Name` and `Order` exactly as it was - they are the contract.
- Copy binding settings from `Web.config` into code (message size, timeouts, security mode).
- Data access inside the service follows the data layer's migration (`ef6-to-efcore-migrator`).

### 4. Prove it

1. Compare the new WSDL with the saved one: same target namespace, port types, operation names, message parts, soapAction values. Differences in host/port only are fine.
2. Run the contract tests against the new endpoint. Faults must have the same code, reason and detail.
3. Point one existing client at the new URL and run its flow (in LegacyShop: the product page's stock check and checkout).

## Path B - gRPC or REST

Only after the user confirms they own every client.

1. Design the new contract from the operations (one RPC / endpoint per operation; faults -> gRPC status codes with details, or problem details for REST).
2. Run old and new side by side; move clients one by one; retire the SOAP endpoint last.
3. Contract tests for the new API, plus a temporary adapter test that calls both and compares results for the same inputs.

## Clients on .NET 10

The client side of WCF still exists on modern .NET (WCF Client packages: `System.ServiceModel.Http`, `System.ServiceModel.Primitives`, ...), but without `system.serviceModel` config:

```csharp
var binding = new BasicHttpBinding { SendTimeout = TimeSpan.FromSeconds(5), OpenTimeout = TimeSpan.FromSeconds(5) };
var address = new EndpointAddress(configuration["Inventory:Url"]);
var factory = new ChannelFactory<IInventoryService>(binding, address);
```

- Register the factory as a singleton; create a channel per call and `Close()` / `Abort()` like before.
- Or regenerate the proxy with `dotnet-svcutil` against the WSDL.
- Move the endpoint URL to `appsettings.json` (per environment) - not to code.

## Rules

- Never change a namespace, operation name or data member order on path A.
- Contract tests before and after, on the same requests.
- The old `.svc` keeps running until every client is confirmed on the new endpoint.
