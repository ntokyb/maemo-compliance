import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { RisksService } from '../../../services/risks.service';
import { RiskCategory, RiskStatus, CreateRiskRequest, UpdateRiskRequest } from '../../../models/risk.model';

@Component({
  selector: 'app-risk-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './risk-form.html',
  styleUrl: './risk-form.scss'
})
export class RiskFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private risksService = inject(RisksService);

  riskForm!: FormGroup;
  riskId: string | null = null;
  isEditMode = false;
  loading = false;
  saving = false;
  error: string | null = null;

  RiskCategory = RiskCategory;
  RiskStatus = RiskStatus;

  categoryOptions = [
    { value: RiskCategory.Operational, label: 'Operational' },
    { value: RiskCategory.Financial, label: 'Financial' },
    { value: RiskCategory.Compliance, label: 'Compliance' },
    { value: RiskCategory.HealthSafety, label: 'Health & Safety' },
    { value: RiskCategory.InformationSecurity, label: 'Information Security' }
  ];

  statusOptions = [
    { value: RiskStatus.Identified, label: 'Identified' },
    { value: RiskStatus.Analysed, label: 'Analysed' },
    { value: RiskStatus.Mitigated, label: 'Mitigated' },
    { value: RiskStatus.Accepted, label: 'Accepted' },
    { value: RiskStatus.Closed, label: 'Closed' }
  ];

  likelihoodOptions = [1, 2, 3, 4, 5];
  impactOptions = [1, 2, 3, 4, 5];

  ngOnInit(): void {
    this.riskId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.riskId;

    this.initForm();

    if (this.isEditMode && this.riskId) {
      this.loadRisk(this.riskId);
    }
  }

  initForm(): void {
    this.riskForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.required]],
      category: [RiskCategory.Operational, Validators.required],
      cause: [''],
      consequences: [''],
      inherentLikelihood: [1, [Validators.required, Validators.min(1), Validators.max(5)]],
      inherentImpact: [1, [Validators.required, Validators.min(1), Validators.max(5)]],
      existingControls: [''],
      residualLikelihood: [1, [Validators.required, Validators.min(1), Validators.max(5)]],
      residualImpact: [1, [Validators.required, Validators.min(1), Validators.max(5)]],
      ownerUserId: [''],
      status: [RiskStatus.Identified, Validators.required]
    });
  }

  loadRisk(id: string): void {
    this.loading = true;
    this.risksService.getRisk(id).subscribe({
      next: (risk) => {
        this.riskForm.patchValue({
          title: risk.title,
          description: risk.description,
          category: risk.category,
          cause: risk.cause || '',
          consequences: risk.consequences || '',
          inherentLikelihood: risk.inherentLikelihood,
          inherentImpact: risk.inherentImpact,
          existingControls: risk.existingControls || '',
          residualLikelihood: risk.residualLikelihood,
          residualImpact: risk.residualImpact,
          ownerUserId: risk.ownerUserId || '',
          status: risk.status
        });
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load risk';
        this.loading = false;
        console.error('Error loading risk:', err);
      }
    });
  }

  onSubmit(): void {
    this.riskForm.markAllAsTouched();
    if (this.riskForm.invalid) {
      return;
    }

    this.saving = true;
    this.error = null;

    const formValue = this.riskForm.value;

    if (this.isEditMode && this.riskId) {
      const request: UpdateRiskRequest = {
        title: formValue.title,
        description: formValue.description,
        category: formValue.category,
        cause: formValue.cause || undefined,
        consequences: formValue.consequences || undefined,
        inherentLikelihood: formValue.inherentLikelihood,
        inherentImpact: formValue.inherentImpact,
        existingControls: formValue.existingControls || undefined,
        residualLikelihood: formValue.residualLikelihood,
        residualImpact: formValue.residualImpact,
        ownerUserId: formValue.ownerUserId || undefined,
        status: formValue.status
      };

      this.risksService.updateRisk(this.riskId, request).subscribe({
        next: () => {
          this.saving = false;
          this.router.navigate(['/risks']);
        },
        error: (err) => {
          this.error = err.message || 'Failed to update risk';
          this.saving = false;
          console.error('Error updating risk:', err);
        }
      });
    } else {
      const request: CreateRiskRequest = {
        title: formValue.title,
        description: formValue.description,
        category: formValue.category,
        cause: formValue.cause || undefined,
        consequences: formValue.consequences || undefined,
        inherentLikelihood: formValue.inherentLikelihood,
        inherentImpact: formValue.inherentImpact,
        existingControls: formValue.existingControls || undefined,
        residualLikelihood: formValue.residualLikelihood,
        residualImpact: formValue.residualImpact,
        ownerUserId: formValue.ownerUserId || undefined,
        status: formValue.status || RiskStatus.Identified
      };

      this.risksService.createRisk(request).subscribe({
        next: () => {
          this.saving = false;
          this.router.navigate(['/risks']);
        },
        error: (err) => {
          this.error = err.message || 'Failed to create risk';
          this.saving = false;
          console.error('Error creating risk:', err);
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/risks']);
  }
}

