export interface PopiaDocumentSummaryDto {
  totalDocuments: number;
  documentsWithNoPersonalInfo: number;
  documentsWithPersonalInfo: number;
  documentsWithSpecialPersonalInfo: number;
  byCategory: PopiaDocumentSummaryByCategoryDto[];
  byDepartment: PopiaDocumentSummaryByDepartmentDto[];
  byOwner: PopiaDocumentSummaryByOwnerDto[];
}

export interface PopiaDocumentSummaryByCategoryDto {
  category: string;
  total: number;
  withNoPersonalInfo: number;
  withPersonalInfo: number;
  withSpecialPersonalInfo: number;
}

export interface PopiaDocumentSummaryByDepartmentDto {
  department: string;
  total: number;
  withNoPersonalInfo: number;
  withPersonalInfo: number;
  withSpecialPersonalInfo: number;
}

export interface PopiaDocumentSummaryByOwnerDto {
  ownerUserId: string;
  total: number;
  withNoPersonalInfo: number;
  withPersonalInfo: number;
  withSpecialPersonalInfo: number;
}

