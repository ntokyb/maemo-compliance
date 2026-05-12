import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { TenantContextService } from './tenant-context.service';

@Injectable({
  providedIn: 'root'
})
export class TenantModulesService {
  private readonly STORAGE_KEY = 'maemo_tenant_modules';
  private _enabledModules = signal<string[]>([]);

  constructor(
    private http: HttpClient,
    private tenantContextService: TenantContextService
  ) {
    // Load modules from localStorage on initialization
    const storedModules = localStorage.getItem(this.STORAGE_KEY);
    if (storedModules) {
      try {
        const modules = JSON.parse(storedModules);
        this._enabledModules.set(modules);
      } catch {
        // Invalid JSON, ignore
      }
    }
  }

  /**
   * Gets the list of enabled modules for the current tenant.
   * Returns empty array if no modules are enabled or tenant is not set.
   */
  getEnabledModules(): string[] {
    return this._enabledModules();
  }

  /**
   * Checks if a specific module is enabled.
   */
  hasModule(moduleName: string): boolean {
    return this._enabledModules().includes(moduleName);
  }

  /**
   * Loads enabled modules from the API.
   * This should be called after tenant selection.
   */
  loadModules(): Observable<string[]> {
    const tenantId = this.tenantContextService.getTenantId();
    if (!tenantId) {
      this._enabledModules.set([]);
      return of([]);
    }

    // Get modules and branding from tenant detail endpoint
    return this.http.get<{ modulesEnabled?: string[]; logoUrl?: string | null; primaryColor?: string | null }>(`${environment.apiBaseUrl}/api/tenants/${tenantId}`).pipe(
      map(response => {
        // Update branding in tenant context
        this.tenantContextService.setBranding(response.logoUrl || null, response.primaryColor || null);
        return response.modulesEnabled || [];
      }),
      map(modules => {
        this._enabledModules.set(modules);
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(modules));
        return modules;
      }),
      catchError(() => {
        // If API call fails, return empty array (fail closed)
        this._enabledModules.set([]);
        return of([]);
      })
    );
  }

  /**
   * Clears the cached modules (e.g., when tenant changes).
   */
  clearModules(): void {
    this._enabledModules.set([]);
    localStorage.removeItem(this.STORAGE_KEY);
  }
}

