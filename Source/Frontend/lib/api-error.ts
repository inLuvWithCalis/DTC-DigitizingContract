import axios from "axios";

export const AUTHORIZATION_ERROR_CODES = {
  authenticationRequired: "AuthenticationRequired",
  employeeInactive: "EmployeeInactive",
  permissionDenied: "PermissionDenied",
  resourceNotFound: "ResourceNotFound",
  staleRowVersion: "StaleRowVersion",
  lastActiveManager: "LastActiveManager",
} as const;

export type AuthorizationErrorCode =
  (typeof AUTHORIZATION_ERROR_CODES)[keyof typeof AUTHORIZATION_ERROR_CODES];

export interface AuthorizationErrorResponse {
  code: AuthorizationErrorCode | string;
  message: string;
}

type ErrorPayload = {
  code?: unknown;
  message?: unknown;
  title?: unknown;
  errors?: unknown;
};

const getPayload = (error: unknown): ErrorPayload | undefined => {
  if (!axios.isAxiosError(error)) return undefined;
  const data = error.response?.data;
  return data && typeof data === "object" ? (data as ErrorPayload) : undefined;
};

export const getApiErrorCode = (error: unknown): string | undefined => {
  const code = getPayload(error)?.code;
  return typeof code === "string" ? code : undefined;
};

export const getApiErrorMessage = (error: unknown, fallback: string) => {
  const payload = getPayload(error);
  if (typeof payload?.message === "string" && payload.message.trim()) {
    return payload.message;
  }
  if (typeof payload?.title === "string" && payload.title.trim()) {
    return payload.title;
  }
  if (Array.isArray(payload?.errors)) {
    const message = payload.errors.filter((item) => typeof item === "string").join("; ");
    if (message) return message;
  }
  if (payload?.errors && typeof payload.errors === "object") {
    const message = Object.values(payload.errors as Record<string, unknown>)
      .flatMap((value) => (Array.isArray(value) ? value : [value]))
      .filter((value): value is string => typeof value === "string")
      .join("; ");
    if (message) return message;
  }
  if (axios.isAxiosError(error) && error.message) return error.message;
  return fallback;
};

export const hasApiErrorCode = (error: unknown, code: AuthorizationErrorCode) =>
  getApiErrorCode(error) === code;

export const isStaleRowVersion = (error: unknown) =>
  hasApiErrorCode(error, AUTHORIZATION_ERROR_CODES.staleRowVersion);

export const isResourceNotFound = (error: unknown) =>
  hasApiErrorCode(error, AUTHORIZATION_ERROR_CODES.resourceNotFound);
