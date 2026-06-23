import axiosClient from "@/lib/axios-interceptor";

export interface LoginRequestDto {
  accountName: string;
  password?: string;
}

export interface LoginResponseDto {
  message: string;
  employeeId: number;
  employeeName: string | null;
}

export const authApi = {
  login: (payload: LoginRequestDto) => {
    return axiosClient.post<any, LoginResponseDto>("/Auth/login", payload, {
      withCredentials: true,
    });
  },

  getMe: () => {
    return axiosClient.get<any, any>("/Auth/me");
  },

  logout: async () => {
    await axiosClient.post<any, void>(
      "/Auth/logout",
      {},
      {
        withCredentials: true,
      },
    );
    sessionStorage.removeItem("accessToken");
    sessionStorage.removeItem("role");
  },
};
