import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject } from '@angular/core';
import { TokenIdpAuthService } from './auth.service';
import { TokenIdpLoginOptions } from './models';

@Component({
  selector: 'tokenidp-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="idp-login-page">
      <section class="idp-login-section-container">
        <div class="idp-login-section idp-redirect-card">
          <div class="idp-content">
            <ng-container *ngIf="logoUrl; else fallbackLogo">
              <div class="idp-logo-image-wrap">
                <img [src]="logoUrl" [alt]="logoAlt" width="250" />
              </div>
            </ng-container>
            <ng-template #fallbackLogo>
              <div class="idp-logo">{{ fallbackBadge }}</div>
            </ng-template>

            <ng-container *ngIf="isLoggedOut; else redirecting">
              <h1>{{ signedOutTitle }}</h1>
              <p>{{ signedOutSubtitle }}</p>
              <button type="button" class="idp-button" (click)="signInAgain()">
                {{ signInAgainLabel }}
              </button>
            </ng-container>

            <ng-template #redirecting>
              <h1>{{ title }}</h1>
              <p>{{ subtitle }}</p>
              <div class="idp-spinner" aria-hidden="true"></div>
            </ng-template>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .idp-login-page {
        min-height: 100vh;
        display: grid;
        place-items: center;
        background: #f5f7fb;
        color: #151923;
        font-family:
          Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      }

      .idp-login-section-container {
        width: min(440px, calc(100vw - 32px));
      }

      .idp-login-section {
        background: #fff;
        border: 1px solid #e4e8f0;
        border-radius: 8px;
        box-shadow: 0 18px 45px rgba(15, 23, 42, 0.08);
      }

      .idp-content {
        padding: 32px;
        text-align: center;
      }

      .idp-logo,
      .idp-logo-image-wrap {
        margin: 0 auto 16px;
      }

      .idp-logo {
        width: 56px;
        height: 56px;
        display: grid;
        place-items: center;
        border-radius: 8px;
        background: #111827;
        color: #fff;
        font-weight: 700;
      }

      h1 {
        margin: 0 0 8px;
        font-size: 24px;
        line-height: 1.25;
      }

      p {
        margin: 0;
        color: #667085;
        line-height: 1.5;
      }

      .idp-button {
        margin-top: 20px;
        min-height: 40px;
        border: 0;
        border-radius: 6px;
        padding: 0 16px;
        background: #0f62fe;
        color: #fff;
        font-weight: 600;
        cursor: pointer;
      }

      .idp-spinner {
        width: 28px;
        height: 28px;
        border: 3px solid #d0d5dd;
        border-top-color: #0f62fe;
        border-radius: 50%;
        margin: 20px auto 0;
        animation: idp-spin 0.8s linear infinite;
      }

      @keyframes idp-spin {
        to {
          transform: rotate(360deg);
        }
      }
    `,
  ],
})
export class IdpLoginComponent implements OnInit {
  @Input() logoUrl = '';
  @Input() logoAlt = 'Application logo';
  @Input() title = 'Redirecting to sign-in...';
  @Input() subtitle = 'Please wait while we securely connect to Identity.';
  @Input() signedOutTitle = 'You have been signed out';
  @Input() signedOutSubtitle = 'Start a new session when you are ready.';
  @Input() signInAgainLabel = 'Sign in again';
  @Input() fallbackBadge = 'ID';
  @Input() loginOptions?: TokenIdpLoginOptions;

  protected isLoggedOut = false;

  private readonly auth = inject(TokenIdpAuthService);

  ngOnInit(): void {
    this.isLoggedOut = new URLSearchParams(window.location.search).get('logged_out') === '1';

    if (!this.isLoggedOut) {
      void this.auth.login(this.loginOptions);
    }
  }

  protected signInAgain(): void {
    void this.auth.login(this.loginOptions);
  }
}
