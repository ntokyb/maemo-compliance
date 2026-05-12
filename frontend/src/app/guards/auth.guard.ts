import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { MsalService } from '@azure/msal-angular';

export const authGuard: CanActivateFn = (route, state) => {
  const msalService = inject(MsalService);
  const router = inject(Router);

  const account = msalService.instance.getActiveAccount();
  if (account) {
    return true;
  }

  // Redirect to login if not authenticated
  router.navigate(['/login']);
  return false;
};

