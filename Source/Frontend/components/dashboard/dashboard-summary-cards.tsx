"use client";

import type { LucideIcon } from "lucide-react";
import {
  CircleCheckBig,
  Clock3,
  FilePenLine,
  Files,
  Minus,
  ScanLine,
  Send,
  TimerOff,
  TrendingDown,
  TrendingUp,
} from "lucide-react";

import type { DashboardSummaryItem } from "@/services/dashboard-api";
import { cn } from "@/lib/utils";

interface Definition {
  label: string;
  icon: LucideIcon;
  badgeClass: string;
  cardBorderClass?: string;
  accentGradient?: string;
}

const definitions: Record<string, Definition> = {
  total: {
    label: "Tổng hợp đồng",
    icon: Files,
    badgeClass: "text-blue-600 bg-blue-500/10 border-blue-500/20 dark:text-blue-400 dark:bg-blue-500/15",
    cardBorderClass: "hover:border-blue-500/40",
    accentGradient: "from-blue-500/5 to-transparent",
  },
  drafting: {
    label: "Đang soạn / đàm phán",
    icon: FilePenLine,
    badgeClass: "text-violet-600 bg-violet-500/10 border-violet-500/20 dark:text-violet-400 dark:bg-violet-500/15",
    cardBorderClass: "hover:border-violet-500/40",
    accentGradient: "from-violet-500/5 to-transparent",
  },
  pendingApproval: {
    label: "Chờ duyệt",
    icon: Clock3,
    badgeClass: "text-amber-600 bg-amber-500/10 border-amber-500/20 dark:text-amber-400 dark:bg-amber-500/15",
    cardBorderClass: "hover:border-amber-500/40",
    accentGradient: "from-amber-500/5 to-transparent",
  },
  pendingSignature: {
    label: "Chờ ký",
    icon: Send,
    badgeClass: "text-cyan-600 bg-cyan-500/10 border-cyan-500/20 dark:text-cyan-400 dark:bg-cyan-500/15",
    cardBorderClass: "hover:border-cyan-500/40",
    accentGradient: "from-cyan-500/5 to-transparent",
  },
  signed: {
    label: "Đã ký",
    icon: ScanLine,
    badgeClass: "text-emerald-600 bg-emerald-500/10 border-emerald-500/20 dark:text-emerald-400 dark:bg-emerald-500/15",
    cardBorderClass: "hover:border-emerald-500/40",
    accentGradient: "from-emerald-500/5 to-transparent",
  },
  completedRejected: {
    label: "Hoàn thành / từ chối",
    icon: CircleCheckBig,
    badgeClass: "text-slate-600 bg-slate-500/10 border-slate-500/20 dark:text-slate-300 dark:bg-slate-500/15",
    cardBorderClass: "hover:border-slate-500/40",
    accentGradient: "from-slate-500/5 to-transparent",
  },
  expiring: {
    label: "Sắp hết hạn",
    icon: TimerOff,
    badgeClass: "text-rose-600 bg-rose-500/10 border-rose-500/20 dark:text-rose-400 dark:bg-rose-500/15",
    cardBorderClass: "hover:border-rose-500/40",
    accentGradient: "from-rose-500/5 to-transparent",
  },
};

export function DashboardSummaryCards({ items }: { items: DashboardSummaryItem[] }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-7">
      {items.map((item) => {
        const definition = definitions[item.key] ?? definitions.total;
        const Icon = definition.icon;
        const delta = item.previousCount == null ? null : item.count - item.previousCount;

        return (
          <div
            key={item.key}
            className={cn(
              "group relative flex flex-col justify-between overflow-hidden rounded-2xl border bg-card p-5 shadow-xs transition-all duration-300 hover:-translate-y-1 hover:shadow-md",
              definition.cardBorderClass,
            )}
          >
            {/* Top subtle gradient effect */}
            <div
              className={cn(
                "pointer-events-none absolute inset-x-0 top-0 h-16 bg-gradient-to-b opacity-50 transition-opacity group-hover:opacity-100",
                definition.accentGradient,
              )}
            />

            <div className="relative">
              <div className="flex items-start justify-between gap-3">
                <p className="text-xs font-medium tracking-wide text-muted-foreground">
                  {definition.label}
                </p>
                <span
                  className={cn(
                    "flex size-10 shrink-0 items-center justify-center rounded-xl border transition-transform duration-300 group-hover:scale-110",
                    definition.badgeClass,
                  )}
                >
                  <Icon className="size-5" />
                </span>
              </div>

              <div className="mt-2">
                <span className="text-3xl font-extrabold tracking-tight text-foreground">
                  {item.count.toLocaleString("vi-VN")}
                </span>
              </div>
            </div>

            <div className="relative mt-4 pt-2 border-t border-border/50">
              {delta != null ? (
                <div className="flex items-center gap-1.5 text-xs">
                  <span
                    className={cn(
                      "inline-flex items-center gap-1 rounded-full px-1.5 py-0.5 font-semibold text-[11px]",
                      delta > 0
                        ? "bg-emerald-500/10 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-400"
                        : delta < 0
                        ? "bg-rose-500/10 text-rose-600 dark:bg-rose-500/15 dark:text-rose-400"
                        : "bg-muted text-muted-foreground",
                    )}
                  >
                    {delta > 0 ? (
                      <TrendingUp className="size-3" />
                    ) : delta < 0 ? (
                      <TrendingDown className="size-3" />
                    ) : (
                      <Minus className="size-3" />
                    )}
                    {delta > 0 ? `+${delta.toLocaleString("vi-VN")}` : delta.toLocaleString("vi-VN")}
                  </span>
                  <span className="text-muted-foreground truncate">so với kỳ trước</span>
                </div>
              ) : (
                <span className="text-xs text-muted-foreground">Kỳ hiện tại</span>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
