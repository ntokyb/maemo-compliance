import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of } from 'rxjs';
import { environment } from '../../environments/environment';

interface OnboardingStatusResponse {
  setupComplete: boolean;
  tenant?: { isActive?: boolean };
}

/** Company setup wizard and growth-plan gate before the main shell. */
export const companySetupGuard: CanActivateFn = (_route, state) => {
  const http = inject(HttpClient);
  const router = inject(Router);

  if (
    state.url.includes('/setup') ||
    state.url.includes('/onboarding') ||
    state.url.includes('/account-pending')
  ) {
    return true;
  }

  return http.get<OnboardingStatusResponse>(`${environment.apiBaseUrl}/api/onboarding/status`).pipe(
    map((s) => {
      if (s.tenant?.isActive === false) {
        return router.createUrlTree(['/account-pending']);
      }
      if (!s.setupComplete) {
        return router.createUrlTree(['/setup']);
      }
      return true;
    }),
    catchError(() => of(true))
  );
};
