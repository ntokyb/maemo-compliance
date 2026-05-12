export interface AdminWorkerSummaryDto {
  name: string;
  lastRunAt: string | null;
  lastStatus: string;
  nextRunAt: string | null;
  lastErrorMessage: string | null;
}

export interface AdminWorkerJobHistoryItemDto {
  timestamp: string;
  status: string;
  duration: string | null;
  errorMessage: string | null;
}

