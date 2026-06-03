<div align="center">
  <img src="TokenIDP.svg" alt="TokenIDP Logo" width="180">

  <p>
    A modern, tenant-aware Identity Provider for OAuth 2.x and OpenID Connect,
    built on .NET 10 with an admin portal and application SDKs.
  </p>

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB)](https://react.dev/)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031)](https://angular.dev/)
[![Status](https://img.shields.io/badge/status-alpha-orange)](#project-status)

</div>

---

For more information, see the [TokenIDP website](https://www.tokenidp.com).

## Why TokenIDP

TokenIDP is designed for teams that need more than a login screen. It provides a complete identity platform with authorization flows, tenant isolation, client and API-resource management, operational workers, and ready-to-use SDKs for modern frontend applications.

The project is organized as a product-grade identity system: domain rules live in the backend, administrative workflows are surfaced through a React portal, and client applications integrate through framework SDKs instead of duplicating OAuth details.

## Platform Capabilities

- OAuth and OpenID Connect flows for browser, service, and device-oriented clients.
- Authorization Code with PKCE, Client Credentials, Password, Refresh Token, Device Authorization, introspection, revocation, logout, discovery, and user-info endpoints.
- Tenant-aware identity, token, client, role, permission, and configuration management.
- Multi-factor authentication support with email-backed verification workflows.
- External identity-provider integration with state, nonce, PKCE, and local user provisioning.
- JWT and reference token issuance, refresh-token rotation, token revocation, and reuse detection.
- Admin APIs for tenants, applications, users, roles, permissions, API resources, settings, activities, tokens, and dashboard metrics.
- Background workers for token lifecycle maintenance, activity projection, outbox processing, email dispatch, and dashboard metrics.
- React and Angular SDKs for Authorization Code + PKCE integration.
- React-based admin portal for day-to-day identity administration.

## Architecture

TokenIDP follows a layered architecture so that identity rules, infrastructure, and hosting concerns stay separate.

| Area           | Path                          | Purpose                                                                                                  |
| -------------- | ----------------------------- | -------------------------------------------------------------------------------------------------------- |
| Domain         | `src/TokenIDP.Domain`         | Aggregates, value objects, domain events, read models, and business invariants.                          |
| Core           | `src/TokenIDP.Core`           | OAuth, admin, security, validation, application services, endpoints, and abstractions.                   |
| Infrastructure | `src/TokenIDP.Infrastructure` | EF Core persistence, repositories, migrations, caching, bootstrapping, projections, outbox, and logging. |
| Workers        | `src/TokenIDP.Workers`        | Background processors for tokens, activity, email, and metrics.                                          |
| Server package | `src/TokenIDP.Server`         | Reusable server components and application setup extensions.                                             |
| Service host   | `src/TokenIDP.Service`        | ASP.NET Core host for running TokenIDP as a service.                                                     |
| Tests          | `src/TokenIDP.Tests`          | Unit and integration coverage for OAuth, tenancy, admin, security, caching, and domain behavior.         |
| Admin portal   | `portal`                      | React admin interface.                                                                                   |
| SDKs           | `sdks`                        | React and Angular authentication SDKs.                                                                   |

## Repository Layout

```text
IDP/
+-- src/
|   +-- TokenIDP.sln
|   +-- TokenIDP.Domain/
|   +-- TokenIDP.Core/
|   +-- TokenIDP.Infrastructure/
|   +-- TokenIDP.Workers/
|   +-- TokenIDP.Server/
|   +-- TokenIDP.Service/
|   +-- TokenIDP.Tests/
+-- portal/
+-- sdks/
|   +-- react-idp-sdk/
|   +-- angular-idp-sdk/
+-- scripts/
+-- artifacts/
```

## Getting Started

### Prerequisites

- .NET SDK 10
- SQL Server or Azure SQL
- Node.js compatible with the frontend or SDK you are building
- npm

### Build the Backend

```powershell
cd src
dotnet restore TokenIDP.sln
dotnet build TokenIDP.sln
```

### Run Tests

```powershell
cd src
dotnet test TokenIDP.sln
```

### Run the Identity Service

Update local configuration before starting the service. At minimum, provide a database connection string and token settings appropriate for your environment.

```powershell
cd src/TokenIDP.Service
dotnet run
```

The service host wires TokenIDP through `AddTokenIDPServices(...)` and starts the full identity platform, including admin APIs, OAuth endpoints, persistence, workers, CORS, and bootstrapping.

### Run the Admin Portal

```powershell
cd portal
npm install
npm start
```

The portal is a React application for managing tenants, applications, users, roles, permissions, API resources, settings, tokens, activities, and dashboard views.

## Configuration

Configuration is supplied through standard ASP.NET Core configuration sources. Important sections include:

- `ConnectionStrings:Identity_DB` for the identity database.
- `Bootstrap` for initial admin client, redirect URLs, and seed behavior.
- `TenantResolution` for host, root-domain, development-host, and tenant lookup behavior.
- `TokenOptions` for issuer, audience, signing key, or signing certificate settings.
- `Cors` for allowed frontend origins.
- `Security` for secret-protection keys.

Production deployments should use secure configuration providers or secret stores. Do not commit live database passwords, encryption keys, certificate private keys, or client secrets.

For signing-key details, see [`src/KEY_MANAGEMENT.md`](src/KEY_MANAGEMENT.md).

## SDKs

### React

The React SDK is located in `sdks/react-idp-sdk` and supports Authorization Code + PKCE, login/logout helpers, callback handling, auth state, tenant propagation, and token refresh.

```powershell
cd sdks/react-idp-sdk
npm install
npm run build
npm run pack
```

### Angular

The Angular SDK is located in `sdks/angular-idp-sdk` and provides Angular-native auth configuration, callback handling, login components, auth state, refresh behavior, and tenant options.

```powershell
cd sdks/angular-idp-sdk
npm install
npm run build
npm run pack
```

## Security Model

TokenIDP treats the backend as the source of truth. Frontend validation improves usability, but tenant boundaries, OAuth rules, role and permission checks, token ownership, secret validation, and lifecycle transitions are enforced server-side.

Core security patterns include:

- tenant-scoped OAuth and administration flows;
- hashed client secrets, refresh tokens, device codes, user codes, and token lookup inputs;
- encrypted tenant external-provider secrets;
- PKCE enforcement for public-client authorization flows;
- refresh-token rotation and consumed-token reuse detection;
- certificate-backed signing support for production deployments;
- soft-delete behavior for key administrative aggregates;
- outbox-based processing for asynchronous side effects.

## Project Status

TokenIDP is currently marked as `alpha`. The repository already contains a broad implementation surface and automated tests, but production deployments should complete environment hardening, secret management, certificate configuration, monitoring, and release validation for their target infrastructure.

## Documentation

- [`src/README.md`](src/README.md) documents backend validation rules and implemented OAuth/admin behavior.
- [`src/KEY_MANAGEMENT.md`](src/KEY_MANAGEMENT.md) documents token signing key and certificate management.
- [`portal/README.md`](portal/README.md) documents the admin portal.
- [`sdks/react-idp-sdk/README.md`](sdks/react-idp-sdk/README.md) documents React SDK usage.
- [`sdks/angular-idp-sdk/README.md`](sdks/angular-idp-sdk/README.md) documents Angular SDK usage.

## License

TokenIDP server package metadata declares the project under the MIT license.
