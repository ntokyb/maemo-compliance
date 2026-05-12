export interface EvidenceItemDto {
  id: string;
  entityType: string; // Document, DocumentVersion, AuditAnswer, etc.
  entityId: string; // ID of the parent entity
  fileName: string;
  storageLocation: string;
  fileHash?: string; // SHA256 hex string
  uploadedAt: string; // ISO string
  uploadedBy?: string;
  tenantId?: string;
  
  // Additional context fields
  documentTitle?: string; // For Document/DocumentVersion
  auditRunId?: string; // For AuditAnswer
  auditQuestionId?: string; // For AuditAnswer
  versionNumber?: number; // For DocumentVersion
}

