import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { EvidenceItemDto } from '../models/evidence-item.dto';
import { AdminApiService } from './admin-api.service';

@Injectable({
  providedIn: 'root'
})
export class EvidenceAdminService {
  constructor(private api: AdminApiService) {}

  /**
   * Get evidence register - all evidence items across tenants
   */
  getEvidenceRegister(params?: {
    tenantId?: string;
    entityType?: string;
    fromDate?: string;
    toDate?: string;
    limit?: number;
  }): Observable<EvidenceItemDto[]> {
    let endpoint = '/evidence';
    const queryParams: string[] = [];
    
    if (params?.tenantId) {
      queryParams.push(`tenantId=${encodeURIComponent(params.tenantId)}`);
    }
    if (params?.entityType) {
      queryParams.push(`entityType=${encodeURIComponent(params.entityType)}`);
    }
    if (params?.fromDate) {
      queryParams.push(`fromDate=${encodeURIComponent(params.fromDate)}`);
    }
    if (params?.toDate) {
      queryParams.push(`toDate=${encodeURIComponent(params.toDate)}`);
    }
    if (params?.limit) {
      queryParams.push(`limit=${params.limit}`);
    }
    
    if (queryParams.length > 0) {
      endpoint += '?' + queryParams.join('&');
    }
    
    return this.api.get<EvidenceItemDto[]>(endpoint);
  }
}

