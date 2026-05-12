import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { RisksService } from '../../../services/risks.service';
import { Risk, RiskCategory, RiskStatus } from '../../../models/risk.model';

@Component({
  selector: 'app-risk-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './risk-list.html',
  styleUrl: './risk-list.scss'
})
export class RiskListComponent implements OnInit {
  private risksService = inject(RisksService);
  private router = inject(Router);

  risks: Risk[] = [];
  loading = false;
  error: string | null = null;

  // Filters
  selectedCategory: number | null = null;
  selectedStatus: number | null = null;

  RiskCategory = RiskCategory;
  RiskStatus = RiskStatus;

  categoryOptions = [
    { value: null, label: 'All Categories' },
    { value: RiskCategory.Operational, label: 'Operational' },
    { value: RiskCategory.Financial, label: 'Financial' },
    { value: RiskCategory.Compliance, label: 'Compliance' },
    { value: RiskCategory.HealthSafety, label: 'Health & Safety' },
    { value: RiskCategory.InformationSecurity, label: 'Information Security' }
  ];

  statusOptions = [
    { value: null, label: 'All Statuses' },
    { value: RiskStatus.Identified, label: 'Identified' },
    { value: RiskStatus.Analysed, label: 'Analysed' },
    { value: RiskStatus.Mitigated, label: 'Mitigated' },
    { value: RiskStatus.Accepted, label: 'Accepted' },
    { value: RiskStatus.Closed, label: 'Closed' }
  ];

  ngOnInit(): void {
    this.loadRisks();
  }

  loadRisks(): void {
    this.loading = true;
    this.error = null;

    const params: any = {};
    if (this.selectedCategory !== null) {
      params.category = this.selectedCategory;
    }
    if (this.selectedStatus !== null) {
      params.status = this.selectedStatus;
    }

    this.risksService.getRisks(params).subscribe({
      next: (risks) => {
        this.risks = risks;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load risks';
        this.loading = false;
        console.error('Error loading risks:', err);
      }
    });
  }

  onFilterChange(): void {
    this.loadRisks();
  }

  getCategoryLabel(category: RiskCategory): string {
    switch (category) {
      case RiskCategory.Operational:
        return 'Operational';
      case RiskCategory.Financial:
        return 'Financial';
      case RiskCategory.Compliance:
        return 'Compliance';
      case RiskCategory.HealthSafety:
        return 'Health & Safety';
      case RiskCategory.InformationSecurity:
        return 'Information Security';
      default:
        return 'Unknown';
    }
  }

  getStatusLabel(status: RiskStatus): string {
    switch (status) {
      case RiskStatus.Identified:
        return 'Identified';
      case RiskStatus.Analysed:
        return 'Analysed';
      case RiskStatus.Mitigated:
        return 'Mitigated';
      case RiskStatus.Accepted:
        return 'Accepted';
      case RiskStatus.Closed:
        return 'Closed';
      default:
        return 'Unknown';
    }
  }

  getRiskLevelClass(riskLevel?: string): string {
    if (!riskLevel) return '';
    switch (riskLevel.toLowerCase()) {
      case 'low':
        return 'risk-level-low';
      case 'medium':
        return 'risk-level-medium';
      case 'high':
        return 'risk-level-high';
      case 'critical':
        return 'risk-level-critical';
      default:
        return '';
    }
  }

  editRisk(id: string): void {
    this.router.navigate(['/risks', id]);
  }

  createNew(): void {
    this.router.navigate(['/risks/new']);
  }
}

