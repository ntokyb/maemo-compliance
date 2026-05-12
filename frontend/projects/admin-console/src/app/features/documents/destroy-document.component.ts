import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DocumentsAdminService } from '../../core/services/documents-admin.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-destroy-document',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './destroy-document.component.html',
  styleUrl: './destroy-document.component.scss'
})
export class DestroyDocumentComponent implements OnInit {
  private documentsService = inject(DocumentsAdminService);
  private toastService = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  documentId: string | null = null;
  documentTitle: string = '';
  confirmationText: string = '';
  reason: string = '';
  destroying = false;
  error: string | null = null;

  ngOnInit(): void {
    this.documentId = this.route.snapshot.paramMap.get('id');
    this.documentTitle = this.route.snapshot.queryParamMap.get('title') || 'Document';
  }

  destroy(): void {
    if (!this.documentId) {
      this.error = 'Document ID is required';
      return;
    }

    if (!this.reason || this.reason.trim().length < 10) {
      this.error = 'Destruction reason is required and must be at least 10 characters';
      return;
    }

    if (this.confirmationText !== this.documentTitle) {
      this.error = 'Confirmation text must match the document title';
      return;
    }

    this.destroying = true;
    this.error = null;

    this.documentsService.destroyDocument(this.documentId, this.reason).subscribe({
      next: () => {
        this.destroying = false;
        this.toastService.show('Document destroyed successfully', 'success');
        this.router.navigate(['/tenants']);
      },
      error: (err) => {
        this.destroying = false;
        this.error = err.error?.error || err.message || 'Failed to destroy document';
        this.toastService.show(this.error ?? 'Failed to destroy document', 'error');
        console.error('Error destroying document:', err);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/tenants']);
  }
}

