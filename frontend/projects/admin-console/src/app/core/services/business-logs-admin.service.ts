import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BusinessAuditLogDto } from '../models/admin-business-log.dto';
import { AdminApiService } from './admin-api.service';

export interface BusinessLogsFilters {
  tenantId?: string;
  action?: string;
  entityType?: string;
  limit?: number;
}

@Injectable({
  providedIn: 'root'
})
export class BusinessLogsAdminService {
  constructor(private api: AdminApiService) {}

  /**
   * Get business audit logs with optional filters.
   */
  getBusinessLogs(filters?: BusinessLogsFilters): Observable<BusinessAuditLogDto[]> {
    // Build query string manually to handle empty values correctly
    const queryParts: string[] = [];
    if (filters?.tenantId) {
      queryParts.push(`tenantId=${encodeURIComponent(filters.tenantId)}`);
    }
    if (filters?.action) {
      queryParts.push(`action=${encodeURIComponent(filters.action)}`);
    }
    if (filters?.entityType) {
      queryParts.push(`entityType=${encodeURIComponent(filters.entityType)}`);
    }
    if (filters?.limit) {
      queryParts.push(`limit=${filters.limit}`);
    }
    
    const queryString = queryParts.length > 0 ? '?' + queryParts.join('&') : '';
    const url = `/logs/business${queryString}`;
    
    return this.api.get<BusinessAuditLogDto[]>(url);
  }

  /**
   * Get audit trail for a specific entity.
   */
  getEntityAuditTrail(entityType: string, entityId: string): Observable<BusinessAuditLogDto[]> {
    return this.api.get<BusinessAuditLogDto[]>(`/logs/business/${entityType}/${entityId}`);
  }
}

