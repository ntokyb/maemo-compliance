import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { OnboardingRequest, OnboardingStatus } from '../models/onboarding.model';

@Injectable({
  providedIn: 'root'
})
export class OnboardingService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/api/onboarding`;

  getStatus(): Observable<OnboardingStatus> {
    return this.http.get<OnboardingStatus>(`${this.apiUrl}/status`);
  }

  runOnboarding(request: OnboardingRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/run`, request);
  }
}

