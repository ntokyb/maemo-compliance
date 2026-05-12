import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { DocumentsService } from '../../../services/documents.service';
import { DocumentStatus, DocumentWorkflowState, PiiDataType, PersonalInformationType, PiiType, CreateDocumentRequest, UpdateDocumentRequest, Document, DocumentVersionDto } from '../../../models/document.model';
import { ToastService } from '../../../services/toast.service';
import { TemplateLibraryService } from '../../../services/template-library.service';
import { TenantContextService } from '../../../services/tenant-context.service';

@Component({
  selector: 'app-document-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './document-form.html',
  styleUrl: './document-form.scss'
})
export class DocumentFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  protected router = inject(Router);
  private route = inject(ActivatedRoute);
  private documentsService = inject(DocumentsService);
  private toastService = inject(ToastService);
  private templateLibraryService = inject(TemplateLibraryService);
  private tenantContextService = inject(TenantContextService);

  documentForm!: FormGroup;
  versionForm!: FormGroup;
  rejectForm!: FormGroup;
  approveForm!: FormGroup;
  documentId: string | null = null;
  document: Document | null = null;
  versions: DocumentVersionDto[] = [];
  loading = false;
  saving = false;
  uploading = false;
  uploadingVersion = false;
  loadingVersions = false;
  processingApproval = false;
  error: string | null = null;
  isEditMode = false;
  selectedFile: File | null = null;
  selectedVersionFile: File | null = null;
  activeTab: 'details' | 'versions' = 'details';
  showRejectDialog = false;
  showApproveDialog = false;
  templateContent: string = '';
  downloadingEvidence = false;
  isLoadingTemplate = false;

  DocumentStatus = DocumentStatus;
  DocumentWorkflowState = DocumentWorkflowState;
  PiiDataType = PiiDataType;
  PersonalInformationType = PersonalInformationType;
  PiiType = PiiType;

  ngOnInit(): void {
    this.documentId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.documentId;

    // Set default review date to 1 year from now
    const defaultReviewDate = new Date();
    defaultReviewDate.setFullYear(defaultReviewDate.getFullYear() + 1);
    const defaultReviewDateStr = defaultReviewDate.toISOString().split('T')[0];

    this.documentForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      category: ['', [Validators.maxLength(100)]],
      department: ['', [Validators.maxLength(100)]],
      ownerUserId: [''],
      reviewDate: [defaultReviewDateStr, Validators.required],
      status: [DocumentStatus.Draft],
      piiDataType: [PiiDataType.None],
      personalInformationType: [PersonalInformationType.None],
      piiType: [PiiType.None],
      piiDescription: [''],
      piiRetentionPeriodInMonths: [null],
            bbbeeLevel: [null],
            bbbeeExpiryDate: [null],
            filePlanSeries: [''],
            filePlanSubSeries: [''],
            filePlanItem: ['']
        });

    this.versionForm = this.fb.group({
      comment: ['']
    });

    this.rejectForm = this.fb.group({
      rejectedReason: ['', [Validators.required, Validators.maxLength(1000)]]
    });

    this.approveForm = this.fb.group({
      comments: ['']
    });

    // Load template if templateId is provided in query params
    this.route.queryParams.subscribe(params => {
      const templateId = params['templateId'];
      if (templateId && !this.isEditMode) {
        this.loadTemplate(templateId);
      }
    });

    if (this.isEditMode && this.documentId) {
      this.loadDocument(this.documentId);
      this.loadVersions(this.documentId);
    }
  }

  loadTemplate(templateId: string): void {
    this.isLoadingTemplate = true;
    this.templateLibraryService.getTemplateById(templateId).subscribe({
      next: (template) => {
        if (template) {
          this.templateLibraryService.loadMarkdown(template.assetPath).subscribe({
            next: (markdown) => {
              if (markdown) {
                // Replace placeholders
                const processedContent = this.replacePlaceholders(markdown, template);
                
                // Pre-populate form
                this.documentForm.patchValue({
                  title: template.title,
                  category: this.deriveCategoryFromTemplate(template)
                });

                // Store template content for display/editing
                this.templateContent = processedContent;
                this.isLoadingTemplate = false;
              } else {
                this.toastService.show('Template content could not be loaded', 'error');
                this.isLoadingTemplate = false;
              }
            },
            error: (err) => {
              console.error('Error loading template markdown:', err);
              this.toastService.show('Failed to load template content', 'error');
              this.isLoadingTemplate = false;
            }
          });
        } else {
          this.toastService.show('Template not found', 'error');
          this.isLoadingTemplate = false;
        }
      },
      error: (err) => {
        console.error('Error loading template:', err);
        this.toastService.show('Failed to load template', 'error');
        this.isLoadingTemplate = false;
      }
    });
  }

  replacePlaceholders(content: string, template: any): string {
    let processed = content;

    // Replace {{CompanyName}}
    const companyName = this.tenantContextService.getDemoTenantName() || '{{CompanyName}}';
    processed = processed.replace(/\{\{CompanyName\}\}/g, companyName);

    // Replace {{ISOStandard}}
    processed = processed.replace(/\{\{ISOStandard\}\}/g, template.standard || 'ISO 9001:2015');

    // Replace {{CurrentDate}}
    const currentDate = new Date().toISOString().split('T')[0]; // YYYY-MM-DD
    processed = processed.replace(/\{\{CurrentDate\}\}/g, currentDate);

    // Replace {{OwnerRole}} (if available, otherwise leave placeholder)
    // For now, leave as-is since we don't have user context easily available

    // Replace {{DepartmentName}} (if available, otherwise leave placeholder)
    // For now, leave as-is

    return processed;
  }

  deriveCategoryFromTemplate(template: any): string {
    // Derive category from template standard
    if (template.standardCode === 'ISO 9001') {
      return 'ISO 9001';
    } else if (template.standardCode === 'POPIA') {
      return 'POPIA';
    } else if (template.standardCode === 'NARSA') {
      return 'NARSA';
    } else if (template.standardCode === 'AGSA') {
      return 'AGSA';
    }
    return template.standard || '';
  }

  loadDocument(id: string): void {
    this.loading = true;
    this.error = null;

    this.documentsService.getDocument(id).subscribe({
      next: (document) => {
        this.document = document;
        // Format date for input (YYYY-MM-DD)
        const reviewDate = document.reviewDate ? new Date(document.reviewDate).toISOString().split('T')[0] : '';

        const bbbeeExpiryDate = document.bbbeeExpiryDate ? new Date(document.bbbeeExpiryDate).toISOString().split('T')[0] : null;
        
        this.documentForm.patchValue({
          title: document.title,
          category: document.category || '',
          department: document.department || '',
          ownerUserId: document.ownerUserId || '',
          reviewDate: reviewDate,
          status: document.status,
          piiDataType: document.piiDataType ?? PiiDataType.None,
          personalInformationType: document.personalInformationType ?? PersonalInformationType.None,
          piiType: document.piiType ?? PiiType.None,
          piiDescription: document.piiDescription || '',
          piiRetentionPeriodInMonths: document.piiRetentionPeriodInMonths ?? null,
          bbbeeLevel: document.bbbeeLevel ?? null,
          bbbeeExpiryDate: bbbeeExpiryDate,
          filePlanSeries: document.filePlanSeries || '',
          filePlanSubSeries: document.filePlanSubSeries || '',
          filePlanItem: document.filePlanItem || ''
        });

        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load document';
        this.loading = false;
        console.error('Error loading document:', err);
      }
    });
  }

  loadVersions(documentId: string): void {
    this.loadingVersions = true;
    this.documentsService.getDocumentVersions(documentId).subscribe({
      next: (versions) => {
        this.versions = versions;
        this.loadingVersions = false;
      },
      error: (err) => {
        console.error('Error loading versions:', err);
        this.loadingVersions = false;
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
    }
  }

  onVersionFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedVersionFile = input.files[0];
    }
  }

  onSubmit(): void {
    if (this.documentForm.invalid) {
      return;
    }

    // Enforce editing restrictions
    if (this.isEditMode && !this.canEdit()) {
      this.error = 'This document cannot be edited in its current state. Only Draft documents can be edited.';
      this.toastService.show(this.error, 'error');
      return;
    }

    this.saving = true;
    this.error = null;

    const formValue = this.documentForm.value;
    const reviewDate = new Date(formValue.reviewDate).toISOString();

    if (this.isEditMode && this.documentId) {
      const updateRequest: UpdateDocumentRequest = {
        title: formValue.title,
        category: formValue.category || undefined,
        department: formValue.department || undefined,
        ownerUserId: formValue.ownerUserId || undefined,
        reviewDate: reviewDate,
        status: formValue.status,
        piiDataType: formValue.piiDataType ?? PiiDataType.None,
        piiType: formValue.piiType ?? PiiType.None,
        piiDescription: formValue.piiDescription || undefined,
        piiRetentionPeriodInMonths: formValue.piiRetentionPeriodInMonths ?? undefined,
        // Note: personalInformationType is not included in UpdateDocumentRequest
        bbbeeLevel: formValue.bbbeeLevel ?? undefined,
        bbbeeExpiryDate: formValue.bbbeeExpiryDate ? new Date(formValue.bbbeeExpiryDate).toISOString() : undefined,
        filePlanSeries: formValue.filePlanSeries || undefined,
        filePlanSubSeries: formValue.filePlanSubSeries || undefined,
        filePlanItem: formValue.filePlanItem || undefined
      };

      this.documentsService.updateDocument(this.documentId, updateRequest).subscribe({
        next: () => {
          this.saving = false;
          this.router.navigate(['/documents']);
        },
        error: (err) => {
          this.error = err.message || 'Failed to update document';
          this.saving = false;
          console.error('Error updating document:', err);
        }
      });
    } else {
      const createRequest: CreateDocumentRequest = {
        title: formValue.title,
        category: formValue.category || undefined,
        department: formValue.department || undefined,
        ownerUserId: formValue.ownerUserId || undefined,
        reviewDate: reviewDate,
        piiDataType: formValue.piiDataType ?? PiiDataType.None,
        personalInformationType: formValue.personalInformationType ?? PersonalInformationType.None,
        piiType: formValue.piiType ?? PiiType.None,
        piiDescription: formValue.piiDescription || undefined,
        piiRetentionPeriodInMonths: formValue.piiRetentionPeriodInMonths ?? undefined,
        bbbeeLevel: formValue.bbbeeLevel ?? undefined,
        bbbeeExpiryDate: formValue.bbbeeExpiryDate ? new Date(formValue.bbbeeExpiryDate).toISOString() : undefined,
        filePlanSeries: formValue.filePlanSeries || undefined,
        filePlanSubSeries: formValue.filePlanSubSeries || undefined,
        filePlanItem: formValue.filePlanItem || undefined
      };

      this.documentsService.createDocument(createRequest).subscribe({
        next: (response) => {
          this.saving = false;
          // If file was selected, upload it
          if (this.selectedFile && response.id) {
            this.uploadFile(response.id);
          } else {
            this.router.navigate(['/documents']);
          }
        },
        error: (err) => {
          this.error = err.message || 'Failed to create document';
          this.saving = false;
          console.error('Error creating document:', err);
        }
      });
    }
  }

  uploadFile(documentId: string): void {
    if (!this.selectedFile) {
      return;
    }

    this.uploading = true;
    this.documentsService.uploadDocumentFile(documentId, this.selectedFile).subscribe({
      next: () => {
        this.uploading = false;
        this.router.navigate(['/documents']);
      },
      error: (err) => {
        this.error = err.message || 'Failed to upload file';
        this.uploading = false;
        console.error('Error uploading file:', err);
      }
    });
  }

  uploadNewVersion(): void {
    if (!this.selectedVersionFile || !this.documentId) {
      return;
    }

    this.uploadingVersion = true;
    const comment = this.versionForm.get('comment')?.value || undefined;

    this.documentsService.createDocumentVersion(this.documentId, this.selectedVersionFile, comment).subscribe({
      next: () => {
        this.uploadingVersion = false;
        this.selectedVersionFile = null;
        this.versionForm.reset();
        // Reset file input
        const fileInput = document.getElementById('versionFile') as HTMLInputElement;
        if (fileInput) {
          fileInput.value = '';
        }
        // Reload versions
        this.loadVersions(this.documentId!);
        // Show success message (you can add a toast service here)
        alert('New version uploaded successfully!');
      },
      error: (err) => {
        this.error = err.message || 'Failed to upload new version';
        this.uploadingVersion = false;
        console.error('Error uploading version:', err);
      }
    });
  }

  downloadVersion(version: DocumentVersionDto): void {
    if (!this.documentId) {
      return;
    }

    this.documentsService.downloadDocumentVersion(this.documentId, version.versionNumber).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = version.fileName;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Error downloading version:', err);
        alert('Failed to download version file');
      }
    });
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }

  setActiveTab(tab: 'details' | 'versions'): void {
    this.activeTab = tab;
  }

  submitForApproval(): void {
    if (!this.documentId) return;

    this.processingApproval = true;
    this.documentsService.submitDocumentForApproval(this.documentId).subscribe({
      next: (document) => {
        this.processingApproval = false;
        this.document = document;
        this.toastService.show('Document submitted for approval successfully', 'success');
        this.loadDocument(this.documentId!);
      },
      error: (err) => {
        this.processingApproval = false;
        const message = err.error?.error || err.message || 'Failed to submit document for approval';
        this.error = message;
        this.toastService.show(message, 'error');
      }
    });
  }

  openApproveDialog(): void {
    this.showApproveDialog = true;
    this.approveForm.reset();
  }

  closeApproveDialog(): void {
    this.showApproveDialog = false;
    this.approveForm.reset();
  }

  approveDocument(): void {
    if (!this.documentId || this.approveForm.invalid) return;

    this.processingApproval = true;
    const comments = this.approveForm.get('comments')?.value || undefined;

    this.documentsService.approveDocument(this.documentId, comments).subscribe({
      next: (document) => {
        this.processingApproval = false;
        this.showApproveDialog = false;
        this.document = document;
        this.toastService.show('Document approved successfully', 'success');
        this.loadDocument(this.documentId!);
      },
      error: (err) => {
        this.processingApproval = false;
        const message = err.error?.error || err.message || 'Failed to approve document';
        this.error = message;
        this.toastService.show(message, 'error');
      }
    });
  }

  openRejectDialog(): void {
    this.showRejectDialog = true;
    this.rejectForm.reset();
  }

  closeRejectDialog(): void {
    this.showRejectDialog = false;
    this.rejectForm.reset();
  }

  rejectDocument(): void {
    if (!this.documentId || this.rejectForm.invalid) return;

    this.processingApproval = true;
    const rejectedReason = this.rejectForm.get('rejectedReason')?.value;

    this.documentsService.rejectDocument(this.documentId, rejectedReason).subscribe({
      next: (document) => {
        this.processingApproval = false;
        this.showRejectDialog = false;
        this.document = document;
        this.toastService.show('Document rejected successfully', 'success');
        this.loadDocument(this.documentId!);
      },
      error: (err) => {
        this.processingApproval = false;
        const message = err.error?.error || err.message || 'Failed to reject document';
        this.error = message;
        this.toastService.show(message, 'error');
      }
    });
  }

  canSubmitForApproval(): boolean {
    return this.document?.workflowState === DocumentWorkflowState.Draft;
  }

  canApprove(): boolean {
    return this.document?.workflowState === DocumentWorkflowState.PendingApproval;
  }

  canReject(): boolean {
    return this.document?.workflowState === DocumentWorkflowState.PendingApproval;
  }

  canActivate(): boolean {
    return this.document?.workflowState === DocumentWorkflowState.Approved;
  }

  canArchive(): boolean {
    return this.document?.workflowState === DocumentWorkflowState.Active;
  }

  activateDocument(): void {
    if (!this.documentId) return;

    this.processingApproval = true;
    this.documentsService.activateDocument(this.documentId).subscribe({
      next: (document) => {
        this.processingApproval = false;
        this.document = document;
        this.toastService.show('Document activated successfully', 'success');
        this.loadDocument(this.documentId!);
      },
      error: (err) => {
        this.processingApproval = false;
        const message = err.error?.error || err.message || 'Failed to activate document';
        this.error = message;
        this.toastService.show(message, 'error');
      }
    });
  }

  archiveDocument(): void {
    if (!this.documentId) return;

    if (!confirm('Are you sure you want to archive this document? Archived documents cannot be modified.')) {
      return;
    }

    this.processingApproval = true;
    this.documentsService.archiveDocument(this.documentId).subscribe({
      next: (document) => {
        this.processingApproval = false;
        this.document = document;
        this.toastService.show('Document archived successfully', 'success');
        this.loadDocument(this.documentId!);
      },
      error: (err) => {
        this.processingApproval = false;
        const message = err.error?.error || err.message || 'Failed to archive document';
        this.error = message;
        this.toastService.show(message, 'error');
      }
    });
  }

  canEdit(): boolean {
    return this.document?.workflowState === DocumentWorkflowState.Draft;
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

  downloadEvidencePack(): void {
    if (!this.documentId) return;

    this.downloadingEvidence = true;
    this.documentsService.getDocumentAuditEvidence(this.documentId).subscribe({
      next: (evidence) => {
        this.generateEvidencePdf(evidence);
        this.downloadingEvidence = false;
      },
      error: (err) => {
        this.downloadingEvidence = false;
        this.toastService.show('Failed to generate evidence pack', 'error');
        console.error('Error generating evidence pack:', err);
      }
    });
  }

  private generateEvidencePdf(evidence: any): void {
    // Create a comprehensive HTML report
    const htmlContent = this.generateEvidenceHtml(evidence);
    
    // Create a new window with the HTML content
    const printWindow = window.open('', '_blank');
    if (printWindow) {
      printWindow.document.write(htmlContent);
      printWindow.document.close();
      
      // Wait for content to load, then print
      printWindow.onload = () => {
        setTimeout(() => {
          printWindow.print();
          // Optionally close after printing
          // printWindow.close();
        }, 250);
      };
    } else {
      // Fallback: download as HTML file
      const blob = new Blob([htmlContent], { type: 'text/html' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `audit-evidence-${evidence.document.title.replace(/[^a-z0-9]/gi, '_')}-${new Date().toISOString().split('T')[0]}.html`;
      link.click();
      URL.revokeObjectURL(url);
    }
  }

  private generateEvidenceHtml(evidence: any): string {
    const doc = evidence.document;
    const generatedDate = new Date(evidence.generatedAt).toLocaleString();
    
    return `
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <title>AGSA Audit Evidence - ${doc.title}</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; color: #333; }
    h1 { color: #1976d2; border-bottom: 3px solid #1976d2; padding-bottom: 10px; }
    h2 { color: #555; margin-top: 30px; border-bottom: 2px solid #ddd; padding-bottom: 5px; }
    h3 { color: #666; margin-top: 20px; }
    table { width: 100%; border-collapse: collapse; margin: 15px 0; }
    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
    th { background-color: #f5f5f5; font-weight: bold; }
    .metadata { background-color: #f9f9f9; padding: 15px; border-radius: 5px; margin: 15px 0; }
    .metadata-item { margin: 5px 0; }
    .label { font-weight: bold; color: #555; }
    .section { margin: 20px 0; page-break-inside: avoid; }
    @media print { body { margin: 0; } .no-print { display: none; } }
  </style>
</head>
<body>
  <h1>AGSA Audit Evidence Report</h1>
  <p><strong>Generated:</strong> ${generatedDate}</p>
  
  <div class="section">
    <h2>1. Document Metadata</h2>
    <div class="metadata">
      <div class="metadata-item"><span class="label">Title:</span> ${doc.title}</div>
      <div class="metadata-item"><span class="label">Category:</span> ${doc.category || 'N/A'}</div>
      <div class="metadata-item"><span class="label">Department:</span> ${doc.department || 'N/A'}</div>
      <div class="metadata-item"><span class="label">Status:</span> ${doc.status}</div>
      <div class="metadata-item"><span class="label">Workflow State:</span> ${doc.workflowState}</div>
      <div class="metadata-item"><span class="label">Review Date:</span> ${doc.reviewDate ? new Date(doc.reviewDate).toLocaleDateString() : 'N/A'}</div>
      <div class="metadata-item"><span class="label">Retention Until:</span> ${doc.retainUntil ? new Date(doc.retainUntil).toLocaleDateString() : 'N/A'}</div>
      <div class="metadata-item"><span class="label">PII Type:</span> ${doc.piiType !== undefined ? doc.piiType : 'None'}</div>
      <div class="metadata-item"><span class="label">File Plan Series:</span> ${doc.filePlanSeries || 'N/A'}</div>
      <div class="metadata-item"><span class="label">File Plan Sub-series:</span> ${doc.filePlanSubSeries || 'N/A'}</div>
      <div class="metadata-item"><span class="label">File Plan Item:</span> ${doc.filePlanItem || 'N/A'}</div>
    </div>
  </div>

  <div class="section">
    <h2>2. Document Versions</h2>
    ${evidence.versions && evidence.versions.length > 0 ? `
    <table>
      <thead>
        <tr>
          <th>Version</th>
          <th>File Name</th>
          <th>Uploaded By</th>
          <th>Uploaded At</th>
          <th>Comment</th>
          <th>Is Latest</th>
        </tr>
      </thead>
      <tbody>
        ${evidence.versions.map((v: any) => `
        <tr>
          <td>${v.versionNumber}</td>
          <td>${v.fileName}</td>
          <td>${v.uploadedBy || 'N/A'}</td>
          <td>${new Date(v.uploadedAt).toLocaleString()}</td>
          <td>${v.comment || '-'}</td>
          <td>${v.isLatest ? 'Yes' : 'No'}</td>
        </tr>
        `).join('')}
      </tbody>
    </table>
    ` : '<p>No versions found.</p>'}
  </div>

  <div class="section">
    <h2>3. Business Audit Logs</h2>
    ${evidence.businessAuditLogs && evidence.businessAuditLogs.length > 0 ? `
    <table>
      <thead>
        <tr>
          <th>Timestamp</th>
          <th>Action</th>
          <th>User</th>
          <th>Metadata</th>
        </tr>
      </thead>
      <tbody>
        ${evidence.businessAuditLogs.map((log: any) => `
        <tr>
          <td>${new Date(log.timestamp).toLocaleString()}</td>
          <td>${log.action}</td>
          <td>${log.username || 'System'}</td>
          <td>${log.metadataJson ? JSON.stringify(JSON.parse(log.metadataJson), null, 2) : '-'}</td>
        </tr>
        `).join('')}
      </tbody>
    </table>
    ` : '<p>No audit logs found.</p>'}
  </div>

  <div class="section">
    <h2>4. Approval History</h2>
    ${evidence.approvalHistory ? `
    <div class="metadata">
      <div class="metadata-item"><span class="label">Current Workflow State:</span> ${evidence.approvalHistory.currentWorkflowState}</div>
      <div class="metadata-item"><span class="label">Approver:</span> ${evidence.approvalHistory.approverUserId || 'N/A'}</div>
      <div class="metadata-item"><span class="label">Approved At:</span> ${evidence.approvalHistory.approvedAt ? new Date(evidence.approvalHistory.approvedAt).toLocaleString() : 'N/A'}</div>
      <div class="metadata-item"><span class="label">Comments:</span> ${evidence.approvalHistory.comments || 'N/A'}</div>
      ${evidence.approvalHistory.rejectedReason ? `<div class="metadata-item"><span class="label">Rejection Reason:</span> ${evidence.approvalHistory.rejectedReason}</div>` : ''}
    </div>
    ` : '<p>No approval history available.</p>'}
  </div>

  <div class="section">
    <h2>5. Linked Non-Conformance Reports (NCRs)</h2>
    ${evidence.linkedNcrs && evidence.linkedNcrs.length > 0 ? `
    <table>
      <thead>
        <tr>
          <th>Title</th>
          <th>Description</th>
          <th>Department</th>
          <th>Link Reason</th>
        </tr>
      </thead>
      <tbody>
        ${evidence.linkedNcrs.map((ncr: any) => `
        <tr>
          <td>${ncr.title}</td>
          <td>${ncr.description}</td>
          <td>${ncr.department || 'N/A'}</td>
          <td>${ncr.linkReason || 'N/A'}</td>
        </tr>
        `).join('')}
      </tbody>
    </table>
    ` : '<p>No linked NCRs found.</p>'}
  </div>

  <div class="section">
    <h2>6. Linked Risks</h2>
    ${evidence.linkedRisks && evidence.linkedRisks.length > 0 ? `
    <table>
      <thead>
        <tr>
          <th>Title</th>
          <th>Description</th>
          <th>Link Reason</th>
        </tr>
      </thead>
      <tbody>
        ${evidence.linkedRisks.map((risk: any) => `
        <tr>
          <td>${risk.title}</td>
          <td>${risk.description}</td>
          <td>${risk.linkReason || 'N/A'}</td>
        </tr>
        `).join('')}
      </tbody>
    </table>
    ` : '<p>No linked risks found.</p>'}
  </div>

  <div style="margin-top: 40px; padding-top: 20px; border-top: 2px solid #ddd; text-align: center; color: #666; font-size: 12px;">
    <p>This report was generated by Maemo Compliance System for AGSA audit purposes.</p>
    <p>Generated on ${generatedDate}</p>
  </div>
</body>
</html>
    `;
  }
}
