import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ToastService } from '../../../services/toast.service';

interface AccessRequestRow {
  id: string;
  companyName: string;
  industry: string;
  contactName: string;
  contactEmail: string;
  targetStandardsSummary: string;
  createdAt: string;
  status: string;
}

@Component({
  selector: 'app-access-requests-admin',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './access-requests-admin.component.html',
  styleUrl: './access-requests-admin.component.scss'
})
export class AccessRequestsAdminComponent implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  private toast = inject(ToastService);

  loading = false;
  rows: AccessRequestRow[] = [];
  filter: 'All' | 'Pending' | 'Approved' | 'Rejected' = 'All';

  filterOptions = ['All', 'Pending', 'Approved', 'Rejected'] as const;

  approveFor: AccessRequestRow | null = null;
  rejectFor: AccessRequestRow | null = null;

  approveForm = this.fb.nonNullable.group({
    companyName: [''],
    plan: ['Starter' as 'Starter' | 'Growth' | 'Enterprise']
  });

  rejectForm = this.fb.nonNullable.group({
    reason: ['']
  });

  plans = ['Starter', 'Growth', 'Enterprise'] as const;

  ngOnInit(): void {
    this.load();
  }

  setFilter(f: 'All' | 'Pending' | 'Approved' | 'Rejected'): void {
    this.filter = f;
    this.load();
  }

  load(): void {
    this.loading = true;
    const q =
      this.filter === 'All'
        ? ''
        : `?status=${encodeURIComponent(this.filter)}`;
    this.http.get<AccessRequestRow[]>(`${environment.apiBaseUrl}/admin/v1/access-requests${q}`).subscribe({
      next: (r) => {
        this.rows = r;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toast.show(err.error?.message || err.message || 'Could not load requests.', 'error');
      }
    });
  }

  openApprove(row: AccessRequestRow): void {
    this.approveFor = row;
    this.approveForm.reset({
      companyName: row.companyName,
      plan: 'Starter'
    });
  }

  openReject(row: AccessRequestRow): void {
    this.rejectFor = row;
    this.rejectForm.reset({ reason: '' });
  }

  closeModals(): void {
    this.approveFor = null;
    this.rejectFor = null;
  }

  confirmApprove(): void {
    if (!this.approveFor) return;
    const v = this.approveForm.getRawValue();
    const id = this.approveFor.id;
    this.http
      .post(`${environment.apiBaseUrl}/admin/v1/access-requests/${id}/approve`, {
        companyName: v.companyName.trim(),
        plan: v.plan
      })
      .subscribe({
        next: () => {
          this.toast.show('Approved — tenant created and invite sent.', 'success');
          this.closeModals();
          this.load();
        },
        error: (err) => {
          this.toast.show(err.error?.message || err.message || 'Approve failed.', 'error');
        }
      });
  }

  confirmReject(): void {
    if (!this.rejectFor) return;
    const id = this.rejectFor.id;
    const reason = this.rejectForm.value.reason?.trim() || null;
    this.http
      .post(`${environment.apiBaseUrl}/admin/v1/access-requests/${id}/reject`, { reason })
      .subscribe({
        next: () => {
          this.toast.show('Request rejected.', 'success');
          this.closeModals();
          this.load();
        },
        error: (err) => {
          this.toast.show(err.error?.message || err.message || 'Reject failed.', 'error');
        }
      });
  }
}
