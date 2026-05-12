import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TenantService, Tenant, TenantDirectoryRowDto } from '../../../services/tenant.service';

type SettingsTab = 'general' | 'sharepoint' | 'subscription' | 'users';

@Component({
  selector: 'app-tenant-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './tenant-settings.html',
  styleUrl: './tenant-settings.scss'
})
export class TenantSettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private tenantService = inject(TenantService);

  activeTab: SettingsTab = 'general';

  generalForm!: FormGroup;
  sharePointForm!: FormGroup;

  loading = false;
  savingGeneral = false;
  savingSharePoint = false;
  testingSharePoint = false;
  error: string | null = null;
  sharePointTestMessage: string | null = null;
  sharePointTestOk: boolean | null = null;

  tenantId: string | null = null;
  tenant: Tenant | null = null;

  directory: TenantDirectoryRowDto[] = [];
  loadingUsers = false;
  inviteEmail = '';
  inviteRole: 'TenantAdmin' | 'User' = 'User';
  inviting = false;
  inviteError: string | null = null;

  readonly moduleLabels: { key: string; label: string }[] = [
    { key: 'Documents', label: 'Document Control' },
    { key: 'NCR', label: 'Non-Conformance Management' },
    { key: 'Risks', label: 'Risk Management' },
    { key: 'Audits', label: 'Audit Management' }
  ];

  ngOnInit(): void {
    this.generalForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      logoUrl: ['', Validators.maxLength(2000)],
      primaryColor: ['', Validators.maxLength(32)]
    });

    this.sharePointForm = this.fb.group({
      sharePointSiteUrl: ['', Validators.maxLength(2000)],
      sharePointClientId: ['', Validators.maxLength(500)],
      sharePointClientSecret: [''],
      sharePointLibraryName: ['Shared Documents', Validators.maxLength(500)]
    });

    this.route.queryParamMap.subscribe((q) => {
      const t = q.get('tab');
      if (t === 'users' || t === 'sharepoint' || t === 'subscription' || t === 'general') {
        this.setTab(t as SettingsTab);
      }
    });

    this.loadTenant();
  }

  setTab(tab: SettingsTab): void {
    this.activeTab = tab;
    this.error = null;
    this.inviteError = null;
    if (tab !== 'sharepoint') {
      this.sharePointTestMessage = null;
      this.sharePointTestOk = null;
    }
    if (tab === 'users') {
      this.loadDirectory();
    }
  }

  loadTenant(): void {
    this.loading = true;
    this.error = null;

    this.tenantService.getCurrentTenant().subscribe({
      next: (response) => {
        this.tenantId = response.tenantId;
        if (!this.tenantId) {
          this.error = 'No tenant ID found';
          this.loading = false;
          return;
        }
        this.tenantService.getTenant(this.tenantId).subscribe({
          next: (tenant) => {
            this.tenant = tenant;
            this.generalForm.patchValue({
              name: tenant.name,
              logoUrl: tenant.logoUrl || '',
              primaryColor: tenant.primaryColor || ''
            });
            this.sharePointForm.patchValue({
              sharePointSiteUrl: tenant.sharePointSiteUrl || '',
              sharePointClientId: tenant.sharePointClientId || '',
              sharePointClientSecret: '',
              sharePointLibraryName: tenant.sharePointLibraryName || 'Shared Documents'
            });
            this.loading = false;
          },
          error: (err) => {
            this.error = err.message || 'Failed to load tenant information';
            this.loading = false;
          }
        });
      },
      error: (err) => {
        this.error = err.message || 'Failed to get current tenant';
        this.loading = false;
      }
    });
  }

  saveGeneral(): void {
    if (this.generalForm.invalid || !this.tenantId) {
      this.markTouched(this.generalForm);
      return;
    }
    this.savingGeneral = true;
    this.error = null;
    const v = this.generalForm.value;
    this.tenantService
      .updatePortalGeneralSettings({
        name: v.name,
        logoUrl: v.logoUrl?.trim() ? v.logoUrl.trim() : null,
        primaryColor: v.primaryColor?.trim() ? v.primaryColor.trim() : null
      })
      .subscribe({
        next: () => {
          this.savingGeneral = false;
          this.loadTenant();
        },
        error: (err) => {
          this.error = err.error?.message || err.message || 'Failed to save';
          this.savingGeneral = false;
        }
      });
  }

  testSharePoint(): void {
    if (!this.tenantId) return;
    this.testingSharePoint = true;
    this.sharePointTestMessage = null;
    this.sharePointTestOk = null;
    this.error = null;
    this.tenantService.testSharePointConnection().subscribe({
      next: (r) => {
        this.testingSharePoint = false;
        this.sharePointTestOk = r.success;
        this.sharePointTestMessage = r.success
          ? `Connected — Library: ${r.libraryUrl || '(see message)'} — ${r.message}`
          : r.message;
      },
      error: (err) => {
        this.testingSharePoint = false;
        this.sharePointTestOk = false;
        const msg = err.error?.message || err.message || 'Connection test failed';
        this.sharePointTestMessage = msg;
        if (err.status === 502) {
          this.sharePointTestMessage = err.error?.message || 'SharePoint / Microsoft Graph error';
        }
      }
    });
  }

  saveSharePoint(): void {
    if (!this.tenantId) return;
    this.savingSharePoint = true;
    this.error = null;
    const v = this.sharePointForm.value;
    this.tenantService
      .updateSharePointSettings({
        sharePointSiteUrl: v.sharePointSiteUrl?.trim() || null,
        sharePointClientId: v.sharePointClientId?.trim() || null,
        sharePointClientSecret: v.sharePointClientSecret?.trim() || undefined,
        sharePointLibraryName: v.sharePointLibraryName?.trim() || 'Shared Documents'
      })
      .subscribe({
        next: () => {
          this.savingSharePoint = false;
          this.sharePointForm.patchValue({ sharePointClientSecret: '' });
          this.loadTenant();
        },
        error: (err) => {
          this.error = err.error?.message || err.message || 'Failed to save SharePoint settings';
          this.savingSharePoint = false;
        }
      });
  }

  moduleEnabled(key: string): boolean {
    const m = this.tenant?.modulesEnabled || [];
    return m.some((x) => x.toLowerCase() === key.toLowerCase());
  }

  maxStorageGb(): string {
    const b = this.tenant?.maxStorageBytes;
    if (b == null) return '—';
    return (b / 1073741824).toFixed(1);
  }

  cancel(): void {
    this.router.navigate(['/dashboard']);
  }

  loadDirectory(): void {
    this.loadingUsers = true;
    this.tenantService.getWorkspaceDirectory().subscribe({
      next: (rows) => {
        this.directory = rows;
        this.loadingUsers = false;
      },
      error: () => {
        this.loadingUsers = false;
        this.inviteError = 'Could not load users.';
      }
    });
  }

  sendInvite(): void {
    const email = this.inviteEmail.trim();
    if (!email) {
      this.inviteError = 'Email is required';
      return;
    }
    this.inviting = true;
    this.inviteError = null;
    this.tenantService.inviteWorkspaceUser(email, this.inviteRole).subscribe({
      next: () => {
        this.inviting = false;
        this.inviteEmail = '';
        this.loadDirectory();
      },
      error: (err) => {
        this.inviting = false;
        this.inviteError = err.error?.message || err.message || 'Invite failed';
      }
    });
  }

  private markTouched(form: FormGroup): void {
    Object.keys(form.controls).forEach((key) => form.get(key)?.markAsTouched());
  }
}
