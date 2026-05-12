import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: string;
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private _toasts = signal<ToastMessage[]>([]);

  get toasts() {
    return this._toasts.asReadonly();
  }

  show(message: string, type: 'success' | 'error' | 'info' | 'warning' = 'info', duration: number = 5000): void {
    const toast: ToastMessage = {
      id: Date.now().toString() + Math.random(),
      message,
      type,
      duration
    };

    this._toasts.update(toasts => [...toasts, toast]);

    if (duration > 0) {
      setTimeout(() => {
        this.remove(toast.id);
      }, duration);
    }
  }

  success(message: string, duration?: number): void {
    this.show(message, 'success', duration);
  }

  error(message: string, duration?: number): void {
    this.show(message, 'error', duration || 7000);
  }

  info(message: string, duration?: number): void {
    this.show(message, 'info', duration);
  }

  warning(message: string, duration?: number): void {
    this.show(message, 'warning', duration);
  }

  remove(id: string): void {
    this._toasts.update(toasts => toasts.filter(t => t.id !== id));
  }

  clear(): void {
    this._toasts.set([]);
  }
}

