# Security Overview, Threat Model, and Operational Policies

This document provides end-user facing security guidance for the IDP solution
(Admin Portal + Admin APIs). It is based on the current codebase and highlights
what is implemented versus what must be configured during deployment.

Status: Draft for review (generated on 2026-01-30).

## Threat model summary (one-page)

### System context
- **Admin Portal (SPA)** performs OAuth 2.0 Authorization Code with PKCE, then
  calls Admin APIs with a Bearer access token.
- **Admin APIs** enforce JWT Bearer authentication for `/admin/*` endpoints.
- **Data layer** stores tenant configuration, clients, tokens, and audit data.

### Assets
- **User identities** and permissions (admin users).
- **OAuth clients** and **client secrets**.
- **Access/refresh/reference tokens** (hashes for stored tokens).
- **Tenant configuration** (security and integration settings).
- **Audit logs** and operational event history.

### Trust boundaries & data flows
1. **Browser -> Identity Provider (IdP)**: OAuth/OIDC login (PKCE).
2. **Browser -> Admin API**: Bearer token used on `/admin/*`.
3. **Admin API -> Database**: persistence for users, clients, tokens, configs.

Primary trust boundaries:
- Internet between browser and IdP/Admin API.
- Service boundary between Admin API and database.
- Admin Portal local storage boundary (tokens stored in browser storage).

### Key threats and mitigations (by category)
- **Spoofing / credential theft**
  - Mitigations: OAuth Authorization Code + PKCE for SPA; JWT bearer on API.
  - Residual risk: user phishing or compromised browser.
- **Token theft / replay**
  - Mitigations: Bearer token used over TLS; refresh token reuse detection in
    domain logic; token hash stored server-side (no raw refresh/reference token).
  - Residual risk: access token stored in browser localStorage is vulnerable to
    XSS if CSP/sanitization is weak; short token lifetimes recommended.
- **Tampering**
  - Mitigations: RBAC checks in UI; admin endpoints require JWT bearer.
  - Residual risk: incomplete server-side authorization should be reviewed.
- **Information disclosure**
  - Mitigations: token values are not stored or returned; only hashes are stored.
  - Residual risk: overly-permissive admin scopes could expose sensitive data.
- **Denial of service**
  - Mitigations: pagination on list endpoints; indexed read models (token search).
  - Residual risk: request rate limiting is not enforced in code.
- **Elevation of privilege**
  - Mitigations: permission-based route gating in Admin Portal; API requires
    authentication.
  - Residual risk: need to confirm server-side authorization for each endpoint.

### Threat model conclusions
The main security strengths are PKCE-based login, token hashing in storage, and
auditable changes. The main residual risks are browser token storage (XSS) and
deployment-dependent controls (TLS enforcement, rate limiting, logging targets).

## Encryption details (at rest and in transit)

### In transit
- Admin Portal uses `Authorization: Bearer <token>` for API calls.
- OAuth flow uses Authorization Code + PKCE.
- **Deployment requirement**: enforce HTTPS/TLS 1.2+ for both IdP and Admin API
  endpoints. Defaults in code use `http://localhost` for dev.

### At rest
- **Token storage**: refresh/reference tokens store a hash (`TokenHash`), not the
  raw token value.
- **Client secrets**: stored as a SHA-256 hash (no raw secret persisted).
- **Deployment requirement**: enable database encryption (TDE / disk encryption)
  and ensure backups are encrypted by the hosting environment.

## Key management approach

Implemented:
- Client secrets are hashed with SHA-256 before storage.

Deployment requirements:
- Store app secrets (DB connection strings, signing keys, SMTP keys, etc.) in a
  managed secrets store (e.g., Key Vault / KMS / HSM-backed store).
- Rotate secrets on a defined cadence and on incident response events.
- Use separate keys per environment (dev/stage/prod) and per tenant if required.

## MFA / SSO support details

Implemented:
- Admin Portal authenticates via OAuth 2.0 Authorization Code with PKCE and
  requests `openid profile email offline_access` scopes.
- SSO and MFA enforcement are delegated to the external IdP configured by
  `REACT_APP_AUTH_BASE_URL` and related OAuth settings.

Not implemented in code:
- Built-in MFA policy enforcement in this repository.
- Direct SAML or other federation protocols.

## Audit log model

Implemented:
- **AuditLog** entity with fields: `TableName`, `ActionType`, `RecordId`,
  `OldValues`, `NewValues`, `CreatedBy`, `CreatedOn`, `UpdatedBy`, `UpdatedOn`.
- **Auditable aggregates**: key domain entities automatically set `CreatedBy`,
  `CreatedOn`, `UpdatedBy`, `UpdatedOn` on save.
- Token revoke/expire flows create audit records (action, old/new values, IP).

Operational guidance:
- Ensure AuditLog is persisted and indexed in the database schema.
- Export logs to SIEM for retention and correlation.

## Incident response policy (lightweight)

1. **Detect**: monitor authentication failures, token anomalies, and admin
   activity via logs and metrics.
2. **Triage**: classify severity (P0-P3), determine scope and affected tenants.
3. **Contain**: revoke tokens, disable client credentials, and lock accounts.
4. **Eradicate**: remove malicious configs, rotate secrets, and patch root cause.
5. **Recover**: restore normal operations, validate system integrity.
6. **Notify**: communicate to stakeholders within SLA based on severity.
7. **Post-incident**: write RCA, record lessons learned, update controls.

## Secure SDLC basics

Baseline practices expected for this repo:
- **Code review** for all changes to auth, token handling, and admin endpoints.
- **Dependency scanning** for the Admin Portal and backend packages.
- **Secrets management**: no secrets in source; use environment or secret store.
- **Security testing**: automated tests around token revoke/expire and RBAC.

## Vulnerability disclosure policy (VDP)

Provide a public channel for responsible disclosure:
- Report vulnerabilities via a dedicated email or ticket intake address.
- Acknowledge receipt within 72 hours.
- Target fix time by severity (e.g., P0 < 7 days, P1 < 30 days).
- Publish release notes or security advisories for fixed issues.

## Transparent documentation (integration + security)

Recommended public docs to publish:
- OAuth/OIDC integration guide (client registration, scopes, redirect URIs).
- Admin API guide (auth headers, required permissions, pagination).
- Security overview (this document), including data handling and audit logging.

## Evidence in codebase (source references)

- OAuth PKCE login flow:
  `Admin-Portal/src/_components/login.jsx`
  `Admin-Portal/src/_components/oauthCallback.jsx`
- Admin API JWT bearer enforcement:
  `Admin.Core/Endpoints/TokenEndpoint.cs`
- Token hash storage:
  `IDP.Domain/AggregateRoots/Tokens/RefreshToken.cs`
  `IDP.Domain/AggregateRoots/Tokens/ReferenceToken.cs`
- Client secret hashing:
  `Admin.Core/Clients/CreateUpdateClientUseCase.cs`
- Audit log model and usage:
  `IDP.Domain/AuditLog.cs`
  `Admin.Core/Tokens/TokenCommandUseCase.cs`
  `IDP.Infrastructure/Persistence/ApplicationDbContext.cs`
