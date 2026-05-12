import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { TenantContextService } from '../services/tenant-context.service';

export const tenantGuard: CanActivateFn = (route, state) => {
  const tenantContextService = inject(TenantContextService);
  const router = inject(Router);

  // If consultant, check if client tenant is selected
  if (tenantContextService.isConsultant()) {
    if (tenantContextService.getClientTenantId()) {
      return true;
    }
    // For consultants, allow access but they'll need to select a client
    // The client switcher will handle this
    return true;
  }

  // For regular users, check if tenant is selected
  if (tenantContextService.hasTenant()) {
    return true;
  }

  // Redirect to tenant selector if no tenant selected
  router.navigate(['/tenant-selector']);
  return false;
};

