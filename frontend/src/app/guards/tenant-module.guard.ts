import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { TenantModulesService } from '../services/tenant-modules.service';
import { ToastService } from '../services/toast.service';

/**
 * Guard that checks if a specific tenant module is enabled.
 * Shows a toast error message and redirects to dashboard if module is disabled.
 */
export const tenantModuleGuard: (moduleName: string) => CanActivateFn = (moduleName: string) => {
  return (route, state) => {
    const tenantModulesService = inject(TenantModulesService);
    const router = inject(Router);
    const toastService = inject(ToastService);

    // Check if the module is enabled
    if (tenantModulesService.hasModule(moduleName)) {
      return true;
    }

    // Module is disabled - show error toast and redirect to dashboard
    toastService.error('Module is disabled for your organisation.');
    router.navigate(['/dashboard']);
    
    return false;
  };
};

