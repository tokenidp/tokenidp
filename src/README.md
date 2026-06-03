# OAuth 2.1 & OpenId Connect - .NET 10 implementation

## Scope

Summary of backend validations, business rules, and business functions implemented in `src`, focused on OAuth, MFA, and admin aggregates.

Reviewed areas:

- `src/TokenIDP.Core/OAuth/**/*`
- `src/TokenIDP.Core/Admin/**/*`
- selected supporting infrastructure/repository behavior used by those flows

## Cross-Cutting Patterns

### Tenant scoping

- OAuth, user lifecycle, token management, and admin flows are tenant-aware.
- Introspection and revocation validate caller tenant/client context before acting.
- Self-registration, MFA, password reset, and external auth all run inside tenant context.

### Soft delete

Delete behavior is soft delete for the reviewed admin aggregates:

- Clients
- ApiResources
- Tenants
- Users
- Roles
- Permissions
- Configurations / Settings

### Secret and token protection

- Client secrets are checked against stored hashes.
- Refresh tokens, device codes, user codes, and revocation/introspection inputs are hashed before persistence/lookup.
- Tenant external provider secrets are encrypted with a tenant/provider-specific purpose string.

### Notifications

- MFA queues email template `MFA_CODE`.
- Password reset queues reset mail.
- Email confirmation queues confirmation mail.

## OAuth Core

## `/token`

Pipeline:

- `TokenEndpoint` creates a validated token request through `TokenEndpointClientAuthService`
- `TokenGrantPipeline` validates the grant and dispatches to the matching handler

### Client authentication rules

Supported methods:

- `client_secret_basic`
- `client_secret_post`
- `none`

Rules:

- `grant_type` required
- `client_id` required
- Basic header secret and body secret cannot both be used
- body `client_id` cannot disagree with Basic auth `client_id`
- client must exist and be active
- public clients cannot authenticate with secrets
- confidential clients are effectively WebApp / Backend
- `client_credentials` cannot use auth method `none`
- invalid secret returns `invalid_client`

### Grant validation

`GrantTypeValidatorUseCase` enforces:

- requested grant must be supported by server
- client must be configured for that grant
- unknown grant -> `unsupported_grant_type`
- grant not allowed for client -> `unauthorized_client`

## Authorization Code grant

### `/authorize`

Request validation:

- `client_id` required
- `redirect_uri` required
- `response_type` required and must be `code`
- `scope` required
- PKCE parameters required
- client must be valid
- redirect URI must be absolute
- redirect URI must use HTTPS unless loopback
- redirect URI must exactly match configured client redirect URIs
- requested scopes must be a subset of client scopes
- `openid` is required

Behavior:

- if SSO session exists, auth code is issued immediately
- otherwise a `PreAuthorization` is created and browser is redirected to `/login?ctx=...`

Resume flow rules:

- `ctx` must resolve to a valid pre-authorization
- authenticated `idp_session` required
- user tenant claim must match the pre-authorization tenant

### Local login inside auth-code flow

`AuthorizationCodeUseCase.Authenticate`:

- authenticates through `IAuthenticationService`
- if MFA is required:
  - generates MFA code
  - returns MFA-needed response
- otherwise signs user into SSO session

Auth code issuance:

- code is a GUID string
- expiry is 5 minutes
- stores PKCE challenge, method, redirect URI, remember-me, scopes

Token exchange rules:

- code must exist
- not expired
- not used
- PKCE verifier must match challenge
- redirect URI must match exactly
- client must still be valid

## Password grant

Handled by `PasswordGrantHandler`.

Rules:

- authenticates supplied username/password
- failure returns token error
- if MFA required:
  - generates MFA code
  - returns success payload indicating 2FA is required
- otherwise builds `password` token context and issues tokens

MFA completion:

- `VerifyMfaCode` validates via `IMfaUseCase`
- on success it issues the password-grant token set

## Client Credentials grant

Handled by `ClientCredentialGrantHandler`.

Rules:

- `grant_type` must be `client_credentials`
- request must exist
- `client_id` required
- secret must match an active stored secret
- builds client token context, not user token context

Behavior:

- issues access token only
- no refresh token
- no ID token

## Device Authorization flow

### `/device_authorization`

Rules:

- client must be valid
- scope required
- requested scopes must be a subset of client scopes

Behavior:

- generates device code and user code
- stores both hashed
- returns `expires_in = 600`, `interval = 5`, verification URI, complete verification URI

### Device authentication and approval

`DeviceAuthenticationUseCase`:

- user code must resolve to pending request
- request must not be expired, consumed, or denied
- authenticates username/password
- if MFA required:
  - generates MFA code
  - returns MFA-needed response
- `ApproveAsync` marks request approved for the user

### Device token polling

`DeviceFlowGrantHandler`:

- hashes device code
- missing request returns `invalid_grant`
- poll is registered
- approved request becomes user token context
- token is issued
- device request is marked consumed

## Refresh Token grant

Handled by `RefreshTokenGrantHandler`.

Rules:

- refresh token required
- stored token must:
  - exist
  - belong to the requesting client
  - not be expired
  - not be consumed
  - be user-bound

Reuse detection:

- consumed refresh token triggers reuse detection and revocation
- returns `invalid_grant`

Behavior:

- requested scope is reused or narrowed safely
- refresh token is rotated

## CIBA

- `CibaGrantHandler` currently returns unsupported grant behavior

## Token context rules

`TokenContextUseCase` enforces:

For user tokens:

- user must exist
- user must have active roles
- omitted scopes default to client scopes
- requested scopes must belong to client
- custom scopes must map to assigned API resources
- standard scopes do not create audiences
- multiple audiences are rejected with `multiple_audiences_not_supported`

For client credentials:

- same scope and audience validation
- no user/role lookup

## Token issuance

`TokenIssuerUseCase`:

- issues JWT or reference token based on client token type
- `client_credentials` never gets refresh token or ID token
- refresh token is issued only for refresh grant or `offline_access`
- old refresh tokens are cleaned up

## MFA

### Policy

`TenantUserMfaPolicy` requires MFA only when:

- tenant two-factor is enabled
- user `TwoFactorEnabled` is true

### MFA generation and verification

`MfaUseCase`:

- stores MFA code against pre-authorization
- expiry is 5 minutes
- sends queued email template `MFA_CODE`
- can create a new pre-authorization for password/device style MFA

Verification rules:

- correlation must exist
- code must match
- user binding must match
- request must be within expiry

Behavior on success:

- pre-authorization marked 2FA verified
- authorization request reconstructed from pre-authorization
- user MFA validation marked

Resend rules:

- correlation ID required
- pre-authorization must exist
- bound user must still match
- resent code gets fresh 5-minute expiry

## Other OAuth endpoints

### `/userinfo`

- access token must contain scopes
- `openid` required
- only supported standard scopes accepted
- current user is loaded and mapped to userinfo

### `/revoke`

- authenticated caller required
- missing token returns silently
- caller must match same client and tenant
- if caller is user-bound, token user must also match
- token is revoked with reason and IP

### `/introspect`

- authenticated caller required
- missing/unauthorized token returns inactive
- same caller-ownership rules as revoke
- may return subject, client, tenant, scope, roles, exp, iat, issuer

### `/logout`

- signs out SSO session
- if `client_id` is present, `post_logout_redirect_uri` must match configured logout URIs
- fallback is first allowed URI or `/login`
- appends `logged_out=1`

### External auth

Challenge:

- valid provider and pre-auth context required
- tenant/client context is set
- state, nonce, PKCE verifier created
- external auth session stored with TTL

Callback:

- `code` and `state` required
- session must exist and state must match
- tenant/client ids must be valid
- external code is exchanged
- local user is found or provisioned
- user is signed into SSO session
- browser is redirected back to `/authorize?ctx=...`
- session and tenant context are always cleaned up

## Admin Aggregates

## Clients

### Validation

`ClientValidators`:

- client name required, max 200
- redirect URI required and absolute
- logout redirect URI optional but absolute if supplied
- access token lifetime > 0
- authorization code lifetime > 0
- refresh expiration > 0
- two-factor code expiry > 0 when 2FA enabled
- grant types required
- auth policy required
- scopes distinct
- API resources distinct
- external providers distinct

### Business rules

`ClientCommandValidator`:

- client ID unique
- assigned API resources must exist and be enabled
- non-standard scopes must exist
- non-standard scopes must belong to assigned API resources
- selected external providers must belong to tenant
- if auto-create users is enabled:
  - default role required
  - role must exist
  - role must be active
  - role must be assignable to new users

### Business functions

`ClientCommandUseCase`:

- create:
  - generates GUID-string client ID
  - validates uniqueness
- update:
  - client must exist
- delete:
  - soft delete
- rotate secret:
  - only for secret-capable client types
  - revokes active secrets
  - creates new 32-byte base64url secret

## ApiResources

`ApiResourceCommandUseCase`:

- name unique per tenant
- scope names unique within resource
- update cannot remove scopes still assigned to clients
- resource rename propagates to client assignments
- delete blocked if assigned to any client
- successful delete is soft delete

## Tenants

### Validation

`TenantValidators`:

- tenant name required, max 200
- email valid if supplied
- auth settings required
- when tenant MFA enabled, code expiry > 0
- enabled external providers require client ID

### Business functions

`TenantCommandUseCase`:

- create:
  - tenant name unique
  - tenant key generated from name
  - auth settings and UI settings created
  - provider secrets encrypted
  - tenant code generated
- update:
  - tenant must exist
  - name unique excluding self
  - blank provider secret preserves stored value
  - missing provider in request is treated as disabled
- delete:
  - active tenant cannot be deleted
  - tenant-context cross-delete blocked
  - inactive tenant is soft deleted

## Users

### Validation

`UserValidators`:

- first name required
- last name required
- username required
- email required and valid
- roles required
- password required and min 8 on create
- status must be known enum if present
- addresses require type, line, city, country
- contacts require type plus email or phone
- forgot-password requires email + clientId
- complete reset requires token + new password
- create-account requires first name, last name, email, username, phone, password

### Business functions

`UserCommandUseCase`:

- create:
  - password mandatory
  - username unique in tenant
  - email unique in tenant
  - user code generated
  - addresses/contacts replaced
- update:
  - user must exist
  - username immutable
  - email uniqueness rechecked
  - addresses/contacts replaced
- status update:
  - uses domain status transition
- delete:
  - soft delete

Other user lifecycle flows:

- `PasswordResetUseCase`
  - self-service returns generic success to prevent enumeration
  - admin reset queues reset email
  - completed reset revokes active user tokens with reason `PasswordReset`
- `CreateAccountUseCase`
  - requires tenant/client context
  - allowed only when tenant and client policy permit
  - client must provide default role
  - created user gets default role and normalized initial flags
- `EmailConfirmationUseCase`
  - creates 24-hour confirmation token
  - completion marks email confirmed and token used

## Roles

### Validation

`RoleValidators`:

- role name required, max 200
- description required, max 500
- permissions list not null
- each permission must have valid id and key

### Business rules

`RoleCommandUseCase`:

- `isAssignableToNewUsers` requires active role
- reserved system role names `admin`, `administrator`, `owner` cannot be assignable to new users
- role name must be unique
- system roles cannot change new-user assignment flag
- delete is soft delete

## Permissions

### Validation

`PermissionValidators`:

- key required, max 200
- name required, max 200
- control type required
- sequence >= 0

### Business rules

`PermissionCommandUseCase`:

- create:
  - key normalized to lowercase
  - name trimmed
  - key unique
  - sequence auto-assigned
  - active by default
- update:
  - permission must exist
  - key immutable
  - sequence preserved
- delete:
  - soft delete

## Settings / Configurations

### Validation

`TenantConfigurationValidation`:

- key normalized by trim + lowercase
- supported value types:
  - String
  - Int
  - Bool
  - Json
- Bool / Int / Json values must parse correctly

### Business functions

- `ConfigurationCommandUseCase`
  - tenant context required
  - key/value required
  - key unique per tenant
- `ConfigurationUpdateCommandUseCase`
  - config must exist
  - read-only configs cannot be updated
  - cache invalidated by key
- `ConfigurationDeleteCommandUseCase`
  - read-only configs cannot be deleted
  - soft delete + cache invalidation
- `ConfigurationUpsertCommandUseCase`
  - create if missing, update if editable
- `ConfigurationsBulkCommandUseCase`
  - tenant context required
  - items required
  - normalized keys must be unique inside request
  - any read-only/validation failure aborts the batch
  - transaction scope used

## Tokens (Admin)

`TokenCommandUseCase`:

- revoke and expire act only on active tokens in current tenant
- revoke reason defaults to `Admin revocation`

## Important Observations

- Backend is the real enforcement layer; frontend mirrors some rules but is not authoritative.
- CIBA exists in models/lookups but is intentionally unsupported at execution time.
- Password flow and device flow both integrate with the same MFA use case.
- Multiple API audiences are explicitly unsupported during token issuance.
