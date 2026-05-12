import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';
import { environment } from '../../environments/environment';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle error responses
      if (error.error && typeof error.error === 'object') {
        // Check if it's our standardized ErrorResponse
        if (error.error.code && error.error.message) {
          const errorResponse = error.error;
          toastService.error(errorResponse.message);
          
          // In development, log detail to console
          if (!environment.production && errorResponse.detail) {
            console.error('Error detail:', errorResponse.detail);
          }
        } else {
          // Generic error
          toastService.error(error.message || 'An error occurred');
        }
      } else {
        // Network or other errors
        toastService.error('Network error. Please check your connection.');
      }

      return throwError(() => error);
    })
  );
};

