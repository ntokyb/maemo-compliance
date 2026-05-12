import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { PublicHeaderComponent } from '../public-header/public-header.component';
import { PublicFooterComponent } from '../public-footer/public-footer.component';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PublicHeaderComponent, PublicFooterComponent],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.scss'
})
export class SignupComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);

  industries = [
    'Manufacturing',
    'Construction',
    'Healthcare',
    'Food & Beverage',
    'Professional Services',
    'Mining & Resources',
    'Other'
  ];

  companySizes = ['1-10', '11-50', '51-200', '200+'];

  roles = [
    'Compliance Manager',
    'Quality Manager',
    'Operations Manager',
    'Director/CEO',
    'Consultant',
    'Other'
  ];

  standardsOptions = ['ISO 9001', 'ISO 14001', 'ISO 45001', 'ISO 27001', 'Other'];

  referralOptions = ['Google', 'LinkedIn', 'Referral', 'WhatsApp', 'Other'];

  submitting = false;
  error: string | null = null;
  success = false;

  form = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],
    industry: ['Manufacturing', Validators.required],
    companySize: ['1-10', Validators.required],
    contactName: ['', [Validators.required, Validators.maxLength(200)]],
    contactEmail: ['', [Validators.required, Validators.email, Validators.maxLength(250)]],
    contactRole: ['Compliance Manager', Validators.required],
    referralSource: ['Google', Validators.required],
    iso9001: [false],
    iso14001: [false],
    iso45001: [false],
    iso27001: [false],
    isoOther: [false]
  });

  targetStandards(): string[] {
    const v = this.form.getRawValue();
    const out: string[] = [];
    if (v.iso9001) out.push('ISO 9001');
    if (v.iso14001) out.push('ISO 14001');
    if (v.iso45001) out.push('ISO 45001');
    if (v.iso27001) out.push('ISO 27001');
    if (v.isoOther) out.push('Other');
    return out;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const standards = this.targetStandards();
    if (standards.length === 0) {
      this.error = 'Select at least one ISO standard you are targeting.';
      return;
    }
    this.error = null;
    this.submitting = true;
    const v = this.form.getRawValue();
    const body = {
      companyName: v.companyName.trim(),
      industry: v.industry,
      companySize: v.companySize,
      contactName: v.contactName.trim(),
      contactEmail: v.contactEmail.trim(),
      contactRole: v.contactRole,
      targetStandards: standards,
      referralSource: v.referralSource
    };

    this.http.post(`${environment.apiBaseUrl}/api/public/request-access`, body).subscribe({
      next: () => {
        this.submitting = false;
        this.success = true;
      },
      error: (err) => {
        this.submitting = false;
        if (err.status === 409) {
          this.error = 'A pending request already exists for this email.';
        } else if (err.status === 429) {
          this.error = 'Too many requests. Please try again later.';
        } else {
          this.error = err.error?.message || err.message || 'Request failed. Please try again.';
        }
      }
    });
  }
}
