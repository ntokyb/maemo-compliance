import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminTenantDetailDto, UpdateTenantBrandingRequest } from '../../core/models/admin-tenant.dto';
import { BusinessAuditLogDto } from '../../core/models/admin-business-log.dto';
import { TenantsAdminService } from '../../core/services/tenants-admin.service';
import { BusinessLogsAdminService } from '../../core/services/business-logs-admin.service';
import { ToastService } from '../../core/services/toast.service';

type AdminTenantTab = 'overview' | 'branding' | 'sharepoint' | 'license' | 'audit';

@Component({
  selector: 'app-tenant-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tenant-detail.component.html',
  styleUrl: './tenant-detail.component.scss'
})
export class TenantDetailComponent implements OnInit {
  tenant: AdminTenantDetailDto | null = null;
  loading = true;
  error: string | null = null;
  updatingStatus = false;
  updatingBranding = false;

  activeTab: AdminTenantTab = 'overview';

  logoUrl: string = '';
  primaryColor: string = '';

  spSiteUrl = '';
  spClientId = '';
  spSecret = '';
  spLibrary = 'Shared Documents';
  savingSp = false;
  testingSp = false;
  spTestMsg: string | null = null;
  spTestOk: boolean | null = null;

  licPlan = 'Starter';
  licMaxUsers = 10;
  licMaxStorageGb = 5;
  licExpiry: string | null = null;
  licDocuments = true;
  licNcr = true;
  licRisks = true;
  licAudits = true;
  savingLic = false;

  auditLogs: BusinessAuditLogDto[] = [];
  loadingAudit = false;

  private toastService = inject(ToastService);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private tenantsService: TenantsAdminService,
    private businessLogsService: BusinessLogsAdminService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadTenantDetail(id);
    }
  }

  setTab(tab: AdminTenantTab): void {
    this.activeTab = tab;
    this.spTestMsg = null;
    this.spTestOk = null;
    if (tab === 'audit' && this.tenant) {
      this.loadAuditTrail();
    }
  }

  loadTenantDetail(id: string): void {
    this.loading = true;
    this.error = null;

    this.tenantsService.getTenantDetail(id).subscribe({
      next: (data) => {
        this.tenant = data;
        this.logoUrl = data.logoUrl || '';
        this.primaryColor = data.primaryColor || '';
        this.spSiteUrl = data.sharePointSiteUrl || '';
        this.spClientId = data.sharePointClientId || '';
        this.spSecret = '';
        this.spLibrary = data.sharePointLibraryName || 'Shared Documents';
        this.licPlan = data.plan || 'Starter';
        this.licMaxUsers = data.maxUsers || 10;
        this.licMaxStorageGb = Math.max(0.1, Math.round((data.maxStorageBytes / 1073741824) * 10) / 10) || 5;
        this.licExpiry = data.licenseExpiryDate
          ? data.licenseExpiryDate.substring(0, 10)
          : null;
        const mods = data.modulesEnabled || [];
        const has = (k: string) => mods.some((m) => m.toLowerCase() === k.toLowerCase());
        this.licDocuments = has('Documents');
        this.licNcr = has('NCR');
        this.licRisks = has('Risks');
        this.licAudits = has('Audits');
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load tenant detail';
        this.loading = false;
        this.toastService.error(err.message || 'Failed to load tenant details');
      }
    });
  }

  loadAuditTrail(): void {
    if (!this.tenant) return;
    this.loadingAudit = true;
    this.businessLogsService
      .getBusinessLogs({ tenantId: this.tenant.id, limit: 200 })
      .subscribe({
        next: (logs) => {
          this.auditLogs = logs;
          this.loadingAudit = false;
        },
        error: () => {
          this.loadingAudit = false;
          this.toastService.error('Failed to load audit log');
        }
      });
  }

  updateStatus(status: 'Active' | 'Suspended'): void {
    if (!this.tenant || this.updatingStatus) {
      return;
    }

    this.updatingStatus = true;

    this.tenantsService.updateTenantStatus(this.tenant.id, status).subscribe({
      next: () => {
        this.loadTenantDetail(this.tenant!.id);
        this.updatingStatus = false;
        this.toastService.success('Tenant status updated successfully');
      },
      error: (err) => {
        this.error = `Failed to update tenant status: ${err.message || 'Unknown error'}`;
        this.updatingStatus = false;
        this.toastService.error(err.message || 'Failed to update tenant status');
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/tenants']);
  }

  getLicenseStatus(): { text: string; class: string } {
    if (!this.tenant?.licenseExpiryDate) {
      return { text: 'No expiry', class: 'license-no-expiry' };
    }

    const expiryDate = new Date(this.tenant.licenseExpiryDate);
    const now = new Date();
    const daysUntilExpiry = Math.floor((expiryDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));

    if (daysUntilExpiry < 0) {
      return { text: 'Expired', class: 'license-expired' };
    }
    if (daysUntilExpiry <= 30) {
      return { text: 'Expiring soon', class: 'license-expiring-soon' };
    }
    return { text: 'Active', class: 'license-active' };
  }

  updateBranding(): void {
    if (!this.tenant || this.updatingBranding) {
      return;
    }

    this.updatingBranding = true;

    const request: UpdateTenantBrandingRequest = {
      logoUrl: this.logoUrl || null,
      primaryColor: this.primaryColor || null
    };

    this.tenantsService.updateTenantBranding(this.tenant.id, request).subscribe({
      next: () => {
        this.loadTenantDetail(this.tenant!.id);
        this.updatingBranding = false;
        this.toastService.success('Tenant branding updated successfully');
      },
      error: (err) => {
        this.error = `Failed to update tenant branding: ${err.message || 'Unknown error'}`;
        this.updatingBranding = false;
        this.toastService.error(err.message || 'Failed to update tenant branding');
      }
    });
  }

  testSharePoint(): void {
    if (!this.tenant) return;
    this.testingSp = true;
    this.spTestMsg = null;
    this.spTestOk = null;
    this.tenantsService.testTenantSharePoint(this.tenant.id).subscribe({
      next: (r) => {
        this.testingSp = false;
        this.spTestOk = r.success;
        this.spTestMsg = r.success
          ? `Connected — ${r.message}${r.libraryUrl ? ` (${r.libraryUrl})` : ''}`
          : r.message;
      },
      error: (err) => {
        this.testingSp = false;
        this.spTestOk = false;
        this.spTestMsg = err.message || 'Test failed';
      }
    });
  }

  saveSharePoint(): void {
    if (!this.tenant) return;
    this.savingSp = true;
    this.tenantsService
      .updateTenantSharePoint(this.tenant.id, {
        sharePointSiteUrl: this.spSiteUrl?.trim() || null,
        sharePointClientId: this.spClientId?.trim() || null,
        sharePointClientSecret: this.spSecret?.trim() || undefined,
        sharePointLibraryName: this.spLibrary?.trim() || 'Shared Documents'
      })
      .subscribe({
        next: () => {
          this.savingSp = false;
          this.spSecret = '';
          this.loadTenantDetail(this.tenant!.id);
          this.toastService.success('SharePoint settings saved');
        },
        error: (err) => {
          this.savingSp = false;
          this.toastService.error(err.message || 'Failed to save SharePoint');
        }
      });
  }

  saveLicense(): void {
    if (!this.tenant) return;
    const enabled: string[] = [];
    if (this.licDocuments) enabled.push('Documents');
    if (this.licNcr) enabled.push('NCR');
    if (this.licRisks) enabled.push('Risks');
    if (this.licAudits) enabled.push('Audits');

    const maxBytes = Math.round(this.licMaxStorageGb * 1073741824);
    this.savingLic = true;
    this.tenantsService
      .updateTenantLicense(this.tenant.id, {
        subscriptionPlan: this.licPlan,
        maxUsers: this.licMaxUsers,
        maxStorageBytes: maxBytes,
        subscriptionExpiresAt: this.licExpiry ? new Date(this.licExpiry).toISOString() : null,
        enabledModules: enabled
      })
      .subscribe({
        next: () => {
          this.savingLic = false;
          this.loadTenantDetail(this.tenant!.id);
          this.toastService.success('License updated');
        },
        error: (err) => {
          this.savingLic = false;
          this.toastService.error(err.message || 'Failed to update license');
        }
      });
  }
}
