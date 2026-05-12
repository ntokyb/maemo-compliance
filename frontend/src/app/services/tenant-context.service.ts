import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class TenantContextService {
  private readonly STORAGE_KEY = 'maemo_tenant_id';
  private readonly CLIENT_TENANT_KEY = 'maemo_client_tenant_id';
  private readonly IS_CONSULTANT_KEY = 'maemo_is_consultant';
  private readonly DEMO_TENANT_NAME_KEY = 'maemo_demo_tenant_name';
  private readonly BRANDING_KEY = 'maemo_tenant_branding';
  
  private _tenantId = signal<string | null>(null);
  private _clientTenantId = signal<string | null>(null);
  private _isConsultant = signal<boolean>(false);
  private _demoTenantName = signal<string | null>(null);
  private _logoUrl = signal<string | null>(null);
  private _primaryColor = signal<string | null>(null);

  constructor() {
    // Load tenant ID from localStorage on initialization
    const storedTenantId = localStorage.getItem(this.STORAGE_KEY);
    if (storedTenantId) {
      this._tenantId.set(storedTenantId);
    }

    // Load client tenant ID for consultants
    const storedClientTenantId = localStorage.getItem(this.CLIENT_TENANT_KEY);
    if (storedClientTenantId) {
      this._clientTenantId.set(storedClientTenantId);
    }

    // Load consultant flag
    const isConsultant = localStorage.getItem(this.IS_CONSULTANT_KEY) === 'true';
    this._isConsultant.set(isConsultant);

    // Load demo tenant name
    const storedDemoTenantName = localStorage.getItem(this.DEMO_TENANT_NAME_KEY);
    if (storedDemoTenantName) {
      this._demoTenantName.set(storedDemoTenantName);
    }

    // Load branding
    const storedBranding = localStorage.getItem(this.BRANDING_KEY);
    if (storedBranding) {
      try {
        const branding = JSON.parse(storedBranding);
        this._logoUrl.set(branding.logoUrl || null);
        this._primaryColor.set(branding.primaryColor || null);
      } catch {
        // Invalid JSON, ignore
      }
    }
  }

  getTenantId(): string | null {
    // If consultant, return client tenant ID; otherwise return regular tenant ID
    if (this._isConsultant() && this._clientTenantId()) {
      return this._clientTenantId();
    }
    return this._tenantId();
  }

  setTenantId(id: string): void {
    this._tenantId.set(id);
    localStorage.setItem(this.STORAGE_KEY, id);
    
    // Clear client tenant ID when setting regular tenant ID
    if (!this._isConsultant()) {
      this._clientTenantId.set(null);
      localStorage.removeItem(this.CLIENT_TENANT_KEY);
    }
  }

  setClientTenantId(id: string): void {
    this._clientTenantId.set(id);
    localStorage.setItem(this.CLIENT_TENANT_KEY, id);
  }

  getClientTenantId(): string | null {
    return this._clientTenantId();
  }

  clearTenantId(): void {
    this._tenantId.set(null);
    this._clientTenantId.set(null);
    localStorage.removeItem(this.STORAGE_KEY);
    localStorage.removeItem(this.CLIENT_TENANT_KEY);
  }

  hasTenant(): boolean {
    return this.getTenantId() !== null;
  }

  setIsConsultant(isConsultant: boolean): void {
    this._isConsultant.set(isConsultant);
    if (isConsultant) {
      localStorage.setItem(this.IS_CONSULTANT_KEY, 'true');
    } else {
      localStorage.removeItem(this.IS_CONSULTANT_KEY);
      // Clear client tenant ID when not consultant
      this._clientTenantId.set(null);
      localStorage.removeItem(this.CLIENT_TENANT_KEY);
    }
  }

  isConsultant(): boolean {
    return this._isConsultant();
  }

  setDemoTenantName(name: string): void {
    this._demoTenantName.set(name);
    localStorage.setItem(this.DEMO_TENANT_NAME_KEY, name);
  }

  getDemoTenantName(): string | null {
    return this._demoTenantName();
  }

  isDemoMode(): boolean {
    return this._demoTenantName() !== null && this._demoTenantName() === 'Demo Manufacturing Co.';
  }

  clearDemoMode(): void {
    this._demoTenantName.set(null);
    localStorage.removeItem(this.DEMO_TENANT_NAME_KEY);
  }

  setBranding(logoUrl: string | null, primaryColor: string | null): void {
    this._logoUrl.set(logoUrl);
    this._primaryColor.set(primaryColor);
    localStorage.setItem(this.BRANDING_KEY, JSON.stringify({ logoUrl, primaryColor }));
  }

  getLogoUrl(): string | null {
    return this._logoUrl();
  }

  getPrimaryColor(): string | null {
    return this._primaryColor();
  }

  clearBranding(): void {
    this._logoUrl.set(null);
    this._primaryColor.set(null);
    localStorage.removeItem(this.BRANDING_KEY);
  }
}

