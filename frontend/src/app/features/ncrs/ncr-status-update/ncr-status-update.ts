import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { NcrService } from '../../../services/ncr.service';
import { RisksService } from '../../../services/risks.service';
import { NcrStatus, UpdateNcrStatusRequest } from '../../../models/ncr.model';
import { Risk } from '../../../models/risk.model';

@Component({
  selector: 'app-ncr-status-update',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './ncr-status-update.html',
  styleUrl: './ncr-status-update.scss'
})
export class NcrStatusUpdateComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private ncrService = inject(NcrService);
  private risksService = inject(RisksService);

  statusForm!: FormGroup;
  ncrId: string | null = null;
  ncrTitle: string = '';
  loading = false;
  saving = false;
  error: string | null = null;

  linkedRisks: Risk[] = [];
  /** Risks not yet linked to this NCR (for dropdown). */
  availableRisks: Risk[] = [];
  private allRisks: Risk[] = [];
  selectedRiskIdForLink = '';
  loadingRisks = false;
  linkingRisk = false;

  NcrStatus = NcrStatus;

  statusOptions = [
    { value: NcrStatus.Open, label: 'Open' },
    { value: NcrStatus.InProgress, label: 'In Progress' },
    { value: NcrStatus.Closed, label: 'Closed' }
  ];

  ngOnInit(): void {
    this.ncrId = this.route.snapshot.paramMap.get('id');
    
    if (!this.ncrId) {
      this.error = 'NCR ID is required';
      return;
    }

    this.statusForm = this.fb.group({
      status: ['', Validators.required],
      dueDate: [''],
      closedAt: ['']
    });

    this.loadNcr();
  }

  loadNcr(): void {
    if (!this.ncrId) return;

    this.loading = true;
    this.error = null;

    this.ncrService.getNcr(this.ncrId).subscribe({
      next: (ncr) => {
        this.ncrTitle = ncr.title;
        const dueDate = ncr.dueDate ? new Date(ncr.dueDate).toISOString().split('T')[0] : '';
        const closedAt = ncr.closedAt ? new Date(ncr.closedAt).toISOString().split('T')[0] : '';

        this.statusForm.patchValue({
          status: ncr.status,
          dueDate: dueDate,
          closedAt: closedAt
        });

        this.loading = false;
        this.loadRiskLinkData();
      },
      error: (err) => {
        this.error = err.message || 'Failed to load NCR';
        this.loading = false;
        console.error('Error loading NCR:', err);
      }
    });
  }

  private loadRiskLinkData(): void {
    if (!this.ncrId) {
      return;
    }
    this.loadingRisks = true;
    forkJoin({
      linked: this.ncrService.getRisksForNcr(this.ncrId),
      all: this.risksService.getRisks(),
    }).subscribe({
      next: ({ linked, all }) => {
        this.linkedRisks = linked;
        this.allRisks = all;
        this.recomputeAvailableRisks();
        this.loadingRisks = false;
      },
      error: (err) => {
        console.error('Error loading risks for NCR link UI:', err);
        this.loadingRisks = false;
      },
    });
  }

  private recomputeAvailableRisks(): void {
    const linkedIds = new Set((this.linkedRisks ?? []).map((r) => r.id).filter(Boolean) as string[]);
    this.availableRisks = (this.allRisks ?? []).filter((r) => r.id && !linkedIds.has(r.id));
  }

  linkRisk(): void {
    if (!this.ncrId || !this.selectedRiskIdForLink) {
      return;
    }
    this.linkingRisk = true;
    this.ncrService.linkNcrToRisk(this.ncrId, this.selectedRiskIdForLink).subscribe({
      next: () => {
        this.selectedRiskIdForLink = '';
        this.linkingRisk = false;
        this.loadRiskLinkData();
      },
      error: (err) => {
        console.error('Error linking risk:', err);
        this.linkingRisk = false;
      }
    });
  }

  unlinkRisk(riskId: string): void {
    if (!this.ncrId) {
      return;
    }
    this.linkingRisk = true;
    this.ncrService.unlinkNcrFromRisk(this.ncrId, riskId).subscribe({
      next: () => {
        this.linkingRisk = false;
        this.loadRiskLinkData();
      },
      error: (err) => {
        console.error('Error unlinking risk:', err);
        this.linkingRisk = false;
      }
    });
  }

  onSubmit(): void {
    if (this.statusForm.invalid || !this.ncrId) {
      return;
    }

    this.saving = true;
    this.error = null;

    const formValue = this.statusForm.value;
    
    const updateRequest: UpdateNcrStatusRequest = {
      status: formValue.status,
      dueDate: formValue.dueDate ? new Date(formValue.dueDate).toISOString() : undefined,
      closedAt: formValue.closedAt ? new Date(formValue.closedAt).toISOString() : undefined
    };

    this.ncrService.updateNcrStatus(this.ncrId, updateRequest).subscribe({
      next: () => {
        this.router.navigate(['/ncrs']);
      },
      error: (err) => {
        this.error = err.message || 'Failed to update NCR status';
        this.saving = false;
        console.error('Error updating NCR status:', err);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/ncrs']);
  }
}

