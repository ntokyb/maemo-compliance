import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PopiaDocumentSummaryDto } from '../models/popia-document-summary.dto';
import { AdminApiService } from './admin-api.service';

@Injectable({
  providedIn: 'root'
})
export class PopiaAdminService {
  constructor(private api: AdminApiService) {}

  /**
   * Get POPIA document summary - documents classified by personal information type
   */
  getDocumentSummary(): Observable<PopiaDocumentSummaryDto> {
    return this.api.get<PopiaDocumentSummaryDto>('/popia/documents/summary');
  }
}

