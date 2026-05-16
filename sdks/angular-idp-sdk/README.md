# TokenIDP Angular SDK

Official Angular SDK for integrating TokenIDP / SmartDevCon Identity Provider into Angular applications.

This SDK handles:

- OAuth 2.1 Authorization Code + PKCE flow
- Token management for access, refresh, and ID tokens
- Login and logout helpers
- Auth state through Angular signals
- Automatic token refresh
- Tenant propagation matching the React SDK

## Version

This package targets Angular `21.2.x`, the latest stable Angular line verified from npm as `@angular/core@21.2.13`.

Angular 21 requires Node.js `^20.19.0 || ^22.12.0 || ^24.0.0`, TypeScript `>=5.9.0 <6.0.0`, and RxJS `^6.5.3 || ^7.4.0`.

## Installation

```bash
npm install tokenidp-angular
```

For local development from this repository:

```bash
cd sdks/angular-idp-sdk
npm install
npm run pack
```

Then install the generated `.tgz` in an Angular app.

## Configure Auth

```ts
import { ApplicationConfig } from '@angular/core';
import { provideRouter, Routes } from '@angular/router';
import {
  IdpAuthCallbackComponent,
  IdpLoginComponent,
  provideTokenIdpAuth,
} from 'tokenidp-angular';

const routes: Routes = [
  { path: 'login', component: IdpLoginComponent },
  { path: 'callback', component: IdpAuthCallbackComponent },
];

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideTokenIdpAuth({
      authority: 'https://idp.example.com',
      clientId: 'angular-client',
      redirectUri: `${window.location.origin}/callback`,
      postLoginRedirectUri: '/dashboard',
      postLogoutRedirectUri: '/login?logged_out=1',
      scope: 'openid profile offline_access',
      storage: 'localStorage',
    }),
  ],
};
```

## Use Auth State

```ts
import { Component, inject } from '@angular/core';
import { TokenIdpAuthService } from 'tokenidp-angular';

@Component({
  standalone: true,
  selector: 'app-login-button',
  template: `
    @if (auth.isAuthenticated()) {
      <button type="button" (click)="auth.logout()">Logout</button>
    } @else {
      <button type="button" (click)="auth.login()">Login</button>
    }
  `,
})
export class LoginButtonComponent {
  readonly auth = inject(TokenIdpAuthService);
}
```

## Built-In Login Component

```ts
import { IdpLoginComponent } from 'tokenidp-angular';

// Route directly to IdpLoginComponent, or use it in a template:
// <tokenidp-login title="Redirecting to TokenIDP..." />
```

## Callback Route

```ts
import { IdpAuthCallbackComponent } from 'tokenidp-angular';

// Route /callback to IdpAuthCallbackComponent.
```

## Tenant Options

```ts
provideTokenIdpAuth({
  authority: 'https://idp.example.com',
  clientId: 'angular-client',
  redirectUri: `${window.location.origin}/callback`,
  tenantKey: 'tenant-a',
  tenantPropagationMode: 'all', // all | api | none
  tenantQueryParameter: 'tenant',
});
```
