import axios, { AxiosError, AxiosInstance, AxiosResponse } from "axios";
import { router } from "expo-router";

const BASE_URL = process.env.EXPO_PUBLIC_API_URL;

const axiosClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  withCredentials: true,
});

axiosClient.interceptors.response.use(
  (response: AxiosResponse) => {
    const res = response.data;
    if (res && typeof res === "object" && "success" in res && "data" in res) {
      return res.data;
    }
    return res;
  },

  (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Navigate to login page with error parameter
      router.replace({ pathname: "/", params: { error: "session_expired" } });
    }

    return Promise.reject(error);
  },
);

export default axiosClient;
