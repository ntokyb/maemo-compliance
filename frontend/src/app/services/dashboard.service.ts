import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DashboardSummary {
  totalDocuments: number;
  activeDocuments: number;
  totalNcrs: number;
  openNcrs: number;
  overdueNcrs: number;
  // Phase 2: Risk metrics
  totalRisks: number;
  highRisks: number;
  mediumRisks: number;
  lowRisks: number;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/api/dashboard`;

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/summary`);
  }
}

