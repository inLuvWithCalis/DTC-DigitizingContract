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
    return axiosClient.post<unknown, LoginResponseDto>(
      "/api/system-auth/login",
      payload,
      {
        withCredentials: true,
      },
    );
  },

  getMe: () => {
    return axiosClient.get<unknown, UserProfileDto>("/api/system-auth/me");
  },

  logout: async () => {
    await axiosClient.post<unknown, void>(
      "/api/system-auth/logout",
      {},
      {
        withCredentials: true,
      },
    );
  },
};
