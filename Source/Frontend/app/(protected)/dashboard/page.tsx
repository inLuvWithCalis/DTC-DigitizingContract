"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  CalendarRange,
  Clock,
  FilePlus2,
  RefreshCw,
  Sparkles,
  UserCheck,
} from "lucide-react";

import { ContractStatusChart } from "@/components/dashboard/contract-status-chart";
import { ContractVolumeChart } from "@/components/dashboard/contract-volume-chart";
import { CurrencyAmountCards } from "@/components/dashboard/currency-amount-cards";
import { DashboardSkeleton } from "@/components/dashboard/dashboard-skeleton";
import { DashboardSummaryCards } from "@/components/dashboard/dashboard-summary-cards";
import { ExpiringContracts } from "@/components/dashboard/expiring-contracts";
import { RecentContractActivities } from "@/components/dashboard/recent-contract-activities";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DateRangeFilter,
  type DateRange,
} from "@/components/ui/custom/date-range-filter";
import { Header } from "@/components/ui/custom/header";
import { useAuthStore } from "@/hooks/use-auth-store";
import { getApiErrorMessage } from "@/lib/api-error";
import { dashboardApi, type DashboardResponse } from "@/services/dashboard-api";
import { cn } from "@/lib/utils";

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

function getGreeting() {
  const hour = new Date().getHours();
  if (hour < 12) return "Chào buổi sáng";
  if (hour < 18) return "Chào buổi chiều";
  return "Chào buổi tối";
}

type PresetKey = "7d" | "30d" | "90d" | "year";

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const [dateRange, setDateRange] = useState<DateRange>(initialRange);
  const [activePreset, setActivePreset] = useState<PresetKey | null>("30d");
  const [data, setData] = useState<DashboardResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    if (dateRange.from && dateRange.to && dateRange.from > dateRange.to) {
      setError("Ngày bắt đầu không được sau ngày kết thúc.");
      return;
    }
    setIsLoading(true);
    setError(null);
    try {
      const response = await dashboardApi.get({
        from: startOfLocalDay(dateRange.from),
        to: endOfLocalDay(dateRange.to),
        expiryDays: 30,
      });
      setData(response);
      setLastUpdated(
        new Date().toLocaleTimeString("vi-VN", {
          hour: "2-digit",
          minute: "2-digit",
          second: "2-digit",
        }),
      );
    } catch (loadError) {
      setError(
        getApiErrorMessage(loadError, "Không thể tải dữ liệu dashboard."),
      );
    } finally {
      setIsLoading(false);
    }
  }, [dateRange.from, dateRange.to]);

  useEffect(() => {
    queueMicrotask(() => void loadDashboard());
  }, [loadDashboard]);

  const handleApplyPreset = (preset: PresetKey) => {
    const to = new Date();
    const from = new Date();
    if (preset === "7d") {
      from.setDate(from.getDate() - 6);
    } else if (preset === "30d") {
      from.setDate(from.getDate() - 29);
    } else if (preset === "90d") {
      from.setDate(from.getDate() - 89);
    } else if (preset === "year") {
      from.setMonth(0, 1);
    }
    setActivePreset(preset);
    setDateRange({ from, to });
  };

  const greeting = useMemo(() => getGreeting(), []);

  return (
    <>
      <Header title="Dashboard" />
      <div className="grow overflow-y-auto">
        <main className="space-y-6 px-4 py-6 sm:px-6 lg:px-8 mx-auto animate-in fade-in duration-300">
          {/* Executive Hero Banner */}
          <div className="relative overflow-hidden rounded-3xl border border-border/70 bg-gradient-to-br from-primary/10 via-card to-card p-5 sm:p-7 shadow-xs">
            <div className="relative z-10 flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
              {/* Welcome Info */}
              <div className="space-y-2">
                <div className="flex flex-wrap items-center gap-2.5">
                  <span className="flex size-9 items-center justify-center rounded-xl bg-primary/10 text-primary">
                    <Sparkles className="size-4" />
                  </span>
                  <h1 className="text-2xl font-extrabold tracking-tight text-foreground sm:text-3xl">
                    {greeting}, {user?.fullName || "Quản trị viên"}
                  </h1>
                  {data && (
                    <Badge
                      variant="outline"
                      className="gap-1.5 py-1 px-3 bg-card/80 backdrop-blur-xs font-semibold text-xs border-primary/20 text-primary"
                    >
                      <span className="size-2 rounded-full bg-emerald-500 animate-pulse" />
                      <span>
                        {data.scope === "Tenant"
                          ? "Toàn tổ chức"
                          : "Hợp đồng phụ trách"}
                      </span>
                    </Badge>
                  )}
                </div>
                <p className="text-sm text-muted-foreground max-w-2xl">
                  Theo dõi tiến độ ký kết, trạng thái phê duyệt, dòng tiền và
                  cảnh báo hạn hợp đồng trong thời gian thực.
                </p>
                {lastUpdated && (
                  <p className="flex items-center gap-1 text-xs text-muted-foreground/80">
                    <Clock className="size-3" />
                    <span>Cập nhật lần cuối: {lastUpdated}</span>
                  </p>
                )}
              </div>

              {/* Action Buttons */}
              <div className="flex flex-wrap items-center gap-2.5">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => void loadDashboard()}
                  disabled={isLoading}
                  className="h-9 gap-1.5 rounded-xl border-border bg-card/80 hover:bg-card shadow-xs"
                >
                  <RefreshCw
                    className={cn(
                      "size-3.5",
                      isLoading && "animate-spin text-primary",
                    )}
                  />
                  <span>Làm mới</span>
                </Button>
                <Button
                  asChild
                  size="sm"
                  className="h-9 gap-1.5 rounded-xl shadow-xs transition-transform hover:scale-102"
                >
                  <Link href="/contracts/create">
                    <FilePlus2 className="size-4" />
                    <span>Tạo hợp đồng</span>
                  </Link>
                </Button>
              </div>
            </div>

            {/* Date Range Control Bar */}
            <div className="relative z-10 mt-6 flex flex-col gap-3 pt-5 border-t border-border/50 lg:flex-row lg:items-center lg:justify-between">
              <div className="flex items-center gap-2">
                <CalendarRange className="size-4 text-muted-foreground hidden sm:block" />
                <span className="text-xs font-semibold text-muted-foreground">
                  Khoảng thời gian:
                </span>
                <div className="flex flex-wrap items-center gap-1.5">
                  {(
                    [
                      { key: "7d", label: "7 ngày qua" },
                      { key: "30d", label: "30 ngày qua" },
                      { key: "90d", label: "90 ngày qua" },
                      { key: "year", label: "Năm nay" },
                    ] as const
                  ).map((preset) => (
                    <Button
                      key={preset.key}
                      variant={
                        activePreset === preset.key ? "default" : "outline"
                      }
                      size="sm"
                      onClick={() => handleApplyPreset(preset.key)}
                      className={cn(
                        "h-8 rounded-lg px-2.5 text-xs transition-colors",
                        activePreset === preset.key
                          ? "font-semibold shadow-xs"
                          : "border-border/70 bg-card/60 text-muted-foreground hover:text-foreground",
                      )}
                    >
                      {preset.label}
                    </Button>
                  ))}
                </div>
              </div>

              {/* Custom Date Range Picker */}
              <div className="flex items-center">
                <DateRangeFilter
                  dateRange={dateRange}
                  onChange={(newRange) => {
                    setActivePreset(null);
                    setDateRange(newRange);
                  }}
                  buttonClassName="bg-card/80 h-8 rounded-lg text-xs"
                />
              </div>
            </div>
          </div>

          {/* Error Message */}
          {error && (
            <Alert variant="destructive" className="rounded-2xl">
              <AlertDescription className="flex items-center justify-between">
                <span>{error}</span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => void loadDashboard()}
                  className="h-7 text-xs"
                >
                  Thử lại
                </Button>
              </AlertDescription>
            </Alert>
          )}

          {/* Main Dashboard Content */}
          {isLoading && !data ? (
            <DashboardSkeleton />
          ) : data ? (
            <div className="space-y-6">
              {/* Section 1: KPI Summary Cards */}
              <DashboardSummaryCards items={data.summary} />

              {/* Section 2: Charts Grid */}
              <div className="grid gap-6 xl:grid-cols-2">
                <ContractVolumeChart data={data.volumeSeries} />
                <ContractStatusChart data={data.statusDistribution} />
              </div>

              {/* Section 3: Financial & Currency Breakdown */}
              <CurrencyAmountCards items={data.amountByCurrency} />

              {/* Section 4: Operational Feeds (Activities & Expiring) */}
              <div className="grid gap-6 xl:grid-cols-2">
                <RecentContractActivities items={data.recentActivities} />
                <ExpiringContracts items={data.expiringContracts} />
              </div>
            </div>
          ) : null}
        </main>
      </div>
    </>
  );
}
