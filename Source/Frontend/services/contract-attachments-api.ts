import axiosClient from "@/lib/axios-interceptor";

export const CONTRACT_DOCUMENT_TYPES = [
  { value: 0, label: "File báo giá" },
  { value: 1, label: "Biên bản nghiệm thu" },
  { value: 2, label: "Biên bản bàn giao" },
  { value: 3, label: "Biên bản thanh lý" },
  { value: 4, label: "Hóa đơn VAT" },
  { value: 5, label: "Bảo lãnh ngân hàng" },
  { value: 6, label: "Bản scan đã ký" },
  { value: 99, label: "Tài liệu khác" },
] as const;

export type ContractDocumentType =
  (typeof CONTRACT_DOCUMENT_TYPES)[number]["value"];

export interface ContractAttachmentResponse {
  attachmentId: number;
  contractId: number;
  contractFileName?: string;
  contractFilePath?: string;
  documentType: ContractDocumentType;
  documentTypeName: string;
  uploadDate?: string;
  uploadEmployeeId?: number;
}

const getBaseUrl = (contractId: number) =>
  `/api/contracts/${contractId}/attachments`;

export const contractAttachmentsApi = {
  getAll(contractId: number) {
    return axiosClient.get<any, ContractAttachmentResponse[]>(
      getBaseUrl(contractId),
    );
  },

  upload(
    contractId: number,
    file: File,
    documentType: ContractDocumentType,
  ) {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("documentType", String(documentType));

    return axiosClient.post<any, ContractAttachmentResponse>(
      getBaseUrl(contractId),
      formData,
      { headers: { "Content-Type": "multipart/form-data" } },
    );
  },

  delete(contractId: number, attachmentId: number) {
    return axiosClient.delete<any, { contractId: number; attachmentId: number }>(
      `${getBaseUrl(contractId)}/${attachmentId}`,
    );
  },
};
