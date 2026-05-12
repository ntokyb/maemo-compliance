import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NcrOfflineService } from '../../../services/ncr-offline.service';
import { OfflineDetectionService } from '../../../services/offline-detection.service';
import { NcrSeverity, CreateNcrRequest } from '../../../models/ncr.model';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-ncr-form-offline',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './ncr-form-offline.html',
  styleUrl: './ncr-form-offline.scss'
})
export class NcrFormOfflineComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private ncrOfflineService = inject(NcrOfflineService);
  private offlineDetection = inject(OfflineDetectionService);
  private toastService = inject(ToastService);

  ncrForm!: FormGroup;
  saving = false;
  error: string | null = null;
  isOffline = false;

  NcrSeverity = NcrSeverity;

  severityOptions = [
    { value: NcrSeverity.Low, label: 'Low' },
    { value: NcrSeverity.Medium, label: 'Medium' },
    { value: NcrSeverity.High, label: 'High' }
  ];

  ngOnInit(): void {
    // Initialize IndexedDB
    this.ncrOfflineService.init().catch(err => {
      console.error('Failed to initialize offline storage:', err);
      this.error = 'Failed to initialize offline storage';
    });

    // Check online status
    this.offlineDetection.onlineStatus$.subscribe(isOnline => {
      this.isOffline = !isOnline;
    });

    this.ncrForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.required]],
      department: ['', [Validators.maxLength(100)]],
      ownerUserId: [''],
      severity: [NcrSeverity.Medium, Validators.required],
      dueDate: ['']
    });
  }

  onSubmit(): void {
    if (this.ncrForm.invalid) {
      this.markFormGroupTouched(this.ncrForm);
      return;
    }

    this.saving = true;
    this.error = null;

    const formValue = this.ncrForm.value;
    
    const createRequest: CreateNcrRequest = {
      title: formValue.title,
      description: formValue.description,
      department: formValue.department || undefined,
      ownerUserId: formValue.ownerUserId || undefined,
      severity: formValue.severity,
      dueDate: formValue.dueDate ? new Date(formValue.dueDate).toISOString() : undefined
    };

    if (this.isOffline) {
      // Save offline
      this.ncrOfflineService.createNcrOffline(createRequest).subscribe({
        next: () => {
          this.saving = false;
          this.toastService.show('NCR saved offline. It will be synced when you are online.', 'success');
          this.router.navigate(['/ncrs']);
        },
        error: (err) => {
          this.error = err.message || 'Failed to save NCR offline';
          this.saving = false;
          console.error('Error saving NCR offline:', err);
        }
      });
    } else {
      // Try online first, fallback to offline if fails
      // For now, we'll use the regular NCR service through the offline service
      // This allows seamless online/offline switching
      this.ncrOfflineService.createNcrOffline(createRequest).subscribe({
        next: (id) => {
          this.saving = false;
          // Try to sync immediately if online
          if (this.offlineDetection.isOnline()) {
            this.ncrOfflineService.getPendingNcrs().subscribe({
              next: (pending) => {
                const latest = pending.find(ncr => ncr.id === id);
                if (latest) {
                  this.ncrOfflineService.syncNcr(latest).subscribe({
                    next: () => {
                      this.toastService.show('NCR created successfully', 'success');
                      this.router.navigate(['/ncrs']);
                    },
                    error: () => {
                      this.toastService.show('NCR saved offline. It will be synced automatically.', 'success');
                      this.router.navigate(['/ncrs']);
                    }
                  });
                } else {
                  this.toastService.show('NCR saved offline. It will be synced automatically.', 'success');
                  this.router.navigate(['/ncrs']);
                }
              },
              error: () => {
                this.toastService.show('NCR saved offline. It will be synced automatically.', 'success');
                this.router.navigate(['/ncrs']);
              }
            });
          } else {
            this.toastService.show('NCR saved offline. It will be synced when you are online.', 'success');
            this.router.navigate(['/ncrs']);
          }
        },
        error: (err) => {
          this.error = err.message || 'Failed to save NCR';
          this.saving = false;
          console.error('Error saving NCR:', err);
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/ncrs']);
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }
}

