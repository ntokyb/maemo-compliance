import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TenantService } from '../../../services/tenant.service';

@Component({
  selector: 'app-billing-info',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './billing-info.html',
  styleUrl: './billing-info.scss'
})
export class BillingInfoComponent implements OnInit {
  private router = inject(Router);
  private tenantService = inject(TenantService);

  loading = false;
  error: string | null = null;
  tenant: any = null;
  canceling = false;

  ngOnInit(): void {
    this.loadTenant();
  }

  loadTenant(): void {
    this.loading = true;
    this.error = null;

    this.tenantService.getCurrentTenant().subscribe({
      next: (response) => {
        const tenantId = response.tenantId;
        if (tenantId) {
          this.tenantService.getTenant(tenantId).subscribe({
            next: (tenant) => {
              this.tenant = tenant;
              this.loading = false;
            },
            error: (err) => {
              this.error = err.message || 'Failed to load billing information';
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

  cancelSubscription(): void {
    if (!confirm('Are you sure you want to cancel your subscription? This action cannot be undone.')) {
      return;
    }

    this.canceling = true;
    this.error = null;

    // TODO: Implement actual cancellation when billing provider is fully implemented
    // For now, this is a stub
    setTimeout(() => {
      alert('Subscription cancellation is not yet implemented. This is a stub.');
      this.canceling = false;
    }, 1000);
  }

  cancel(): void {
    this.router.navigate(['/dashboard']);
  }
}

