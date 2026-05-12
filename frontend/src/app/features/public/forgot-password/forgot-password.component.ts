import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { PublicHeaderComponent } from '../public-header/public-header.component';
import { PublicFooterComponent } from '../public-footer/public-footer.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    PublicHeaderComponent,
    PublicFooterComponent
  ],
  template: `
    <div class="public-page">
      <app-public-header />
      <main class="contain">
        <h1>Reset your password</h1>
        <p class="hint">If that email is registered, you will receive a link shortly.</p>
        <form [formGroup]="form" (ngSubmit)="submit()" class="card">
          <label for="em">Email</label>
          <input id="em" type="email" formControlName="email" class="ctrl" autocomplete="email" />
          <button type="submit" class="btn primary" [disabled]="submitting || form.invalid">
            {{ submitting ? 'Sending…' : 'Send reset link' }}
          </button>
          <p class="back"><a routerLink="/login">← Back to login</a></p>
        </form>
        <p *ngIf="done" class="banner ok">{{ done }}</p>
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
        max-width: 440px;
        margin: 0 auto;
        padding: 2rem 1.25rem;
      }
      .card {
        margin-top: 1rem;
        padding: 1.5rem;
        border: 1px solid #e5e7eb;
        border-radius: 12px;
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }
      .ctrl {
        height: 44px;
        border: 1.5px solid #d1d5db;
        border-radius: 8px;
        padding: 0 14px;
        font-size: 15px;
      }
      .btn.primary {
        margin-top: 0.5rem;
        height: 44px;
        border: none;
        border-radius: 8px;
        background: #0f4c81;
        color: #fff;
        font-weight: 600;
        cursor: pointer;
      }
      .hint {
        color: #6b7280;
      }
      .banner.ok {
        margin-top: 1rem;
        color: #047857;
      }
      .back {
        margin: 0.5rem 0 0;
        font-size: 14px;
      }
    `
  ]
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

  submitting = false;
  done: string | null = null;

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.submitting = true;
    this.auth.forgotPassword(this.form.getRawValue().email.trim()).subscribe({
      next: (r: any) => {
        this.submitting = false;
        this.done = r?.message ?? 'If that email is registered, you will receive a link shortly.';
      },
      error: () => {
        this.submitting = false;
        this.done = 'If that email is registered, you will receive a link shortly.';
      }
    });
  }
}
