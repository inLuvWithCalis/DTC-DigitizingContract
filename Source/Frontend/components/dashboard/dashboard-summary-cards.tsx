import type { LucideIcon } from "lucide-react";
import {
  CircleCheckBig,
  Clock3,
  FilePenLine,
  Files,
  ScanLine,
  Send,
  TimerOff,
} from "lucide-react";

import type { DashboardSummaryItem } from "@/services/dashboard-api";

const definitions: Record<
  string,
  { label: string; icon: LucideIcon; tone: string }
> = {
  total: { label: "Tổng hợp đồng", icon: Files, tone: "text-blue-600 bg-blue-500/10" },
  drafting: { label: "Đang soạn / đàm phán", icon: FilePenLine, tone: "text-violet-600 bg-violet-500/10" },
  pendingApproval: { label: "Chờ duyệt", icon: Clock3, tone: "text-amber-600 bg-amber-500/10" },
  pendingSignature: { label: "Chờ ký", icon: Send, tone: "text-cyan-600 bg-cyan-500/10" },
  signed: { label: "Đã ký", icon: ScanLine, tone: "text-emerald-600 bg-emerald-500/10" },
  completedRejected: { label: "Hoàn thành / từ chối", icon: CircleCheckBig, tone: "text-slate-600 bg-slate-500/10" },
  expiring: { label: "Sắp hết hạn", icon: TimerOff, tone: "text-rose-600 bg-rose-500/10" },
};

export function DashboardSummaryCards({ items }: { items: DashboardSummaryItem[] }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      {items.map((item) => {
        const definition = definitions[item.key] ?? definitions.total;
        const Icon = definition.icon;
        const delta = item.previousCount == null ? null : item.count - item.previousCount;
        return (
          <div key={item.key} className="rounded-2xl border bg-card p-5 shadow-sm">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">{definition.label}</p>
                <p className="mt-2 text-3xl font-bold tracking-tight">{item.count.toLocaleString("vi-VN")}</p>
              </div>
              <span className={`flex size-11 items-center justify-center rounded-xl ${definition.tone}`}>
                <Icon className="size-5" />
              </span>
            </div>
            {delta != null && (
              <p className="mt-3 text-xs text-muted-foreground">
                <span className={delta > 0 ? "text-emerald-600" : delta < 0 ? "text-rose-600" : undefined}>
                  {delta > 0 ? "+" : ""}{delta.toLocaleString("vi-VN")}
                </span>{" "}
                so với kỳ trước
              </p>
            )}
          </div>
        );
      })}
    </div>
  );
}
