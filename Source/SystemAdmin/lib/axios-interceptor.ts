import axios, { AxiosError, AxiosInstance, AxiosResponse } from "axios";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL;

const axiosClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  withCredentials: true,
});

const unwrapApiResponse = (response: AxiosResponse) => {
  const data = response.data;
  if (data && typeof data === "object" && "success" in data && "data" in data) {
    return data.data;
  }
  return data;
};

const getResponseErrorCode = (error: AxiosError) => {
  const data = error.response?.data;
  if (data && typeof data === "object" && "code" in data) {
    return typeof data.code === "string" ? data.code : undefined;
  }
  return undefined;
};

axiosClient.interceptors.response.use(
  unwrapApiResponse,

  (error: AxiosError) => {
    if (
      error.response?.status === 403 &&
      getResponseErrorCode(error) === "MustChangePassword" &&
      typeof window !== "undefined" &&
      window.location.pathname !== "/change-password"
    ) {
      window.location.replace("/change-password?required=1");
    }
    if (error.response?.status === 401) {
      if (typeof window !== "undefined") {
        if (window.location.pathname !== "/") {
          window.location.replace("/?error=session_expired");
        }
      }
    }

    return Promise.reject(error);
  },
);

export default axiosClient;
