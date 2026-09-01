import axiosClient from "@/lib/axios-interceptor";
import { ContractStatus } from "@/services/contract-api";

const BASE_URL = "/api/contracts";

export enum ContractPaymentStatus { Active = 1, Voided = 2 }
export interface ContractAcceptanceEvidenceResponse { acceptanceEvidenceId: number; contractId: number; versionId: number; versionNo: number; fileId: number; fileName: string; fileType: string; contentType: string; fileSize: number; sha256: string; uploadedByEmployeeId: number; uploadedByEmployeeName?: string | null; uploadedAt: string; rowVersion: string; }
export interface ContractPaymentResponse { contractPaymentId: number; contractId: number; versionId: number; versionNo: number; paymentDate: string; amount: number; currencyCode: string; paymentMethod: string; referenceCode: string; evidenceFileId?: number | null; evidenceFileName?: string | null; status: ContractPaymentStatus; createdByEmployeeId: number; createdByEmployeeName?: string | null; createdAt: string; voidReason?: string | null; voidedByEmployeeId?: number | null; voidedByEmployeeName?: string | null; voidedAt?: string | null; rowVersion: string; }
export interface ContractCompletionReadinessResponse { signed: boolean; acceptanceEvidenceAvailable: boolean; totalAmount: number; paidAmount: number; remainingAmount: number; currencyCode: string; ready: boolean; blockers: { code: string; message: string }[]; }
export interface ContractCompletionDetailResponse { contractId: number; contractStatus: ContractStatus; versionId: number; versionNo: number; contractRowVersion: string; versionRowVersion: string; acceptanceEvidence?: ContractAcceptanceEvidenceResponse | null; payments: ContractPaymentResponse[]; readiness: ContractCompletionReadinessResponse; }

const multipart = (values: Record<string, string | number | File | undefined>) => {
  const data = new FormData();
  Object.entries(values).forEach(([key, value]) => { if (value !== undefined) data.append(key, value instanceof File ? value : String(value)); });
  return data;
};

export const contractCompletionApi = {
  get: (contractId: number) => axiosClient.get<unknown, ContractCompletionDetailResponse>(`${BASE_URL}/${contractId}/completion`),
  getReadiness: (contractId: number) => axiosClient.get<unknown, ContractCompletionReadinessResponse>(`${BASE_URL}/${contractId}/completion-readiness`),
  uploadAcceptance: (contractId: number, values: { file: File; currentVersionId: number; contractRowVersion: string; versionRowVersion: string }) =>
    axiosClient.post<unknown, ContractAcceptanceEvidenceResponse>(`${BASE_URL}/${contractId}/acceptance-evidence`, multipart(values), { headers: { "Content-Type": "multipart/form-data" } }),
  addPayment: (contractId: number, values: { evidenceFile?: File; currentVersionId: number; contractRowVersion: string; versionRowVersion: string; paymentDate: string; amount: number; currencyCode: string; paymentMethod: string; referenceCode: string }) =>
    axiosClient.post<unknown, ContractPaymentResponse>(`${BASE_URL}/${contractId}/payments`, multipart(values), { headers: { "Content-Type": "multipart/form-data" } }),
  voidPayment: (contractId: number, paymentId: number, values: { contractRowVersion: string; versionRowVersion: string; paymentRowVersion: string; reason: string }) =>
    axiosClient.post<unknown, ContractPaymentResponse>(`${BASE_URL}/${contractId}/payments/${paymentId}/void`, values),
  complete: (contractId: number, values: { currentVersionId: number; contractRowVersion: string; versionRowVersion: string }) =>
    axiosClient.post<unknown, ContractCompletionDetailResponse>(`${BASE_URL}/${contractId}/complete`, values),
};
