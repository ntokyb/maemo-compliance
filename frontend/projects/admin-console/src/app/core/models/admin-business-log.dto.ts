/**
 * Business audit log DTO for Admin Console.
 */
export interface BusinessAuditLogDto {
  id: string;
  tenantId?: string | null;
  userId?: string | null;
  username?: string | null;
  action: string;
  entityType: string;
  entityId: string;
  timestamp: string; // ISO string
  metadataJson?: string | null;
}

