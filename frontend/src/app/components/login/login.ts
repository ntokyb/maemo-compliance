import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { TenantContextService } from '../../services/tenant-context.service';
import { BrandingService } from '../../services/branding.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-login',
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private tenantContextService = inject(TenantContextService);
  private brandingService = inject(BrandingService);
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);

  demoLoading = false;
  demoError: string | null = null;

  tab: 'email' | 'microsoft' = 'email';
  localLoading = false;
  localError: string | null = null;
  verifyHint: string | null = null;

  loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  ngOnInit(): void {
    this.brandingService.loadBranding();

    const q = this.router.routerState.snapshot.root.queryParamMap;
    if (q.get('pw') === '1') {
      this.verifyHint = 'Password updated. Please sign in.';
    }

    if (this.authService.isAuthenticated()) {
      this.checkTenantAndRedirect();
    }
  }

  get branding() {
    return this.brandingService.getBranding();
  }

  setTab(tab: 'email' | 'microsoft'): void {
    this.tab = tab;
    this.localError = null;
  }

  submitLocal(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    this.localLoading = true;
    this.localError = null;
    const { email, password } = this.loginForm.getRawValue();
    this.authService.loginLocal(email, password).subscribe({
      next: () => {
        this.localLoading = false;
        void this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.localLoading = false;
        const code = err?.error?.code;
        if (err?.status === 403 && code === 'EmailNotVerified') {
          this.localError = 'Please verify your email first. Check your inbox for the link.';
          return;
        }
        if (err?.status === 403 && code === 'TenantPendingApproval') {
          void this.router.navigate(['/account-pending']);
          return;
        }
        if (err?.status === 400 && code === 'UseMicrosoft') {
          this.localError = err?.error?.message ?? 'Use the Microsoft tab to sign in.';
          return;
        }
        this.localError = err?.error?.message ?? 'Email or password incorrect.';
      },
    });
  }

  loginMicrosoft(): void {
    this.authService.loginWithMicrosoft();
  }

  switchToDemo(): void {
    this.demoLoading = true;
    this.demoError = null;

    this.http.get<{ tenantId: string; tenantName: string; code: string }>(`${environment.apiBaseUrl}/api/demo/tenant`).subscribe({
      next: (response) => {
        this.tenantContextService.setTenantId(response.tenantId);
        this.tenantContextService.setDemoTenantName(response.tenantName);
        this.demoLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.demoLoading = false;
        this.demoError = 'Demo tenant is currently unavailable.';
        console.error('Error loading demo tenant:', err);
      },
    });
  }

  private checkTenantAndRedirect(): void {
    const invite = sessionStorage.getItem('maemo_invite_token');
    if (invite) {
      this.router.navigate(['/accept-invite'], { queryParams: { token: invite } });
      return;
    }

    if (this.tenantContextService.hasTenant()) {
      this.router.navigate(['/dashboard']);
    } else {
      this.router.navigate(['/tenant-selector']);
    }
  }
}
