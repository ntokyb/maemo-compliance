import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ErrorResponse } from '../models/error-response.model';

@Injectable({
  providedIn: 'root'
})
export class AdminApiService {
  private readonly baseUrl = environment.adminApiBaseUrl;

  constructor(private http: HttpClient) {}

  /**
   * Base method for making GET requests to admin API
   */
  get<T>(endpoint: string): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}${endpoint}`).pipe(
      catchError(this.handleError.bind(this))
    );
  }

  /**
   * Base method for making POST requests to admin API
   */
  post<T>(endpoint: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${endpoint}`, body).pipe(
      catchError(this.handleError.bind(this))
    );
  }

  /**
   * Base method for making PUT requests to admin API
   */
  put<T>(endpoint: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}${endpoint}`, body).pipe(
      catchError(this.handleError.bind(this))
    );
  }

  /**
   * Base method for making DELETE requests to admin API
   */
  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}${endpoint}`).pipe(
      catchError(this.handleError.bind(this))
    );
  }

  /**
   * Handles HTTP errors and extracts ErrorResponse if available.
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorResponse: ErrorResponse;

    // Try to parse ErrorResponse from response body
    if (error.error && typeof error.error === 'object') {
      const body = error.error;
      if (body.code && body.message) {
        errorResponse = {
          code: body.code,
          message: body.message,
          detail: body.detail || null,
          correlationId: body.correlationId || null
        };
      } else {
        // Fallback to generic error if ErrorResponse structure not found
        errorResponse = {
          code: `Http${error.status}`,
          message: body.message || body.error || error.message || 'An unexpected error occurred',
          detail: body.detail || null,
          correlationId: body.correlationId || null
        };
      }
    } else {
      // Fallback for non-JSON errors
      errorResponse = {
        code: `Http${error.status}`,
        message: error.message || 'An unexpected error occurred',
        detail: null,
        correlationId: null
      };
    }

    return throwError(() => errorResponse);
  }
}

