export const formatDateTime = (dateString?: string | null) => {
  if (!dateString) return "N/A";

  let safeDateString = dateString;

  if (!safeDateString.includes("Z") && !safeDateString.includes("+")) {
    safeDateString = safeDateString.replace(" ", "T") + "Z";
  }

  return new Date(safeDateString).toLocaleString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};
