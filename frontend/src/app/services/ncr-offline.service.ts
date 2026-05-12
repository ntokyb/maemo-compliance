import { Injectable, inject } from '@angular/core';
import { Observable, from, of } from 'rxjs';
import { map, catchError, switchMap } from 'rxjs/operators';
import { IndexedDbService } from './indexed-db.service';
import { NcrService } from './ncr.service';
import { CreateNcrRequest } from '../models/ncr.model';
import { PendingNcr } from '../models/offline-ncr.model';

/**
 * Service for managing offline NCR creation and sync.
 */
@Injectable({
  providedIn: 'root'
})
export class NcrOfflineService {
  private indexedDb = inject(IndexedDbService);
  private ncrService = inject(NcrService);
  private storeName = 'pendingNcrs';

  /**
   * Initialize the offline service.
   */
  async init(): Promise<void> {
    await this.indexedDb.init();
  }

  /**
   * Create an NCR offline (store in IndexedDB).
   */
  createNcrOffline(request: CreateNcrRequest): Observable<number> {
    const pendingNcr: PendingNcr = {
      request,
      createdAt: new Date().toISOString(),
      synced: false
    };

    return this.indexedDb.add<PendingNcr>(this.storeName, pendingNcr).pipe(
      map(id => Number(id))
    );
  }

  /**
   * Get all pending NCRs.
   */
  getPendingNcrs(): Observable<PendingNcr[]> {
    return this.indexedDb.getAll<PendingNcr>(this.storeName).pipe(
      map(ncrs => ncrs.filter(ncr => !ncr.synced)),
      catchError(error => {
        console.warn('Failed to get pending NCRs from IndexedDB:', error);
        return of([]); // Return empty array on error
      })
    );
  }

  /**
   * Get count of pending NCRs.
   */
  getPendingCount(): Observable<number> {
    return this.getPendingNcrs().pipe(
      map(ncrs => ncrs.length),
      catchError(error => {
        console.warn('Failed to get pending NCR count:', error);
        return of(0); // Return 0 on error
      })
    );
  }

  /**
   * Sync a pending NCR to the server.
   */
  syncNcr(pendingNcr: PendingNcr): Observable<void> {
    if (!pendingNcr.id) {
      return from(Promise.reject(new Error('Pending NCR must have an ID')));
    }

    return this.ncrService.createNcr(pendingNcr.request).pipe(
      switchMap(() => {
        // Delete from IndexedDB after successful sync
        if (pendingNcr.id) {
          return this.indexedDb.delete(this.storeName, pendingNcr.id);
        }
        return of(undefined);
      }),
      map(() => undefined),
      catchError(error => {
        // Update with error (don't delete, keep for retry)
        const updated: PendingNcr = {
          ...pendingNcr,
          syncError: error.message || 'Sync failed'
        };
        if (pendingNcr.id) {
          this.indexedDb.update(this.storeName, updated as PendingNcr & { id: number }).subscribe();
        }
        return from(Promise.reject(error));
      })
    );
  }

  /**
   * Sync all pending NCRs.
   */
  syncAllPendingNcrs(): Observable<{ synced: number; failed: number }> {
    return new Observable(observer => {
      this.getPendingNcrs().subscribe({
        next: async (pendingNcrs) => {
          if (pendingNcrs.length === 0) {
            observer.next({ synced: 0, failed: 0 });
            observer.complete();
            return;
          }

          let synced = 0;
          let failed = 0;

          for (const ncr of pendingNcrs) {
            try {
              await new Promise<void>((resolve, reject) => {
                this.syncNcr(ncr).subscribe({
                  next: () => resolve(),
                  error: (err) => reject(err),
                  complete: () => resolve()
                });
              });
              synced++;
            } catch (error) {
              failed++;
            }
          }

          observer.next({ synced, failed });
          observer.complete();
        },
        error: (err) => {
          observer.error(err);
        }
      });
    });
  }

  /**
   * Delete a pending NCR (after successful sync or user cancellation).
   */
  deletePendingNcr(id: number): Observable<void> {
    return this.indexedDb.delete(this.storeName, id);
  }
}

