"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { RefreshCw, ShieldAlert } from "lucide-react";

import { AdminDashboardSummaryCards } from "@/components/dashboard/admin-dashboard-summary";
import { SecurityTrendChart } from "@/components/dashboard/security-trend-chart";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DateRangeFilter, type DateRange } from "@/components/ui/custom/date-range-filter";
import { Header } from "@/components/ui/custom/header";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuthStore } from "@/hooks/use-auth-store";
import { getApiErrorMessage } from "@/lib/api-error";
import { adminDashboardApi, type AdminDashboardResponse } from "@/services/admin-dashboard-api";

function initialRange(): DateRange {
  const to = new Date();
  const from = new Date();
  from.setDate(from.getDate() - 29);
  return { from, to };
}

function toIso(value: Date | undefined, endOfDay: boolean) {
  if (!value) return undefined;
  const date = new Date(value);
  date.setHours(endOfDay ? 23 : 0, endOfDay ? 59 : 0, endOfDay ? 59 : 0, endOfDay ? 999 : 0);
  return date.toISOString();
}

const tenantStatusLabels: Record<string, string> = {
  Pending: "Đang chờ",
  Provisioning: "Đang khởi tạo",
  Active: "Hoạt động",
  Failed: "Lỗi",
  Suspended: "Tạm ngưng",
};

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const [dateRange, setDateRange] = useState<DateRange>(initialRange);
  const [data, setData] = useState<AdminDashboardResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setData(await adminDashboardApi.get({ from: toIso(dateRange.from, false), to: toIso(dateRange.to, true) }));
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, "Không thể tải dashboard quản trị hệ thống."));
    } finally {
      setIsLoading(false);
    }
  }, [dateRange.from, dateRange.to]);

  useEffect(() => { queueMicrotask(() => void loadDashboard()); }, [loadDashboard]);

  return <>
    <Header title="Dashboard" />
    <div className="grow overflow-y-auto">
      <main className="space-y-6 p-3 sm:p-6 lg:p-10">
        <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-end">
          <div><h1 className="text-2xl font-bold tracking-tight sm:text-3xl">Xin chào, {user?.fullName || "System Admin"}</h1><p className="mt-1 text-sm text-muted-foreground">Tổng quan vận hành lấy từ Central Database, không truy vấn fan-out các tenant.</p></div>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center"><DateRangeFilter dateRange={dateRange} onChange={setDateRange} /><Button variant="outline" size="sm" onClick={() => void loadDashboard()} disabled={isLoading}><RefreshCw className={isLoading ? "animate-spin" : undefined} /> Tải lại</Button></div>
        </div>

        {error && <Alert variant="destructive"><AlertDescription>{error}</AlertDescription></Alert>}
        {isLoading && !data ? <div className="space-y-6"><div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">{Array.from({ length: 4 }, (_, index) => <Skeleton key={index} className="h-32 rounded-2xl" />)}</div><div className="grid gap-6 xl:grid-cols-2"><Skeleton className="h-80 rounded-2xl" /><Skeleton className="h-80 rounded-2xl" /></div></div> : data ? <>
          <AdminDashboardSummaryCards items={data.summary} />
          <div className="grid gap-6 xl:grid-cols-2">
            <SecurityTrendChart data={data.securitySeries} />
            <Card className="rounded-2xl"><CardHeader><CardTitle className="text-base">Tenant mới</CardTitle></CardHeader><CardContent className="space-y-1">{data.recentTenants.length === 0 ? <p className="py-10 text-center text-sm text-muted-foreground">Không có tenant mới trong khoảng đã chọn.</p> : data.recentTenants.map((tenant) => <Link href="/tenants" key={tenant.tenantId} className="flex items-center justify-between gap-3 rounded-xl p-3 hover:bg-muted/60"><span className="min-w-0"><span className="block truncate text-sm font-semibold">{tenant.tenantName}</span><span className="text-xs text-muted-foreground">{tenant.tenantCode} · {new Date(tenant.createdAt).toLocaleString("vi-VN")}</span></span><Badge variant={tenant.status === "Active" ? "default" : "secondary"}>{tenantStatusLabels[tenant.status] ?? tenant.status}</Badge></Link>)}</CardContent></Card>
          </div>
          {data.provisioningFailures.length > 0 && <Card className="rounded-2xl border-destructive/30"><CardHeader><CardTitle className="flex items-center gap-2 text-base text-destructive"><ShieldAlert className="size-4" /> Provisioning gần đây bị lỗi</CardTitle></CardHeader><CardContent className="space-y-2">{data.provisioningFailures.map((failure) => <div key={failure.tenantId} className="flex flex-col justify-between gap-1 rounded-xl bg-destructive/5 p-3 sm:flex-row sm:items-center"><span className="text-sm font-medium">{failure.tenantName} ({failure.tenantCode})</span><span className="text-xs text-muted-foreground">{failure.failureCode} · {new Date(failure.occurredAt).toLocaleString("vi-VN")}</span></div>)}</CardContent></Card>}
          <Card className="rounded-2xl"><CardHeader className="flex-row items-center justify-between"><CardTitle className="text-base">Audit trung tâm gần đây</CardTitle><Button asChild variant="ghost" size="sm"><Link href="/audit-logs">Xem tất cả</Link></Button></CardHeader><CardContent className="space-y-1">{data.recentAudits.length === 0 ? <p className="py-10 text-center text-sm text-muted-foreground">Chưa có audit trong khoảng đã chọn.</p> : data.recentAudits.map((audit) => <Link href="/audit-logs" key={audit.auditId} className="flex flex-col justify-between gap-1 rounded-xl p-3 hover:bg-muted/60 sm:flex-row sm:items-center"><span className="min-w-0"><span className="block truncate text-sm font-semibold">{audit.action}</span><span className="text-xs text-muted-foreground">{audit.actorDisplayName || "Hệ thống"}{audit.tenantCode ? ` · ${audit.tenantCode}` : ""}</span></span><span className="flex items-center gap-2"><Badge variant={audit.result === "Success" ? "default" : "destructive"}>{audit.result}</Badge><span className="text-xs text-muted-foreground">{new Date(audit.occurredAt).toLocaleString("vi-VN")}</span></span></Link>)}</CardContent></Card>
        </> : null}
      </main>
    </div>
  </>;
}
