import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminTenantListItemDto } from '../../core/models/admin-tenant.dto';
import { TenantsAdminService } from '../../core/services/tenants-admin.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-tenants-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './tenants-list.component.html',
  styleUrl: './tenants-list.component.scss'
})
export class TenantsListComponent implements OnInit {
  tenants: AdminTenantListItemDto[] = [];
  loading = true;
  error: string | null = null;
  private toastService = inject(ToastService);

  constructor(private tenantsService: TenantsAdminService) {}

  ngOnInit(): void {
    this.loadTenants();
  }

  loadTenants(): void {
    this.loading = true;
    this.error = null;

    this.tenantsService.getTenants().subscribe({
      next: (data) => {
        this.tenants = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load tenants';
        this.loading = false;
        this.toastService.error('Failed to load tenants');
        console.error('Tenants error:', err);
      }
    });
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'status-active' : 'status-suspended';
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Active' : 'Suspended';
  }
}

