import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { UserProfileService, TenantMeProfile } from '../../../services/user-profile.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-user-onboarding-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './user-onboarding-page.component.html',
  styleUrl: './user-onboarding-page.component.scss'
})
export class UserOnboardingPageComponent implements OnInit {
  private fb = inject(FormBuilder);
  private profileService = inject(UserProfileService);
  private toast = inject(ToastService);
  private router = inject(Router);

  step = 1;
  loading = false;
  loadError: string | null = null;

  standardsOptions = ['ISO 9001', 'ISO 14001', 'ISO 45001', 'ISO 27001', 'Other'];

  step1 = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    jobTitle: [''],
    phone: ['']
  });

  step2 = this.fb.nonNullable.group({
    organisationName: [''],
    logoUrl: [''],
    organisationAddress: ['']
    // primary contact captured in organisationAddress (helper text)
  });

  step3 = this.fb.nonNullable.group({
    iso9001: [false],
    iso14001: [false],
    iso45001: [false],
    iso27001: [false],
    isoOther: [false]
  });

  step4 = this.fb.nonNullable.group({
    e1: [''],
    e2: [''],
    e3: [''],
    e4: [''],
    e5: ['']
  });

  ngOnInit(): void {
    this.profileService.getMe().subscribe({
      next: (p: TenantMeProfile) => {
        const parts = (p.fullName || '').trim().split(/\s+/);
        const first = parts[0] || '';
        const last = parts.length > 1 ? parts.slice(1).join(' ') : '';
        this.step1.patchValue({
          firstName: first,
          lastName: last,
          jobTitle: p.jobTitle || '',
          phone: p.phone || ''
        });
        this.step2.patchValue({
          organisationAddress: p.addressLine || ''
        });
        const st = p.complianceStandards || [];
        this.step3.patchValue({
          iso9001: st.includes('ISO 9001'),
          iso14001: st.includes('ISO 14001'),
          iso45001: st.includes('ISO 45001'),
          iso27001: st.includes('ISO 27001'),
          isoOther: st.includes('Other')
        });
      },
      error: () => {
        this.loadError = 'Could not load your profile. You can still continue.';
      }
    });
  }

  standardsList(): string[] {
    const v = this.step3.getRawValue();
    const o: string[] = [];
    if (v.iso9001) o.push('ISO 9001');
    if (v.iso14001) o.push('ISO 14001');
    if (v.iso45001) o.push('ISO 45001');
    if (v.iso27001) o.push('ISO 27001');
    if (v.isoOther) o.push('Other');
    return o;
  }

  teamEmailsList(): string[] {
    const v = this.step4.getRawValue();
    return [v.e1, v.e2, v.e3, v.e4, v.e5]
      .map((e) => e.trim())
      .filter((e) => e.length > 0);
  }

  next(): void {
    if (this.step === 1 && this.step1.invalid) {
      this.step1.markAllAsTouched();
      return;
    }
    if (this.step === 3) {
      if (this.standardsList().length === 0) {
        this.toast.show('Select at least one standard.', 'error');
        return;
      }
    }
    if (this.step < 5) {
      this.step++;
    }
  }

  back(): void {
    if (this.step > 1) {
      this.step--;
    }
  }

  skipInvites(): void {
    this.finish([]);
  }

  sendInvites(): void {
    this.finish(this.teamEmailsList());
  }

  goDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  private finish(teamEmails: string[]): void {
    if (this.step1.invalid) {
      this.step1.markAllAsTouched();
      this.step = 1;
      return;
    }
    const s = this.standardsList();
    if (s.length === 0) {
      this.step = 3;
      this.toast.show('Select at least one standard.', 'error');
      return;
    }
    const v1 = this.step1.getRawValue();
    const v2 = this.step2.getRawValue();
    this.loading = true;
    this.profileService
      .completeOnboardingWizard({
        firstName: v1.firstName.trim(),
        lastName: v1.lastName.trim(),
        jobTitle: v1.jobTitle.trim() || null,
        phone: v1.phone.trim() || null,
        organisationAddress: v2.organisationAddress.trim() || null,
        organisationName: v2.organisationName.trim() || null,
        logoUrl: v2.logoUrl.trim() || null,
        standards: s,
        teamEmails
      })
      .subscribe({
        next: () => {
          this.loading = false;
          this.step = 5;
          this.toast.show('Onboarding complete.', 'success');
        },
        error: (err: unknown) => {
          this.loading = false;
          const msg =
            err && typeof err === 'object' && 'error' in err
              ? (err as { error?: { message?: string } }).error?.message
              : undefined;
          const fallback = err instanceof Error ? err.message : 'Could not save.';
          this.toast.show(msg || fallback, 'error');
        }
      });
  }
}
