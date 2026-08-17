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

axiosClient.interceptors.response.use(
  unwrapApiResponse,

  (error: AxiosError) => {
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
