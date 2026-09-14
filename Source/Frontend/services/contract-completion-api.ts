import axiosClient from "@/lib/axios-interceptor";
import { ContractStatus } from "@/services/contract-api";

const BASE_URL = "/api/contracts";

export enum ContractPaymentStatus { Active = 1, Voided = 2 }
export enum ContractPaymentDueAnchor { ContractSigned = 1, ContractEffectiveDate = 2, AcceptanceCompleted = 3, PreviousMilestonePaid = 4, ManualDate = 5 }
export enum ContractPaymentDayCountMode { CalendarDays = 1, BusinessDays = 2 }
export enum ContractPaymentMilestoneStatus { Unpaid = 0, Paid = 1 }
export interface ContractAcceptanceEvidenceResponse { acceptanceEvidenceId: number; contractId: number; versionId: number; versionNo: number; fileId: number; fileName: string; fileType: string; contentType: string; fileSize: number; sha256: string; uploadedByEmployeeId: number; uploadedByEmployeeName?: string | null; uploadedAt: string; rowVersion: string; }
export interface ContractPaymentResponse { contractPaymentId: number; contractId: number; versionId: number; versionNo: number; paymentMilestoneId?: number | null; paymentDate: string; amount: number; currencyCode: string; paymentMethod: string; referenceCode: string; evidenceFileId?: number | null; evidenceFileName?: string | null; status: ContractPaymentStatus; createdByEmployeeId: number; createdByEmployeeName?: string | null; createdAt: string; voidReason?: string | null; voidedByEmployeeId?: number | null; voidedByEmployeeName?: string | null; voidedAt?: string | null; rowVersion: string; }
export interface ContractPaymentMilestoneResponse { paymentMilestoneId: number; sourceTemplatePaymentMilestoneId?: number | null; versionId: number; milestoneCode: string; titleVi: string; titleEn?: string | null; paymentPercent: number; amount: number; paidAmount: number; remainingAmount: number; dueAnchor: ContractPaymentDueAnchor; dueOffsetDays: number; dayCountMode: ContractPaymentDayCountMode; conditionVi?: string | null; conditionEn?: string | null; displayOrder: number; anchorDate?: string | null; dueDate?: string | null; paymentStatus: ContractPaymentMilestoneStatus; isOverdue: boolean; paidAt?: string | null; paidByEmployeeId?: number | null; activePayment?: ContractPaymentResponse | null; rowVersion: string; }
export interface CompletePaymentMilestoneResponse { milestone: ContractPaymentMilestoneResponse; payment: ContractPaymentResponse; }
export interface ReopenPaymentMilestoneResponse { milestone: ContractPaymentMilestoneResponse; payment: ContractPaymentResponse; }
export interface ContractCompletionReadinessResponse { signed: boolean; acceptanceEvidenceAvailable: boolean; totalAmount: number; paidAmount: number; remainingAmount: number; currencyCode: string; ready: boolean; blockers: { code: string; message: string }[]; }
export interface ContractCompletionDetailResponse { contractId: number; contractStatus: ContractStatus; versionId: number; versionNo: number; contractRowVersion: string; versionRowVersion: string; acceptanceEvidence?: ContractAcceptanceEvidenceResponse | null; payments: ContractPaymentResponse[]; paymentMilestones: ContractPaymentMilestoneResponse[]; readiness: ContractCompletionReadinessResponse; }

const multipart = (values: Record<string, string | number | File | undefined>) => {
  const data = new FormData();
  Object.entries(values).forEach(([key, value]) => { if (value !== undefined) data.append(key, value instanceof File ? value : String(value)); });
  return data;
};

export const contractCompletionApi = {
  get: (contractId: number) => axiosClient.get<unknown, ContractCompletionDetailResponse>(`${BASE_URL}/${contractId}/completion`),
  getReadiness: (contractId: number) => axiosClient.get<unknown, ContractCompletionReadinessResponse>(`${BASE_URL}/${contractId}/completion-readiness`),
  getPaymentMilestones: (contractId: number, versionId: number) => axiosClient.get<unknown, ContractPaymentMilestoneResponse[]>(`${BASE_URL}/${contractId}/versions/${versionId}/payment-milestones`),
  uploadAcceptance: (contractId: number, values: { file: File; currentVersionId: number; contractRowVersion: string; versionRowVersion: string }) =>
    axiosClient.post<unknown, ContractAcceptanceEvidenceResponse>(`${BASE_URL}/${contractId}/acceptance-evidence`, multipart(values), { headers: { "Content-Type": "multipart/form-data" } }),
  addPayment: (contractId: number, values: { evidenceFile?: File; currentVersionId: number; contractRowVersion: string; versionRowVersion: string; paymentDate: string; amount: number; currencyCode: string; paymentMethod: string; referenceCode: string }) =>
    axiosClient.post<unknown, ContractPaymentResponse>(`${BASE_URL}/${contractId}/payments`, multipart(values), { headers: { "Content-Type": "multipart/form-data" } }),
  completePaymentMilestone: (contractId: number, versionId: number, milestoneId: number, values: { evidenceFile: File; currentVersionId: number; contractRowVersion: string; versionRowVersion: string; milestoneRowVersion: string; paymentDate: string; paymentMethod: string; referenceCode: string }) =>
    axiosClient.post<unknown, CompletePaymentMilestoneResponse>(`${BASE_URL}/${contractId}/versions/${versionId}/payment-milestones/${milestoneId}/complete`, multipart(values), { headers: { "Content-Type": "multipart/form-data" } }),
  reopenPaymentMilestone: (contractId: number, versionId: number, milestoneId: number, values: { currentVersionId: number; contractRowVersion: string; versionRowVersion: string; milestoneRowVersion: string; paymentRowVersion: string; reason: string }) =>
    axiosClient.post<unknown, ReopenPaymentMilestoneResponse>(`${BASE_URL}/${contractId}/versions/${versionId}/payment-milestones/${milestoneId}/reopen`, values),
  downloadEvidence: (fileId: number) =>
    axiosClient.get<unknown, Blob>(`/api/files/${fileId}/download`, { responseType: "blob" }),
  voidPayment: (contractId: number, paymentId: number, values: { contractRowVersion: string; versionRowVersion: string; paymentRowVersion: string; reason: string }) =>
    axiosClient.post<unknown, ContractPaymentResponse>(`${BASE_URL}/${contractId}/payments/${paymentId}/void`, values),
  complete: (contractId: number, values: { currentVersionId: number; contractRowVersion: string; versionRowVersion: string }) =>
    axiosClient.post<unknown, ContractCompletionDetailResponse>(`${BASE_URL}/${contractId}/complete`, values),
};
