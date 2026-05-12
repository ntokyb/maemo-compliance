export interface ConsultantDashboardSummary {
  totalClients: number;
  totalOpenNcrs: number;
  totalHighSeverityNcrs: number;
  totalHighRisks: number;
  upcomingDocumentReviews: number;
}

export interface ConsultantClient {
  tenantId: string;
  tenantName: string;
  plan?: string;
  isActive: boolean;
}

export interface ConsultantBranding {
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  loginBannerUrl?: string;
}

export interface AuditTemplate {
  id: string;
  consultantUserId: string;
  name: string;
  description?: string;
  createdAt: string;
}

export interface AuditQuestion {
  id: string;
  auditTemplateId: string;
  category: string;
  questionText: string;
  maxScore: number;
}

export interface AuditRun {
  id: string;
  tenantId: string;
  auditTemplateId: string;
  templateName?: string;
  startedAt: string;
  completedAt?: string;
  auditorUserId?: string;
}

export interface AuditAnswer {
  id: string;
  auditRunId: string;
  auditQuestionId: string;
  questionText?: string;
  category?: string;
  score: number;
  maxScore?: number;
  evidenceFileUrl?: string;
  comment?: string;
}

export interface CreateAuditTemplateRequest {
  name: string;
  description?: string;
}

export interface AddAuditQuestionRequest {
  category: string;
  questionText: string;
  maxScore: number;
}

export interface StartAuditRunRequest {
  auditTemplateId: string;
  auditorUserId?: string;
}

export interface SubmitAuditAnswerRequest {
  auditQuestionId: string;
  score: number;
  evidenceFileUrl?: string;
  comment?: string;
}

