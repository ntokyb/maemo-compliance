import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PopiaAdminService } from '../../core/services/popia-admin.service';
import { PopiaDocumentSummaryDto } from '../../core/models/popia-document-summary.dto';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-popia',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './popia.component.html',
  styleUrl: './popia.component.scss'
})
export class PopiaComponent implements OnInit {
  private popiaService = inject(PopiaAdminService);
  private toastService = inject(ToastService);

  summary: PopiaDocumentSummaryDto | null = null;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.loadSummary();
  }

  loadSummary(): void {
    this.loading = true;
    this.error = null;

    this.popiaService.getDocumentSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.error || err.message || 'Failed to load POPIA summary';
        this.loading = false;
        this.toastService.show(this.error ?? 'Failed to load POPIA summary', 'error');
        console.error('Error loading POPIA summary:', err);
      }
    });
  }
}

