export const formatCurrency = (val?: number | null) => {
  if (val == null) return "0 ₫";
  return val.toLocaleString("vi-VN", { style: "currency", currency: "VND" });
};
