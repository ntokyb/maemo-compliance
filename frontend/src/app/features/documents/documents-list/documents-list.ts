import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { DocumentsService } from '../../../services/documents.service';
import { Document, DocumentStatus, DocumentWorkflowState } from '../../../models/document.model';
import { DocumentTemplatePickerComponent } from '../document-template-picker/document-template-picker.component';
import { DocumentTemplateMeta } from '../../../services/template-library.service';

@Component({
  selector: 'app-documents-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, DocumentTemplatePickerComponent],
  templateUrl: './documents-list.html',
  styleUrl: './documents-list.scss'
})
export class DocumentsListComponent implements OnInit {
  private documentsService = inject(DocumentsService);
  private router = inject(Router);

  documents: Document[] = [];
  loading = false;
  error: string | null = null;
  selectedCategory: string | null = null;
  showTemplatePicker = false;
  categoryOptions: { value: string | null; label: string }[] = [
    { value: null, label: 'All Categories' },
    { value: 'BBBEE Certificate', label: 'BBBEE Certificate' }
  ];

  DocumentStatus = DocumentStatus;
  DocumentWorkflowState = DocumentWorkflowState;

  ngOnInit(): void {
    // Check for category filter in query params
    const categoryParam = new URLSearchParams(window.location.search).get('category');
    if (categoryParam) {
      this.selectedCategory = categoryParam;
    }
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.loading = true;
    this.error = null;

    const params: { category?: string } = {};
    if (this.selectedCategory) {
      params.category = this.selectedCategory;
    }

    this.documentsService.getDocuments(params).subscribe({
      next: (documents) => {
        this.documents = documents;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load documents';
        this.loading = false;
        console.error('Error loading documents:', err);
      }
    });
  }

  getStatusLabel(status: DocumentStatus): string {
    switch (status) {
      case DocumentStatus.Draft:
        return 'Draft';
      case DocumentStatus.Active:
        return 'Active';
      case DocumentStatus.Archived:
        return 'Archived';
      default:
        return 'Unknown';
    }
  }

  getWorkflowStateLabel(state: DocumentWorkflowState): string {
    switch (state) {
      case DocumentWorkflowState.Draft:
        return 'Draft';
      case DocumentWorkflowState.PendingApproval:
        return 'Pending Approval';
      case DocumentWorkflowState.Approved:
        return 'Approved';
      case DocumentWorkflowState.Active:
        return 'Active';
      case DocumentWorkflowState.Archived:
        return 'Archived';
      default:
        return 'Unknown';
    }
  }

  getWorkflowStateClass(state: DocumentWorkflowState): string {
    switch (state) {
      case DocumentWorkflowState.Draft:
        return 'workflow-draft';
      case DocumentWorkflowState.PendingApproval:
        return 'workflow-pending';
      case DocumentWorkflowState.Approved:
        return 'workflow-approved';
      case DocumentWorkflowState.Active:
        return 'workflow-active';
      case DocumentWorkflowState.Archived:
        return 'workflow-archived';
      default:
        return '';
    }
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString();
  }

  getRetentionWarningClass(retainUntil: string): string {
    if (!retainUntil) return '';
    const retainDate = new Date(retainUntil);
    if (Number.isNaN(retainDate.getTime())) {
      return '';
    }
    const today = new Date();
    const daysUntil = Math.floor((retainDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    
    if (daysUntil < 0) {
      return 'retention-expired'; // Expired
    } else if (daysUntil <= 7) {
      return 'retention-warning-critical'; // 7 days or less
    } else if (daysUntil <= 30) {
      return 'retention-warning-high'; // 30 days or less
    } else if (daysUntil <= 90) {
      return 'retention-warning-medium'; // 90 days or less
    }
    return ''; // More than 90 days
  }

  isPastRetention(retainUntil: string | undefined | null): boolean {
    if (!retainUntil) return false;
    const retainDate = new Date(retainUntil);
    if (Number.isNaN(retainDate.getTime())) {
      return false;
    }
    return retainDate.getTime() < new Date().getTime();
  }

  editDocument(id: string): void {
    this.router.navigate(['/documents', id]);
  }

  createNew(): void {
    this.router.navigate(['/documents/new']);
  }

  createFromTemplate(): void {
    this.showTemplatePicker = true;
  }

  onTemplateSelected(template: DocumentTemplateMeta): void {
    this.showTemplatePicker = false;
    this.router.navigate(['/documents/new'], { 
      queryParams: { templateId: template.id } 
    });
  }

  onTemplatePickerCancelled(): void {
    this.showTemplatePicker = false;
  }

  onCategoryFilterChange(): void {
    this.loadDocuments();
  }
}

