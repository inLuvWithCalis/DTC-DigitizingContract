import {
  Building2,
  CircleCheckBig,
  CircleOff,
  LoaderCircle,
  Timer,
  TriangleAlert,
  type LucideIcon,
} from "lucide-react";

import type { AdminDashboardSummary } from "@/services/admin-dashboard-api";
import { cn } from "@/lib/utils";

interface Definition {
  label: string;
  icon: LucideIcon;
  badgeClass: string;
  borderHoverClass: string;
  accentGradient: string;
}

const definitions: Record<string, Definition> = {
  total: {
    label: "Tổng tổ chức (Tenant)",
    icon: Building2,
    badgeClass: "bg-blue-500/10 text-blue-600 border-blue-500/20 dark:text-blue-400 dark:bg-blue-500/15",
    borderHoverClass: "hover:border-blue-500/40",
    accentGradient: "from-blue-500/5 to-transparent",
  },
  active: {
    label: "Đang hoạt động",
    icon: CircleCheckBig,
    badgeClass: "bg-emerald-500/10 text-emerald-600 border-emerald-500/20 dark:text-emerald-400 dark:bg-emerald-500/15",
    borderHoverClass: "hover:border-emerald-500/40",
    accentGradient: "from-emerald-500/5 to-transparent",
  },
  pending: {
    label: "Đang chờ kích hoạt",
    icon: Timer,
    badgeClass: "bg-slate-500/10 text-slate-600 border-slate-500/20 dark:text-slate-400 dark:bg-slate-500/15",
    borderHoverClass: "hover:border-slate-500/40",
    accentGradient: "from-slate-500/5 to-transparent",
  },
  provisioning: {
    label: "Đang khởi tạo",
    icon: LoaderCircle,
    badgeClass: "bg-cyan-500/10 text-cyan-600 border-cyan-500/20 dark:text-cyan-400 dark:bg-cyan-500/15",
    borderHoverClass: "hover:border-cyan-500/40",
    accentGradient: "from-cyan-500/5 to-transparent",
  },
  suspended: {
    label: "Tạm ngưng hoạt động",
    icon: CircleOff,
    badgeClass: "bg-amber-500/10 text-amber-600 border-amber-500/20 dark:text-amber-400 dark:bg-amber-500/15",
    borderHoverClass: "hover:border-amber-500/40",
    accentGradient: "from-amber-500/5 to-transparent",
  },
  failed: {
    label: "Khởi tạo lỗi",
    icon: TriangleAlert,
    badgeClass: "bg-rose-500/10 text-rose-600 border-rose-500/20 dark:text-rose-400 dark:bg-rose-500/15",
    borderHoverClass: "hover:border-rose-500/40",
    accentGradient: "from-rose-500/5 to-transparent",
  },
};

export function AdminDashboardSummaryCards({ items }: { items: AdminDashboardSummary[] }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
      {items.map((item) => {
        const definition = definitions[item.key] ?? definitions.total;
        const Icon = definition.icon;

        return (
          <div
            key={item.key}
            className={cn(
              "group relative flex flex-col justify-between overflow-hidden rounded-2xl border bg-card p-4 shadow-xs transition-all duration-300 hover:-translate-y-1 hover:shadow-md",
              definition.borderHoverClass,
            )}
          >
            {/* Subtle gradient effect */}
            <div
              className={cn(
                "pointer-events-none absolute inset-x-0 top-0 h-14 bg-gradient-to-b opacity-50 transition-opacity group-hover:opacity-100",
                definition.accentGradient,
              )}
            />

            <div className="relative">
              <div className="flex items-start justify-between gap-2">
                <p className="text-xs font-medium tracking-wide text-muted-foreground line-clamp-1">
                  {definition.label}
                </p>
                <span
                  className={cn(
                    "flex size-9 shrink-0 items-center justify-center rounded-xl border transition-transform duration-300 group-hover:scale-110",
                    definition.badgeClass,
                  )}
                >
                  <Icon className="size-4.5" />
                </span>
              </div>

              <div className="mt-2">
                <span className="text-2xl sm:text-3xl font-extrabold tracking-tight text-foreground">
                  {item.count.toLocaleString("vi-VN")}
                </span>
              </div>
            </div>

            <div className="relative mt-3 pt-2 border-t border-border/50">
              <span className="text-[11px] text-muted-foreground">
                Trạng thái hệ thống
              </span>
            </div>
          </div>
        );
      })}
    </div>
  );
}
