# Token Signing Key Management

TokenIDP signs access tokens and identity tokens with asymmetric RSA signing material. APIs and clients trust those tokens only when they can validate the signature against the public key exposed by TokenIDP discovery and JWKS endpoints.

This document describes the signing-key options supported by the current implementation and how to configure them safely for development and production.

## How TokenIDP Resolves Signing Material

TokenIDP reads signing settings from the `TokenOptions` configuration section.

Supported options are:

- `TokenOptions:KeyPath` - path to a PEM file or a file containing a base64-encoded RSA private key.
- `TokenOptions:Key` - inline PEM or base64-encoded RSA private key.
- `TokenOptions:CertificateThumbprint` - certificate lookup by thumbprint.
- `TokenOptions:CertificateSubjectName` - certificate lookup by subject name.
- `TokenOptions:CertificateStoreName` - certificate store name. Defaults to `My`.
- `TokenOptions:CertificateStoreLocation` - certificate store location. Defaults to `CurrentUser`.
- `TokenOptions:Issuer` - issuer URL used in generated tokens and discovery metadata.
- `TokenOptions:Audience` - token audience. When using `AddTokenIDPServices(...)`, the audience is supplied by the method argument and overrides `TokenOptions:Audience` from configuration.

Resolution order is:

1. If certificate configuration is present, TokenIDP loads the configured certificate and uses it for signing.
2. In non-production environments, if no certificate is configured, TokenIDP uses `TokenOptions:KeyPath` or `TokenOptions:Key`.
3. In Development, if no key or certificate is configured, `AddTokenIDPServices(...)` generates an ephemeral RSA key in memory.
4. In production, TokenIDP requires certificate configuration and fails startup when neither `CertificateThumbprint` nor `CertificateSubjectName` is configured.

The generated Development key changes whenever the process starts, so previously
issued tokens stop validating after a restart. Staging and other shared
non-Production environments must receive stable signing material from their
deployment secret store. Production must use a signing certificate.

## Recommended Local Development Setup

For local development, keep non-secret values in `appsettings.json` and keep the private signing key outside source control.

Example `appsettings.json`:

```json
{
  "TokenOptions": {
    "Issuer": "https://localhost:5001"
  }
}
```

Store the private key path with .NET user secrets:

```powershell
cd src/TokenIDP.Host
dotnet user-secrets set "TokenOptions:KeyPath" "D:\TokenIDP\signing-key.pem"
```

The file at `D:\TokenIDP\signing-key.pem` should contain a PEM-formatted RSA private key:

```text
-----BEGIN PRIVATE KEY-----
...
-----END PRIVATE KEY-----
```

Do not commit the PEM file to source control. Store it outside the project folder or add the containing folder to `.gitignore`.

`WebApplication.CreateBuilder(args)` loads user secrets automatically when the host runs in the `Development` environment, so `TokenOptions:KeyPath` becomes available to TokenIDP without placing the private key in `appsettings.json`.

## Inline Development Key

`TokenOptions:Key` is also supported for local development, but it is less convenient and easier to leak.

If you use an inline PEM value from PowerShell, use PowerShell newline escapes so the value contains real PEM newlines:

```powershell
dotnet user-secrets set "TokenOptions:Key" "-----BEGIN PRIVATE KEY-----`n...`n-----END PRIVATE KEY-----"
```

Prefer `TokenOptions:KeyPath` for local development because the key remains a normal PEM file and command history does not contain the full private key.

## Production Setup

Production requires certificate-based signing.

Use one of:

- `TokenOptions:CertificateThumbprint`
- `TokenOptions:CertificateSubjectName`

The certificate must be installed in the configured certificate store and must have an accessible private key. TokenIDP uses the private key to sign tokens and publishes the public key through JWKS.

Example with certificate thumbprint:

```json
{
  "TokenOptions": {
    "Issuer": "https://id.example.com",
    "CertificateThumbprint": "ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12",
    "CertificateStoreName": "My",
    "CertificateStoreLocation": "LocalMachine"
  }
}
```

Example with subject name:

```json
{
  "TokenOptions": {
    "Issuer": "https://id.example.com",
    "CertificateSubjectName": "CN=IDP Signing",
    "CertificateStoreName": "My",
    "CertificateStoreLocation": "LocalMachine"
  }
}
```

When `CertificateSubjectName` is used, TokenIDP selects the newest matching certificate with a private key by expiration date. This supports certificate rotation by installing a newer certificate with the same subject name and then restarting the host.

## JWKS Publishing

TokenIDP publishes public signing key material at:

```text
/.well-known/jwks.json
```

Downstream APIs should validate tokens by using TokenIDP discovery and JWKS rather than copying public keys manually. After key rotation, confirm the new public key appears in JWKS and that downstream validators refresh their key cache.

## Rotation Runbook

For planned production rotation:

1. Create or obtain a new signing certificate with a private key.
2. Install the certificate in the configured store.
3. Update configuration if using thumbprint-based lookup.
4. Restart or redeploy TokenIDP.
5. Confirm `/.well-known/jwks.json` exposes the new key.
6. Issue a test token and validate it against a downstream API.
7. Monitor token validation failures while downstream caches refresh.
8. Remove the retired certificate only after all validators have moved to the new key.

For emergency rollover after a private-key compromise:

1. Remove or disable the compromised key.
2. Deploy a new signing certificate.
3. Restart TokenIDP and verify JWKS.
4. Revoke sensitive refresh tokens if compromise scope is unclear.
5. Review audit logs for unusual token issuance or administrative changes.

## Common Pitfalls

- Putting a private signing key in `appsettings.json`.
- Committing PEM files, `.pfx` files, or user-secrets files to source control.
- Using `TokenOptions:KeyPath` in production. The current server setup requires certificate configuration in production.
- Expecting `TokenOptions:Audience` in configuration to win over `AddTokenIDPServices(...)`. The method argument sets the effective audience.
- Rotating a certificate without confirming downstream JWKS cache behavior.

## Troubleshooting

- If local startup says the signing key file was not found, verify the exact `TokenOptions:KeyPath` value and confirm the process account can read the file.
- If local startup says the key must be PEM or base64-encoded RSA private key, confirm the PEM file contains a private key, not a public key.
- If production startup fails, verify `TokenOptions:CertificateThumbprint` or `TokenOptions:CertificateSubjectName` is configured and the certificate exists in the configured store.
- If token validation fails after rotation, compare the token header `kid`, the JWKS response, and the downstream API key-cache behavior.
