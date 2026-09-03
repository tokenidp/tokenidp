<div align="center">
  <img src="TokenIDP.svg" alt="TokenIDP Logo" width="180">

  <p>
    A modern, tenant-aware Identity Provider for OAuth 2.x and OpenID Connect,
    built on .NET 10 with an admin portal and application SDKs.
  </p>

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB)](https://react.dev/)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031)](https://angular.dev/)
[![NuGet](https://img.shields.io/nuget/v/TokenIDP.Server?label=NuGet)](https://www.nuget.org/packages/TokenIDP.Server/1.9.1)
[![Status](https://img.shields.io/badge/status-stable-0A7F2E)](#project-status)

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

![TokenIDP integration workflow](Integration%20Workflow.png)

The diagram shows the main runtime relationships. Client applications request authorization and tokens from TokenIDP, optionally use an external identity provider, and present access tokens when calling protected APIs through an API gateway or directly.

| Area           | Path                          | Purpose                                                                                                  |
| -------------- | ----------------------------- | -------------------------------------------------------------------------------------------------------- |
| Domain         | `src/TokenIDP.Domain`         | Aggregates, value objects, domain events, read models, and business invariants.                          |
| Core           | `src/TokenIDP.Core`           | OAuth, admin, security, validation, application services, endpoints, and abstractions.                   |
| Infrastructure | `src/TokenIDP.Infrastructure` | EF Core persistence, repositories, migrations, caching, bootstrapping, projections, outbox, and logging. |
| Workers        | `src/TokenIDP.Workers`        | Background processors for tokens, activity, email, and metrics.                                          |
| Server package | `src/TokenIDP.Server`         | Reusable server components and application setup extensions.                                             |
| Host           | `src/TokenIDP.Host`           | Deployable ASP.NET Core host used to run the complete identity service.                                  |
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
|   +-- TokenIDP.Host/
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

- [Git](https://git-scm.com/) for cloning the repository
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server, SQL Server Express, Azure SQL, or PostgreSQL
- [Node.js 20](https://nodejs.org/) and npm for the Admin Portal

The checked-in host configuration uses `localhost\SQLEXPRESS` with Windows authentication. If your database is elsewhere, provide your own `ConnectionStrings:Identity_DB` value before running the host.

### Clone the Repository

```powershell
git clone https://github.com/tokenidp/tokenidp.git
cd tokenidp
```

These instructions run TokenIDP from its source code. If you are adding TokenIDP to a separate application through the NuGet package, follow the [installation guide](https://www.tokenidp.com/docs/tutorials/getting-started/) instead.

### Build the Backend

```powershell
dotnet restore src/TokenIDP.sln
dotnet build src/TokenIDP.sln
```

### Run Tests

```powershell
dotnet test src/TokenIDP.sln
```

### Run the Identity Service

The source repository uses `src/TokenIDP.Host` as its deployable ASP.NET Core host. Its project file already contains a User Secrets ID, so you do not need to run `dotnet user-secrets init` when working from this repository.

Before the first local run, open PowerShell in the repository root and store a unique AES-256 encryption key and temporary administrator password outside `appsettings.json`:

```powershell
cd src/TokenIDP.Host

$rng = [Security.Cryptography.RandomNumberGenerator]::Create()

try {
    $securityKeyBytes = New-Object byte[] 32
    $rng.GetBytes($securityKeyBytes)
    $securityKey = [Convert]::ToBase64String($securityKeyBytes)
    dotnet user-secrets set "Security:KeyBase64" $securityKey

    $passwordBytes = New-Object byte[] 24
    $rng.GetBytes($passwordBytes)
    $bootstrapPassword = "Aa1!" + [Convert]::ToBase64String($passwordBytes)
    dotnet user-secrets set "Bootstrap:AdminTempPassword" $bootstrapPassword
    dotnet user-secrets set "Bootstrap:Enable" "true"
}
finally {
    $rng.Dispose()
    Remove-Variable rng, securityKeyBytes, securityKey, passwordBytes, bootstrapPassword -ErrorAction SilentlyContinue
}
```

If you do not use the checked-in SQL Server Express connection, set a local connection string as a user secret. For example:

```powershell
dotnet user-secrets set "ConnectionStrings:Identity_DB" "<your-development-connection-string>"
```

Start the HTTPS launch profile:

```powershell
dotnet run --launch-profile https
```

The default HTTPS URL is `https://localhost:7292`. If the HTTPS development certificate is not trusted, run `dotnet dev-certs https --trust` and restart the host.

On its first successful bootstrap, TokenIDP creates the system tenant, the `idp-admin` portal client, and the administrator account. In another PowerShell window, retrieve the generated local credentials:

```powershell
cd src/TokenIDP.Host
dotnet user-secrets list
```

Sign in with:

- Username: `admin`
- Password: the complete value of `Bootstrap:AdminTempPassword`

Do not share or commit the output of `dotnet user-secrets list`. After the database has been bootstrapped successfully, stop the host and disable bootstrap for later runs:

```powershell
dotnet user-secrets set "Bootstrap:Enable" "false"
```

Changing the user-secret password after bootstrap does not change the existing administrator's password. Change that password through the supported administration flow instead.

Development creates an ephemeral RSA signing key when no signing material is configured. Tokens issued before a host restart will then stop validating. This is acceptable for an initial local setup; use a stable development key when you need tokens to survive restarts, and use protected signing material for shared environments. See [Token signing key management](src/KEY_MANAGEMENT.md).

### Run the Admin Portal

Keep the identity host running, then open a second PowerShell window from the repository root. The tracked `.env.development.example` contains matching localhost URLs and public OAuth client settings; copy it to the ignored `.env.development` file:

```powershell
cd portal
Copy-Item .env.development.example .env.development
npm ci
npm start
```

Open `http://localhost:3000` and sign in with the bootstrapped administrator credentials. The backend and portal settings must continue to match:

| Host setting | Portal setting | Local value |
| --- | --- | --- |
| `Bootstrap:ClientId` | `REACT_APP_OAUTH_CLIENT_ID` | `idp-admin` |
| `Bootstrap:RedirectUri` | `REACT_APP_OAUTH_REDIRECT_URI` | `http://localhost:3000/auth/callback` |
| `Bootstrap:LogoutRedirectUri` | `REACT_APP_OAUTH_POST_LOGOUT_REDIRECT_URI` | `http://localhost:3000/logout/callback` |
| `TokenOptions:Issuer` | `REACT_APP_AUTH_BASE_URL` | `https://localhost:7292` |

The `.env.development` file is for local builds and is intentionally ignored by Git. Browser-visible `REACT_APP_*` values are public configuration; never put passwords, private keys, API tokens, or client secrets in them.

The portal manages tenants, applications, users, roles, permissions, API resources, settings, tokens, activities, and dashboard views. For deployment of the prebuilt static portal, use `public/config.json` as described in the [Admin Portal documentation](portal/README.md).

## Configuration

Configuration is supplied through standard ASP.NET Core configuration sources. Important sections include:

- `ConnectionStrings:Identity_DB` for the identity database.
- `Bootstrap` for initial admin client, redirect URLs, and seed behavior.
- `TenantResolution` for host, root-domain, development-host, and tenant lookup behavior.
- `TokenOptions` for issuer, audience, signing key, or signing certificate settings.
- `Cors` for allowed frontend origins.
- `Security` for secret-protection keys.

Production deployments should use secure configuration providers or secret stores. Do not commit live database passwords, encryption keys, certificate private keys, or client secrets.

For the complete local and GitHub Actions configuration matrix, see
[`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md).

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

TokenIDP `v1.9.1` is the first stable open-source release. The project continues to evolve and has not yet undergone an independent security audit. Before production use, evaluate it against your organization's requirements and complete environment hardening, secret management, certificate configuration, backup and recovery planning, monitoring, capacity testing, and release validation for the target infrastructure.

## Documentation

- [`src/README.md`](src/README.md) documents backend validation rules and implemented OAuth/admin behavior.
- [`src/KEY_MANAGEMENT.md`](src/KEY_MANAGEMENT.md) documents token signing key and certificate management.
- [`portal/README.md`](portal/README.md) documents the admin portal.
- [`sdks/react-idp-sdk/README.md`](sdks/react-idp-sdk/README.md) documents React SDK usage.
- [`sdks/angular-idp-sdk/README.md`](sdks/angular-idp-sdk/README.md) documents Angular SDK usage.

## License

TokenIDP is available under the [MIT License](LICENSE).
