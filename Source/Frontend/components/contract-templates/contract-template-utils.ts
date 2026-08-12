export const getContractTemplateErrorMessage = (
  error: unknown,
  fallback = "Không thể thực hiện thao tác với mẫu hợp đồng.",
) => {
  const apiError = error as {
    response?: {
      status?: number;
      data?: { message?: string; errors?: string[] | Record<string, string[]> };
    };
    message?: string;
  };
  const data = apiError.response?.data;

  if (apiError.response?.status === 409) {
    return "Dữ liệu đã được thay đổi bởi người khác. Vui lòng tải lại và thử lại.";
  }

  if (Array.isArray(data?.errors) && data.errors.length > 0) {
    return data.errors.join("; ");
  }

  if (data?.errors && !Array.isArray(data.errors)) {
    return Object.values(data.errors).flat().join("; ");
  }

  return data?.message || apiError.message || fallback;
};

export const downloadBlob = (blob: Blob, fileName: string) => {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
};

export const parseValidationMessages = (message?: string | null) => {
  if (!message) return [];

  return message
    .split(";")
    .map((item) => item.trim())
    .filter(Boolean)
    .map((item) => {
      if (item === "UnknownPlaceholder") {
        return "Tài liệu có placeholder không nằm trong catalog.";
      }
      if (item.startsWith("MissingRequiredPlaceholder:")) {
        return `Thiếu placeholder bắt buộc {{${item.split(":")[1]}}}.`;
      }
      if (item.startsWith("MultiplicityViolation:")) {
        return `Placeholder {{${item.split(":")[1]}}} xuất hiện sai số lần cho phép.`;
      }
      return item;
    });
};
