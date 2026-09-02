import axiosClient from "@/lib/axios-interceptor";
import type { EmployeeType } from "@/services/employees-api";

const BASE_URL = "/api/auth";

export interface EmployeeProfile {
  employeeId: number;
  employeeCode: string | null;
  account: string | null;
  fullName: string | null;
  birthDate: string | null;
  gender: string | null;
  maritalStatus: string | null;
  mobile: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  departmentId: number | null;
  departmentName: string | null;
  titleId: number | null;
  titleName: string | null;
  employeeType: EmployeeType | null;
  roleName: string;
  status: number | null;
  imageUrl: string | null;
  coverImageUrl: string | null;
  defaultPage: string | null;
  mustChangePassword: boolean;
  passwordChangedAt: string | null;
  rowVersion: string;
}

export interface UpdateEmployeeProfileRequest {
  fullName: string;
  birthDate: string | null;
  gender: string | null;
  maritalStatus: string | null;
  mobile: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  rowVersion: string;
}

export interface ChangeOwnPasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export const profileApi = {
  getProfile: () =>
    axiosClient.get<unknown, EmployeeProfile>(`${BASE_URL}/profile`),

  updateProfile: (payload: UpdateEmployeeProfileRequest) =>
    axiosClient.put<unknown, EmployeeProfile>(`${BASE_URL}/profile`, payload),

  changePassword: (payload: ChangeOwnPasswordRequest) =>
    axiosClient.put<unknown, { message: string }>(
      `${BASE_URL}/password`,
      payload,
    ),

  uploadAvatar: (file: File, rowVersion: string) =>
    uploadProfileImage("avatar", file, rowVersion),

  uploadCover: (file: File, rowVersion: string) =>
    uploadProfileImage("cover", file, rowVersion),

  deleteAvatar: (rowVersion: string) =>
    deleteProfileImage("avatar", rowVersion),

  deleteCover: (rowVersion: string) =>
    deleteProfileImage("cover", rowVersion),
};

const uploadProfileImage = (
  kind: "avatar" | "cover",
  file: File,
  rowVersion: string,
) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("rowVersion", rowVersion);
  return axiosClient.post<unknown, EmployeeProfile>(
    `${BASE_URL}/profile/${kind}`,
    formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  );
};

const deleteProfileImage = (
  kind: "avatar" | "cover",
  rowVersion: string,
) =>
  axiosClient.delete<unknown, EmployeeProfile>(
    `${BASE_URL}/profile/${kind}`,
    { params: { rowVersion } },
  );

export const resolveProfileImageUrl = (value: string | null) => {
  if (!value) return undefined;
  if (/^https?:\/\//i.test(value)) return value;
  const apiOrigin = (process.env.NEXT_PUBLIC_API_URL ?? "").replace(/\/$/, "");
  return `${apiOrigin}${value}`;
};
