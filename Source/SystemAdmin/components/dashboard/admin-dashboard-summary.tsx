import { Building2, CircleCheckBig, CircleOff, LoaderCircle, Timer, TriangleAlert, type LucideIcon } from "lucide-react";

import type { AdminDashboardSummary } from "@/services/admin-dashboard-api";

const definitions: Record<string, { label: string; icon: LucideIcon; tone: string }> = {
  total: { label: "Tổng tenant", icon: Building2, tone: "bg-blue-500/10 text-blue-600" },
  active: { label: "Đang hoạt động", icon: CircleCheckBig, tone: "bg-emerald-500/10 text-emerald-600" },
  pending: { label: "Đang chờ", icon: Timer, tone: "bg-slate-500/10 text-slate-600" },
  provisioning: { label: "Đang khởi tạo", icon: LoaderCircle, tone: "bg-cyan-500/10 text-cyan-600" },
  suspended: { label: "Tạm ngưng", icon: CircleOff, tone: "bg-amber-500/10 text-amber-600" },
  failed: { label: "Provisioning lỗi", icon: TriangleAlert, tone: "bg-rose-500/10 text-rose-600" },
};

export function AdminDashboardSummaryCards({ items }: { items: AdminDashboardSummary[] }) {
  return <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">{items.map((item) => {
    const definition = definitions[item.key] ?? definitions.total;
    const Icon = definition.icon;
    return <div key={item.key} className="rounded-2xl border bg-card p-5 shadow-sm"><div className="flex items-start justify-between"><div><p className="text-sm font-medium text-muted-foreground">{definition.label}</p><p className="mt-2 text-3xl font-bold">{item.count.toLocaleString("vi-VN")}</p></div><span className={`flex size-11 items-center justify-center rounded-xl ${definition.tone}`}><Icon className="size-5" /></span></div></div>;
  })}</div>;
}
