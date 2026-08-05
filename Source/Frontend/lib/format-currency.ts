export const formatCurrency = (
  val?: number | null,
  currencyCode: string = "VND",
) => {
  const currency = currencyCode.trim().toUpperCase() || "VND";
  if (val == null) {
    val = 0;
  }

  return val.toLocaleString(currency === "VND" ? "vi-VN" : "en-US", {
    style: "currency",
    currency,
    maximumFractionDigits: currency === "VND" ? 0 : 2,
  });
};
