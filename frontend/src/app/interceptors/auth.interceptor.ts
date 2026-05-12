import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { from, switchMap, catchError } from 'rxjs';
import { environment } from '../../environments/environment';
import { TenantContextService } from '../services/tenant-context.service';
import { AuthService } from '../services/auth.service';

function isMaemoBackendRequest(url: string): boolean {
  if (!url.startsWith('http://') && !url.startsWith('https://')) {
    return (
      url.startsWith('/api') ||
      url.startsWith('/engine') ||
      url.startsWith('/admin') ||
      url.startsWith('/health')
    );
  }
  try {
    const parsed = new URL(url);
    const p = parsed.pathname;
    return (
      p.startsWith('/api') ||
      p.startsWith('/engine') ||
      p.startsWith('/admin') ||
      p.startsWith('/health')
    );
  } catch {
    return false;
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const msalService = inject(MsalService);
  const tenantContextService = inject(TenantContextService);
  const authService = inject(AuthService);

  if (!isMaemoBackendRequest(req.url)) {
    return next(req);
  }

  const headers: { [key: string]: string } = {};

  const tenantId = tenantContextService.getTenantId();
  if (tenantId) {
    headers['X-Tenant-Id'] = tenantId;
  }

  const localToken = authService.getToken();
  if (localToken) {
    headers['Authorization'] = `Bearer ${localToken}`;
    return next(req.clone({ setHeaders: headers }));
  }

  const account = msalService.instance.getActiveAccount();
  if (account) {
    return from(
      msalService.acquireTokenSilent({
        scopes: [environment.azureAd.apiScope],
        account: account
      })
    ).pipe(
      switchMap((response) => {
        headers['Authorization'] = `Bearer ${response.accessToken}`;
        return next(req.clone({ setHeaders: headers }));
      }),
      catchError(() => {
        if (Object.keys(headers).length > 0) {
          return next(req.clone({ setHeaders: headers }));
        }
        return next(req);
      })
    );
  }

  if (Object.keys(headers).length > 0) {
   return next(req.clone({ setHeaders: headers }));
  }

  return next(req);
};
