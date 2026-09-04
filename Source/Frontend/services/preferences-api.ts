import axiosClient from "@/lib/axios-interceptor";

const BASE_URL = "/api/auth/preferences";

export const TABLE_PAGE_SIZE_STORAGE_KEY = "dtc.default-table-page-size";
export const TABLE_PAGE_SIZE_OPTIONS = [5, 10, 20, 50, 100] as const;

export interface EmployeeLandingPageOption {
  path: string;
  label: string;
}

export interface EmployeePreferences {
  defaultPage: string;
  availableLandingPages: EmployeeLandingPageOption[];
  rowVersion: string;
}

export interface UpdateEmployeePreferencesRequest {
  defaultPage: string;
  rowVersion: string;
}

export const getStoredTablePageSize = (): number | null => {
  if (typeof window === "undefined") return null;
  const value = Number(window.localStorage.getItem(TABLE_PAGE_SIZE_STORAGE_KEY));
  return TABLE_PAGE_SIZE_OPTIONS.includes(
    value as (typeof TABLE_PAGE_SIZE_OPTIONS)[number],
  )
    ? value
    : null;
};

export const storeTablePageSize = (pageSize: number) => {
  if (
    typeof window !== "undefined" &&
    TABLE_PAGE_SIZE_OPTIONS.includes(
      pageSize as (typeof TABLE_PAGE_SIZE_OPTIONS)[number],
    )
  ) {
    window.localStorage.setItem(TABLE_PAGE_SIZE_STORAGE_KEY, String(pageSize));
  }
};

export const preferencesApi = {
  get: () =>
    axiosClient.get<unknown, EmployeePreferences>(BASE_URL),

  update: (payload: UpdateEmployeePreferencesRequest) =>
    axiosClient.put<unknown, EmployeePreferences>(BASE_URL, payload),
};
