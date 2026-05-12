import { Injectable, inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { AuthenticationResult } from '@azure/msal-browser';
import { Observable, from } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private msalService = inject(MsalService);

  /** Starts interactive login (full-page redirect). */
  login(): void {
    this.msalService.loginRedirect({
      scopes: [environment.azureAd.apiScope]
    });
  }

  logout(): void {
    this.msalService.logoutRedirect({
      postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : environment.azureAd.redirectUri
    });
  }

  isAuthenticated(): boolean {
    return this.msalService.instance.getActiveAccount() !== null;
  }

  getCurrentUser(): any {
    const account = this.msalService.instance.getActiveAccount();
    return account ? {
      id: account.homeAccountId,
      name: account.name,
      username: account.username
    } : null;
  }

  acquireToken(): Observable<AuthenticationResult> {
    return from(this.msalService.acquireTokenSilent({
      scopes: [environment.azureAd.apiScope],
      account: this.msalService.instance.getActiveAccount()!
    }));
  }
}

