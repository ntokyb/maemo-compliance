import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/** Profile for the current user in the active tenant (GET /api/tenant/me). */
export interface TenantMeProfile {
  email: string;
  fullName: string;
  onboardingComplete: boolean;
  jobTitle: string | null;
  phone: string | null;
  addressLine: string | null;
  complianceStandards: string[];
}

@Injectable({
  providedIn: 'root'
})
export class UserProfileService {
  private http = inject(HttpClient);

  getMe(): Observable<TenantMeProfile> {
    return this.http.get<TenantMeProfile>(`${environment.apiBaseUrl}/api/tenant/me`);
  }

  completeOnboardingWizard(body: {
    firstName: string;
    lastName: string;
    jobTitle?: string | null;
    phone?: string | null;
    organisationAddress?: string | null;
    organisationName?: string | null;
    logoUrl?: string | null;
    standards: string[];
    teamEmails: string[];
  }): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/tenant/onboarding/complete-wizard`, body);
  }
}
