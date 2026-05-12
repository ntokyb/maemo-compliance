import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { from, switchMap, catchError } from 'rxjs';
import { environment } from '../../environments/environment';
import { TenantContextService } from '../services/tenant-context.service';

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

  if (!isMaemoBackendRequest(req.url)) {
    return next(req);
  }

  // Build headers object
  const headers: { [key: string]: string } = {};

  // Add tenant ID header if available
  const tenantId = tenantContextService.getTenantId();
  if (tenantId) {
    headers['X-Tenant-Id'] = tenantId;
  }

  // Add authorization token if authenticated
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
        const clonedRequest = req.clone({
          setHeaders: headers
        });
        return next(clonedRequest);
      }),
      catchError((error) => {
        console.error('Token acquisition failed:', error);
        // Still send request with tenant ID even if token acquisition fails
        const clonedRequest = req.clone({
          setHeaders: headers
        });
        return next(clonedRequest);
      })
    );
  }

  // If not authenticated, still send request with tenant ID if available
  if (Object.keys(headers).length > 0) {
    const clonedRequest = req.clone({
      setHeaders: headers
    });
    return next(clonedRequest);
  }

  return next(req);
};

