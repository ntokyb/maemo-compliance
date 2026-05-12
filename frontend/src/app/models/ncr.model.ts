export enum NcrSeverity {
  Low = 0,
  Medium = 1,
  High = 2
}

export enum NcrStatus {
  Open = 0,
  InProgress = 1,
  Closed = 2
}

export interface Ncr {
  id?: string;
  title: string;
  description: string;
  department?: string;
  ownerUserId?: string;
  severity: NcrSeverity;
  status: NcrStatus;
  createdAt?: string;
  dueDate?: string;
  closedAt?: string;
}

export interface CreateNcrRequest {
  title: string;
  description: string;
  department?: string;
  ownerUserId?: string;
  severity: NcrSeverity;
  dueDate?: string;
}

export interface UpdateNcrStatusRequest {
  status: NcrStatus;
  dueDate?: string;
  closedAt?: string;
}

