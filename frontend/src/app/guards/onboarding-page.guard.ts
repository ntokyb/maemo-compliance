import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { TenantMeProfile } from '../services/user-profile.service';

/** Block `/onboarding` once the wizard is already completed. */
export const onboardingPageGuard: CanActivateFn = () => {
  const http = inject(HttpClient);
  const router = inject(Router);

  return http.get<TenantMeProfile>(`${environment.apiBaseUrl}/api/tenant/me`).pipe(
    map((p) => {
      if (p?.onboardingComplete) {
        return router.createUrlTree(['/dashboard']);
      }
      return true;
    }),
    catchError(() => of(true))
  );
};
