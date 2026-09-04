"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ArrowUpRight,
  Building2,
  CalendarRange,
  ChevronRight,
  Clock,
  History,
  RefreshCw,
  Server,
  ShieldAlert,
  Sparkles,
  User,
} from "lucide-react";

import { AdminDashboardSummaryCards } from "@/components/dashboard/admin-dashboard-summary";
import { SecurityTrendChart } from "@/components/dashboard/security-trend-chart";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  DateRangeFilter,
  type DateRange,
} from "@/components/ui/custom/date-range-filter";
import { Header } from "@/components/ui/custom/header";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuthStore } from "@/hooks/use-auth-store";
import { getApiErrorMessage } from "@/lib/api-error";
import {
  adminDashboardApi,
  type AdminDashboardResponse,
} from "@/services/admin-dashboard-api";
import { cn } from "@/lib/utils";

function initialRange(): DateRange {
  const to = new Date();
  const from = new Date();
  from.setDate(from.getDate() - 29);
  return { from, to };
}

function toIso(value: Date | undefined, endOfDay: boolean) {
  if (!value) return undefined;
  const date = new Date(value);
  date.setHours(
    endOfDay ? 23 : 0,
    endOfDay ? 59 : 0,
    endOfDay ? 59 : 0,
    endOfDay ? 999 : 0,
  );
  return date.toISOString();
}

function getGreeting() {
  const hour = new Date().getHours();
  if (hour < 12) return "Chào buổi sáng";
  if (hour < 18) return "Chào buổi chiều";
  return "Chào buổi tối";
}

const tenantStatusLabels: Record<string, string> = {
  Pending: "Đang chờ",
  Provisioning: "Đang khởi tạo",
  Active: "Hoạt động",
  Failed: "Lỗi",
  Suspended: "Tạm ngưng",
};

function getTenantStatusBadgeClass(status: string) {
  switch (status) {
    case "Active":
      return "border-emerald-500/30 bg-emerald-500/10 text-emerald-600 dark:text-emerald-400";
    case "Provisioning":
      return "border-cyan-500/30 bg-cyan-500/10 text-cyan-600 dark:text-cyan-400";
    case "Suspended":
      return "border-amber-500/30 bg-amber-500/10 text-amber-600 dark:text-amber-400";
    case "Failed":
      return "border-rose-500/30 bg-rose-500/10 text-rose-600 dark:text-rose-400";
    default:
      return "border-slate-500/30 bg-slate-500/10 text-slate-600 dark:text-slate-400";
  }
}

type PresetKey = "7d" | "30d" | "90d" | "year";

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const [dateRange, setDateRange] = useState<DateRange>(initialRange);
  const [activePreset, setActivePreset] = useState<PresetKey | null>("30d");
  const [data, setData] = useState<AdminDashboardResponse | null>(null);
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
      const response = await adminDashboardApi.get({
        from: toIso(dateRange.from, false),
        to: toIso(dateRange.to, true),
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
        getApiErrorMessage(
          loadError,
          "Không thể tải dashboard quản trị hệ thống.",
        ),
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
                    {greeting}, {user?.fullName || "System Admin"}
                  </h1>
                  <Badge
                    variant="outline"
                    className="gap-1.5 py-1 px-3 bg-card/80 backdrop-blur-xs font-semibold text-xs border-primary/20 text-primary"
                  >
                    <span className="size-2 rounded-full bg-emerald-500 animate-pulse" />
                    <span>Central Database</span>
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground max-w-2xl">
                  Tổng quan vận hành hệ thống tập trung từ Central Database,
                  không truy vấn fan-out các tenant.
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
                  <Link href="/tenants">
                    <Building2 className="size-4" />
                    <span>Quản lý Tenant</span>
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
                />
              </div>
            </div>
          </div>

          {/* Error Alert */}
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

          {/* Dashboard Main Content */}
          {isLoading && !data ? (
            <div className="space-y-6 animate-pulse">
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
                {Array.from({ length: 6 }).map((_, index) => (
                  <div
                    key={index}
                    className="flex flex-col justify-between rounded-2xl border bg-card p-4 shadow-xs"
                  >
                    <div className="flex items-start justify-between">
                      <Skeleton className="h-4 w-20 rounded-md" />
                      <Skeleton className="size-9 rounded-xl" />
                    </div>
                    <Skeleton className="mt-3 h-8 w-16 rounded-md" />
                    <Skeleton className="mt-3 h-3 w-24 rounded-md" />
                  </div>
                ))}
              </div>
              <div className="grid gap-6 xl:grid-cols-2">
                <Card className="rounded-2xl border bg-card shadow-sm">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <Skeleton className="h-5 w-44 rounded-md" />
                    <Skeleton className="h-6 w-28 rounded-full" />
                  </CardHeader>
                  <CardContent className="pt-3">
                    <Skeleton className="h-72 w-full rounded-xl" />
                  </CardContent>
                </Card>
                <Card className="rounded-2xl border bg-card shadow-sm">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <Skeleton className="h-5 w-32 rounded-md" />
                    <Skeleton className="h-8 w-20 rounded-lg" />
                  </CardHeader>
                  <CardContent className="space-y-3 pt-3">
                    {Array.from({ length: 4 }).map((_, i) => (
                      <div
                        key={i}
                        className="flex items-center justify-between rounded-xl border p-3"
                      >
                        <div className="space-y-1.5">
                          <Skeleton className="h-4 w-36" />
                          <Skeleton className="h-3 w-28" />
                        </div>
                        <Skeleton className="h-6 w-16 rounded-full" />
                      </div>
                    ))}
                  </CardContent>
                </Card>
              </div>
              <Card className="rounded-2xl border bg-card shadow-sm">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <Skeleton className="h-5 w-48 rounded-md" />
                  <Skeleton className="h-8 w-24 rounded-lg" />
                </CardHeader>
                <CardContent className="space-y-3 pt-3">
                  {Array.from({ length: 3 }).map((_, i) => (
                    <div
                      key={i}
                      className="flex items-center justify-between rounded-xl border p-3"
                    >
                      <div className="space-y-1.5">
                        <Skeleton className="h-4 w-40" />
                        <Skeleton className="h-3 w-32" />
                      </div>
                      <Skeleton className="h-6 w-20 rounded-full" />
                    </div>
                  ))}
                </CardContent>
              </Card>
            </div>
          ) : data ? (
            <>
              {/* KPI Summary Cards */}
              <AdminDashboardSummaryCards items={data.summary} />

              {/* Charts & Recent Tenants Grid */}
              <div className="grid gap-6 xl:grid-cols-2">
                <SecurityTrendChart data={data.securitySeries} />

                {/* Recent Tenants Card */}
                <Card className="rounded-2xl border bg-card shadow-sm">
                  <CardHeader className="flex flex-row items-center justify-between pb-3">
                    <div className="space-y-1">
                      <CardTitle className="flex items-center gap-2.5 text-base font-semibold">
                        <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
                          <Building2 className="size-4" />
                        </div>
                        <span>Tổ chức (Tenant) mới</span>
                      </CardTitle>
                      <p className="text-xs text-muted-foreground">
                        Các tenant vừa được tạo trong khoảng thời gian đã chọn
                      </p>
                    </div>

                    <Button
                      variant="ghost"
                      size="sm"
                      asChild
                      className="h-8 gap-1 text-xs text-muted-foreground hover:text-primary"
                    >
                      <Link href="/tenants">
                        <span>Xem tất cả</span>
                        <ArrowUpRight className="size-3.5" />
                      </Link>
                    </Button>
                  </CardHeader>

                  <CardContent>
                    {data.recentTenants.length === 0 ? (
                      <div className="flex flex-col items-center justify-center py-10 text-center">
                        <div className="flex size-12 items-center justify-center rounded-2xl bg-muted/60 text-muted-foreground">
                          <Building2 className="size-6 stroke-[1.5]" />
                        </div>
                        <p className="mt-3 text-sm font-semibold text-foreground">
                          Không có tenant mới
                        </p>
                        <p className="mt-1 text-xs text-muted-foreground">
                          Không có tổ chức nào được tạo trong khoảng thời gian
                          đã chọn.
                        </p>
                      </div>
                    ) : (
                      <div className="space-y-2.5 max-h-100 overflow-y-auto pr-1">
                        {data.recentTenants.map((tenant) => (
                          <Link
                            href="/tenants"
                            key={tenant.tenantId}
                            className="group flex items-center justify-between gap-3 rounded-xl border border-border/60 bg-muted/20 p-3.5 transition-all duration-200 hover:border-primary/40 hover:bg-muted/40 hover:shadow-sm"
                          >
                            <div className="min-w-0 flex-1 space-y-1">
                              <span className="block truncate text-sm font-semibold text-foreground group-hover:text-primary transition-colors">
                                {tenant.tenantName}
                              </span>
                              <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                                <span className="font-mono font-bold text-primary">
                                  {tenant.tenantCode}
                                </span>
                                <span>•</span>
                                <span>
                                  {new Date(tenant.createdAt).toLocaleString(
                                    "vi-VN",
                                    {
                                      day: "2-digit",
                                      month: "2-digit",
                                      year: "numeric",
                                      hour: "2-digit",
                                      minute: "2-digit",
                                    },
                                  )}
                                </span>
                              </div>
                            </div>

                            <div className="flex items-center gap-2 shrink-0">
                              <Badge
                                variant="outline"
                                className={cn(
                                  "text-xs font-semibold",
                                  getTenantStatusBadgeClass(tenant.status),
                                )}
                              >
                                {tenantStatusLabels[tenant.status] ??
                                  tenant.status}
                              </Badge>
                              <ChevronRight className="size-4 text-muted-foreground transition-transform group-hover:translate-x-0.5 group-hover:text-primary" />
                            </div>
                          </Link>
                        ))}
                      </div>
                    )}
                  </CardContent>
                </Card>
              </div>

              {/* Provisioning Failures Card (if any) */}
              {data.provisioningFailures.length > 0 && (
                <Card className="rounded-2xl border-rose-500/30 bg-rose-500/5 shadow-xs">
                  <CardHeader className="flex flex-row items-center justify-between pb-3">
                    <CardTitle className="flex items-center gap-2.5 text-base font-semibold text-rose-600 dark:text-rose-400">
                      <div className="flex size-8 items-center justify-center rounded-lg bg-rose-500/15 text-rose-600 dark:text-rose-400">
                        <ShieldAlert className="size-4" />
                      </div>
                      <span>Cảnh báo: Khởi tạo (Provisioning) bị lỗi</span>
                    </CardTitle>
                    <Badge
                      variant="outline"
                      className="border-rose-500/30 bg-rose-500/10 text-rose-600 dark:text-rose-400 font-semibold text-xs"
                    >
                      {data.provisioningFailures.length} lỗi cần xử lý
                    </Badge>
                  </CardHeader>
                  <CardContent className="space-y-2.5">
                    {data.provisioningFailures.map((failure) => (
                      <div
                        key={failure.tenantId}
                        className="flex flex-col justify-between gap-2 rounded-xl border border-rose-500/20 bg-card p-3.5 sm:flex-row sm:items-center shadow-xs"
                      >
                        <div className="space-y-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="font-semibold text-sm text-foreground">
                              {failure.tenantName}
                            </span>
                            <span className="font-mono text-xs font-bold text-primary">
                              ({failure.tenantCode})
                            </span>
                          </div>
                          <p className="text-xs text-muted-foreground">
                            Xảy ra lúc:{" "}
                            {new Date(failure.occurredAt).toLocaleString(
                              "vi-VN",
                            )}
                          </p>
                        </div>
                        <Badge
                          variant="destructive"
                          className="self-start sm:self-center font-mono text-xs"
                        >
                          Mã lỗi: {failure.failureCode}
                        </Badge>
                      </div>
                    ))}
                  </CardContent>
                </Card>
              )}

              {/* Central Audits Card */}
              <Card className="rounded-2xl border bg-card shadow-sm">
                <CardHeader className="flex flex-row items-center justify-between pb-3">
                  <div className="space-y-1">
                    <CardTitle className="flex items-center gap-2.5 text-base font-semibold">
                      <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
                        <History className="size-4" />
                      </div>
                      <span>Nhật ký Audit trung tâm gần đây</span>
                    </CardTitle>
                    <p className="text-xs text-muted-foreground">
                      Các thao tác cấu hình hệ thống, cấp quyền và sự kiện an
                      ninh mới nhất
                    </p>
                  </div>

                  <Button
                    asChild
                    variant="ghost"
                    size="sm"
                    className="h-8 gap-1 text-xs text-muted-foreground hover:text-primary"
                  >
                    <Link href="/audit-logs">
                      <span>Xem tất cả</span>
                      <ArrowUpRight className="size-3.5" />
                    </Link>
                  </Button>
                </CardHeader>

                <CardContent>
                  {data.recentAudits.length === 0 ? (
                    <div className="flex flex-col items-center justify-center py-10 text-center">
                      <div className="flex size-12 items-center justify-center rounded-2xl bg-muted/60 text-muted-foreground">
                        <History className="size-6 stroke-[1.5]" />
                      </div>
                      <p className="mt-3 text-sm font-semibold text-foreground">
                        Chưa có nhật ký audit nào
                      </p>
                      <p className="mt-1 text-xs text-muted-foreground">
                        Không phát sinh sự kiện audit trong khoảng thời gian đã
                        chọn.
                      </p>
                    </div>
                  ) : (
                    <div className="space-y-2.5 max-h-100 overflow-y-auto pr-1">
                      {data.recentAudits.map((audit) => (
                        <Link
                          href="/audit-logs"
                          key={audit.auditId}
                          className="group flex flex-col justify-between gap-2 rounded-xl border border-border/60 bg-muted/20 p-3.5 transition-all duration-200 hover:border-primary/40 hover:bg-muted/40 hover:shadow-sm sm:flex-row sm:items-center"
                        >
                          <div className="min-w-0 space-y-1">
                            <span className="block truncate text-sm font-semibold text-foreground group-hover:text-primary transition-colors">
                              {audit.action}
                            </span>
                            <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                              <span className="flex items-center gap-1">
                                <User className="size-3" />
                                <span>
                                  {audit.actorDisplayName || "Hệ thống"}
                                </span>
                              </span>
                              {audit.tenantCode && (
                                <>
                                  <span>•</span>
                                  <span className="font-mono font-bold text-primary">
                                    {audit.tenantCode}
                                  </span>
                                </>
                              )}
                            </div>
                          </div>

                          <div className="flex items-center gap-3 shrink-0 self-start sm:self-center">
                            <Badge
                              variant="outline"
                              className={cn(
                                "text-xs font-semibold",
                                audit.result === "Success"
                                  ? "border-emerald-500/30 bg-emerald-500/10 text-emerald-600 dark:text-emerald-400"
                                  : "border-rose-500/30 bg-rose-500/10 text-rose-600 dark:text-rose-400",
                              )}
                            >
                              {audit.result}
                            </Badge>
                            <span className="text-xs text-muted-foreground">
                              {new Date(audit.occurredAt).toLocaleString(
                                "vi-VN",
                                {
                                  day: "2-digit",
                                  month: "2-digit",
                                  year: "numeric",
                                  hour: "2-digit",
                                  minute: "2-digit",
                                },
                              )}
                            </span>
                            <ChevronRight className="size-4 text-muted-foreground transition-transform group-hover:translate-x-0.5 group-hover:text-primary hidden sm:block" />
                          </div>
                        </Link>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            </>
          ) : null}
        </main>
      </div>
    </>
  );
}
