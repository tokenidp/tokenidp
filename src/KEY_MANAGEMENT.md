# Token Signing Key Management

This document explains how token signing keys are handled in this solution for
development and production, including PEM keys and X.509 certificates.

## Overview

JWTs are signed by the authorization server. In production, we use an X.509
certificate with a private key stored in a secure store. In development, a PEM
key can be used for convenience.

Key configuration is provided via `TokenOptions` (appsettings or code).

## Production (X.509 Certificate)

Production requires a signing certificate:
- The authorization server uses the certificate *private key* to sign tokens.
- Resource servers validate tokens using the public key (JWKS or public cert).
- Client apps that only request tokens do not need the certificate.

Certificate selection:
- Preferred: `TokenOptions:CertificateThumbprint`
- Optional: `TokenOptions:CertificateSubjectName`
- Optional: `TokenOptions:CertificateStoreName` (default: `My`)
- Optional: `TokenOptions:CertificateStoreLocation` (default: `CurrentUser`)

When `CertificateSubjectName` is used, the newest certificate with a private key
is selected (by `NotAfter`), enabling rotation.

If the environment is Production and no certificate is configured, startup
fails with an error.

## Development (PEM or dev key)

Development allows PEM keys:
- `TokenOptions:KeyPath` (file path to PEM or base64 key), or
- `TokenOptions:Key` (inline PEM or base64 key)

If neither PEM nor certificate is provided in development, a built-in dev key
is used.

## Audience Requirement

`TokenOptions:Audience` is required. It is validated in JWT bearer auth via
`ValidAudience`.

## Configuration Examples

### Production with certificate thumbprint

```json
{
  "TokenOptions": {
    "Audience": "admin.api",
    "Issuer": "https://idp.example.com",
    "CertificateThumbprint": "YOUR_CERT_THUMBPRINT",
    "CertificateStoreName": "My",
    "CertificateStoreLocation": "LocalMachine"
  }
}
```

### Production with subject name (rotation)

```json
{
  "TokenOptions": {
    "Audience": "admin.api",
    "Issuer": "https://idp.example.com",
    "CertificateSubjectName": "CN=IDP Signing",
    "CertificateStoreName": "My",
    "CertificateStoreLocation": "LocalMachine"
  }
}
```

### Development with PEM

```json
{
  "TokenOptions": {
    "Audience": "admin.api",
    "Issuer": "https://localhost:5001",
    "KeyPath": "C:\\secrets\\dev-signing-key.pem"
  }
}
```

## Operational Notes

- Private keys must never be committed to source control.
- For production, use a secure store (OS cert store, HSM, or cloud KMS).
- Rotate certificates by installing a newer cert with the same subject name.
- Distribute public keys to resource servers via JWKS or a public certificate.
