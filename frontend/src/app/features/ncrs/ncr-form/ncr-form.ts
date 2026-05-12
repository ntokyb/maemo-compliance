import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NcrService } from '../../../services/ncr.service';
import { NcrSeverity, CreateNcrRequest } from '../../../models/ncr.model';

@Component({
  selector: 'app-ncr-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './ncr-form.html',
  styleUrl: './ncr-form.scss'
})
export class NcrFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private ncrService = inject(NcrService);

  ncrForm!: FormGroup;
  saving = false;
  error: string | null = null;

  NcrSeverity = NcrSeverity;

  severityOptions = [
    { value: NcrSeverity.Low, label: 'Low' },
    { value: NcrSeverity.Medium, label: 'Medium' },
    { value: NcrSeverity.High, label: 'High' }
  ];

  ngOnInit(): void {
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

    this.ncrService.createNcr(createRequest).subscribe({
      next: () => {
        this.router.navigate(['/ncrs']);
      },
      error: (err) => {
        this.error = err.message || 'Failed to create NCR';
        this.saving = false;
        console.error('Error creating NCR:', err);
      }
    });
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

