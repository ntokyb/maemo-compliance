export interface AdminDashboardSummaryDto {
  totalTenants: number;
  activeTenants: number;
  suspendedTenants: number;
  trialTenants: number;
  totalApiCallsLast24h: number;
  totalErrorsLast24h: number;
  webhookFailuresLast24h: number;
  workerFailuresLast24h: number;
}

