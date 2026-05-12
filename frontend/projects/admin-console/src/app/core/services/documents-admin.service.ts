import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiService } from './admin-api.service';

@Injectable({
  providedIn: 'root'
})
export class DocumentsAdminService {
  constructor(private api: AdminApiService) {}

  /**
   * Destroy a document according to retention rules
   */
  destroyDocument(documentId: string, reason: string): Observable<void> {
    return this.api.post<void>(`/documents/${documentId}/destroy`, { reason });
  }
}

