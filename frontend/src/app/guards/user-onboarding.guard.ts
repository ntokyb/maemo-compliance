import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { TenantContextService } from '../services/tenant-context.service';
import { TenantMeProfile } from '../services/user-profile.service';

/** Send users who have not finished the first-run wizard to `/onboarding`. */
export const userOnboardingGuard: CanActivateFn = () => {
  const tenantContext = inject(TenantContextService);
  const http = inject(HttpClient);
  const router = inject(Router);

  if (tenantContext.isConsultant()) {
    return true;
  }

  return http.get<TenantMeProfile>(`${environment.apiBaseUrl}/api/tenant/me`).pipe(
    map((p) => {
      if (p?.onboardingComplete) {
        return true;
      }
      return router.createUrlTree(['/onboarding']);
    }),
    catchError(() => of(true))
  );
};
