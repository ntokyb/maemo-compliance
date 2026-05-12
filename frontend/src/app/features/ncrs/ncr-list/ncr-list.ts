import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NcrService } from '../../../services/ncr.service';
import { Ncr, NcrSeverity, NcrStatus } from '../../../models/ncr.model';

@Component({
  selector: 'app-ncr-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './ncr-list.html',
  styleUrl: './ncr-list.scss'
})
export class NcrListComponent implements OnInit {
  private ncrService = inject(NcrService);
  private router = inject(Router);

  ncrs: Ncr[] = [];
  loading = false;
  error: string | null = null;

  // Filters
  selectedStatus: number | null = null;
  selectedSeverity: number | null = null;

  NcrSeverity = NcrSeverity;
  NcrStatus = NcrStatus;

  statusOptions = [
    { value: null, label: 'All Statuses' },
    { value: NcrStatus.Open, label: 'Open' },
    { value: NcrStatus.InProgress, label: 'In Progress' },
    { value: NcrStatus.Closed, label: 'Closed' }
  ];

  severityOptions = [
    { value: null, label: 'All Severities' },
    { value: NcrSeverity.Low, label: 'Low' },
    { value: NcrSeverity.Medium, label: 'Medium' },
    { value: NcrSeverity.High, label: 'High' }
  ];

  ngOnInit(): void {
    this.loadNcrs();
  }

  loadNcrs(): void {
    this.loading = true;
    this.error = null;

    const params: any = {};
    if (this.selectedStatus !== null) {
      params.status = this.selectedStatus;
    }
    if (this.selectedSeverity !== null) {
      params.severity = this.selectedSeverity;
    }

    this.ncrService.getNcrs(params).subscribe({
      next: (ncrs) => {
        this.ncrs = ncrs;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load NCRs';
        this.loading = false;
        console.error('Error loading NCRs:', err);
      }
    });
  }

  onFilterChange(): void {
    this.loadNcrs();
  }

  getSeverityLabel(severity: NcrSeverity): string {
    switch (severity) {
      case NcrSeverity.Low:
        return 'Low';
      case NcrSeverity.Medium:
        return 'Medium';
      case NcrSeverity.High:
        return 'High';
      default:
        return 'Unknown';
    }
  }

  getStatusLabel(status: NcrStatus): string {
    switch (status) {
      case NcrStatus.Open:
        return 'Open';
      case NcrStatus.InProgress:
        return 'In Progress';
      case NcrStatus.Closed:
        return 'Closed';
      default:
        return 'Unknown';
    }
  }

  formatDate(dateString?: string): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString();
  }

  isOverdue(ncr: Ncr): boolean {
    if (ncr.status === NcrStatus.Closed || !ncr.dueDate) {
      return false;
    }
    const dueDate = new Date(ncr.dueDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return dueDate < today;
  }

  updateStatus(id: string): void {
    this.router.navigate(['/ncrs', id, 'status']);
  }

  createNew(): void {
    this.router.navigate(['/ncrs/new']);
  }
}

