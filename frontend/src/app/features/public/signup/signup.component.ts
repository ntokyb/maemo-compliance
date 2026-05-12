import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { PublicHeaderComponent } from '../public-header/public-header.component';
import { PublicFooterComponent } from '../public-footer/public-footer.component';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PublicHeaderComponent, PublicFooterComponent],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.scss'
})
export class SignupComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);

  step: 1 | 2 | 3 = 1;
  submitting = false;
  error: string | null = null;
  successMessage: string | null = null;
  requiresReview = false;

  industries = [
    'Manufacturing',
    'Construction',
    'Healthcare',
    'Food & Beverage',
    'Professional Services',
    'Mining & Resources',
    'Education',
    'Retail',
    'Other'
  ];

  companySizes = ['1-10', '11-50', '51-200', '200+'];

  step1 = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(120)]],
    lastName: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', [Validators.required]]
  });

  step2 = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],
    industry: ['Manufacturing', Validators.required],
    companySize: ['1-10', Validators.required],
    iso9001: [true],
    iso14001: [false],
    iso45001: [false],
    iso27001: [false],
    popia: [false],
    other: [false]
  });

  selectedPlan: 'Starter' | 'Growth' | null = null;

  targetStandards(): string[] {
    const v = this.step2.getRawValue();
    const o: string[] = [];
    if (v.iso9001) {
      o.push('ISO 9001');
    }
    if (v.iso14001) {
      o.push('ISO 14001');
    }
    if (v.iso45001) {
      o.push('ISO 45001');
    }
    if (v.iso27001) {
      o.push('ISO 27001');
    }
    if (v.popia) {
      o.push('POPIA');
    }
    if (v.other) {
      o.push('Other');
    }
    return o;
  }

  nextFrom1(): void {
    if (this.step1.invalid) {
      this.step1.markAllAsTouched();
      return;
    }
    const s = this.step1.getRawValue();
    if (s.password !== s.confirm) {
      this.error = 'Passwords do not match.';
      return;
    }
    if (!/[A-Z]/.test(s.password) || !/\d/.test(s.password)) {
      this.error = 'Password needs one uppercase letter and one number.';
      return;
    }
    this.error = null;
    this.step = 2;
  }

  nextFrom2(): void {
    if (this.step2.invalid) {
      this.step2.markAllAsTouched();
      return;
    }
    const std = this.targetStandards();
    if (std.length === 0) {
      this.error = 'Select at least one compliance focus area.';
      return;
    }
    this.error = null;
    this.step = 3;
  }

  choosePlan(plan: 'Starter' | 'Growth'): void {
    this.selectedPlan = plan;
  }

  submit(): void {
    if (!this.selectedPlan) {
      this.error = 'Choose a plan to continue.';
      return;
    }
    const a = this.step1.getRawValue();
    const b = this.step2.getRawValue();
    this.error = null;
    this.submitting = true;
    const body = {
      companyName: b.companyName.trim(),
      firstName: a.firstName.trim(),
      lastName: a.lastName.trim(),
      email: a.email.trim(),
      password: a.password,
      plan: this.selectedPlan,
      industry: b.industry,
      companySize: b.companySize,
      targetStandards: this.targetStandards()
    };
    this.http.post(`${environment.apiBaseUrl}/api/auth/register`, body).subscribe({
      next: (res: any) => {
        this.submitting = false;
        this.requiresReview = !!res?.requiresReview;
        this.successMessage = res?.message ?? 'Check your email to continue.';
      },
      error: (err) => {
        this.submitting = false;
        if (err.status === 409) {
          this.error = 'An account or pending request already exists for this email.';
        } else {
          this.error = err.error?.message || err.message || 'Signup failed.';
        }
      }
    });
  }
}
