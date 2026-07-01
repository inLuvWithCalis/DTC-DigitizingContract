"use client";

import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

interface StatusBadgeProps {
  status: string;
  className?: string;
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  let colorClasses = "";
  let label = status;

  switch (status) {
    case "Pending":
      colorClasses =
        "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-500/10 dark:text-amber-400 dark:border-amber-500/20";
      label = "Chờ duyệt";
      break;
    case "Draft":
      colorClasses =
        "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-500/10 dark:text-amber-400 dark:border-amber-500/20";
      label = "Bản nháp";
      break;
    case "Approved":
      colorClasses =
        "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-500/10 dark:text-emerald-400 dark:border-emerald-500/20";
      label = "Đã duyệt";
      break;
    case "Rejected":
      colorClasses =
        "bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-500/10 dark:text-rose-400 dark:border-rose-500/20";
      label = "Từ chối";
      break;
    case "Sent":
      colorClasses =
        "bg-slate-50 text-slate-700 border-slate-200 dark:bg-slate-500/10 dark:text-slate-400 dark:border-slate-500/20";
      label = "Đã gửi";
      break;
    default:
      colorClasses = "bg-secondary text-secondary-foreground border-border";
      break;
  }

  return (
    <Badge variant="outline" className={cn(colorClasses, className)}>
      {label}
    </Badge>
  );
}
