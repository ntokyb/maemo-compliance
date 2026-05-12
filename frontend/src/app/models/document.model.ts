export enum DocumentStatus {
  Draft = 0,
  UnderReview = 1,
  Active = 2,
  Archived = 3
}

export enum DocumentWorkflowState {
  Draft = 0,
  PendingApproval = 1,
  Approved = 2,
  Active = 3,
  Archived = 4
}

export enum PiiDataType {
  None = 0,
  Personal = 1,
  SpecialPersonal = 2
}

export enum PersonalInformationType {
  None = 0,
  PersonalInfo = 1,
  SpecialPersonalInfo = 2
}

export enum PiiType {
  None = 0,
  PersonalInfo = 1,
  SpecialPersonalInfo = 2,
  Children = 3,
  Financial = 4,
  Health = 5
}

export interface Document {
  id?: string;
  title: string;
  category?: string;
  department?: string;
  ownerUserId?: string;
  reviewDate: string; // ISO string
  status: DocumentStatus;
  workflowState: DocumentWorkflowState;
  rejectedReason?: string;
  piiDataType?: PiiDataType;
  personalInformationType?: PersonalInformationType;
  piiType?: PiiType;
  piiDescription?: string;
  piiRetentionPeriodInMonths?: number;
  bbbeeExpiryDate?: string; // ISO string
  bbbeeLevel?: number; // 1-8
  retainUntil?: string; // ISO string
  isRetentionLocked?: boolean;
  isPendingArchive?: boolean;
  version?: number;
  latestVersionNumber?: number;
  hasVersions?: boolean;
  latestVersion?: DocumentVersionDto;
  storageLocation?: string;
  approverUserId?: string;
  approvedAt?: string;
  comments?: string; // Approval comments
  filePlanSeries?: string; // National Archives file plan
  filePlanSubSeries?: string;
  filePlanItem?: string;
}

export interface DocumentVersionDto {
  id: string;
  documentId: string;
  versionNumber: number;
  fileName: string;
  storageLocation: string;
  uploadedBy?: string;
  uploadedAt: string;
  comment?: string;
  isLatest: boolean;
}

export interface CreateDocumentRequest {
  title: string;
  category?: string;
  department?: string;
  ownerUserId?: string;
  reviewDate: string; // ISO string
  piiDataType?: PiiDataType;
  personalInformationType?: PersonalInformationType;
  piiType?: PiiType;
  piiDescription?: string;
  piiRetentionPeriodInMonths?: number;
  bbbeeExpiryDate?: string; // ISO string
  bbbeeLevel?: number; // 1-8
  retainUntil?: string; // ISO string
  isRetentionLocked?: boolean;
  filePlanSeries?: string; // National Archives file plan
  filePlanSubSeries?: string;
  filePlanItem?: string;
}

export interface UpdateDocumentRequest {
  title: string;
  category?: string;
  department?: string;
  ownerUserId?: string;
  reviewDate: string; // ISO string
  status: DocumentStatus;
  piiDataType?: PiiDataType;
  piiType?: PiiType;
  piiDescription?: string;
  piiRetentionPeriodInMonths?: number;
  bbbeeExpiryDate?: string; // ISO string
  bbbeeLevel?: number; // 1-8
  retainUntil?: string; // ISO string
  isRetentionLocked?: boolean;
  filePlanSeries?: string; // National Archives file plan
  filePlanSubSeries?: string;
  filePlanItem?: string;
}

export interface PopiaTrailReportItem {
  documentId: string;
  documentTitle: string;
  piiDataType: PiiDataType;
  department?: string;
  accessedBy: string;
  accessedAt: string; // ISO string
}

export interface AuditEvidence {
  document: Document;
  versions: DocumentVersionDto[];
  businessAuditLogs: BusinessAuditLogEntry[];
  linkedNcrs: LinkedNcr[];
  linkedRisks: LinkedRisk[];
  approvalHistory?: ApprovalHistory;
  generatedAt: string; // ISO string
}

export interface BusinessAuditLogEntry {
  id: string;
  action: string;
  entityType: string;
  entityId: string;
  timestamp: string; // ISO string
  username?: string;
  metadataJson?: string;
}

export interface LinkedNcr {
  id: string;
  title: string;
  description: string;
  department?: string;
  linkReason?: string;
}

export interface LinkedRisk {
  id: string;
  title: string;
  description: string;
  linkReason?: string;
}

export interface ApprovalHistory {
  approverUserId?: string;
  approvedAt?: string; // ISO string
  comments?: string;
  currentWorkflowState: DocumentWorkflowState;
  rejectedReason?: string;
}
