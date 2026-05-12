import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';

/**
 * Admin Auth Interceptor - Adds authentication headers for admin API calls
 * In development, adds X-Dev-Admin header for bypassing auth.
 * TODO: Integrate with MSAL or JWT token service when authentication is implemented
 */
export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
  // Only intercept requests to admin API
  if (req.url.startsWith(environment.adminApiBaseUrl)) {
    // In development, add X-Dev-Admin header if enabled
    if (!environment.production) {
      // Enable dev admin mode by default in development
      const devAdminEnabled = sessionStorage.getItem('dev-admin-enabled');
      if (devAdminEnabled !== 'false') {
        // Set to true if not explicitly disabled
        if (devAdminEnabled !== 'true') {
          sessionStorage.setItem('dev-admin-enabled', 'true');
        }
        req = req.clone({
          setHeaders: {
            'X-Dev-Admin': 'true'
          }
        });
      }
    }

    // TODO: Get token from auth service (MSAL or JWT)
    // const token = inject(AuthService).getAccessToken();
    // if (token) {
    //   req = req.clone({
    //     setHeaders: {
    //       Authorization: `Bearer ${token}`
    //     }
    //   });
    // }
  }

  return next(req);
};

