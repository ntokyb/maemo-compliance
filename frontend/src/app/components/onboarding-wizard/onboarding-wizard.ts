import { Component, OnInit, inject, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OnboardingService } from '../../services/onboarding.service';
import { ToastService } from '../../services/toast.service';
import { ISO_STANDARDS, INDUSTRIES, COMPANY_SIZES, OnboardingRequest } from '../../models/onboarding.model';

@Component({
  selector: 'app-onboarding-wizard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './onboarding-wizard.html',
  styleUrl: './onboarding-wizard.scss'
})
export class OnboardingWizardComponent implements OnInit {
  private onboardingService = inject(OnboardingService);
  private toastService = inject(ToastService);

  @Output() onComplete = new EventEmitter<void>();

  currentStep = 1;
  totalSteps = 3;
  loading = false;

  // Step 1: ISO Standards
  isoStandards = ISO_STANDARDS;
  selectedIsoStandards: string[] = [];

  // Step 2: Industry
  industries = INDUSTRIES;
  selectedIndustry = '';

  // Step 3: Company Size
  companySizes = COMPANY_SIZES;
  selectedCompanySize = '';

  ngOnInit(): void {
    // Wizard starts at step 1
  }

  toggleIsoStandard(standard: string): void {
    const index = this.selectedIsoStandards.indexOf(standard);
    if (index > -1) {
      this.selectedIsoStandards.splice(index, 1);
    } else {
      this.selectedIsoStandards.push(standard);
    }
  }

  isIsoStandardSelected(standard: string): boolean {
    return this.selectedIsoStandards.includes(standard);
  }

  canProceedToNextStep(): boolean {
    switch (this.currentStep) {
      case 1:
        return this.selectedIsoStandards.length > 0;
      case 2:
        return !!this.selectedIndustry;
      case 3:
        return !!this.selectedCompanySize;
      default:
        return false;
    }
  }

  nextStep(): void {
    if (this.canProceedToNextStep() && this.currentStep < this.totalSteps) {
      this.currentStep++;
    }
  }

  previousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  completeOnboarding(): void {
    if (!this.canProceedToNextStep()) {
      return;
    }

    this.loading = true;

    const request: OnboardingRequest = {
      isoStandards: this.selectedIsoStandards,
      industry: this.selectedIndustry,
      companySize: this.selectedCompanySize
    };

    this.onboardingService.runOnboarding(request).subscribe({
      next: () => {
        this.loading = false;
        this.toastService.show('Onboarding completed successfully!', 'success');
        // Emit completion event
        this.onComplete.emit();
      },
      error: (err) => {
        this.loading = false;
        const errorMessage = err.error?.error || err.message || 'Failed to complete onboarding';
        this.toastService.show(errorMessage, 'error');
      }
    });
  }

  close(): void {
    // Prevent closing during onboarding - user must complete it
    // Only allow closing if not on first step (for testing/debugging)
    if (this.currentStep === 1) {
      return;
    }
    // Emit completion event to parent
    this.onComplete.emit();
  }

  getStepTitle(): string {
    switch (this.currentStep) {
      case 1:
        return 'Choose ISO Standards';
      case 2:
        return 'Choose Industry';
      case 3:
        return 'Choose Company Size';
      default:
        return '';
    }
  }

  getStepDescription(): string {
    switch (this.currentStep) {
      case 1:
        return 'Select the ISO standards that apply to your organization. You can select multiple standards.';
      case 2:
        return 'Select the industry your organization operates in.';
      case 3:
        return 'Select the size of your organization.';
      default:
        return '';
    }
  }
}

