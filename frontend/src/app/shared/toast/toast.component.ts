import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.scss'
})
export class ToastComponent {
  toastService = inject(ToastService);

  get toasts() {
    return this.toastService.toasts();
  }

  remove(id: string): void {
    this.toastService.remove(id);
  }
}

