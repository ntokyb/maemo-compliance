import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { DocumentsService } from '../../../services/documents.service';
import { Document, DocumentWorkflowState } from '../../../models/document.model';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-approval-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './approval-dashboard.html',
  styleUrl: './approval-dashboard.scss'
})
export class ApprovalDashboardComponent implements OnInit {
  private documentsService = inject(DocumentsService);
  private toastService = inject(ToastService);
  protected router = inject(Router);

  pendingDocuments: Document[] = [];
  loading = false;
  error: string | null = null;
  DocumentWorkflowState = DocumentWorkflowState;

  ngOnInit(): void {
    this.loadPendingApprovalDocuments();
  }

  loadPendingApprovalDocuments(): void {
    this.loading = true;
    this.error = null;

    this.documentsService.getPendingApprovalDocuments().subscribe({
      next: (documents) => {
        this.pendingDocuments = documents;
        this.loading = false;
      },
      error: (err) => {
        const message = err.error?.error || err.message || 'Failed to load pending approval documents';
        this.error = message;
        this.loading = false;
        this.toastService.show(message, 'error');
      }
    });
  }

  navigateToDocument(id: string): void {
    this.router.navigate(['/documents', id]);
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }
}

