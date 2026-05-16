import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TokenIdpAuthService } from './auth.service';

@Component({
  selector: 'tokenidp-auth-callback',
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
            <h1>Completing sign-in</h1>
            <p>{{ error || 'Please wait while we finish authentication.' }}</p>
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
    `,
  ],
})
export class IdpAuthCallbackComponent implements OnInit {
  @Input() redirectTo = '';
  @Input() logoUrl = '';
  @Input() logoAlt = 'Application logo';
  @Input() fallbackBadge = 'ID';

  protected error = '';

  private readonly auth = inject(TokenIdpAuthService);
  private readonly router = inject(Router);

  async ngOnInit(): Promise<void> {
    const qs = new URLSearchParams(window.location.search);
    const code = qs.get('code');
    const state = qs.get('state');
    const err = qs.get('error');
    const errDesc = qs.get('error_description');

    if (err) {
      this.setError(errDesc || err || 'Authentication error.');
      return;
    }

    if (!code || !state) {
      this.setError('Missing authorization code or state.');
      return;
    }

    try {
      await this.auth.handleCallback({ code, state });
      await this.router.navigateByUrl(this.redirectTo || this.auth.landingPage() || '/', {
        replaceUrl: true,
      });
    } catch (error) {
      this.setError(error instanceof Error ? error.message : 'Login callback failed.');
    }
  }

  private setError(message: string): void {
    this.error = message;
    this.auth.setError(message);
  }
}
