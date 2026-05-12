import { Injectable } from '@angular/core';
import { AdminApiService } from './admin-api.service';
import { Observable } from 'rxjs';
import { AdminWorkerSummaryDto, AdminWorkerJobHistoryItemDto } from '../models/admin-worker.dto';

@Injectable({
  providedIn: 'root'
})
export class WorkersAdminService {

  constructor(private adminApi: AdminApiService) { }

  getWorkers(): Observable<AdminWorkerSummaryDto[]> {
    return this.adminApi.get<AdminWorkerSummaryDto[]>('/workers');
  }

  getWorkerHistory(workerName: string, limit: number = 50): Observable<AdminWorkerJobHistoryItemDto[]> {
    return this.adminApi.get<AdminWorkerJobHistoryItemDto[]>(`/workers/${encodeURIComponent(workerName)}/history?limit=${limit}`);
  }
}

