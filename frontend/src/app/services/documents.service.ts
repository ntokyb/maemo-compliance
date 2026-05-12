import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Document, CreateDocumentRequest, UpdateDocumentRequest, DocumentVersionDto, AuditEvidence } from '../models/document.model';

@Injectable({
  providedIn: 'root'
})
export class DocumentsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/api/documents`;

  getDocuments(params?: { status?: number; department?: string; category?: string }): Observable<Document[]> {
    let httpParams = new HttpParams();
    
    if (params?.status !== undefined) {
      httpParams = httpParams.set('status', params.status.toString());
    }
    
    if (params?.department) {
      httpParams = httpParams.set('department', params.department);
    }

    if (params?.category) {
      httpParams = httpParams.set('category', params.category);
    }

    return this.http.get<Document[]>(this.apiUrl, { params: httpParams });
  }

  getDocument(id: string): Observable<Document> {
    return this.http.get<Document>(`${this.apiUrl}/${id}`);
  }

  createDocument(request: CreateDocumentRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.apiUrl, request);
  }

  updateDocument(id: string, request: UpdateDocumentRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, request);
  }

  uploadDocumentFile(documentId: string, file: File): Observable<{ storageLocation: string }> {
    const formData = new FormData();
    formData.append('file', file);
    
    return this.http.post<{ storageLocation: string }>(`${this.apiUrl}/${documentId}/upload`, formData);
  }

  getDocumentVersions(documentId: string): Observable<DocumentVersionDto[]> {
    return this.http.get<DocumentVersionDto[]>(`${this.apiUrl}/${documentId}/versions`);
  }

  createDocumentVersion(documentId: string, file: File, comment?: string): Observable<{ id: string }> {
    const formData = new FormData();
    formData.append('file', file);
    if (comment) {
      formData.append('comment', comment);
    }
    
    return this.http.post<{ id: string }>(`${this.apiUrl}/${documentId}/versions`, formData);
  }

  downloadDocumentVersion(documentId: string, version: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${documentId}/versions/${version}/download`, {
      responseType: 'blob'
    });
  }

  submitDocumentForApproval(documentId: string): Observable<Document> {
    return this.http.post<Document>(`${this.apiUrl}/${documentId}/submit-for-approval`, {});
  }

  approveDocument(documentId: string, comments?: string): Observable<Document> {
    return this.http.post<Document>(`${this.apiUrl}/${documentId}/approve`, { comments });
  }

  rejectDocument(documentId: string, rejectedReason: string): Observable<Document> {
    return this.http.post<Document>(`${this.apiUrl}/${documentId}/reject`, { rejectedReason });
  }

  activateDocument(documentId: string): Observable<Document> {
    return this.http.post<Document>(`${this.apiUrl}/${documentId}/activate`, {});
  }

  archiveDocument(documentId: string): Observable<Document> {
    return this.http.post<Document>(`${this.apiUrl}/${documentId}/archive`, {});
  }

  getPendingApprovalDocuments(): Observable<Document[]> {
    return this.http.get<Document[]>(`${this.apiUrl}/pending-approval`);
  }

  getBbbeeCertificatesExpiringSoon(days: number = 90): Observable<Document[]> {
    return this.http.get<Document[]>(`${this.apiUrl}/bbbee-certificates/expiring-soon`, {
      params: new HttpParams().set('days', days.toString())
    });
  }

  getDocumentsPastRetention(): Observable<Document[]> {
    return this.http.get<Document[]>(`${this.apiUrl}/past-retention`);
  }

  getDocumentAuditEvidence(id: string): Observable<AuditEvidence> {
    return this.http.get<AuditEvidence>(`${this.apiUrl}/${id}/audit-evidence`);
  }
}

