import axiosClient from "@/lib/axios-interceptor";

export interface LoginRequestDto {
  username: string;
  password?: string;
}

export interface LoginResponseDto {
  message: string;
  systemAdminId: number;
  fullName: string | null;
}

export interface UserProfileDto {
  systemAdminId: number;
  username: string;
  fullName: string;
  email: string;
  isActive: boolean;
}

export const authApi = {
  login: (payload: LoginRequestDto) => {
    return axiosClient.post<any, LoginResponseDto>(
      "/system-auth/login",
      payload,
      {
        withCredentials: true,
      },
    );
  },

  getMe: () => {
    return axiosClient.get<any, UserProfileDto>("/system-auth/me");
  },

  logout: async () => {
    await axiosClient.post<any, void>(
      "/system-auth/logout",
      {},
      {
        withCredentials: true,
      },
    );
  },
};
