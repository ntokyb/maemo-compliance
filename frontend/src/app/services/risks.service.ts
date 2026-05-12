import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Risk, CreateRiskRequest, UpdateRiskRequest } from '../models/risk.model';

@Injectable({
  providedIn: 'root'
})
export class RisksService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/api/risks`;

  getRisks(params?: { category?: number; status?: number }): Observable<Risk[]> {
    let httpParams = new HttpParams();
    
    if (params?.category !== undefined) {
      httpParams = httpParams.set('category', params.category.toString());
    }
    
    if (params?.status !== undefined) {
      httpParams = httpParams.set('status', params.status.toString());
    }

    return this.http.get<Risk[]>(this.apiUrl, { params: httpParams });
  }

  getRisk(id: string): Observable<Risk> {
    return this.http.get<Risk>(`${this.apiUrl}/${id}`);
  }

  createRisk(request: CreateRiskRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.apiUrl, request);
  }

  updateRisk(id: string, request: UpdateRiskRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, request);
  }
}

