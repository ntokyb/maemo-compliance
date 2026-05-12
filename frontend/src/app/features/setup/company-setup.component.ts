import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-company-setup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './company-setup.component.html',
  styleUrl: './company-setup.component.scss'
})
export class CompanySetupComponent implements OnInit {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  step = 1;
  companyName = '';
  firstName = '';
  saving = false;
  error: string | null = null;

  stdIso9001 = false;
  stdIso14001 = false;
  stdIso45001 = false;
  stdIso27001 = false;
  stdPopia = false;
  stdOther = false;

  inviteEntries: { email: string; role: string }[] = [{ email: '', role: 'Viewer' }];

  profileForm = this.fb.nonNullable.group({
    city: [''],
    province: [''],
    logoUrl: ['']
  });

  ngOnInit(): void {
    const u = this.auth.getUser();
    this.companyName = u?.tenant?.name ?? '';
    this.firstName = u?.firstName ?? u?.fullName?.split(' ')[0] ?? 'there';
    setTimeout(() => {
      if (this.step === 1) {
        this.step = 2;
      }
    }, 1600);
  }

  continueProfile(): void {
    this.error = null;
    this.saving = true;
    const v = this.profileForm.getRawValue();
    this.http
      .post(`${environment.apiBaseUrl}/api/onboarding/company-profile`, {
        logoUrl: v.logoUrl || null,
        city: v.city || null,
        province: v.province || null
      })
      .subscribe({
        next: () => {
          this.saving = false;
          this.step = 3;
        },
        error: (err) => {
          this.saving = false;
          this.error = err?.error?.message || 'Could not save profile.';
        }
      });
  }

  selectedStandards(): string[] {
    const s: string[] = [];
    if (this.stdIso9001) {
      s.push('ISO 9001');
    }
    if (this.stdIso14001) {
      s.push('ISO 14001');
    }
    if (this.stdIso45001) {
      s.push('ISO 45001');
    }
    if (this.stdIso27001) {
      s.push('ISO 27001');
    }
    if (this.stdPopia) {
      s.push('POPIA');
    }
    if (this.stdOther) {
      s.push('Other');
    }
    return s;
  }

  continueStandards(): void {
    const standards = this.selectedStandards();
    if (standards.length === 0) {
      this.error = 'Select at least one focus area.';
      return;
    }
    this.error = null;
    this.saving = true;
    this.http.post(`${environment.apiBaseUrl}/api/onboarding/target-standards`, { standards }).subscribe({
      next: () => {
        this.saving = false;
        this.step = 4;
      },
      error: (err) => {
        this.saving = false;
        this.error = err?.error?.message || 'Could not save.';
      }
    });
  }

  skipInvites(): void {
    this.finishWizard();
  }

  sendInvites(): void {
    const emails = this.inviteEntries
      .map((e) => ({ email: e.email.trim(), role: e.role || 'Viewer' }))
      .filter((e) => e.email.length > 0);

    if (emails.length === 0) {
      this.finishWizard();
      return;
    }

    this.error = null;
    this.saving = true;
    this.http.post(`${environment.apiBaseUrl}/api/onboarding/invite-team`, { emails }).subscribe({
      next: () => {
        this.saving = false;
        this.finishWizard();
      },
      error: (err) => {
        this.saving = false;
        this.error = err?.error?.message || 'Could not send invites.';
      }
    });
  }

  addInviteRow(): void {
    if (this.inviteEntries.length >= 5) {
      return;
    }
    this.inviteEntries = [...this.inviteEntries, { email: '', role: 'Viewer' }];
  }

  removeInviteRow(i: number): void {
    if (this.inviteEntries.length <= 1) {
      return;
    }
    this.inviteEntries = this.inviteEntries.filter((_, idx) => idx !== i);
  }

  finishWizard(): void {
    this.saving = true;
    this.http.post(`${environment.apiBaseUrl}/api/onboarding/complete`, {}).subscribe({
      next: () => {
        this.saving = false;
        this.step = 5;
      },
      error: (err) => {
        this.saving = false;
        this.error = err?.error?.message || 'Could not finish setup.';
      }
    });
  }

  goDashboard(): void {
    void this.router.navigate(['/dashboard']);
  }
}
