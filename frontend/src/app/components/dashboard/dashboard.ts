import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { DashboardService, DashboardSummary } from '../../services/dashboard.service';
import { TenantModulesService } from '../../services/tenant-modules.service';
import { TenantContextService } from '../../services/tenant-context.service';
import { DocumentsService } from '../../services/documents.service';
import { TenantService, OnboardingStatusDto } from '../../services/tenant.service';
import { Document } from '../../models/document.model';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private dashboardService = inject(DashboardService);
  private tenantModulesService = inject(TenantModulesService);
  private tenantContextService = inject(TenantContextService);
  protected router = inject(Router);
  private http = inject(HttpClient);

  summary: DashboardSummary | null = null;
  error: string | null = null;
  loading = false;
  latestAuditRunId: string | null = null;
  loadingAuditRun = false;
  pendingApprovalDocuments: Document[] = [];
  loadingPendingApproval = false;
  bbbeeCertificatesExpiring: Document[] = [];
  loadingBbbeeCertificates = false;
  popiaPersonalInfoDocuments: Document[] = [];
  loadingPopiaDocuments = false;
  documentsNearRetentionExpiry: Document[] = [];
  loadingRetentionExpiry = false;
  private documentsService = inject(DocumentsService);
  private tenantService = inject(TenantService);

  onboarding: OnboardingStatusDto | null = null;
  onboardingError: string | null = null;
  dismissingOnboarding = false;

  ngOnInit(): void {
    this.loadSummary();
    this.loadOnboarding();
    if (this.hasModule('Audits')) {
      this.loadLatestAuditRun();
    }
    if (this.hasModule('Documents')) {
      this.loadPendingApprovalDocuments();
      this.loadBbbeeCertificatesExpiring();
      this.loadPopiaPersonalInfoDocuments();
      this.loadDocumentsNearRetentionExpiry();
    }
  }

  loadSummary(): void {
    this.loading = true;
    this.error = null;

    this.dashboardService.getSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load dashboard summary';
        this.loading = false;
        console.error('Error loading dashboard summary:', err);
      }
    });
  }

  loadLatestAuditRun(): void {
    this.loadingAuditRun = true;
    const tenantId = this.tenantContextService.getTenantId();
    if (!tenantId) {
      this.loadingAuditRun = false;
      return;
    }

    // Query for latest audit run
    this.http.get<any[]>(`${environment.apiBaseUrl}/api/audits/runs?tenantId=${tenantId}`).subscribe({
      next: (runs) => {
        if (runs && runs.length > 0) {
          // Get the most recent run
          const latestRun = runs.sort((a, b) => 
            new Date(b.startedAt || b.createdAt).getTime() - new Date(a.startedAt || a.createdAt).getTime()
          )[0];
          this.latestAuditRunId = latestRun.id;
        }
        this.loadingAuditRun = false;
      },
      error: () => {
        this.loadingAuditRun = false;
      }
    });
  }

  hasModule(moduleName: string): boolean {
    return this.tenantModulesService.hasModule(moduleName);
  }

  navigateToAuditRun(): void {
    if (this.latestAuditRunId) {
      this.router.navigate(['/consultant/audit-run', this.latestAuditRunId]);
    } else {
      this.router.navigate(['/consultant/audit-run']);
    }
  }

  loadPendingApprovalDocuments(): void {
    this.loadingPendingApproval = true;
    this.documentsService.getPendingApprovalDocuments().subscribe({
      next: (documents) => {
        this.pendingApprovalDocuments = documents;
        this.loadingPendingApproval = false;
      },
      error: (err) => {
        console.error('Error loading pending approval documents:', err);
        this.loadingPendingApproval = false;
      }
    });
  }

  navigateToDocumentApproval(): void {
    this.router.navigate(['/documents'], { queryParams: { workflowState: 'PendingApproval' } });
  }

  loadBbbeeCertificatesExpiring(): void {
    this.loadingBbbeeCertificates = true;
    this.documentsService.getBbbeeCertificatesExpiringSoon(90).subscribe({
      next: (certificates) => {
        this.bbbeeCertificatesExpiring = certificates;
        this.loadingBbbeeCertificates = false;
      },
      error: (err) => {
        console.error('Error loading BBBEE certificates expiring soon:', err);
        this.loadingBbbeeCertificates = false;
      }
    });
  }

  navigateToBbbeeCertificates(): void {
    this.router.navigate(['/documents'], { queryParams: { category: 'BBBEE Certificate' } });
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString();
  }

  getDaysUntilExpiry(expiryDate: string): number {
    if (!expiryDate) return 0;
    const expiry = new Date(expiryDate);
    const today = new Date();
    const diffTime = expiry.getTime() - today.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  loadPopiaPersonalInfoDocuments(): void {
    this.loadingPopiaDocuments = true;
    this.http.get<Document[]>(`${environment.apiBaseUrl}/api/dashboard/popia-personal-info-documents`).subscribe({
      next: (documents) => {
        this.popiaPersonalInfoDocuments = documents;
        this.loadingPopiaDocuments = false;
      },
      error: (err) => {
        console.error('Error loading POPIA personal info documents:', err);
        this.loadingPopiaDocuments = false;
      }
    });
  }

  loadDocumentsNearRetentionExpiry(): void {
    this.loadingRetentionExpiry = true;
    this.http.get<Document[]>(`${environment.apiBaseUrl}/api/dashboard/documents-near-retention-expiry?daysAhead=90`).subscribe({
      next: (documents) => {
        this.documentsNearRetentionExpiry = documents;
        this.loadingRetentionExpiry = false;
      },
      error: (err) => {
        console.error('Error loading documents near retention expiry:', err);
        this.loadingRetentionExpiry = false;
      }
    });
  }

  navigateToPopiaReport(): void {
    this.router.navigate(['/admin/popia-report']);
  }

  loadOnboarding(): void {
    this.onboardingError = null;
    this.tenantService.getOnboardingStatus().subscribe({
      next: (o) => {
        this.onboarding = o;
      },
      error: (err) => {
        this.onboardingError = err.message || null;
      }
    });
  }

  dismissOnboarding(): void {
    this.dismissingOnboarding = true;
    this.tenantService.dismissOnboardingChecklist().subscribe({
      next: () => {
        this.dismissingOnboarding = false;
        this.loadOnboarding();
      },
      error: () => {
        this.dismissingOnboarding = false;
      }
    });
  }

  openOnboardingLink(link: string): void {
    this.router.navigateByUrl(link);
  }

  getPiiTypeLabel(piiType: number): string {
    switch (piiType) {
      case 0: return 'None';
      case 1: return 'Personal Info';
      case 2: return 'Special Personal Info';
      case 3: return 'Children';
      case 4: return 'Financial';
      case 5: return 'Health';
      default: return 'Unknown';
    }
  }
}
