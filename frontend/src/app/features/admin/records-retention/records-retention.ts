import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DocumentsService } from '../../../services/documents.service';
import { Document } from '../../../models/document.model';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-records-retention',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './records-retention.html',
  styleUrl: './records-retention.scss'
})
export class RecordsRetentionComponent implements OnInit {
  private documentsService = inject(DocumentsService);
  private toastService = inject(ToastService);

  documents: Document[] = [];
  loading = false;
  error: string | null = null;
  archivingDocumentId: string | null = null;

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.loading = true;
    this.error = null;

    this.documentsService.getDocumentsPastRetention().subscribe({
      next: (documents) => {
        this.documents = documents;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load documents past retention';
        this.loading = false;
        console.error('Error loading documents past retention:', err);
      }
    });
  }

  archiveDocument(document: Document): void {
    if (!document.id) {
      return;
    }

    if (!confirm(`Are you sure you want to archive "${document.title}"?`)) {
      return;
    }

    this.archivingDocumentId = document.id;
    this.documentsService.archiveDocument(document.id).subscribe({
      next: () => {
        this.toastService.show('Document archived successfully', 'success');
        this.loadDocuments(); // Reload list
        this.archivingDocumentId = null;
      },
      error: (err) => {
        this.toastService.show(err.message || 'Failed to archive document', 'error');
        this.archivingDocumentId = null;
        console.error('Error archiving document:', err);
      }
    });
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString();
  }

  getDaysPastRetention(retainUntil: string | undefined): number {
    if (!retainUntil) return 0;
    const expiry = new Date(retainUntil);
    const today = new Date();
    const diffTime = today.getTime() - expiry.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  exportToCsv(): void {
    if (this.documents.length === 0) {
      alert('No data to export');
      return;
    }

    const headers = ['Title', 'Category', 'Department', 'Retention Date', 'Days Past Retention', 'Status', 'PII Type'];
    const rows = this.documents.map(doc => [
      this.escapeCsvField(doc.title),
      doc.category || '',
      doc.department || '',
      doc.retainUntil ? this.formatDate(doc.retainUntil) : '',
      this.getDaysPastRetention(doc.retainUntil).toString(),
      doc.status?.toString() || '',
      doc.piiType?.toString() || 'None'
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `records-retention-${new Date().toISOString().split('T')[0]}.csv`);
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

