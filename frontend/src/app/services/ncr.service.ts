import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Ncr, CreateNcrRequest, UpdateNcrStatusRequest } from '../models/ncr.model';
import { Risk } from '../models/risk.model';

@Injectable({
  providedIn: 'root'
})
export class NcrService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/api/ncrs`;

  getNcrs(params?: { status?: number; severity?: number; department?: string }): Observable<Ncr[]> {
    let httpParams = new HttpParams();
    
    if (params?.status !== undefined) {
      httpParams = httpParams.set('status', params.status.toString());
    }
    
    if (params?.severity !== undefined) {
      httpParams = httpParams.set('severity', params.severity.toString());
    }
    
    if (params?.department) {
      httpParams = httpParams.set('department', params.department);
    }

    return this.http.get<Ncr[]>(this.apiUrl, { params: httpParams });
  }

  getNcr(id: string): Observable<Ncr> {
    return this.http.get<Ncr>(`${this.apiUrl}/${id}`);
  }

  createNcr(request: CreateNcrRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.apiUrl, request);
  }

  updateNcrStatus(id: string, request: UpdateNcrStatusRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/status`, request);
  }

  /** Linked risks for an NCR (same IDs as POST/DELETE link). */
  getRisksForNcr(ncrId: string): Observable<Risk[]> {
    return this.http.get<Risk[]>(`${this.apiUrl}/${ncrId}/risks`);
  }

  /** POST /api/ncrs/{ncrId}/risks/{riskId} — path params only, no body. */
  linkNcrToRisk(ncrId: string, riskId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${ncrId}/risks/${riskId}`, null);
  }

  unlinkNcrFromRisk(ncrId: string, riskId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${ncrId}/risks/${riskId}`);
  }
}

