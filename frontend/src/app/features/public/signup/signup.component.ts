import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.scss'
})
export class SignupComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  step = 1;
  submitting = false;
  error: string | null = null;
  successEmail: string | null = null;

  industries = ['Manufacturing', 'Healthcare', 'Financial Services', 'Government', 'Other'];

  companyForm = this.fb.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],
    industry: ['Manufacturing', Validators.required],
    plan: ['Starter', Validators.required],
    iso9001: [true],
    iso14001: [false],
    iso27001: [false],
    iso45001: [false],
    iso31000: [false]
  });

  adminForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(250)]]
  });

  nextFromCompany(): void {
    if (this.companyForm.invalid) {
      this.companyForm.markAllAsTouched();
      return;
    }
    if (this.companyForm.value.plan === 'GovOnPrem') {
      this.error = 'Government / on-premise deployments are provisioned by the Maemo team. Please contact sales.';
      return;
    }
    this.error = null;
    this.step = 2;
  }

  nextFromAdmin(): void {
    if (this.adminForm.invalid) {
      this.adminForm.markAllAsTouched();
      return;
    }
    this.error = null;
    this.step = 3;
  }

  back(): void {
    this.error = null;
    if (this.step > 1) {
      this.step--;
    }
  }

  isoList(): string[] {
    const c = this.companyForm.value;
    const keys: string[] = [];
    if (c.iso9001) keys.push('ISO 9001');
    if (c.iso14001) keys.push('ISO 14001');
    if (c.iso27001) keys.push('ISO 27001');
    if (c.iso45001) keys.push('ISO 45001');
    if (c.iso31000) keys.push('ISO 31000');
    return keys;
  }

  submit(): void {
    if (this.companyForm.invalid || this.adminForm.invalid) {
      return;
    }
    this.submitting = true;
    this.error = null;

    const body = {
      companyName: this.companyForm.value.companyName!.trim(),
      adminEmail: this.adminForm.value.email!.trim(),
      adminFirstName: this.adminForm.value.firstName!.trim(),
      adminLastName: this.adminForm.value.lastName!.trim(),
      industry: this.companyForm.value.industry!,
      plan: this.companyForm.value.plan!,
      isoFrameworks: this.isoList()
    };

    this.http
      .post<{ tenantId: string; message: string; nextStep: string }>(
        `${environment.apiBaseUrl}/public/signup`,
        body
      )
      .subscribe({
        next: () => {
          this.submitting = false;
          this.successEmail = this.adminForm.value.email!.trim();
        },
        error: (err) => {
          this.submitting = false;
          this.error =
            err.error?.message || err.message || 'Signup failed. Please try again or contact support.';
        }
      });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
