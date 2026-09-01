# TokenIDP Server

TokenIDP Server is the ASP.NET Core host package for TokenIDP, a modern identity provider for OAuth and OpenID Connect applications.

It is designed for teams that need a complete identity foundation: secure sign-in flows, tenant-aware authentication, token issuance, client management, user administration, and operational background processing.

For more information, see the [TokenIDP website](https://www.tokenidp.com).

Source and documentation are available on GitHub at [tokenidp/tokenidp](https://github.com/tokenidp/tokenidp).

## What It Provides

- OAuth and OpenID Connect endpoints for application sign-in and token handling.
- Tenant-aware identity and access management.
- JWT and reference token support.
- Refresh-token rotation and revocation support.
- Multi-factor authentication workflows.
- External sign-in provider integration.
- Admin APIs for users, applications, tenants, roles, permissions, settings, and tokens.
- Background services for token lifecycle, email, activity, and metrics processing.
- Health checks and telemetry hooks for production hosting.

## Typical Usage

Use this package when you want to host TokenIDP inside an ASP.NET Core application.

```csharp
builder.AddTokenIDPServices(
    connectionStringName: "Identity_DB",
    audience: "tokenidp.admin.api");
```

```csharp
await app.UseTokenIDPAsync(
    allowedOrigins: builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? []);
```

## Configuration

At minimum, configure:

- database connection string
- token issuer and audience
- signing key for development, or signing certificate for production
- allowed CORS origins
- tenant resolution settings

Production deployments should use a secure configuration provider for database passwords, signing certificates, encryption keys, and client secrets.

## Client Applications

TokenIDP can be used by web, SPA, mobile, desktop, backend, and device-oriented clients. React and Angular SDKs are available separately in the TokenIDP repository for browser application integration.

## Status

This package is currently in alpha. Use it with production hardening appropriate to your deployment environment, including certificate-backed signing, secure secret storage, monitoring, backups, and release validation.
