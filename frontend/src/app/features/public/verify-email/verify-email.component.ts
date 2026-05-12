import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { PublicHeaderComponent } from '../public-header/public-header.component';
import { PublicFooterComponent } from '../public-footer/public-footer.component';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, PublicHeaderComponent, PublicFooterComponent],
  template: `
    <div class="public-page">
      <app-public-header />
      <main class="contain">
        <div *ngIf="loading" class="state">
          <p>Verifying your email…</p>
        </div>
        <div *ngIf="error" class="state err">
          <p>{{ error }}</p>
          <button type="button" class="btn" (click)="resend()" [disabled]="resendBusy || !emailForResend">
            Resend verification email
          </button>
        </div>
        <div *ngIf="success" class="state ok">
          <p><strong>Email verified.</strong></p>
          <p>You will be redirected to set up your workspace…</p>
        </div>
      </main>
      <app-public-footer />
    </div>
  `,
  styles: [
    `
      .public-page {
        min-height: 100vh;
        display: flex;
        flex-direction: column;
        font-family: system-ui, -apple-system, sans-serif;
        background: #fff;
      }
      .contain {
        flex: 1;
        max-width: 520px;
        margin: 0 auto;
        padding: 2.5rem 1.25rem;
      }
      .state {
        padding: 1.5rem;
        border-radius: 12px;
        border: 1px solid #e5e7eb;
      }
      .ok {
        border-color: #10b981;
        background: #ecfdf5;
      }
      .err {
        border-color: #fecaca;
        background: #fef2f2;
      }
      .btn {
        margin-top: 1rem;
        height: 44px;
        padding: 0 1rem;
        border-radius: 8px;
        border: none;
        background: #0f4c81;
        color: #fff;
        font-weight: 600;
        cursor: pointer;
      }
      .btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    `
  ]
})
export class VerifyEmailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(AuthService);

  loading = true;
  success = false;
  error: string | null = null;
  resendBusy = false;
  emailForResend = '';

  ngOnInit(): void {
    this.emailForResend = this.route.snapshot.queryParamMap.get('email') ?? '';
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!token) {
      this.loading = false;
      this.error = 'This verification link is missing a token.';
      return;
    }
    this.auth.verifyEmailToken(token).subscribe({
      next: () => {
        this.loading = false;
        this.success = true;
        this.emailForResend = this.auth.getUser()?.email ?? '';
        setTimeout(() => void this.router.navigate(['/dashboard']), 2000);
      },
      error: () => {
        this.loading = false;
        this.error = 'This verification link has expired or has already been used.';
      }
    });
  }

  resend(): void {
    if (!this.emailForResend) {
      return;
    }
    this.resendBusy = true;
    this.auth.resendVerification(this.emailForResend).subscribe({
      next: () => {
        this.resendBusy = false;
        this.error = 'If this address is registered, a new link has been sent.';
      },
      error: () => {
        this.resendBusy = false;
      }
    });
  }
}
