import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OfflineDetectionService } from '../../services/offline-detection.service';
import { NcrOfflineService } from '../../services/ncr-offline.service';
import { NcrSyncService } from '../../services/ncr-sync.service';

@Component({
  selector: 'app-offline-banner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './offline-banner.html',
  styleUrl: './offline-banner.scss'
})
export class OfflineBannerComponent implements OnInit {
  private offlineDetection = inject(OfflineDetectionService);
  private ncrOfflineService = inject(NcrOfflineService);
  private ncrSyncService = inject(NcrSyncService);

  isOffline = false;
  pendingNcrCount = 0;

  ngOnInit(): void {
    // Initialize offline service FIRST before any operations
    this.ncrOfflineService.init().then(() => {
      // Subscribe to online status
      this.offlineDetection.onlineStatus$.subscribe(isOnline => {
        this.isOffline = !isOnline;
        
        // Start auto-sync when online
        if (isOnline) {
          this.ncrSyncService.startAutoSync();
        } else {
          this.ncrSyncService.stopAutoSync();
        }
      });

      // Subscribe to pending NCR count (only after initialization)
      this.ncrOfflineService.getPendingCount().subscribe({
        next: count => {
          this.pendingNcrCount = count;
        },
        error: err => {
          console.warn('Failed to get pending NCR count:', err);
          this.pendingNcrCount = 0;
        }
      });

      // Update pending count periodically
      setInterval(() => {
        this.ncrOfflineService.getPendingCount().subscribe({
          next: count => {
            this.pendingNcrCount = count;
          },
          error: err => {
            console.warn('Failed to get pending NCR count:', err);
          }
        });
      }, 5000);

      // Start auto-sync if online
      if (this.offlineDetection.isOnline()) {
        this.ncrSyncService.startAutoSync();
      }
    }).catch(err => {
      console.error('Failed to initialize offline service:', err);
      // Continue without offline functionality
      this.pendingNcrCount = 0;
    });
  }

  syncNow(): void {
    this.ncrSyncService.syncPendingNcrs();
  }
}

