import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MsalService } from '@azure/msal-angular';
import { AuthenticationResult } from '@azure/msal-browser';
import { Observable, from, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { TenantContextService } from './tenant-context.service';

export interface AuthTenantSummary {
  id?: string;
  name: string;
  plan: string;
  setupComplete: boolean;
  setupStep: number;
  logoUrl?: string | null;
  isActive?: boolean;
}

export interface AuthUser {
  userId: string;
  tenantId: string | null;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  authProvider: 'Local' | 'AzureAD' | string;
  fullName?: string;
  tenant?: AuthTenantSummary;
}

const TOKEN_KEY = 'maemo_token';
const USER_KEY = 'maemo_user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private msalService = inject(MsalService);
  private http = inject(HttpClient);
  private router = inject(Router);
  private tenantContext = inject(TenantContextService);

  loginWithMicrosoft(): void {
    this.clearLocalSession();
    this.msalService.loginRedirect({
      scopes: [environment.azureAd.apiScope]
    });
  }

  /** MSAL interactive login used by legacy `login()` name. */
  login(): void {
    this.loginWithMicrosoft();
  }

  logout(): void {
    this.clearLocalSession();
    this.tenantContext.clearTenantId();
    this.tenantContext.clearBranding();
    this.msalService.logoutRedirect({
      postLogoutRedirectUri:
        environment.azureAd.postLogoutRedirectUri ??
        (typeof window !== 'undefined' ? window.location.origin : environment.azureAd.redirectUri)
    });
  }

  isAuthenticated(): boolean {
    return this.hasLocalToken() || this.msalService.instance.getActiveAccount() !== null;
  }

  hasLocalToken(): boolean {
    return !!this.getToken();
  }

  isMsalAuthenticated(): boolean {
    return this.msalService.instance.getActiveAccount() !== null;
  }

  getToken(): string | null {
    if (typeof window === 'undefined') {
      return null;
    }
    return localStorage.getItem(TOKEN_KEY);
  }

  getUser(): AuthUser | null {
    if (typeof window === 'undefined') {
      return null;
    }
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }

  loginLocal(email: string, password: string): Observable<{ token: string; user: AuthUser; tenant: AuthTenantSummary }> {
    return this.http
      .post<{
        token: string;
        expiresAt: string;
        user: AuthUser;
        tenant: AuthTenantSummary;
      }>(`${environment.apiBaseUrl}/api/auth/login`, { email, password })
      .pipe(
        tap((res) => {
          this.applyLocalSession(res.token, res.user, res.tenant);
        })
      );
  }

  register(body: Record<string, unknown>): Observable<unknown> {
    return this.http.post(`${environment.apiBaseUrl}/api/auth/register`, body);
  }

  verifyEmailToken(token: string): Observable<{ token: string; user: AuthUser; tenant: AuthTenantSummary }> {
    return this.http
      .post<{
        token: string;
        expiresAt: string;
        user: AuthUser;
        tenant: AuthTenantSummary;
      }>(`${environment.apiBaseUrl}/api/auth/verify-email`, { token })
      .pipe(
        tap((res) => {
          this.applyLocalSession(res.token, res.user, res.tenant);
        })
      );
  }

  resendVerification(email: string): Observable<unknown> {
    return this.http.post(`${environment.apiBaseUrl}/api/auth/resend-verification`, { email });
  }

  forgotPassword(email: string): Observable<unknown> {
    return this.http.post(`${environment.apiBaseUrl}/api/auth/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<unknown> {
    return this.http.post(`${environment.apiBaseUrl}/api/auth/reset-password`, { token, newPassword });
  }

  applyLocalSession(token: string, user: AuthUser, tenant: AuthTenantSummary): void {
    localStorage.setItem(TOKEN_KEY, token);
    const normalized: AuthUser = {
      ...user,
      tenant
    };
    localStorage.setItem(USER_KEY, JSON.stringify(normalized));
    if (tenant?.id) {
      this.tenantContext.setTenantId(tenant.id);
    } else if (user.tenantId) {
      this.tenantContext.setTenantId(user.tenantId);
    }
    if (tenant?.logoUrl != null || tenant != null) {
      this.tenantContext.setBranding(tenant.logoUrl ?? null, null);
    }
  }

  clearLocalSession(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    }
  }

  /** Sign out email/password session without Microsoft redirect. */
  logoutLocal(): void {
    this.clearLocalSession();
    this.tenantContext.clearTenantId();
    this.tenantContext.clearBranding();
    void this.router.navigate(['/login']);
  }

  getCurrentUser(): { id?: string; name?: string; username?: string } | null {
    const local = this.getUser();
    if (local) {
      return { id: local.userId, name: local.fullName ?? `${local.firstName} ${local.lastName}`, username: local.email };
    }
    const account = this.msalService.instance.getActiveAccount();
    return account
      ? {
          id: account.homeAccountId,
          name: account.name,
          username: account.username
        }
      : null;
  }

  acquireToken(): Observable<AuthenticationResult> {
    return from(
      this.msalService.acquireTokenSilent({
        scopes: [environment.azureAd.apiScope],
        account: this.msalService.instance.getActiveAccount()!
      })
    );
  }

  isSetupComplete(): boolean {
    return this.getUser()?.tenant?.setupComplete === true;
  }
}
