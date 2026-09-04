export const parseApiDate = (dateString: string) => {
  const normalized = dateString.trim().replace(" ", "T");
  const hasTimeZone = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(normalized);

  // SQL Server DateTime không giữ DateTimeKind nên ASP.NET có thể trả UTC
  // nhưng thiếu hậu tố Z. Chuẩn hóa về UTC trước khi đưa cho trình duyệt.
  return new Date(hasTimeZone ? normalized : `${normalized}Z`);
};

export const formatDateTime = (dateString?: string | null) => {
  if (!dateString) return "N/A";

  return parseApiDate(dateString).toLocaleString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};
