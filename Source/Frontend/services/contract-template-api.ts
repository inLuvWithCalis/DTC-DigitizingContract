import axiosClient from "@/lib/axios-interceptor";
import { ContractLanguageMode } from "@/services/contract-api";

export enum TemplateDocumentType {
  Quotation = 1,
  SoftwareSupplyContract = 2,
  PaymentRequest = 3,
  HandoverRecord = 4,
  AcceptanceRecord = 5,
  LiquidationRecord = 6,
  SoftwareMaintenanceContract = 7,
  SoftwareUpkeepContract = 8,
  Other = 99,
}

export enum TemplateVersionStatus {
  Draft = 0,
  Published = 1,
  Retired = 2,
}

export enum TemplateValidationStatus {
  NotValidated = 0,
  Valid = 1,
  Invalid = 2,
}

export enum TemplatePlaceholderDataKind {
  Scalar = 1,
  DynamicBlock = 2,
}

export enum TemplatePlaceholderMultiplicity {
  ExactlyOne = 1,
  ZeroOrOne = 2,
}

export const getTemplateDocumentTypeLabel = (type?: TemplateDocumentType) => {
  switch (type) {
    case TemplateDocumentType.SoftwareSupplyContract:
      return "Hợp đồng cung cấp phần mềm";
    case TemplateDocumentType.SoftwareMaintenanceContract:
      return "Hợp đồng bảo trì phần mềm";
    case TemplateDocumentType.SoftwareUpkeepContract:
      return "Hợp đồng duy trì phần mềm";
    case TemplateDocumentType.Quotation:
      return "Báo giá";
    case TemplateDocumentType.PaymentRequest:
      return "Đề nghị thanh toán";
    case TemplateDocumentType.HandoverRecord:
      return "Biên bản bàn giao";
    case TemplateDocumentType.AcceptanceRecord:
      return "Biên bản nghiệm thu";
    case TemplateDocumentType.LiquidationRecord:
      return "Biên bản thanh lý";
    default:
      return "Tài liệu khác";
  }
};

export const getTemplateVersionStatusLabel = (
  status?: TemplateVersionStatus,
) => {
  switch (status) {
    case TemplateVersionStatus.Draft:
      return "Bản nháp";
    case TemplateVersionStatus.Published:
      return "Đã phát hành";
    case TemplateVersionStatus.Retired:
      return "Ngừng sử dụng";
    default:
      return "Chưa xác định";
  }
};

export const getTemplateValidationStatusLabel = (
  status?: TemplateValidationStatus,
) => {
  switch (status) {
    case TemplateValidationStatus.NotValidated:
      return "Chưa kiểm tra";
    case TemplateValidationStatus.Valid:
      return "Hợp lệ";
    case TemplateValidationStatus.Invalid:
      return "Không hợp lệ";
    default:
      return "Chưa xác định";
  }
};

export interface ContractTemplateFilterRequest {
  page?: number;
  pageSize?: number;
  keyword?: string;
}

export interface CreateContractTemplateRequest {
  templateCode: string;
  templateName: string;
  templateNameEn?: string | null;
  languageMode: ContractLanguageMode;
  description?: string | null;
  initialChangeNote?: string | null;
}

export interface UpdateContractTemplateRequest {
  templateName: string;
  templateNameEn?: string | null;
  description?: string | null;
  rowVersion: string;
}

export interface CopyContractTemplateVersionRequest {
  rowVersion: string;
  changeNote?: string | null;
}

export interface GenerateContractTemplatePreviewRequest {
  versionRowVersion: string;
}

export interface PublishContractTemplateVersionRequest {
  versionRowVersion: string;
}

export interface RetireContractTemplateVersionRequest {
  versionRowVersion: string;
}

export interface CreateContractTemplateTermRequest {
  termCode: string;
  termTitle: string;
  termTitleEn?: string | null;
  termContent?: string | null;
  termContentEn?: string | null;
  isNegotiable: boolean;
  displayOrder: number;
  versionRowVersion: string;
}

export interface UpdateContractTemplateTermRequest
  extends CreateContractTemplateTermRequest {
  rowVersion: string;
}

export interface DeleteContractTemplateTermRequest {
  rowVersion: string;
  versionRowVersion: string;
}

export interface ReorderContractTemplateTermItem {
  termId: number;
  rowVersion: string;
  displayOrder: number;
}

export interface ReorderContractTemplateTermsRequest {
  versionRowVersion: string;
  terms: ReorderContractTemplateTermItem[];
}

export interface SoftwareSupplyPlaceholderDefinition {
  key: string;
  label: string;
  isRequired: boolean;
  dataKind: TemplatePlaceholderDataKind;
  multiplicity: TemplatePlaceholderMultiplicity;
  dataSource: string;
}

export interface SoftwareSupplyPlaceholderCatalogResponse {
  catalogVersion: string;
  items: SoftwareSupplyPlaceholderDefinition[];
}

export interface ContractTemplateResponse {
  templateId: number;
  templateCode: string;
  templateName: string;
  templateNameEn?: string | null;
  documentType: TemplateDocumentType;
  languageMode: ContractLanguageMode;
  description?: string | null;
  currentPublishedVersionId?: number | null;
  isActive: boolean;
  createdEmployeeId: number;
  createdDate: string;
  updatedEmployeeId?: number | null;
  updatedDate?: string | null;
  rowVersion: string;
}

export interface ContractTemplateVersionSummaryResponse {
  templateVersionId: number;
  versionNo: number;
  changeNote?: string | null;
  status: TemplateVersionStatus;
  validationStatus: TemplateValidationStatus;
  documentFileId?: number | null;
  publishedPreviewPdfFileId?: number | null;
  rowVersion: string;
  createdDate: string;
  updatedDate?: string | null;
}

export interface ContractTemplateDetailResponse
  extends ContractTemplateResponse {
  versions: ContractTemplateVersionSummaryResponse[];
}

export interface ContractTemplateTermResponse {
  templateTermId: number;
  templateVersionId: number;
  termCode: string;
  termTitle: string;
  termTitleEn?: string | null;
  termContent?: string | null;
  termContentEn?: string | null;
  isNegotiable: boolean;
  displayOrder: number;
  createdEmployeeId: number;
  createdDate: string;
  updatedEmployeeId?: number | null;
  updatedDate?: string | null;
  rowVersion: string;
}

export interface ContractTemplateVersionDetailResponse {
  templateVersionId: number;
  templateId: number;
  templateCode: string;
  versionNo: number;
  changeNote?: string | null;
  status: TemplateVersionStatus;
  validationStatus: TemplateValidationStatus;
  validationMessage?: string | null;
  documentFileId?: number | null;
  documentHash?: string | null;
  previewFileId?: number | null;
  publishedPreviewPdfFileId?: number | null;
  previewSourceHash?: string | null;
  previewedAt?: string | null;
  previewedByEmployeeId?: number | null;
  createdDate: string;
  updatedDate?: string | null;
  rowVersion: string;
  terms: ContractTemplateTermResponse[];
}

export interface ContractTemplatePreviewResponse {
  templateVersionId: number;
  previewFileId: number;
  previewedAt: string;
  previewedByEmployeeId: number;
  isCurrent: boolean;
  isReused: boolean;
  rowVersion: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const BASE_URL = "/api/contract-templates";

export const contractTemplateApi = {
  getPlaceholderCatalog: () =>
    axiosClient.get<unknown, SoftwareSupplyPlaceholderCatalogResponse>(
      `${BASE_URL}/placeholder-catalog`,
    ),

  getList: (params: ContractTemplateFilterRequest) =>
    axiosClient.get<unknown, PagedResult<ContractTemplateResponse>>(BASE_URL, {
      params,
    }),

  create: (data: CreateContractTemplateRequest) =>
    axiosClient.post<unknown, ContractTemplateDetailResponse>(BASE_URL, data),

  getById: (templateId: number) =>
    axiosClient.get<unknown, ContractTemplateDetailResponse>(
      `${BASE_URL}/${templateId}`,
    ),

  update: (templateId: number, data: UpdateContractTemplateRequest) =>
    axiosClient.put<unknown, ContractTemplateDetailResponse>(
      `${BASE_URL}/${templateId}`,
      data,
    ),

  getVersion: (versionId: number) =>
    axiosClient.get<unknown, ContractTemplateVersionDetailResponse>(
      `${BASE_URL}/versions/${versionId}`,
    ),

  copyVersion: (
    sourceVersionId: number,
    data: CopyContractTemplateVersionRequest,
  ) =>
    axiosClient.post<unknown, ContractTemplateVersionDetailResponse>(
      `${BASE_URL}/versions/${sourceVersionId}/copy`,
      data,
    ),

  uploadDocument: (
    versionId: number,
    file: File,
    versionRowVersion: string,
  ) => {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("versionRowVersion", versionRowVersion);
    return axiosClient.post<unknown, ContractTemplateVersionDetailResponse>(
      `${BASE_URL}/versions/${versionId}/document`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } },
    );
  },

  generatePreview: (
    versionId: number,
    data: GenerateContractTemplatePreviewRequest,
  ) =>
    axiosClient.post<unknown, ContractTemplatePreviewResponse>(
      `${BASE_URL}/versions/${versionId}/preview`,
      data,
    ),

  downloadPreview: (versionId: number) =>
    axiosClient.get<unknown, Blob>(
      `${BASE_URL}/versions/${versionId}/preview`,
      { responseType: "blob" },
    ),

  publish: (
    versionId: number,
    data: PublishContractTemplateVersionRequest,
  ) =>
    axiosClient.post<unknown, ContractTemplateVersionDetailResponse>(
      `${BASE_URL}/versions/${versionId}/publish`,
      data,
    ),

  retire: (
    versionId: number,
    data: RetireContractTemplateVersionRequest,
  ) =>
    axiosClient.post<unknown, ContractTemplateVersionDetailResponse>(
      `${BASE_URL}/versions/${versionId}/retire`,
      data,
    ),

  downloadPublishedPreviewPdf: (versionId: number) =>
    axiosClient.get<unknown, Blob>(
      `${BASE_URL}/versions/${versionId}/preview/pdf`,
      { responseType: "blob" },
    ),

  addTerm: (versionId: number, data: CreateContractTemplateTermRequest) =>
    axiosClient.post<unknown, ContractTemplateTermResponse>(
      `${BASE_URL}/versions/${versionId}/terms`,
      data,
    ),

  reorderTerms: (
    versionId: number,
    data: ReorderContractTemplateTermsRequest,
  ) =>
    axiosClient.put<unknown, ContractTemplateVersionDetailResponse>(
      `${BASE_URL}/versions/${versionId}/terms/order`,
      data,
    ),

  updateTerm: (
    versionId: number,
    termId: number,
    data: UpdateContractTemplateTermRequest,
  ) =>
    axiosClient.put<unknown, ContractTemplateTermResponse>(
      `${BASE_URL}/versions/${versionId}/terms/${termId}`,
      data,
    ),

  deleteTerm: (
    versionId: number,
    termId: number,
    data: DeleteContractTemplateTermRequest,
  ) =>
    axiosClient.delete<unknown, { versionId: number; termId: number }>(
      `${BASE_URL}/versions/${versionId}/terms/${termId}`,
      { data },
    ),
};
