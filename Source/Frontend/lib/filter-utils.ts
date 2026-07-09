import { startOfDay, endOfDay } from "date-fns";

export interface DateRange {
  from: Date | undefined;
  to: Date | undefined;
}

interface FilterOptions<T> {
  data: T[];
  statusValue?: string;
  statusKey?: keyof T;
  statusAllValue?: string;
  dateRange?: DateRange;
  dateKey?: keyof T;
}

export function applyTableFilters<T>({
  data,
  statusValue,
  statusKey,
  statusAllValue = "All",
  dateRange,
  dateKey,
}: FilterOptions<T>): T[] {
  if (!Array.isArray(data)) {
    return [];
  }

  return data.filter((item) => {
    // 1. Lọc theo trạng thái (Nếu có truyền statusKey và statusValue)
    let matchesStatus = true;
    if (statusKey && statusValue && statusValue !== statusAllValue) {
      matchesStatus = item[statusKey] === statusValue;
    }

    // 2. Lọc theo khoảng ngày (Nếu có truyền dateKey và dateRange)
    let matchesDate = true;
    if (dateKey && (dateRange?.from || dateRange?.to)) {
      const itemDateValue = item[dateKey];

      if (!itemDateValue) {
        matchesDate = false; // Có filter ngày nhưng item không có ngày -> Loại
      } else {
        const itemDate = new Date(itemDateValue as string | number | Date);

        // Tối ưu logic: So sánh trực tiếp thay vì tạo isWithinInterval với ngày giả định
        if (dateRange.from && itemDate < startOfDay(dateRange.from)) {
          matchesDate = false;
        }
        if (dateRange.to && itemDate > endOfDay(dateRange.to)) {
          matchesDate = false;
        }
      }
    }

    return matchesStatus && matchesDate;
  });
}
