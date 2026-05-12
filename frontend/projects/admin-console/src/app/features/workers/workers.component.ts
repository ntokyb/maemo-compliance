import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkersAdminService } from '../../core/services/workers-admin.service';
import { ToastService } from '../../core/services/toast.service';
import { AdminWorkerSummaryDto, AdminWorkerJobHistoryItemDto } from '../../core/models/admin-worker.dto';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

@Component({
  selector: 'app-workers',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './workers.component.html',
  styleUrl: './workers.component.scss'
})
export class WorkersComponent implements OnInit {
  workers$: Observable<AdminWorkerSummaryDto[]> = of([]);
  selectedWorker: string | null = null;
  workerHistory$: Observable<AdminWorkerJobHistoryItemDto[]> = of([]);
  loading = true;
  loadingHistory = false;
  error: string | null = null;
  private toastService = inject(ToastService);

  constructor(private workersService: WorkersAdminService) { }

  ngOnInit(): void {
    this.loadWorkers();
  }

  loadWorkers(): void {
    this.loading = true;
    this.error = null;
    this.workers$ = this.workersService.getWorkers().pipe(
      tap(() => this.loading = false),
      catchError(err => {
        this.error = 'Failed to load workers. Please try again.';
        this.loading = false;
        this.toastService.error('Failed to load workers');
        console.error('Workers load error:', err);
        return of([]);
      })
    );
  }

  selectWorker(workerName: string): void {
    if (this.selectedWorker === workerName) {
      // Deselect if clicking the same worker
      this.selectedWorker = null;
      this.workerHistory$ = of([]);
      return;
    }

    this.selectedWorker = workerName;
    this.loadWorkerHistory(workerName);
  }

  loadWorkerHistory(workerName: string): void {
    this.loadingHistory = true;
    this.workerHistory$ = this.workersService.getWorkerHistory(workerName).pipe(
      tap(() => this.loadingHistory = false),
      catchError(err => {
        this.loadingHistory = false;
        console.error('Worker history load error:', err);
        return of([]);
      })
    );
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'success':
        return 'status-success';
      case 'failed':
        return 'status-failed';
      case 'running':
        return 'status-running';
      default:
        return 'status-unknown';
    }
  }

  formatDuration(duration: string | null): string {
    if (!duration) return '-';
    
    // Parse ISO 8601 duration string (e.g., "PT0.123S", "PT1H2M3S")
    try {
      // Simple parser for common formats
      const match = duration.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/);
      if (match) {
        const hours = parseInt(match[1] || '0', 10);
        const minutes = parseInt(match[2] || '0', 10);
        const seconds = parseFloat(match[3] || '0');
        
        const parts: string[] = [];
        if (hours > 0) parts.push(`${hours}h`);
        if (minutes > 0) parts.push(`${minutes}m`);
        if (seconds > 0 || parts.length === 0) {
          parts.push(`${seconds.toFixed(2)}s`);
        }
        
        return parts.join(' ');
      }
    } catch {
      // If parsing fails, return as-is
    }
    
    return duration;
  }
}

