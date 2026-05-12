export interface AdminTenantListItemDto {
  id: string;
  name: string;
  domain: string | null;
  adminEmail: string;
  isActive: boolean;
  edition: string | null;
  plan: string | null;
  createdAt: string;
  documentCount: number;
  ncrCount: number;
  riskCount: number;
  modulesSummary: string;
  sharePointConnected: boolean;
  maxUsers: number;
}

export interface AdminTenantDetailDto {
  id: string;
  name: string;
  domain: string | null;
  adminEmail: string;
  isActive: boolean;
  edition: string | null;
  plan: string | null;
  subscriptionId: string | null;
  trialEndsAt: string | null;
  licenseExpiryDate: string | null;
  modulesEnabled: string[];
  logoUrl: string | null;
  primaryColor: string | null;
  createdAt: string;
  createdBy: string | null;
  modifiedAt: string | null;
  modifiedBy: string | null;
  documentCount: number;
  ncrCount: number;
  riskCount: number;
  sharePointSiteUrl: string | null;
  sharePointLibraryName: string | null;
  sharePointClientId: string | null;
  sharePointClientSecretConfigured: boolean;
  azureAdTenantId: string | null;
  maxUsers: number;
  maxStorageBytes: number;
}

export interface TenantSharePointSettingsDto {
  sharePointSiteUrl?: string | null;
  sharePointLibraryName?: string | null;
  sharePointClientId?: string | null;
  clientSecretMasked: string;
}

export interface TenantLicenseSettingsDto {
  subscriptionPlan: string;
  edition?: string | null;
  maxUsers: number;
  maxStorageBytes: number;
  subscriptionExpiresAt?: string | null;
  enabledModules: string[];
}

export interface AdminTenantBrandingDto {
  logoUrl: string | null;
  primaryColor: string | null;
}

export interface UpdateTenantBrandingRequest {
  logoUrl?: string | null;
  primaryColor?: string | null;
}

export interface UpdateAdminTenantStatusRequest {
  status: string;
}

