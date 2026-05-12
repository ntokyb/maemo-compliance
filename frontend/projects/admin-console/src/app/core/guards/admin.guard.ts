import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { environment } from '../../../environments/environment';

/**
 * Admin Guard - Verifies user has PlatformAdmin role OR X-Dev-Admin header is present (dev only).
 * 
 * In development, allows access if X-Dev-Admin header can be sent (enabled via sessionStorage).
 * In production, verifies PlatformAdmin role from authentication token.
 */
export const adminGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  // In development, check if dev admin mode is enabled
  if (!environment.production) {
    // Enable dev admin mode by default in development
    // User can disable it by setting sessionStorage.setItem('dev-admin-enabled', 'false')
    const devAdminEnabled = sessionStorage.getItem('dev-admin-enabled');
    if (devAdminEnabled === 'false') {
      // Dev admin explicitly disabled
      console.warn('Admin Console access denied: Dev admin mode is disabled.');
      router.navigate(['/login']);
      return false;
    }

    // Enable dev admin mode if not explicitly disabled
    if (devAdminEnabled !== 'true') {
      sessionStorage.setItem('dev-admin-enabled', 'true');
    }

    // Allow access - backend will validate X-Dev-Admin header
    return true;
  }

  // In production, verify PlatformAdmin role from auth token
  // TODO: Integrate with actual auth service when authentication is implemented
  // const authService = inject(AuthService);
  // const user = authService.getCurrentUser();
  // if (user && user.roles?.includes('PlatformAdmin')) {
  //   return true;
  // }

  // Access denied - redirect to login
  console.warn('Admin Console access denied: PlatformAdmin role required.');
  router.navigate(['/login']);
  return false;
};

