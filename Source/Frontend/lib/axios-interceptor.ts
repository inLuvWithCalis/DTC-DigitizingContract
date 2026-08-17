import axios, { AxiosError, AxiosInstance, AxiosResponse } from "axios";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL;

const axiosClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  withCredentials: true,
});

export const publicAxiosClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  withCredentials: true,
});

const unwrapApiResponse = (response: AxiosResponse) => {
  const res = response.data;
  if (res && typeof res === "object" && "success" in res && "data" in res) {
    return res.data;
  }
  return res;
};

const getEmployeeLoginError = (error: AxiosError) => {
  const data = error.response?.data;
  if (data && typeof data === "object" && "code" in data) {
    const code = (data as { code?: unknown }).code;
    if (code === "EmployeeInactive") return "employee_inactive";
  }
  return "session_expired";
};

axiosClient.interceptors.response.use(
  unwrapApiResponse,

  (error: AxiosError) => {
    if (error.response?.status === 401) {
      if (typeof window !== "undefined") {
        if (window.location.pathname !== "/") {
          window.location.replace(`/?error=${getEmployeeLoginError(error)}`);
        }
      }
    }

    return Promise.reject(error);
  },
);

publicAxiosClient.interceptors.response.use(
  unwrapApiResponse,
  (error: AxiosError) => Promise.reject(error),
);

export default axiosClient;
