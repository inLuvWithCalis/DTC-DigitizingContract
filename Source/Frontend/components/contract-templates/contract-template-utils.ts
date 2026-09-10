export const getContractTemplateErrorMessage = (
  error: unknown,
  fallback = "Không thể thực hiện thao tác với mẫu hợp đồng.",
) => {
  const apiError = error as {
    response?: {
      status?: number;
      data?: {
        code?: string;
        message?: string;
        errors?: string[] | Record<string, string[]>;
      };
    };
    message?: string;
  };
  const data = apiError.response?.data;

  if (Array.isArray(data?.errors) && data.errors.some(code =>
    code.startsWith("Placeholder") || code === "SystemPlaceholderImmutable" || code === "CustomPlaceholdersDisabled")) {
    return data.message || fallback;
  }

  if (apiError.response?.status === 409) {
    if (
      data?.code === "DraftVersionAlreadyExists" ||
      (Array.isArray(data?.errors) &&
        data.errors.includes("DraftVersionAlreadyExists"))
    ) {
      return "Mẫu hợp đồng đã có một bản nháp đang làm việc. Hãy tiếp tục chỉnh sửa bản nháp hiện tại.";
    }

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
      if (item.startsWith("UnknownPlaceholder:")) {
        return `Placeholder {{${item.split(":")[1]}}} không nằm trong catalog.`;
      }
      if (item.startsWith("InvalidPlaceholderSyntax:")) {
        return `Placeholder {{${item.split(":")[1]}}} sai cú pháp. Key phải viết hoa và chỉ chứa chữ, số hoặc dấu gạch dưới.`;
      }
      if (item === "InvalidPlaceholderSyntax") {
        return "Tài liệu có placeholder sai hoặc chưa đóng đủ {{ }}.";
      }
      if (item.startsWith("MissingRequiredPlaceholder:")) {
        return `Thiếu placeholder bắt buộc {{${item.split(":")[1]}}}.`;
      }
      if (item.startsWith("InactivePlaceholder:")) {
        return `Placeholder {{${item.split(":")[1]}}} đã ngừng dùng. Hãy kích hoạt lại hoặc thay placeholder trong DOCX.`;
      }
      if (item.startsWith("MultiplicityViolation:")) {
        return `Placeholder {{${item.split(":")[1]}}} xuất hiện sai số lần cho phép.`;
      }
      return item;
    });
};
