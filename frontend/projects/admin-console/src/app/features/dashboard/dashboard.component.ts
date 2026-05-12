import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminDashboardSummaryDto } from '../../core/models/admin-dashboard.dto';
import { DashboardAdminService } from '../../core/services/dashboard-admin.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  summary: AdminDashboardSummaryDto | null = null;
  loading = true;
  error: string | null = null;
  private toastService = inject(ToastService);

  constructor(private dashboardService: DashboardAdminService) {}

  ngOnInit(): void {
    this.loadDashboardSummary();
  }

  loadDashboardSummary(): void {
    this.loading = true;
    this.error = null;

    this.dashboardService.getDashboardSummary().subscribe({
      next: (data) => {
        this.summary = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load dashboard summary';
        this.loading = false;
        this.toastService.error('Failed to load dashboard summary');
        console.error('Dashboard error:', err);
      }
    });
  }
}

