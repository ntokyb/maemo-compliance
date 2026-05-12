import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PopiaService } from '../../../services/popia.service';
import { PopiaTrailReportItem, PiiDataType, PiiType, Document } from '../../../models/document.model';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-popia-report',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './popia-report.html',
  styleUrl: './popia-report.scss'
})
export class PopiaReportComponent implements OnInit {
  private popiaService = inject(PopiaService);
  private http = inject(HttpClient);

  reportItems: PopiaTrailReportItem[] = [];
  filteredItems: PopiaTrailReportItem[] = [];
  loading = false;
  error: string | null = null;
  days = 30;
  selectedPiiDataType: PiiDataType | null = null;
  PiiDataType = PiiDataType;
  PiiType = PiiType;

  // Personal Info Inventory
  personalInfoDocuments: Document[] = [];
  loadingPersonalInfo = false;

  // Retention Expiry List
  retentionExpiryDocuments: Document[] = [];
  loadingRetentionExpiry = false;
  retentionDaysAhead = 90;

  ngOnInit(): void {
    this.loadReport();
    this.loadPersonalInfoInventory();
    this.loadRetentionExpiryList();
  }

  loadReport(): void {
    this.loading = true;
    this.error = null;

    this.popiaService.getTrailReport(this.days).subscribe({
      next: (items) => {
        this.reportItems = items;
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load POPIA trail report';
        this.loading = false;
        console.error('Error loading POPIA report:', err);
      }
    });
  }

  getPiiDataTypeLabel(piiDataType: PiiDataType): string {
    switch (piiDataType) {
      case PiiDataType.None:
        return 'None';
      case PiiDataType.Personal:
        return 'Personal';
      case PiiDataType.SpecialPersonal:
        return 'Special Personal';
      default:
        return 'Unknown';
    }
  }

  getPiiDataTypeClass(piiDataType: PiiDataType): string {
    switch (piiDataType) {
      case PiiDataType.None:
        return 'pii-none';
      case PiiDataType.Personal:
        return 'pii-personal';
      case PiiDataType.SpecialPersonal:
        return 'pii-special';
      default:
        return '';
    }
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleString();
  }

  onDaysChange(): void {
    this.loadReport();
  }

  getUniqueDocumentCount(): number {
    const uniqueDocumentIds = new Set(this.filteredItems.map(item => item.documentId));
    return uniqueDocumentIds.size;
  }

  applyFilters(): void {
    this.filteredItems = this.reportItems.filter(item => {
      if (this.selectedPiiDataType !== null && item.piiDataType !== this.selectedPiiDataType) {
        return false;
      }
      return true;
    });
  }

  onPiiDataTypeFilterChange(): void {
    this.applyFilters();
  }

  exportToCsv(): void {
    if (this.filteredItems.length === 0) {
      alert('No data to export');
      return;
    }

    const headers = ['Document Title', 'PII Type', 'Department', 'Accessed By', 'Accessed At'];
    const rows = this.filteredItems.map(item => [
      this.escapeCsvField(item.documentTitle),
      this.getPiiDataTypeLabel(item.piiDataType),
      item.department || '',
      item.accessedBy,
      this.formatDate(item.accessedAt)
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `popia-trail-report-${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  private escapeCsvField(field: string): string {
    if (field.includes(',') || field.includes('"') || field.includes('\n')) {
      return `"${field.replace(/"/g, '""')}"`;
    }
    return field;
  }

  loadPersonalInfoInventory(): void {
    this.loadingPersonalInfo = true;
    this.http.get<Document[]>(`${environment.apiBaseUrl}/api/dashboard/popia-personal-info-documents`).subscribe({
      next: (documents) => {
        this.personalInfoDocuments = documents;
        this.loadingPersonalInfo = false;
      },
      error: (err) => {
        console.error('Error loading personal info inventory:', err);
        this.loadingPersonalInfo = false;
      }
    });
  }

  loadRetentionExpiryList(): void {
    this.loadingRetentionExpiry = true;
    this.http.get<Document[]>(`${environment.apiBaseUrl}/api/dashboard/documents-near-retention-expiry?daysAhead=${this.retentionDaysAhead}`).subscribe({
      next: (documents) => {
        this.retentionExpiryDocuments = documents;
        this.loadingRetentionExpiry = false;
      },
      error: (err) => {
        console.error('Error loading retention expiry list:', err);
        this.loadingRetentionExpiry = false;
      }
    });
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

  getDaysUntilExpiry(expiryDate: string): number {
    if (!expiryDate) return 0;
    const expiry = new Date(expiryDate);
    const today = new Date();
    const diffTime = expiry.getTime() - today.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  onRetentionDaysChange(): void {
    this.loadRetentionExpiryList();
  }
}

