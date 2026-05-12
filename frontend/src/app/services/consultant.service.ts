import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import type {
  ConsultantDashboardSummary,
  ConsultantClient,
  ConsultantBranding,
  AuditTemplate,
  AuditQuestion,
  AuditRun,
  AuditAnswer,
  CreateAuditTemplateRequest,
  AddAuditQuestionRequest,
  StartAuditRunRequest,
  SubmitAuditAnswerRequest
} from '../models/consultant.model';

@Injectable({
  providedIn: 'root'
})
export class ConsultantService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/api/consultants`;
  private auditsApiUrl = `${environment.apiBaseUrl}/api/audits`;

  // Dashboard
  getDashboardSummary(): Observable<ConsultantDashboardSummary> {
    return this.http.get<ConsultantDashboardSummary>(`${this.apiUrl}/dashboard`);
  }

  // Clients
  getClients(): Observable<ConsultantClient[]> {
    return this.http.get<ConsultantClient[]>(`${this.apiUrl}/me/clients`);
  }

  // Branding
  getBranding(): Observable<ConsultantBranding> {
    return this.http.get<ConsultantBranding>(`${this.apiUrl}/branding`);
  }

  updateBranding(branding: ConsultantBranding): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/branding`, branding);
  }

  uploadBrandingFile(fileType: 'logo' | 'loginBanner', file: File): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${this.apiUrl}/branding/${fileType}`, formData);
  }

  // Audit Templates
  getAuditTemplates(): Observable<AuditTemplate[]> {
    return this.http.get<AuditTemplate[]>(`${this.auditsApiUrl}/templates`);
  }

  createAuditTemplate(request: CreateAuditTemplateRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.auditsApiUrl}/templates`, request);
  }

  getAuditQuestions(templateId: string): Observable<AuditQuestion[]> {
    return this.http.get<AuditQuestion[]>(`${this.auditsApiUrl}/templates/${templateId}/questions`);
  }

  addAuditQuestion(templateId: string, request: AddAuditQuestionRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.auditsApiUrl}/templates/${templateId}/questions`, request);
  }

  // Audit Runs
  getAuditRuns(): Observable<AuditRun[]> {
    return this.http.get<AuditRun[]>(`${this.auditsApiUrl}/runs`);
  }

  startAuditRun(request: StartAuditRunRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.auditsApiUrl}/runs`, request);
  }

  getAuditRun(runId: string): Observable<AuditRun> {
    return this.http.get<AuditRun>(`${this.auditsApiUrl}/runs/${runId}`);
  }

  completeAuditRun(runId: string): Observable<AuditRun> {
    return this.http.post<AuditRun>(`${this.auditsApiUrl}/runs/${runId}/complete`, {});
  }

  getAuditAnswers(runId: string): Observable<AuditAnswer[]> {
    return this.http.get<AuditAnswer[]>(`${this.auditsApiUrl}/runs/${runId}/answers`);
  }

  submitAuditAnswer(runId: string, request: SubmitAuditAnswerRequest): Observable<void> {
    return this.http.post<void>(`${this.auditsApiUrl}/runs/${runId}/answers`, request);
  }

  uploadAuditEvidence(runId: string, questionId: string, file: File): Observable<{ evidenceFileUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ evidenceFileUrl: string }>(
      `${this.auditsApiUrl}/runs/${runId}/questions/${questionId}/evidence`,
      formData
    );
  }
}

