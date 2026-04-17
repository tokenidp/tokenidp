# FRONTEND

## Scope

Summary of UI validations, client-configuration rules, and admin behaviors implemented in `portal/src`.

Reviewed areas:

- auth shell: `index.js`, `App.js`, `privateRoute.js`, `useApiClient.js`
- client/app wizard: `applications/*`
- admin aggregates: `apiResources/*`, `tenants/*`, `users/*`, `roles/*`, `permissions/*`, `settings/*`, `tokens/*`, `activities/*`

## Cross-Cutting Rules

### Auth shell

- The portal uses `IdpAuthProvider` from `tokenidp-react`.
- Config comes from:
  - `REACT_APP_AUTH_BASE_URL`
  - `REACT_APP_OAUTH_CLIENT_ID`
  - `REACT_APP_OAUTH_REDIRECT_URI`
  - `REACT_APP_OAUTH_POST_LOGOUT_REDIRECT_URI`
  - `REACT_APP_OAUTH_SCOPE`
- Tokens are stored in `localStorage`.
- Post-login redirect is `/dashboard`.
- Post-logout redirect defaults to `/login`.

### Login/logout flow

- `/login` delegates to hosted auth via `auth.login()`.
- `/auth/callback` is handled by `AuthCallback`.
- If `logged_out=1` is present, the login page shows a signed-out state and offers re-login.
- The portal does not implement its own username/password, MFA challenge, device activation, self-registration, or forgot-password completion UI.

### Route authorization

- `PrivateRoute` requires `isAuthenticated`.
- Access is granted when either:
  - required permission keys satisfy `requiredAnyOf` / `requiredAllOf`
  - a permission entry has a `url` matching the route and its value is not false
- Permission keys are normalized to lowercase.

### API client behavior

- `useApiClient` injects `Bearer` access tokens unless `skipAuth` is true.
- Error payloads are normalized from multiple server shapes and pushed to global error UI.

### List behavior

- Most search screens debounce requests by `250ms` to `400ms`.
- Most text searches do not query until at least 3 characters are entered.
- After create/update/delete/admin actions, lists refresh from the server.

## OAuth / Grant Logic In The Client Wizard

Main source: `applicationWizard.jsx` plus `wizard/steps/*`.

### App types

- `0` SPA
- `1` Mobile
- `2` Desktop
- `3` WebApp
- `4` Backend
- `5` Device/IOT

SPA, Mobile, and Desktop are treated as public clients. WebApp and Backend are secret-capable. Device/IOT is forced into device flow assumptions.

### Grant catalog

- `authorization_code`
- `refresh_token`
- `client_credentials`
- `device_code`
- `ciba`
- `password`

### Grant compatibility enforced in UI

- SPA:
  - must include `authorization_code`
  - may include `refresh_token`
  - cannot use `client_credentials`
  - cannot use `password`
- Mobile:
  - may use `authorization_code`
  - may use `refresh_token`
  - may use `password`
- Desktop:
  - may use `authorization_code`
  - may use `refresh_token`
  - may use `device_code`
  - may use `password`
- WebApp:
  - may use `authorization_code`
  - may use `refresh_token`
  - may use `password`
  - may use `ciba`
- Backend:
  - may use `client_credentials`
  - may use `refresh_token`
  - may use `password`
- Device/IOT:
  - must use `device_code`

Additional rules:

- `refresh_token` requires `authorization_code` or `password`.
- Public clients and Device/IOT clients cannot have client secrets.
- `client_credentials` is blocked for SPA, Mobile, Desktop.
- `password` is blocked for SPA and Device/IOT.
- `ciba` is limited to WebApp and also marked in the UI as under development.
- `device_code` is limited to Mobile, Desktop, Device/IOT.

### Redirect URI validation

- Required only when `authorization_code` is enabled.
- Disabled for `client_credentials`-only clients.
- One URI per line.
- Wildcards `*` rejected.
- Fragments `#` rejected.
- Values starting with `?` rejected.
- Each line must parse as a URL.
- Only `https:` allowed, except `http://localhost`.

Logout redirect URIs use the same validation when supplied.

### Secret handling

- Secret fields are disabled for public clients and Device/IOT.
- Secret regeneration is edit-mode only.
- The UI warns that regenerated secrets are shown once and then only the hash is stored.
- Secret expiry is configured in days.

### Scope and audience rules

- `openid` is described as required for OIDC login / ID token issuance.
- `offline_access` is disabled unless `refresh_token` is enabled.
- For `client_credentials`, the UI disables:
  - `openid`
  - `profile`
  - `email`
  - `offline_access`
- Selecting an API scope auto-selects its owning API resource.
- Deselecting an API resource removes its owned scopes.
- The wizard warns that the IDP issues single-audience tokens and rejects multi-resource token requests.

### Token settings

- Token type is required.
- Token type choices are `JWT` and `Reference`.
- Selecting reference tokens shows an introspection/performance warning.
- Access token lifetime:
  - required
  - minimum 1
- Authorization code lifetime:
  - required only if `authorization_code` is enabled
  - minimum 1
- Refresh token expiration:
  - required only if `refresh_token` is enabled
  - minimum 1
  - maximum 30 days enforced in UI

### Client auth policy options exposed

- `allowLocalLoginOverride`
- `allowSelfRegistrationOverride`
- `mfaPolicyOverride`
- `showStaySignedIn`
- `showCreateAccountLink`
- `showExternalProviders`
- `autoCreateUsers`
- `defaultRoleId`

UI rule:

- if `autoCreateUsers` is enabled, a default role is expected
- if external providers are hidden, selected providers are omitted from payload

### Protection step

- Permit limit, queue limit, time window, and interaction tracking are editable.
- The screen explicitly marks them as saved-but-not-enforced yet.
- `timeWindow` must match supported duration formats.

## MFA In The Frontend

There is no dedicated MFA challenge UI in the portal.

Frontend MFA behavior is administrative:

- tenant form can enable tenant MFA and set code expiry minutes
- user form can toggle per-user `twoFactorEnabled`
- client form can set `mfaPolicyOverride`
- dashboard shows MFA challenge metrics
- settings may hold MFA-related keys but validation there is type-based only

## Admin Aggregates

### Applications / Clients

- list filters: app type, token type, status, search
- search waits for 3 chars
- table and card views exist
- delete is permission-gated by `applications.delete`
- export includes client name, client ID, app type, token type, status
- payloads trim strings and normalize enum-like values to numbers

### ApiResources

- `name` required
- `displayName` required
- scope draft requires `name` and `displayName`
- scopes can be added/edited/removed locally before submit
- delete refreshes the list after success

### Tenants

- `tenantName` required
- `authenticationMode` required
- email uses email input semantics
- `twoFactorCodeExpiry` must be at least 1 when MFA is enabled
- tenant code is disabled/system-managed
- delete is disabled in the list while tenant is active
- search waits for 3 chars
- enabled external providers require client ID in UI validation
- existing provider secrets are masked, revealable only in edit mode, and auto-hide after 6 seconds

### Users

- required:
  - status
  - first name
  - last name
  - username
  - email
  - phone
  - at least one role
- password required only on create
- username is read-only on edit
- address requires:
  - address type
  - address line 1
  - city
  - state/province
  - postal code
  - country
- contact section is optional, but if any contact detail is entered then `contactType` becomes required
- lockout end is editable only when lockout is enabled
- admin functions from list:
  - password reset email
  - active/inactive status toggle
  - soft-delete/archive

### Roles

- role name required
- UI does not require description, though backend does
- inactive role forces `isAssignableToNewUsers` off
- non-editable/system roles cannot change new-user assignment flag
- permission selection uses a tree with cascading menu selection
- permission search activates at 3+ chars
- delete is permission-gated by `roles.delete`

### Permissions

- permission name required
- permission key required
- key forced to lowercase while typing
- key pattern only allows lowercase letters, digits, `_`, `.`
- `Action` and `WorkflowAction` require a parent/root menu
- `accessUrl`, if supplied, must start with `/`
- list supports filters for name, key, control type, status
- name/key searches require at least 3 chars

### Settings

- edit permission depends on `settings.edit`
- delete permission depends on `settings.delete`
- bulk save validates every pending item
- each item needs a non-empty key
- value rules:
  - `Int`: whole number
  - `Bool`: `true` or `false`
  - `Json`: valid JSON
  - all types reject empty values
- read-only entries are locked from edit/delete
- settings are grouped by scope:
  - System
  - Security
  - Notification
  - Branding
  - Integration

### Tokens

- search by token ID / user waits for 3 chars
- revoke / force-expire enabled only for token types interpreted as refresh or reference
- revoked tokens cannot be managed again from the UI
- token detail is loaded lazily in inspect modal

### Activities

- read-only audit list
- filters: date range, event type, actor type, status, search
- search waits for 3 chars

## Important Observations

- The portal is primarily an admin console plus hosted-auth shell.
- End-user OAuth login, MFA verification, device approval, self-registration, and password reset completion are handled outside this React app.
- The densest frontend business-rule layer is the Application wizard; most other screens are thin validation plus API orchestration.
