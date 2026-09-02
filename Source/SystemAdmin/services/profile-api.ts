import axiosClient from "@/lib/axios-interceptor";

const BASE_URL = "/api/system-auth";

export interface SystemAdminProfile {
  systemAdminId: number;
  username: string;
  fullName: string;
  email: string | null;
  roleName: string;
  isActive: boolean;
  mustChangePassword: boolean;
  passwordChangedAt: string | null;
  imageUrl: string | null;
  coverImageUrl: string | null;
  rowVersion: string;
}

export interface UpdateSystemAdminProfileRequest {
  fullName: string;
  email: string | null;
  rowVersion: string;
}

export interface ChangeSystemAdminPasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export const profileApi = {
  getProfile: () =>
    axiosClient.get<unknown, SystemAdminProfile>(`${BASE_URL}/profile`),

  updateProfile: (payload: UpdateSystemAdminProfileRequest) =>
    axiosClient.put<unknown, SystemAdminProfile>(`${BASE_URL}/profile`, payload),

  changePassword: (payload: ChangeSystemAdminPasswordRequest) =>
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
  return axiosClient.post<unknown, SystemAdminProfile>(
    `${BASE_URL}/profile/${kind}`,
    formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  );
};

const deleteProfileImage = (
  kind: "avatar" | "cover",
  rowVersion: string,
) =>
  axiosClient.delete<unknown, SystemAdminProfile>(
    `${BASE_URL}/profile/${kind}`,
    { params: { rowVersion } },
  );

export const resolveProfileImageUrl = (value: string | null) => {
  if (!value) return undefined;
  if (/^https?:\/\//i.test(value)) return value;
  const apiOrigin = (process.env.NEXT_PUBLIC_API_URL ?? "").replace(/\/$/, "");
  return `${apiOrigin}${value}`;
};
