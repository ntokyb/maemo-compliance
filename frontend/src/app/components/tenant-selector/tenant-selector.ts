import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { TenantService } from '../../services/tenant.service';
import { TenantContextService } from '../../services/tenant-context.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-tenant-selector',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tenant-selector.html',
  styleUrl: './tenant-selector.scss'
})
export class TenantSelectorComponent implements OnInit {
  private router = inject(Router);
  private tenantService = inject(TenantService);
  private tenantContextService = inject(TenantContextService);
  private http = inject(HttpClient);

  loading = false;
  error: string | null = null;
  tenants: any[] = [];
  selectedTenantId: string | null = null;
  demoLoading = false;
  demoError: string | null = null;

  ngOnInit(): void {
    this.loadTenants();
  }

  loadTenants(): void {
    this.loading = true;
    this.error = null;

    // Check if tenant is already stored
    const storedTenantId = this.tenantContextService.getTenantId();
    if (storedTenantId) {
      this.selectedTenantId = storedTenantId;
      this.loading = false;
      return;
    }

    // Try to get current tenant from API
    this.tenantService.getCurrentTenant().subscribe({
      next: (response) => {
        // If user already has a tenant, use it
        if (response.tenantId) {
          this.selectedTenantId = response.tenantId;
          this.loading = false;
        } else {
          // Try to load all tenants for selection
          this.loadAllTenants();
        }
      },
      error: (err) => {
        // If current tenant endpoint fails, try to get all tenants
        console.error('Error getting current tenant:', err);
        this.loadAllTenants();
      }
    });
  }

  private loadAllTenants(): void {
    this.tenantService.getAllTenants().subscribe({
      next: (tenants) => {
        this.tenants = tenants;
        this.loading = false;
        if (tenants.length === 0) {
          this.error = 'No tenants available. Please contact your administrator.';
        }
      },
      error: (err) => {
        console.error('Error loading tenants:', err);
        this.loading = false;
        this.error = 'Unable to load tenants. You can enter your tenant ID manually below.';
      }
    });
  }

  selectTenant(): void {
    if (!this.selectedTenantId) {
      return;
    }

    // Validate GUID format
    const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    if (!guidPattern.test(this.selectedTenantId)) {
      this.error = 'Please enter a valid tenant ID (GUID format)';
      return;
    }

    this.tenantContextService.setTenantId(this.selectedTenantId);
    // Redirect to dashboard after selection
    this.router.navigate(['/dashboard']);
  }

  onTenantChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedTenantId = select.value;
  }

  onTenantIdInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedTenantId = input.value;
  }

  switchToDemo(): void {
    this.demoLoading = true;
    this.demoError = null;

    this.http.get<{ tenantId: string; tenantName: string }>(`${environment.apiBaseUrl}/api/demo/tenant`).subscribe({
      next: (response) => {
        this.tenantContextService.setTenantId(response.tenantId);
        this.demoLoading = false;
        // Redirect to dashboard after selecting demo tenant
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.demoLoading = false;
        this.demoError = 'Demo tenant not available. Please ensure demo data has been seeded.';
        console.error('Error loading demo tenant:', err);
      }
    });
  }
}

