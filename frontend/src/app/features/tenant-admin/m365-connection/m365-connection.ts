import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TenantService, ConnectMicrosoft365Request } from '../../../services/tenant.service';

@Component({
  selector: 'app-m365-connection',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './m365-connection.html',
  styleUrl: './m365-connection.scss'
})
export class M365ConnectionComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private tenantService = inject(TenantService);

  m365Form!: FormGroup;
  loading = false;
  connecting = false;
  error: string | null = null;
  success: string | null = null;
  tenantId: string | null = null;

  ngOnInit(): void {
    this.m365Form = this.fb.group({
      clientId: ['', [Validators.required]],
      clientSecret: ['', [Validators.required]],
      tenantId: ['', [Validators.required]],
      sharePointSiteId: [''],
      sharePointDriveId: ['']
    });

    this.loadTenant();
  }

  loadTenant(): void {
    this.loading = true;
    this.error = null;

    this.tenantService.getCurrentTenant().subscribe({
      next: (response) => {
        this.tenantId = response.tenantId;
        if (this.tenantId) {
          this.tenantService.getTenant(this.tenantId).subscribe({
            next: (tenant) => {
              // Pre-fill form if already connected
              if (tenant.azureAdClientId) {
                this.m365Form.patchValue({
                  clientId: tenant.azureAdClientId,
                  tenantId: tenant.azureAdTenantId || '',
                  sharePointSiteId: tenant.sharePointSiteId || '',
                  sharePointDriveId: tenant.sharePointDriveId || ''
                });
              }
              this.loading = false;
            },
            error: (err) => {
              this.error = err.message || 'Failed to load tenant information';
              this.loading = false;
              console.error('Error loading tenant:', err);
            }
          });
        } else {
          this.error = 'No tenant ID found';
          this.loading = false;
        }
      },
      error: (err) => {
        this.error = err.message || 'Failed to get current tenant';
        this.loading = false;
        console.error('Error getting current tenant:', err);
      }
    });
  }

  onSubmit(): void {
    if (this.m365Form.invalid || !this.tenantId) {
      this.markFormGroupTouched(this.m365Form);
      return;
    }

    this.connecting = true;
    this.error = null;
    this.success = null;

    const formValue = this.m365Form.value;
    const request: ConnectMicrosoft365Request = {
      clientId: formValue.clientId,
      clientSecret: formValue.clientSecret,
      tenantId: formValue.tenantId,
      sharePointSiteId: formValue.sharePointSiteId || undefined,
      sharePointDriveId: formValue.sharePointDriveId || undefined
    };

    this.tenantService.connectMicrosoft365(this.tenantId, request).subscribe({
      next: () => {
        this.connecting = false;
        this.success = 'Microsoft 365 connection successful!';
        // Reload tenant data to show updated connection status
        setTimeout(() => this.loadTenant(), 1000);
      },
      error: (err) => {
        this.error = err.message || 'Failed to connect Microsoft 365';
        this.connecting = false;
        console.error('Error connecting Microsoft 365:', err);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/dashboard']);
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }
}

