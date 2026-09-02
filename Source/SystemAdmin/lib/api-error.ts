import axios from "axios";

export const SYSTEM_AUTH_ERROR_CODES = {
  authenticationRequired: "AuthenticationRequired",
  permissionDenied: "PermissionDenied",
  resourceNotFound: "ResourceNotFound",
  staleRowVersion: "StaleRowVersion",
  lastActiveManager: "LastActiveManager",
  currentPasswordIncorrect: "CurrentPasswordIncorrect",
  passwordPolicyViolation: "PasswordPolicyViolation",
  passwordReuseNotAllowed: "PasswordReuseNotAllowed",
  mustChangePassword: "MustChangePassword",
} as const;

export const getApiErrorCode = (error: unknown) => {
  if (!axios.isAxiosError(error)) return undefined;
  const data = error.response?.data;
  if (!data || typeof data !== "object" || !("code" in data)) return undefined;
  return typeof data.code === "string" ? data.code : undefined;
};

export const getApiErrorMessage = (error: unknown, fallback: string) => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;
    if (data && typeof data === "object") {
      if ("message" in data && typeof data.message === "string") {
        return data.message;
      }
      if ("title" in data && typeof data.title === "string") {
        return data.title;
      }
      if ("errors" in data && data.errors && typeof data.errors === "object") {
        const message = Object.values(data.errors as Record<string, unknown>)
          .flatMap((value) => (Array.isArray(value) ? value : [value]))
          .filter((value): value is string => typeof value === "string")
          .join("; ");
        if (message) return message;
      }
    }
    if (error.message) return error.message;
  }
  return fallback;
};
