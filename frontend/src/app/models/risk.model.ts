export enum RiskCategory {
  Operational = 0,
  Financial = 1,
  Compliance = 2,
  HealthSafety = 3,
  InformationSecurity = 4
}

export enum RiskStatus {
  Identified = 0,
  Analysed = 1,
  Mitigated = 2,
  Accepted = 3,
  Closed = 4
}

export interface Risk {
  id?: string;
  title: string;
  description: string;
  category: RiskCategory;
  cause?: string;
  consequences?: string;
  inherentLikelihood: number;
  inherentImpact: number;
  inherentScore: number;
  existingControls?: string;
  residualLikelihood: number;
  residualImpact: number;
  residualScore: number;
  ownerUserId?: string;
  status: RiskStatus;
  createdAt?: string;
  modifiedAt?: string;
  riskLevel?: string; // Computed property from backend
}

export interface CreateRiskRequest {
  title: string;
  description: string;
  category: RiskCategory;
  cause?: string;
  consequences?: string;
  inherentLikelihood: number;
  inherentImpact: number;
  existingControls?: string;
  residualLikelihood: number;
  residualImpact: number;
  ownerUserId?: string;
  status?: RiskStatus;
}

export interface UpdateRiskRequest {
  title: string;
  description: string;
  category: RiskCategory;
  cause?: string;
  consequences?: string;
  inherentLikelihood: number;
  inherentImpact: number;
  existingControls?: string;
  residualLikelihood: number;
  residualImpact: number;
  ownerUserId?: string;
  status: RiskStatus;
}

