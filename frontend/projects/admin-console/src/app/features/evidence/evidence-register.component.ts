import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EvidenceAdminService } from '../../core/services/evidence-admin.service';
import { EvidenceItemDto } from '../../core/models/evidence-item.dto';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-evidence-register',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './evidence-register.component.html',
  styleUrl: './evidence-register.component.scss'
})
export class EvidenceRegisterComponent implements OnInit {
  private evidenceService = inject(EvidenceAdminService);
  private toastService = inject(ToastService);

  evidenceItems: EvidenceItemDto[] = [];
  loading = false;
  error: string | null = null;

  // Filters
  selectedTenantId: string | null = null;
  selectedEntityType: string | null = null;
  fromDate: string | null = null;
  toDate: string | null = null;
  limit: number = 100;

  entityTypes = [
    { value: null, label: 'All Types' },
    { value: 'Document', label: 'Document' },
    { value: 'DocumentVersion', label: 'Document Version' },
    { value: 'AuditAnswer', label: 'Audit Evidence' }
  ];

  ngOnInit(): void {
    // Set default date range to last 30 days
    const toDate = new Date();
    const fromDate = new Date();
    fromDate.setDate(fromDate.getDate() - 30);
    
    this.toDate = toDate.toISOString().split('T')[0];
    this.fromDate = fromDate.toISOString().split('T')[0];
    
    this.loadEvidence();
  }

  loadEvidence(): void {
    this.loading = true;
    this.error = null;

    const params: any = {
      limit: this.limit
    };

    if (this.selectedTenantId) {
      params.tenantId = this.selectedTenantId;
    }
    if (this.selectedEntityType) {
      params.entityType = this.selectedEntityType;
    }
    if (this.fromDate) {
      params.fromDate = this.fromDate;
    }
    if (this.toDate) {
      params.toDate = this.toDate;
    }

    this.evidenceService.getEvidenceRegister(params).subscribe({
      next: (items) => {
        this.evidenceItems = items;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.error || err.message || 'Failed to load evidence register';
        this.loading = false;
        this.toastService.show(this.error ?? 'Failed to load evidence register', 'error');
        console.error('Error loading evidence register:', err);
      }
    });
  }

  formatDate(dateString: string): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleString();
  }

  copyHash(hash: string | undefined): void {
    if (!hash) return;
    navigator.clipboard.writeText(hash).then(() => {
      this.toastService.show('Hash copied to clipboard', 'success');
    });
  }

  getEntityTypeLabel(entityType: string): string {
    return entityType === 'DocumentVersion' ? 'Doc Version' : entityType;
  }
}

