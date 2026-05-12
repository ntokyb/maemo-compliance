import { Injectable, inject, OnDestroy } from '@angular/core';
import { Subscription, interval } from 'rxjs';
import { filter, switchMap } from 'rxjs/operators';
import { NcrOfflineService } from './ncr-offline.service';
import { OfflineDetectionService } from './offline-detection.service';
import { ToastService } from './toast.service';

/**
 * Service that automatically syncs pending NCRs when online.
 */
@Injectable({
  providedIn: 'root'
})
export class NcrSyncService implements OnDestroy {
  private ncrOfflineService = inject(NcrOfflineService);
  private offlineDetection = inject(OfflineDetectionService);
  private toastService = inject(ToastService);
  private syncSubscription?: Subscription;
  private syncInterval = 30000; // Sync every 30 seconds when online

  /**
   * Start automatic syncing.
   */
  startAutoSync(): void {
    // Stop any existing sync
    this.stopAutoSync();

    // Sync when coming online
    this.offlineDetection.onlineStatus$
      .pipe(
        filter(isOnline => isOnline),
        switchMap(() => interval(this.syncInterval))
      )
      .subscribe(() => {
        this.syncPendingNcrs();
      });

    // Also sync immediately when coming online
    this.offlineDetection.onlineStatus$
      .pipe(filter(isOnline => isOnline))
      .subscribe(() => {
        // Small delay to ensure network is stable
        setTimeout(() => this.syncPendingNcrs(), 1000);
      });
  }

  /**
   * Stop automatic syncing.
   */
  stopAutoSync(): void {
    if (this.syncSubscription) {
      this.syncSubscription.unsubscribe();
      this.syncSubscription = undefined;
    }
  }

  /**
   * Manually sync pending NCRs.
   */
  syncPendingNcrs(): void {
    if (!this.offlineDetection.isOnline()) {
      return;
    }

    this.ncrOfflineService.syncAllPendingNcrs().subscribe({
      next: (result) => {
        if (result.synced > 0) {
          this.toastService.show(
            `Synced ${result.synced} NCR${result.synced > 1 ? 's' : ''} successfully`,
            'success'
          );
        }
        if (result.failed > 0) {
          this.toastService.show(
            `Failed to sync ${result.failed} NCR${result.failed > 1 ? 's' : ''}`,
            'error'
          );
        }
      },
      error: (err) => {
        console.error('Error syncing NCRs:', err);
      }
    });
  }

  ngOnDestroy(): void {
    this.stopAutoSync();
  }
}

