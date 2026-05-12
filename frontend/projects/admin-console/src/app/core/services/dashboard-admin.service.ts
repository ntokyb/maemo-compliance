import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminDashboardSummaryDto } from '../models/admin-dashboard.dto';
import { AdminApiService } from './admin-api.service';

@Injectable({
  providedIn: 'root'
})
export class DashboardAdminService {
  constructor(private api: AdminApiService) {}

  /**
   * Get admin dashboard summary - platform-wide metrics
   */
  getDashboardSummary(): Observable<AdminDashboardSummaryDto> {
    return this.api.get<AdminDashboardSummaryDto>('/dashboard/summary');
  }
}

