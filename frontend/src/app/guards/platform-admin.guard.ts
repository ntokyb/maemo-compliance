import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PlatformAdminService } from '../services/platform-admin.service';

export const platformAdminGuard: CanActivateFn = () => {
  const platform = inject(PlatformAdminService);
  const router = inject(Router);
  if (platform.isPlatformAdmin()) {
    return true;
  }
  return router.createUrlTree(['/dashboard']);
};
