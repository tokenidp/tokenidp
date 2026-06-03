# TokenIDP React SDK

Official React SDK for integrating TokenIDP OAuth and OpenID Connect authentication into React applications.

The SDK handles Authorization Code + PKCE, OAuth callback processing, token persistence, refresh-token rotation, logout and revocation, hosted-login redirects, auth state, and tenant propagation.

## Package

```bash
npm install tokenidp-react
```

For local development from this repository:

```bash
cd sdks/react-idp-sdk
npm install
npm run build
npm run pack
```

Then install the generated `.tgz` in a React app.

## Exports

```js
import {
  AuthCallback,
  IdpAuthProvider,
  LoginPage,
  defaultAuthConfig,
  useAuth,
} from "tokenidp-react";
```

## Configure Auth

Wrap the app with `IdpAuthProvider`.

```jsx
import { IdpAuthProvider } from "tokenidp-react";

export function Root() {
  return (
    <IdpAuthProvider
      config={{
        authority: "https://idp.example.com",
        clientId: "react-client",
        redirectUri: `${window.location.origin}/auth/callback`,
        postLoginRedirectUri: "/dashboard",
        postLogoutRedirectUri: "/login?logged_out=1",
        scope: "openid profile offline_access",
        storage: "sessionStorage",
      }}
    >
      <App />
    </IdpAuthProvider>
  );
}
```

Required values:

- `authority`
- `clientId`
- `redirectUri`

Common optional values:

- `scope`, default `openid profile offline_access`
- `audience`
- `postLoginRedirectUri`, default `/`
- `postLogoutRedirectUri`, default `/login`
- `storage`, one of `memory`, `sessionStorage`, or `localStorage`
- `autoRefresh`, default `true`
- `refreshSkewSeconds`, default `180`

## Routes

Add a login route and callback route with React Router.

```jsx
import { AuthCallback, LoginPage } from "tokenidp-react";

<Routes>
  <Route path="/login" element={<LoginPage />} />
  <Route path="/auth/callback" element={<AuthCallback />} />
</Routes>
```

`LoginPage` automatically redirects to TokenIDP unless the URL contains `logged_out=1`, in which case it shows a signed-out state and a sign-in button.

`AuthCallback` reads `code` and `state`, validates OAuth state, exchanges the code for tokens, stores the session, and navigates to `postLoginRedirectUri` unless `redirectTo` is supplied.

## Use Auth State

```jsx
import { useAuth } from "tokenidp-react";

export function LoginButton() {
  const auth = useAuth();

  if (auth.isAuthenticated) {
    return <button onClick={auth.logout}>Logout</button>;
  }

  return <button onClick={() => auth.login()}>Login</button>;
}
```

The `useAuth()` value includes:

- `isAuthenticated`
- `tenantKey`
- `landingPage`
- `accessToken`
- `refreshToken`
- `idToken`
- `expiresAt`
- `error`
- `login(options)`
- `logout()`
- `handleCallback({ code, state })`
- `refresh()`
- `setError(message)`

## Login Options

`login(options)` supports:

- `prompt`
- `loginHint`
- `audience`
- `tenantKey`

```jsx
auth.login({
  loginHint: "user@example.com",
  tenantKey: "tenant-a",
});
```

## Tenant Propagation

Tenant propagation is controlled by:

- `tenantKey`
- `tenantPropagationMode`: `all`, `api`, or `none`
- `tenantQueryParameter`, default `tenant`
- `tenantHeaderName`, default `X-Tenant-Key`
- `tenantKeyStorageKey`, default `idp_tenant_key`

Modes:

- `all`: add tenant query parameter to hosted authorization and built-in auth endpoint calls
- `api`: resolve and persist tenant for application API usage without adding tenant to hosted auth endpoint calls
- `none`: do not resolve or propagate tenant automatically

The SDK resolves tenant key from explicit login options, provider config, the current URL query parameter, or session storage.

## Logout

`logout()` attempts to revoke the refresh token, clears the local session, and redirects to the TokenIDP logout endpoint with `client_id` and `post_logout_redirect_uri`.

## Notes

- PKCE uses `S256`.
- OAuth state is stored in `sessionStorage` and validated during callback handling.
- Refresh uses the configured refresh token and keeps the previous refresh token if the server does not return a rotated value.
- The SDK stores tokens according to the configured storage mode. Use `sessionStorage` or `memory` for browser clients unless the application explicitly accepts the persistence tradeoff of `localStorage`.
