import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface Tenant {
  id: string;
  name: string;
  domain?: string;
  adminEmail: string;
  isActive: boolean;
  plan?: string;
  subscriptionId?: string;
  createdAt: string;
  trialEndsAt?: string;
  modulesEnabled?: string[];
  azureAdTenantId?: string;
  azureAdClientId?: string;
  azureAdClientSecret?: string;
  sharePointSiteId?: string;
  sharePointDriveId?: string;
  edition?: string;
  licenseExpiryDate?: string;
  maxUsers?: number;
  maxStorageBytes?: number;
  sharePointSiteUrl?: string;
  sharePointLibraryName?: string;
  sharePointClientId?: string;
  sharePointClientSecretConfigured?: boolean;
  logoUrl?: string;
  primaryColor?: string;
}

export interface TenantSharePointSettingsDto {
  sharePointSiteUrl?: string | null;
  sharePointLibraryName?: string | null;
  sharePointClientId?: string | null;
  clientSecretMasked: string;
}

export interface SharePointTestResultDto {
  success: boolean;
  message: string;
  libraryUrl?: string | null;
}

export interface OnboardingStepStatusDto {
  id: number;
  label: string;
  complete: boolean;
  link: string;
}

export interface OnboardingStatusDto {
  steps: OnboardingStepStatusDto[];
  completedCount: number;
  totalCount: number;
  allComplete: boolean;
  dismissed: boolean;
}

export interface TenantDirectoryRowDto {
  email: string;
  name?: string | null;
  role: string;
  status: string;
  lastLoginAt?: string | null;
}

export interface UpdateTenantSettingsRequest {
  name: string;
  domain?: string;
  adminEmail: string;
  isActive: boolean;
  plan?: string;
  trialEndsAt?: string;
}

export interface ConnectMicrosoft365Request {
  clientId: string;
  clientSecret: string;
  tenantId: string;
  sharePointSiteId?: string;
  sharePointDriveId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/api/tenants`;
  private tenantSelfUrl = `${environment.apiBaseUrl}/api/tenant`;

  getCurrentTenant(): Observable<{ tenantId: string }> {
    return this.http.get<{ tenantId: string }>(`${this.apiUrl}/current`);
  }

  getAllTenants(): Observable<Tenant[]> {
    return this.http.get<Tenant[]>(this.apiUrl);
  }

  getTenant(id: string): Observable<Tenant> {
    return this.http.get<Tenant>(`${this.apiUrl}/${id}`);
  }

  getTenantModules(id: string): Observable<string[]> {
    return this.http.get<Tenant>(`${this.apiUrl}/${id}`).pipe(
      map(tenant => tenant.modulesEnabled || [])
    );
  }

  updateTenantSettings(id: string, request: UpdateTenantSettingsRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, request);
  }

  connectMicrosoft365(tenantId: string, request: ConnectMicrosoft365Request): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${tenantId}/connect-m365`, request);
  }

  updatePortalGeneralSettings(request: { name: string; logoUrl?: string | null; primaryColor?: string | null }): Observable<void> {
    return this.http.put<void>(`${this.tenantSelfUrl}/settings`, request);
  }

  updateSharePointSettings(request: {
    sharePointSiteUrl?: string | null;
    sharePointClientId?: string | null;
    sharePointClientSecret?: string | null;
    sharePointLibraryName?: string | null;
  }): Observable<TenantSharePointSettingsDto> {
    return this.http.put<TenantSharePointSettingsDto>(`${this.tenantSelfUrl}/sharepoint`, request);
  }

  testSharePointConnection(): Observable<SharePointTestResultDto> {
    return this.http.post<SharePointTestResultDto>(`${this.tenantSelfUrl}/sharepoint/test`, {});
  }

  getOnboardingStatus(): Observable<OnboardingStatusDto> {
    return this.http.get<OnboardingStatusDto>(`${this.tenantSelfUrl}/onboarding-status`);
  }

  dismissOnboardingChecklist(): Observable<void> {
    return this.http.post<void>(`${this.tenantSelfUrl}/onboarding/dismiss`, {});
  }

  getWorkspaceDirectory(): Observable<TenantDirectoryRowDto[]> {
    return this.http.get<TenantDirectoryRowDto[]>(`${this.tenantSelfUrl}/users`);
  }

  inviteWorkspaceUser(email: string, role: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.tenantSelfUrl}/users/invite`, { email, role });
  }

  acceptInvitation(token: string): Observable<void> {
    return this.http.post<void>(`${this.tenantSelfUrl}/invitations/accept`, { token });
  }
}

