import axiosClient from "@/lib/axios-interceptor";

export interface UpsertTenantLegalProfileRequest {
  legalEntityName: string;
  taxCode: string;
  address: string;
  representativeName: string;
  representativeTitle: string;
  rowVersion?: string | null;
}

export interface TenantLegalProfileResponse {
  tenantLegalProfileId: number;
  legalEntityName: string;
  taxCode: string;
  address: string;
  representativeName: string;
  representativeTitle: string;
  createdByEmployeeId: number;
  createdAt: string;
  updatedByEmployeeId: number;
  updatedAt: string;
  rowVersion: string;
}

const BASE_URL = "/api/admin/tenant-legal-profile";

export const tenantLegalProfileApi = {
  get: () => {
    return axiosClient.get<any, TenantLegalProfileResponse | null>(BASE_URL);
  },
  upsert: (data: UpsertTenantLegalProfileRequest) => {
    return axiosClient.put<any, TenantLegalProfileResponse>(BASE_URL, data);
  },
};
