import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

export interface DocumentTemplateMeta {
  id: string;
  standard: string;
  title: string;
  standardCode: string; // e.g. "ISO 9001"
  assetPath: string; // e.g. "assets/templates/iso9001/documents/control-of-documents.md"
}

@Injectable({
  providedIn: 'root'
})
export class TemplateLibraryService {
  private http = inject(HttpClient);

  // Built-in templates matching the backend templates
  private readonly builtInTemplates: DocumentTemplateMeta[] = [
    // ISO 9001 Templates
    {
      id: 'iso9001-doc-quality-manual',
      standard: 'ISO 9001',
      title: 'Quality Manual Overview',
      standardCode: 'ISO 9001',
      assetPath: 'assets/templates/iso9001/documents/quality-manual-overview.md'
    },
    {
      id: 'iso9001-doc-control-of-documents',
      standard: 'ISO 9001',
      title: 'Control of Documents Procedure',
      standardCode: 'ISO 9001',
      assetPath: 'assets/templates/iso9001/documents/control-of-documents.md'
    },
    {
      id: 'iso9001-doc-control-of-records',
      standard: 'ISO 9001',
      title: 'Control of Records Procedure',
      standardCode: 'ISO 9001',
      assetPath: 'assets/templates/iso9001/documents/control-of-records.md'
    },
    {
      id: 'iso9001-doc-ncr-capa',
      standard: 'ISO 9001',
      title: 'Nonconformity and Corrective Action Procedure',
      standardCode: 'ISO 9001',
      assetPath: 'assets/templates/iso9001/documents/ncr-capa.md'
    },
    {
      id: 'iso9001-doc-internal-audit-procedure',
      standard: 'ISO 9001',
      title: 'Internal Audit Procedure',
      standardCode: 'ISO 9001',
      assetPath: 'assets/templates/iso9001/documents/internal-audit-procedure.md'
    },
    {
      id: 'iso9001-doc-management-review',
      standard: 'ISO 9001',
      title: 'Management Review Procedure',
      standardCode: 'ISO 9001',
      assetPath: 'assets/templates/iso9001/documents/management-review.md'
    },
    {
      id: 'iso9001-doc-risk-opportunity',
      standard: 'ISO 9001',
      title: 'Risk and Opportunity Management Procedure',
      standardCode: 'ISO 9001',
      assetPath: 'assets/templates/iso9001/documents/risk-opportunity.md'
    },
    {
      id: 'iso9001-doc-training-competence',
      standard: 'ISO 9001',
      title: 'Training and Competence Procedure',
      standardCode: 'ISO 9001',
      assetPath: 'assets/templates/iso9001/documents/training-competence.md'
    },
    // Other standards
    {
      id: 'popia-form-data-breach-notification',
      standard: 'POPIA',
      title: 'POPIA Data Breach Notification Template',
      standardCode: 'POPIA',
      assetPath: 'assets/templates/popia/manuals/popia-data-breach-notification.md'
    },
    {
      id: 'narsa-records-retention-schedule',
      standard: 'NARSA',
      title: 'Records Retention Schedule',
      standardCode: 'NARSA',
      assetPath: 'assets/templates/narsa/records/records-retention-schedule.md'
    },
    {
      id: 'agsa-audit-evidence-register',
      standard: 'AGSA',
      title: 'Audit Evidence Register',
      standardCode: 'AGSA',
      assetPath: 'assets/templates/agsa/audits/audit-evidence-register.md'
    }
  ];

  /**
   * Gets all document templates, optionally filtered by standard.
   */
  getDocumentTemplates(standard?: string): Observable<DocumentTemplateMeta[]> {
    let templates = this.builtInTemplates;

    if (standard) {
      templates = templates.filter(t => 
        t.standard.toLowerCase() === standard.toLowerCase()
      );
    }

    return of(templates);
  }

  /**
   * Gets a specific template by ID.
   */
  getTemplateById(id: string): Observable<DocumentTemplateMeta | null> {
    const template = this.builtInTemplates.find(t => t.id === id);
    return of(template || null);
  }

  /**
   * Loads the Markdown content for a template from the assets folder.
   */
  loadMarkdown(assetPath: string): Observable<string> {
    return this.http.get(assetPath, { responseType: 'text' }).pipe(
      catchError(error => {
        console.error(`Error loading template from ${assetPath}:`, error);
        return of('');
      })
    );
  }

  /**
   * Gets all available standards.
   */
  getStandards(): Observable<string[]> {
    const standards = [...new Set(this.builtInTemplates.map(t => t.standard))];
    return of(standards);
  }
}

