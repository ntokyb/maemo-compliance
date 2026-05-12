import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { PublicHeaderComponent } from '../public-header/public-header.component';
import { PublicFooterComponent } from '../public-footer/public-footer.component';

@Component({
  selector: 'app-reset-password',
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
        <h1>Set a new password</h1>
        <form [formGroup]="form" (ngSubmit)="submit()" class="card">
          <label for="p1">New password</label>
          <input id="p1" type="password" formControlName="password" class="ctrl" autocomplete="new-password" />
          <label for="p2">Confirm password</label>
          <input id="p2" type="password" formControlName="confirm" class="ctrl" autocomplete="new-password" />
          <p *ngIf="error" class="err">{{ error }}</p>
          <button type="submit" class="btn primary" [disabled]="submitting || form.invalid">
            {{ submitting ? 'Saving…' : 'Update password' }}
          </button>
          <p class="back"><a routerLink="/login">← Back to login</a></p>
        </form>
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
      .err {
        color: #b91c1c;
        font-size: 14px;
      }
    `
  ]
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(AuthService);

  token = '';
  submitting = false;
  error: string | null = null;

  form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
  }

  submit(): void {
    const v = this.form.getRawValue();
    if (v.password !== v.confirm) {
      this.error = 'Passwords do not match.';
      return;
    }
    if (!/[A-Z]/.test(v.password) || !/\d/.test(v.password)) {
      this.error = 'Use at least 8 characters with one uppercase letter and one number.';
      return;
    }
    if (!this.token) {
      this.error = 'Invalid or missing reset token.';
      return;
    }
    this.error = null;
    this.submitting = true;
    this.auth.resetPassword(this.token, v.password).subscribe({
      next: () => void this.router.navigate(['/login'], { queryParams: { pw: '1' } }),
      error: (err) => {
        this.submitting = false;
        this.error = err?.error?.message || 'Reset failed.';
      }
    });
  }
}
