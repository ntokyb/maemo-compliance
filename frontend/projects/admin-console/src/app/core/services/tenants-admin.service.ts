import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminTenantDetailDto, AdminTenantListItemDto, UpdateAdminTenantStatusRequest, AdminTenantBrandingDto, UpdateTenantBrandingRequest, TenantSharePointSettingsDto, TenantLicenseSettingsDto } from '../models/admin-tenant.dto';
import { AdminApiService } from './admin-api.service';

@Injectable({
  providedIn: 'root'
})
export class TenantsAdminService {
  constructor(private api: AdminApiService) {}

  /**
   * Get list of all tenants
   */
  getTenants(): Observable<AdminTenantListItemDto[]> {
    return this.api.get<AdminTenantListItemDto[]>('/tenants');
  }

  /**
   * Get tenant detail by ID
   */
  getTenantDetail(id: string): Observable<AdminTenantDetailDto> {
    return this.api.get<AdminTenantDetailDto>(`/tenants/${id}`);
  }

  /**
   * Update tenant status (Active or Suspended)
   */
  updateTenantStatus(id: string, status: string): Observable<void> {
    const request: UpdateAdminTenantStatusRequest = { status };
    return this.api.post<void>(`/tenants/${id}/status`, request);
  }

  /**
   * Get tenant branding
   */
  getTenantBranding(id: string): Observable<AdminTenantBrandingDto> {
    return this.api.get<AdminTenantBrandingDto>(`/tenants/${id}/branding`);
  }

  /**
   * Update tenant branding
   */
  updateTenantBranding(id: string, payload: UpdateTenantBrandingRequest): Observable<void> {
    return this.api.put<void>(`/tenants/${id}/branding`, payload);
  }

  updateTenantSharePoint(
    id: string,
    body: {
      sharePointSiteUrl?: string | null;
      sharePointClientId?: string | null;
      sharePointClientSecret?: string | null;
      sharePointLibraryName?: string | null;
    }
  ): Observable<TenantSharePointSettingsDto> {
    return this.api.put<TenantSharePointSettingsDto>(`/tenants/${id}/sharepoint`, body);
  }

  testTenantSharePoint(id: string): Observable<{ success: boolean; message: string; libraryUrl?: string | null }> {
    return this.api.post<{ success: boolean; message: string; libraryUrl?: string | null }>(`/tenants/${id}/sharepoint/test`, {});
  }

  updateTenantLicense(
    id: string,
    body: {
      subscriptionPlan: string;
      maxUsers: number;
      maxStorageBytes: number;
      subscriptionExpiresAt?: string | null;
      enabledModules: string[];
    }
  ): Observable<TenantLicenseSettingsDto> {
    return this.api.put<TenantLicenseSettingsDto>(`/tenants/${id}/license`, body);
  }
}

