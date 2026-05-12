/**
 * Standardized error response model matching backend ErrorResponse.
 */
export interface ErrorResponse {
  code: string;
  message: string;
  detail?: string | null;
  correlationId?: string | null;
}

