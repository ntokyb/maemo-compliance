import { CreateNcrRequest, NcrSeverity } from './ncr.model';

/**
 * Offline NCR stored in IndexedDB before sync.
 */
export interface PendingNcr {
  id?: number; // IndexedDB auto-increment key
  request: CreateNcrRequest;
  createdAt: string; // ISO string
  synced: boolean;
  syncError?: string;
}

