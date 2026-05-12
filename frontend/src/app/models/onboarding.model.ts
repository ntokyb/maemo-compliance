export interface OnboardingRequest {
  isoStandards: string[];
  industry: string;
  companySize: string;
}

export interface OnboardingStatus {
  onboardingCompleted: boolean;
  onboardingCompletedAt?: string;
}

export const ISO_STANDARDS = [
  'ISO 9001',
  'ISO 14001',
  'ISO 45001',
  'ISO 27001',
  'ISO 22000',
  'ISO 13485'
];

export const INDUSTRIES = [
  'Manufacturing',
  'Construction',
  'Healthcare',
  'Financial Services',
  'Technology',
  'Retail',
  'Education',
  'Other'
];

export const COMPANY_SIZES = [
  'Small (1-50 employees)',
  'Medium (51-250 employees)',
  'Large (251-1000 employees)',
  'Enterprise (1000+ employees)'
];

