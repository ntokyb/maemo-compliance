import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PopiaTrailReportItem } from '../models/document.model';

@Injectable({
  providedIn: 'root'
})
export class PopiaService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/api/popiatrail`;

  getTrailReport(days: number = 30): Observable<PopiaTrailReportItem[]> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<PopiaTrailReportItem[]>(`${this.apiUrl}/report`, { params });
  }
}

