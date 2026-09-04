"use client";

import { useCallback, useEffect, useState } from "react";
import { RefreshCw, WalletCards } from "lucide-react";

import { ContractStatusChart } from "@/components/dashboard/contract-status-chart";
import { ContractVolumeChart } from "@/components/dashboard/contract-volume-chart";
import { DashboardSkeleton } from "@/components/dashboard/dashboard-skeleton";
import { DashboardSummaryCards } from "@/components/dashboard/dashboard-summary-cards";
import { ExpiringContracts } from "@/components/dashboard/expiring-contracts";
import { RecentContractActivities } from "@/components/dashboard/recent-contract-activities";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DateRangeFilter, type DateRange } from "@/components/ui/custom/date-range-filter";
import { Header } from "@/components/ui/custom/header";
import { useAuthStore } from "@/hooks/use-auth-store";
import { getApiErrorMessage } from "@/lib/api-error";
import { dashboardApi, type DashboardResponse } from "@/services/dashboard-api";

function initialRange(): DateRange {
  const to = new Date();
  const from = new Date();
  from.setDate(from.getDate() - 29);
  return { from, to };
}

function startOfLocalDay(value?: Date) {
  if (!value) return undefined;
  const result = new Date(value);
  result.setHours(0, 0, 0, 0);
  return result.toISOString();
}

function endOfLocalDay(value?: Date) {
  if (!value) return undefined;
  const result = new Date(value);
  result.setHours(23, 59, 59, 999);
  return result.toISOString();
}

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const [dateRange, setDateRange] = useState<DateRange>(initialRange);
  const [data, setData] = useState<DashboardResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    if (dateRange.from && dateRange.to && dateRange.from > dateRange.to) {
      setError("Ngày bắt đầu không được sau ngày kết thúc.");
      return;
    }
    setIsLoading(true);
    setError(null);
    try {
      setData(await dashboardApi.get({
        from: startOfLocalDay(dateRange.from),
        to: endOfLocalDay(dateRange.to),
        expiryDays: 30,
      }));
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, "Không thể tải dữ liệu dashboard."));
    } finally {
      setIsLoading(false);
    }
  }, [dateRange.from, dateRange.to]);

  useEffect(() => {
    queueMicrotask(() => void loadDashboard());
  }, [loadDashboard]);

  return (
    <>
      <Header title="Dashboard" />
      <div className="grow overflow-y-auto">
        <main className="animate-in space-y-6 px-3 py-5 fade-in slide-in-from-bottom-4 duration-500 sm:px-6 sm:py-8 lg:px-10">
          <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-end">
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">Xin chào, {user?.fullName || "bạn"}</h1>
                {data && <Badge variant="secondary">{data.scope === "Tenant" ? "Toàn tenant" : "Hợp đồng phụ trách"}</Badge>}
              </div>
              <p className="mt-1 text-sm text-muted-foreground">Tổng quan vòng đời các hợp đồng bạn được phép xem.</p>
            </div>
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
              <DateRangeFilter dateRange={dateRange} onChange={setDateRange} />
              <Button variant="outline" size="sm" onClick={() => void loadDashboard()} disabled={isLoading}>
                <RefreshCw className={isLoading ? "animate-spin" : undefined} /> Tải lại
              </Button>
            </div>
          </div>

          {error && <Alert variant="destructive"><AlertDescription>{error}</AlertDescription></Alert>}

          {isLoading && !data ? <DashboardSkeleton /> : data ? (
            <>
              <DashboardSummaryCards items={data.summary} />

              <div className="grid gap-6 xl:grid-cols-2">
                <ContractVolumeChart data={data.volumeSeries} />
                <ContractStatusChart data={data.statusDistribution} />
              </div>

              <Card className="rounded-2xl">
                <CardHeader><CardTitle className="flex items-center gap-2 text-base"><WalletCards className="size-4 text-primary" /> Giá trị hợp đồng theo tiền tệ</CardTitle></CardHeader>
                <CardContent>
                  {data.amountByCurrency.length === 0 ? <p className="py-6 text-center text-sm text-muted-foreground">Chưa có giá trị hợp đồng.</p> : (
                    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                      {data.amountByCurrency.map((item) => (
                        <div key={item.currency} className="rounded-xl border bg-muted/25 p-4">
                          <p className="text-xs font-medium text-muted-foreground">{item.currency}</p>
                          <p className="mt-1 text-xl font-bold">{new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 2 }).format(item.amount)}</p>
                        </div>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>

              <div className="grid gap-6 xl:grid-cols-2">
                <RecentContractActivities items={data.recentActivities} />
                <ExpiringContracts items={data.expiringContracts} />
              </div>
            </>
          ) : null}
        </main>
      </div>
    </>
  );
}
