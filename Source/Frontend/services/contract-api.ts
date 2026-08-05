import axiosClient from "@/lib/axios-interceptor";

export enum ContractType {
  SoftwareSupply = 1,
  SoftwareMaintenance = 2,
  SoftwareUpkeep = 3,
}

export const getContractTypeLabel = (type?: ContractType) => {
  switch (type) {
    case ContractType.SoftwareSupply:
      return "Cung cấp phần mềm";
    case ContractType.SoftwareMaintenance:
      return "Bảo trì phần mềm";
    case ContractType.SoftwareUpkeep:
      return "Duy trì phần mềm";
    default:
      return "Chưa cập nhật";
  }
};

export enum ContractLanguageMode {
  Vietnamese = 1,
  Bilingual = 2,
}

export const getContractLanguageModeLabel = (mode?: ContractLanguageMode) => {
  switch (mode) {
    case ContractLanguageMode.Vietnamese:
      return "Tiếng Việt";
    case ContractLanguageMode.Bilingual:
      return "Song ngữ";
    default:
      return "Chưa cập nhật";
  }
};

export enum ContractItemType {
  Product = 1,
  Service = 2,
}

export enum ContractItemDiscountMode {
  None = 0,
  Percentage = 1,
  FixedAmount = 2,
}

export const getContractItemTypeLabel = (type?: ContractItemType) => {
  switch (type) {
    case ContractItemType.Product:
      return "Sản phẩm";
    case ContractItemType.Service:
      return "Dịch vụ";
    default:
      return "Chưa cập nhật";
  }
};

export enum ContractStatus {
  Draft = 0,
  Negotiating = 1,
  PendingApproval = 2,
  PendingSignature = 3,
  Signed = 4,
  Completed = 5,
  Cancelled = 6,
  Rejected = 7,
}

export const getContractStatusLabel = (status?: ContractStatus) => {
  switch (status) {
    case ContractStatus.Draft:
      return "Nháp";
    case ContractStatus.Negotiating:
      return "Đang đàm phán";
    case ContractStatus.PendingApproval:
      return "Chờ duyệt";
    case ContractStatus.PendingSignature:
      return "Chờ ký";
    case ContractStatus.Signed:
      return "Đã ký";
    case ContractStatus.Completed:
      return "Hoàn thành";
    case ContractStatus.Cancelled:
      return "Đã hủy";
    case ContractStatus.Rejected:
      return "Từ chối";
    default:
      return "Chưa cập nhật";
  }
};

export enum ApprovalRequestStatus {
  Pending = 0,
  Approved = 1,
  Returned = 2,
  Rejected = 3,
  Withdrawn = 4,
}

export const getApprovalRequestStatusLabel = (
  status?: ApprovalRequestStatus,
) => {
  switch (status) {
    case ApprovalRequestStatus.Pending:
      return "Chờ xử lý";
    case ApprovalRequestStatus.Approved:
      return "Đã duyệt";
    case ApprovalRequestStatus.Returned:
      return "Yêu cầu sửa lại";
    case ApprovalRequestStatus.Rejected:
      return "Từ chối";
    case ApprovalRequestStatus.Withdrawn:
      return "Đã rút";
    default:
      return "Chưa cập nhật";
  }
};

export enum DocumentType {
  QuotationFile = 0,
  AcceptanceRecord = 1,
  HandoverRecord = 2,
  LiquidationRecord = 3,
  VATInvoice = 4,
  BankGuarantee = 5,
  SignedScanCopy = 6,
  Other = 99,
}

export const getDocumentTypeLabel = (type?: DocumentType | number) => {
  switch (type) {
    case DocumentType.QuotationFile:
      return "File báo giá";
    case DocumentType.AcceptanceRecord:
      return "Biên bản nghiệm thu";
    case DocumentType.HandoverRecord:
      return "Biên bản bàn giao";
    case DocumentType.LiquidationRecord:
      return "Biên bản thanh lý";
    case DocumentType.VATInvoice:
      return "Hóa đơn VAT";
    case DocumentType.BankGuarantee:
      return "Bảo lãnh ngân hàng";
    case DocumentType.SignedScanCopy:
      return "Bản scan đã ký";
    case DocumentType.Other:
      return "Tài liệu khác";
    default:
      return "Khác";
  }
};

export const statusClasses: Record<number, string> = {
  [ContractStatus.Draft]:
    "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-500/10 dark:text-amber-400 dark:border-amber-500/20",
  [ContractStatus.Negotiating]:
    "bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-500/10 dark:text-blue-400 dark:border-blue-500/20",
  [ContractStatus.PendingApproval]:
    "bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-500/10 dark:text-indigo-400 dark:border-indigo-500/20",
  [ContractStatus.PendingSignature]:
    "bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-500/10 dark:text-purple-400 dark:border-purple-500/20",
  [ContractStatus.Signed]:
    "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-500/10 dark:text-emerald-400 dark:border-emerald-500/20",
  [ContractStatus.Completed]:
    "bg-slate-50 text-slate-700 border-slate-200 dark:bg-slate-500/10 dark:text-slate-400 dark:border-slate-500/20",
  [ContractStatus.Cancelled]:
    "bg-red-50 text-red-700 border-red-200 dark:bg-red-500/10 dark:text-red-400 dark:border-red-500/20",
  [ContractStatus.Rejected]:
    "bg-red-50 text-red-700 border-red-200 dark:bg-red-500/10 dark:text-red-400 dark:border-red-500/20",
};

export interface CreateContractItemRequest {
  itemType: ContractItemType;
  sourceProductId?: number | null;
  sourceServiceId?: number | null;
  itemCode?: string | null;
  itemName: string;
  itemNameEn?: string | null;
  itemDescription?: string | null;
  itemDescriptionEn?: string | null;
  unitName?: string | null;
  unitNameEn?: string | null;
  quantity: number;
  unitPrice: number;
  /**
   * Optional while the current create/edit UI is being migrated.
   * The API client always normalizes and sends this field to Backend.
   */
  discountMode?: ContractItemDiscountMode;
  discountPercent: number;
  fixedDiscountAmount?: number;
  isTaxable?: boolean;
  vatPercent: number;
  displayOrder: number;
}

export interface UpdateContractItemRequest extends CreateContractItemRequest {
  contractItemId?: number | null;
  rowVersion?: string | null;
}

export interface CreateContractRequest {
  customerId: number;
  responsibleEmployeeId?: number | null;
  contractType: ContractType;
  templateVersionId: number;
  parentContractId?: number | null;
  contractName: string;
  contractNameEn?: string | null;
  effectiveDate?: string | null;
  expireDate?: string | null;
  currencyCode: string;
  languageMode: ContractLanguageMode;
  items: CreateContractItemRequest[];
}

export interface UpdateContractTermRequest {
  termId?: number | null;
  rowVersion?: string | null;
  termCode: string;
  termTitle: string;
  termTitleEn?: string | null;
  termContent?: string | null;
  termContentEn?: string | null;
  isNegotiable: boolean;
  displayOrder: number;
}

export interface UpdateContractDraftRequest {
  rowVersion: string;
  currentVersionId: number;
  currentVersionRowVersion: string;
  customerId: number;
  contractName: string;
  contractNameEn?: string | null;
  effectiveDate?: string | null;
  expireDate?: string | null;
  currencyCode: string;
  items: UpdateContractItemRequest[];
  terms: UpdateContractTermRequest[];
}

export interface StartContractNegotiationRequest {
  rowVersion: string;
}

export interface CreateContractNegotiationRoundRequest {
  currentVersionId: number;
  rowVersion: string;
  currentVersionRowVersion: string;
  changeNote: string;
}

export interface CreateContractNegotiationCommentRequest {
  currentVersionId: number;
  termId?: number | null;
  parentCommentId?: number | null;
  content: string;
}

export interface UpdateContractNegotiationCommentStateRequest {
  rowVersion: string;
}

export interface SubmitContractForApprovalRequest {
  rowVersion: string;
  currentVersionId: number;
  currentVersionRowVersion: string;
  workflowId?: number | null;
}

export interface ContractItemDetailResponse {
  contractItemId: number;
  itemType: ContractItemType;
  sourceProductId?: number | null;
  sourceServiceId?: number | null;
  itemCode?: string | null;
  itemName: string;
  itemNameEn?: string | null;
  itemDescription?: string | null;
  itemDescriptionEn?: string | null;
  unitName?: string | null;
  unitNameEn?: string | null;
  quantity: number;
  unitPrice: number;
  lineSubtotal: number;
  discountMode: ContractItemDiscountMode;
  discountPercent: number;
  fixedDiscountAmount: number;
  discountAmount: number;
  isTaxable: boolean;
  vatPercent: number;
  vatAmount: number;
  lineTotal: number;
  displayOrder: number;
  rowVersion: string;
}

export interface ContractTermDetailResponse {
  termId: number;
  sourceTemplateTermId?: number | null;
  termCode: string;
  termTitle: string;
  termTitleEn?: string | null;
  termContent?: string | null;
  termContentEn?: string | null;
  isNegotiable: boolean;
  displayOrder: number;
  rowVersion: string;
}

export enum ContractNegotiationCommentState {
  Open = 0,
  Resolved = 1,
}

export enum ContractNegotiationCommentEventType {
  Created = 1,
  Resolved = 2,
  Reopened = 3,
}

export interface ContractNegotiationCommentEventResponse {
  commentEventId: number;
  commentId: number;
  eventType: ContractNegotiationCommentEventType;
  employeeId: number;
  occurredAt: string;
}

export interface ContractNegotiationCommentResponse {
  commentId: number;
  contractId: number;
  versionId: number;
  termId?: number | null;
  parentCommentId?: number | null;
  content: string;
  source: string;
  externalFeedback: boolean;
  recordedByEmployeeId: number;
  createdEmployeeId: number;
  state: ContractNegotiationCommentState;
  createdDate: string;
  updatedDate?: string | null;
  rowVersion: string;
  events: ContractNegotiationCommentEventResponse[];
}

export interface ContractVersionHistoryResponse {
  versionId: number;
  versionNo: number;
  sourceVersionId?: number | null;
  changeNote?: string | null;
  isLocked: boolean;
  lockedDate?: string | null;
  lockedByEmployeeId?: number | null;
  createdEmployeeId: number;
  createdDate: string;
  rowVersion: string;
}

export interface ContractVersionDetailResponse {
  versionId: number;
  versionNo: number;
  sourceVersionId?: number | null;
  templateVersionId?: number | null;
  changeNote?: string | null;
  currencyCode: string;
  subtotal: number;
  totalDiscount: number;
  totalVat: number;
  totalPayment: number;
  snapshotHash?: string | null;
  isLocked: boolean;
  lockedDate?: string | null;
  lockedByEmployeeId?: number | null;
  createdEmployeeId: number;
  createdDate: string;
  rowVersion: string;
  items: ContractItemDetailResponse[];
  terms: ContractTermDetailResponse[];
  comments: ContractNegotiationCommentResponse[];
}

export interface ContractCustomerSummaryResponse {
  customerId: number;
  customerCode?: string | null;
  customerFullName?: string | null;
  customerCompany?: string | null;
  customerTaxCode?: string | null;
  customerEmail?: string | null;
  customerMobile?: string | null;
  customerAddress?: string | null;
}

export interface ContractEmployeeSummaryResponse {
  employeeId: number;
  employeeCode?: string | null;
  employeeFullName?: string | null;
  employeeEmail?: string | null;
  employeeMobile?: string | null;
}

export interface ContractDetailResponse {
  contractId: number;
  contractCode?: string | null;
  contractName: string;
  contractNameEn?: string | null;
  contractType: ContractType;
  templateVersionId?: number | null;
  parentContractId?: number | null;
  status: ContractStatus;
  signDate?: string | null;
  effectiveDate?: string | null;
  expireDate?: string | null;
  totalAmount: number;
  subtotal: number;
  totalDiscount: number;
  totalVat: number;
  totalPayment: number;
  currencyCode: string;
  languageMode: ContractLanguageMode;
  isLegacy: boolean;
  createdEmployeeId: number;
  createdDate: string;
  updatedDate?: string | null;
  rowVersion: string;
  customer: ContractCustomerSummaryResponse;
  responsibleEmployee: ContractEmployeeSummaryResponse;
  currentVersion: ContractVersionDetailResponse;
}

export interface CreateContractResponse {
  contractId: number;
  contractCode: string;
  contractName: string;
  status: ContractStatus;
  currentVersionId: number;
  versionNo: number;
  customerId: number;
  contractType: ContractType;
  templateVersionId: number;
  totalAmount: number;
  subtotal: number;
  totalDiscount: number;
  totalVat: number;
  totalPayment: number;
  currencyCode: string;
  languageMode: ContractLanguageMode;
  employeeId: number;
  createdDate: string;
  itemCount: number;
  termCount: number;
  rowVersion: string;
  currentVersionRowVersion: string;
}

export interface SubmitContractForApprovalResponse {
  approvalRequestId: number;
  contractId: number;
  versionId: number;
  contractStatus: ContractStatus;
  approvalStatus: ApprovalRequestStatus;
  submittedDate: string;
  snapshotHash: string;
  contractRowVersion: string;
  versionRowVersion: string;
}

export interface ContractNegotiationRoundVersionResponse {
  versionId: number;
  versionNo: number;
  sourceVersionId?: number | null;
  isLocked: boolean;
  lockedDate?: string | null;
  snapshotHash?: string | null;
  rowVersion: string;
}

export interface ContractFinancialTotalsResponse {
  currencyCode: string;
  subtotal: number;
  totalDiscount: number;
  totalVat: number;
  totalPayment: number;
}

export interface CreateContractNegotiationRoundResponse {
  contractId: number;
  status: ContractStatus;
  rowVersion: string;
  sourceVersion: ContractNegotiationRoundVersionResponse;
  currentVersion: ContractNegotiationRoundVersionResponse;
  totals: ContractFinancialTotalsResponse;
}

export interface ContractFilterRequest {
  page?: number;
  pageSize?: number;
  keyword?: string | null;
  status?: ContractStatus | null;
  contractType?: ContractType | null;
  customerId?: number | null;
}

export interface ContractListItemResponse {
  contractId: number;
  contractCode?: string | null;
  contractName: string;
  contractType: ContractType;
  status: ContractStatus;
  customerId: number;
  customerCode?: string | null;
  customerName?: string | null;
  customerCompany?: string | null;
  responsibleEmployeeId: number;
  responsibleEmployeeName?: string | null;
  currentVersionId?: number | null;
  currentVersionNo?: number | null;
  isCurrentVersionLocked: boolean;
  totalAmount: number;
  currencyCode: string;
  effectiveDate?: string | null;
  expireDate?: string | null;
  createdDate: string;
  updatedDate?: string | null;
}

export interface EligibleParentContractFilterRequest {
  page?: number;
  pageSize?: number;
  keyword?: string | null;
  customerId: number;
  targetContractType: ContractType;
}

export interface EligibleParentContractResponse {
  contractId: number;
  contractCode?: string | null;
  contractName: string;
  contractType: ContractType;
  status: ContractStatus;
  effectiveDate?: string | null;
  expireDate?: string | null;
}

export interface TransferContractResponsibilityRequest {
  newResponsibleEmployeeId: number;
  reason: string;
  rowVersion: string;
}

export interface TransferContractResponsibilityResponse {
  contractId: number;
  previousResponsibleEmployeeId: number;
  responsibleEmployeeId: number;
  transferredByEmployeeId: number;
  transferredAt: string;
  rowVersion: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const BASE_URL = "/contracts";

/**
 * Chuẩn hóa dữ liệu tài chính trước khi gửi và bảo đảm hai kiểu chiết khấu
 * không cùng tồn tại trong một item.
 */
const normalizeContractItemFinance = <T extends CreateContractItemRequest>(
  item: T,
) => {
  const requestedFixedDiscount = item.fixedDiscountAmount ?? 0;
  const discountMode =
    item.discountMode ??
    (requestedFixedDiscount > 0
      ? ContractItemDiscountMode.FixedAmount
      : item.discountPercent > 0
        ? ContractItemDiscountMode.Percentage
        : ContractItemDiscountMode.None);
  const isTaxable = item.isTaxable ?? true;

  return {
    ...item,
    discountMode,
    discountPercent:
      discountMode === ContractItemDiscountMode.Percentage
        ? item.discountPercent
        : 0,
    fixedDiscountAmount:
      discountMode === ContractItemDiscountMode.FixedAmount
        ? requestedFixedDiscount
        : 0,
    isTaxable,
    vatPercent: isTaxable ? item.vatPercent : 0,
  };
};

const normalizeCreateContractRequest = (data: CreateContractRequest) => ({
  ...data,
  items: data.items.map(normalizeContractItemFinance),
});

const normalizeUpdateContractDraftRequest = (
  data: UpdateContractDraftRequest,
) => ({
  ...data,
  items: data.items.map(normalizeContractItemFinance),
});

export const contractApi = {
  getList: (params: ContractFilterRequest) => {
    return axiosClient.get<any, PagedResult<ContractListItemResponse>>(
      BASE_URL,
      { params },
    );
  },
  getEligibleParents: (params: EligibleParentContractFilterRequest) => {
    return axiosClient.get<any, PagedResult<EligibleParentContractResponse>>(
      `${BASE_URL}/eligible-parents`,
      { params },
    );
  },
  getDetail: (id: number) => {
    return axiosClient.get<any, ContractDetailResponse>(`${BASE_URL}/${id}`);
  },
  create: (data: CreateContractRequest) => {
    return axiosClient.post<any, CreateContractResponse>(
      BASE_URL,
      normalizeCreateContractRequest(data),
    );
  },
  transferResponsibility: (
    id: number,
    data: TransferContractResponsibilityRequest,
  ) => {
    return axiosClient.post<any, TransferContractResponsibilityResponse>(
      `${BASE_URL}/${id}/transfer-responsibility`,
      data,
    );
  },
  updateDraft: (id: number, data: UpdateContractDraftRequest) => {
    return axiosClient.put<any, ContractDetailResponse>(
      `${BASE_URL}/${id}/draft`,
      normalizeUpdateContractDraftRequest(data),
    );
  },
  startNegotiation: (id: number, data: StartContractNegotiationRequest) => {
    return axiosClient.post<any, ContractDetailResponse>(
      `${BASE_URL}/${id}/start-negotiation`,
      data,
    );
  },
  createNegotiationRound: (
    id: number,
    data: CreateContractNegotiationRoundRequest,
  ) => {
    return axiosClient.post<any, CreateContractNegotiationRoundResponse>(
      `${BASE_URL}/${id}/negotiation-rounds`,
      data,
    );
  },
  getVersionHistory: (id: number) => {
    return axiosClient.get<any, ContractVersionHistoryResponse[]>(
      `${BASE_URL}/${id}/versions`,
    );
  },
  getVersionDetail: (id: number, versionId: number) => {
    return axiosClient.get<any, ContractVersionDetailResponse>(
      `${BASE_URL}/${id}/versions/${versionId}`,
    );
  },
  createExternalFeedback: (
    id: number,
    data: CreateContractNegotiationCommentRequest,
  ) => {
    return axiosClient.post<any, ContractNegotiationCommentResponse>(
      `${BASE_URL}/${id}/comments/external-feedback`,
      data,
    );
  },
  resolveComment: (
    id: number,
    commentId: number,
    data: UpdateContractNegotiationCommentStateRequest,
  ) => {
    return axiosClient.post<any, ContractNegotiationCommentResponse>(
      `${BASE_URL}/${id}/comments/${commentId}/resolve`,
      data,
    );
  },
  reopenComment: (
    id: number,
    commentId: number,
    data: UpdateContractNegotiationCommentStateRequest,
  ) => {
    return axiosClient.post<any, ContractNegotiationCommentResponse>(
      `${BASE_URL}/${id}/comments/${commentId}/reopen`,
      data,
    );
  },
  submitApproval: (id: number, data: SubmitContractForApprovalRequest) => {
    return axiosClient.post<any, SubmitContractForApprovalResponse>(
      `${BASE_URL}/${id}/submit-approval`,
      data,
    );
  },
};
