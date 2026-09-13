"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  AlertCircle,
  Building2,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Copy,
  Download,
  History,
  KeyRound,
  RefreshCw,
  Search,
  ShieldAlert,
  ShieldCheck,
  SlidersHorizontal,
  User,
  UserCog,
} from "lucide-react";
import { toast } from "sonner";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  DateRange,
  DateRangeFilter,
} from "@/components/ui/custom/date-range-filter";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { getApiErrorMessage } from "@/lib/api-error";
import { formatDateTime } from "@/lib/format-date-time";
import { cn } from "@/lib/utils";
import {
  CENTRAL_SECURITY_AUDIT_ACTIONS,
  CENTRAL_SECURITY_AUDIT_RESULTS,
  centralSecurityAuditApi,
  type CentralSecurityAuditFilterRequest,
  type CentralSecurityAuditPagedResult,
  type CentralSecurityAuditResponse,
} from "@/services/security-audits-api";

interface AuditFilters {
  action: string;
  result: string;
  tenantCode: string;
  tenantId: string;
  actorId: string;
  dateRange: DateRange;
}

const EMPTY_DATE_RANGE: DateRange = { from: undefined, to: undefined };

const initialFilters: AuditFilters = {
  action: "all",
  result: "all",
  tenantCode: "",
  tenantId: "",
  actorId: "",
  dateRange: EMPTY_DATE_RANGE,
};

const ACTION_CONFIG: Record<
  string,
  {
    label: string;
    icon: React.ComponentType<{ className?: string }>;
    description?: string;
  }
> = {
  CentralApiAccessDenied: {
    label: "Từ chối truy cập API trung tâm",
    icon: ShieldAlert,
    description: "Yêu cầu gọi API trung tâm bị hệ thống từ chối quyền truy cập",
  },
  SystemAdminLogin: {
    label: "Đăng nhập System Admin",
    icon: KeyRound,
    description: "System Admin đăng nhập thành công vào trang quản trị",
  },
  TenantProvisioned: {
    label: "Khởi tạo Tenant mới",
    icon: Building2,
    description: "Một tổ chức thuê bao mới đã được cấp phát trên Central Database",
  },
  ManagerRoleChanged: {
    label: "Thay đổi vai trò Manager",
    icon: UserCog,
    description: "Cập nhật phân quyền hoặc bổ nhiệm tài khoản Quản trị Tenant",
  },
};

const RESULT_LABELS: Record<string, string> = {
  Success: "Thành công",
  Denied: "Bị từ chối",
  Failed: "Thất bại",
};

const RESULT_STYLES: Record<string, string> = {
  Success:
    "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-300",
  Failed:
    "border-red-200 bg-red-50 text-red-700 dark:border-red-900 dark:bg-red-950/50 dark:text-red-300",
  Denied:
    "border-orange-200 bg-orange-50 text-orange-700 dark:border-orange-900 dark:bg-orange-950/50 dark:text-orange-300",
};

const TENANT_STATUS_LABELS: Record<number, string> = {
  0: "Đang hoạt động",
  1: "Đang khởi tạo",
  2: "Tạm khóa",
  3: "Lỗi khởi tạo",
  4: "Đang chờ",
};

const toPositiveInteger = (value: string) => {
  if (!value.trim()) return undefined;
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined;
};

const toStartOfDayUtc = (date?: Date) => {
  if (!date) return undefined;
  const value = new Date(date);
  value.setHours(0, 0, 0, 0);
  return value.toISOString();
};

const toEndOfDayUtc = (date?: Date) => {
  if (!date) return undefined;
  const value = new Date(date);
  value.setHours(23, 59, 59, 999);
  return value.toISOString();
};

export function CentralSecurityAuditLog() {
  const [data, setData] = useState<CentralSecurityAuditPagedResult>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
  });

  const [draftFilters, setDraftFilters] = useState<AuditFilters>(initialFilters);
  const [appliedFilters, setAppliedFilters] = useState<AuditFilters>(initialFilters);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const [filterError, setFilterError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isExporting, setIsExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const activeFilterCount = useMemo(() => {
    return [
      appliedFilters.action !== "all",
      appliedFilters.result !== "all",
      appliedFilters.tenantCode.trim(),
      appliedFilters.tenantId.trim(),
      appliedFilters.actorId.trim(),
      appliedFilters.dateRange.from || appliedFilters.dateRange.to,
    ].filter(Boolean).length;
  }, [appliedFilters]);

  const loadAudits = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    const fromUtc = toStartOfDayUtc(appliedFilters.dateRange.from);
    const toUtc = toEndOfDayUtc(appliedFilters.dateRange.to);

    if (fromUtc && toUtc && new Date(fromUtc) > new Date(toUtc)) {
      setError("Thời điểm bắt đầu không được sau thời điểm kết thúc.");
      setIsLoading(false);
      return;
    }

    const params: CentralSecurityAuditFilterRequest = {
      page,
      pageSize,
      action: appliedFilters.action === "all" ? undefined : appliedFilters.action,
      result: appliedFilters.result === "all" ? undefined : appliedFilters.result,
      tenantCode: appliedFilters.tenantCode.trim() || undefined,
      tenantId: toPositiveInteger(appliedFilters.tenantId),
      actorSystemAdminId: toPositiveInteger(appliedFilters.actorId),
      fromUtc,
      toUtc,
    };

    try {
      const response = await centralSecurityAuditApi.getList(params);
      setData(response);
    } catch (loadError) {
      setData({ items: [], totalCount: 0, page: 1, pageSize, totalPages: 0 });
      setError(
        getApiErrorMessage(loadError, "Không thể tải nhật ký bảo mật trung tâm."),
      );
    } finally {
      setIsLoading(false);
    }
  }, [appliedFilters, page, pageSize]);

  useEffect(() => {
    queueMicrotask(() => void loadAudits());
  }, [loadAudits]);

  const applyFilters = () => {
    if (
      draftFilters.dateRange.from &&
      draftFilters.dateRange.to &&
      draftFilters.dateRange.from > draftFilters.dateRange.to
    ) {
      setFilterError("Thời điểm bắt đầu không được lớn hơn thời điểm kết thúc.");
      return;
    }

    setFilterError(null);
    setPage(1);
    setAppliedFilters(draftFilters);
    setIsFiltersOpen(false);
  };

  const clearFilters = () => {
    setDraftFilters(initialFilters);
    setAppliedFilters(initialFilters);
    setFilterError(null);
    setPage(1);
    setIsFiltersOpen(false);
  };

  const handleCopy = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    toast.success(`Đã sao chép ${label}: ${text}`);
  };

  const exportCsv = () => {
    if (data.items.length === 0) {
      toast.error("Không có dữ liệu nhật ký để xuất.");
      return;
    }

    try {
      setIsExporting(true);
      const headers = [
        "Audit ID",
        "Thời gian",
        "Tenant Code",
        "Tenant ID",
        "Hành động",
        "Kết quả",
        "Người thực hiện",
        "Đối tượng",
        "Mã lỗi",
        "Địa chỉ IP",
        "Correlation ID",
      ];

      const rows = data.items.map((item) => [
        item.centralSecurityAuditId,
        `"${formatDateTime(item.occurredAt)}"`,
        `"${item.tenantCode || ""}"`,
        item.tenantId ?? "",
        `"${ACTION_CONFIG[item.action]?.label || item.action}"`,
        `"${RESULT_LABELS[item.result] || item.result}"`,
        `"${item.actorDisplayName || (item.actorSystemAdminId ? `System Admin #${item.actorSystemAdminId}` : "")}"`,
        `"${(item.targetType || "") + (item.targetId ? ` #${item.targetId}` : "")}"`,
        `"${item.failureCode || ""}"`,
        `"${item.ipAddress || ""}"`,
        `"${item.correlationId || ""}"`,
      ]);

      const csvContent =
        "\uFEFF" + [headers.join(","), ...rows.map((r) => r.join(","))].join("\n");
      const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `central-security-audits-${new Date()
        .toISOString()
        .slice(0, 10)}.csv`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
      toast.success("Đã xuất nhật ký bảo mật (CSV) thành công.");
    } catch {
      toast.error("Không thể xuất file CSV.");
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <Card className="gap-0 overflow-hidden py-0 border-border shadow-sm">
      <CardHeader className="border-b py-5">
        <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
          <div>
            <CardTitle className="flex items-center gap-2 text-lg">
              <History className="size-5 text-primary" />
              Lịch sử hoạt động bảo mật
            </CardTitle>
            <CardDescription className="mt-1.5">
              Theo dõi các sự kiện an ninh, cấp quyền và thao tác quản trị trên Central Database.
            </CardDescription>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={exportCsv}
              disabled={isLoading || isExporting || data.items.length === 0}
            >
              <Download
                className={cn("size-4", isExporting && "animate-pulse")}
              />
              Xuất CSV
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => void loadAudits()}
              disabled={isLoading}
            >
              <RefreshCw
                className={cn("size-4", isLoading && "animate-spin")}
              />
              Làm mới
            </Button>
          </div>
        </div>
      </CardHeader>

      <Collapsible open={isFiltersOpen} onOpenChange={setIsFiltersOpen}>
        <div className="flex items-center justify-between gap-3 px-6 py-3 border-b bg-muted/20">
          <CollapsibleTrigger asChild>
            <Button
              type="button"
              variant="ghost"
              className="-ml-3 flex-1 justify-start sm:flex-none"
            >
              <SlidersHorizontal className="size-4 mr-2" />
              Bộ lọc
              {activeFilterCount > 0 && (
                <Badge className="ml-2 min-w-5 rounded-full px-1.5">
                  {activeFilterCount}
                </Badge>
              )}
              <ChevronDown
                className={cn(
                  "ml-auto size-4 transition-transform duration-200 sm:ml-1",
                  isFiltersOpen && "rotate-180",
                )}
              />
            </Button>
          </CollapsibleTrigger>
          {!isFiltersOpen && activeFilterCount > 0 && (
            <span className="hidden text-xs text-muted-foreground sm:inline">
              Đang áp dụng {activeFilterCount} bộ lọc
            </span>
          )}
        </div>

        <CollapsibleContent className="overflow-hidden border-b data-[state=closed]:animate-out data-[state=closed]:fade-out data-[state=closed]:slide-out-to-top-2 data-[state=open]:animate-in data-[state=open]:fade-in data-[state=open]:slide-in-from-top-2">
          <CardContent className="py-4">
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              <div className="space-y-1.5">
                <Label>Hành động</Label>
                <Select
                  value={draftFilters.action}
                  onValueChange={(val) =>
                    setDraftFilters((curr) => ({ ...curr, action: val }))
                  }
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent showSearch={false}>
                    <SelectItem value="all">Tất cả hành động</SelectItem>
                    {CENTRAL_SECURITY_AUDIT_ACTIONS.map((action) => (
                      <SelectItem key={action} value={action}>
                        {ACTION_CONFIG[action]?.label || action}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label>Kết quả</Label>
                <Select
                  value={draftFilters.result}
                  onValueChange={(val) =>
                    setDraftFilters((curr) => ({ ...curr, result: val }))
                  }
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent showSearch={false}>
                    <SelectItem value="all">Tất cả kết quả</SelectItem>
                    {CENTRAL_SECURITY_AUDIT_RESULTS.map((res) => (
                      <SelectItem key={res} value={res}>
                        {RESULT_LABELS[res] || res}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label>Khoảng thời gian</Label>
                <DateRangeFilter
                  dateRange={draftFilters.dateRange}
                  onChange={(dateRange) =>
                    setDraftFilters((curr) => ({ ...curr, dateRange }))
                  }
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="audit-tenant-code">Mã Tenant</Label>
                <Input
                  id="audit-tenant-code"
                  value={draftFilters.tenantCode}
                  onChange={(e) =>
                    setDraftFilters((curr) => ({
                      ...curr,
                      tenantCode: e.target.value,
                    }))
                  }
                  placeholder="Ví dụ: tenant_demo"
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="audit-actor-id">ID System Admin</Label>
                <Input
                  id="audit-actor-id"
                  inputMode="numeric"
                  value={draftFilters.actorId}
                  onChange={(e) =>
                    setDraftFilters((curr) => ({
                      ...curr,
                      actorId: e.target.value.replace(/\D/g, ""),
                    }))
                  }
                  placeholder="Chỉ nhập số"
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="audit-tenant-id">ID Tenant</Label>
                <Input
                  id="audit-tenant-id"
                  inputMode="numeric"
                  value={draftFilters.tenantId}
                  onChange={(e) =>
                    setDraftFilters((curr) => ({
                      ...curr,
                      tenantId: e.target.value.replace(/\D/g, ""),
                    }))
                  }
                  placeholder="Chỉ nhập số"
                />
              </div>
            </div>

            {filterError && (
              <p className="mt-3 text-sm text-destructive">{filterError}</p>
            )}

            <div className="mt-4 flex flex-wrap justify-end gap-2">
              <Button type="button" variant="ghost" onClick={clearFilters}>
                Xóa bộ lọc
              </Button>
              <Button type="button" onClick={applyFilters}>
                <Search className="size-4 mr-1.5" /> Áp dụng
              </Button>
            </div>
          </CardContent>
        </CollapsibleContent>
      </Collapsible>

      <CardContent className="py-5">
        {error ? (
          <Alert variant="destructive">
            <AlertCircle className="size-4" />
            <AlertTitle>Không tải được lịch sử</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        ) : isLoading ? (
          <div className="space-y-4">
            {Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="flex gap-3">
                <Skeleton className="size-9 shrink-0 rounded-full" />
                <div className="flex-1 space-y-2">
                  <Skeleton className="h-5 w-2/3" />
                  <Skeleton className="h-4 w-1/2" />
                  <Skeleton className="h-14 w-full" />
                </div>
              </div>
            ))}
          </div>
        ) : data.items.length === 0 ? (
          <div className="py-12 text-center">
            <History className="mx-auto size-10 text-muted-foreground/50" />
            <p className="mt-3 font-medium text-foreground">
              Chưa có hoạt động phù hợp
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              Thử thay đổi bộ lọc hoặc làm mới dữ liệu.
            </p>
          </div>
        ) : (
          <div className="space-y-0">
            {data.items.map((audit: CentralSecurityAuditResponse, index: number) => {
              const actionMeta = ACTION_CONFIG[audit.action];
              const ActionIcon = actionMeta?.icon || ShieldCheck;
              const actionLabel = actionMeta?.label || audit.action;

              return (
                <article
                  key={audit.centralSecurityAuditId}
                  className="relative grid grid-cols-[36px_minmax(0,1fr)] gap-3 pb-6 last:pb-0"
                >
                  {index < data.items.length - 1 && (
                    <span className="absolute bottom-0 left-[17px] top-9 w-px bg-border" />
                  )}
                  <span className="z-10 flex size-9 items-center justify-center rounded-full border bg-background text-primary shadow-sm">
                    <ActionIcon className="size-4" />
                  </span>

                  <div className="min-w-0 rounded-lg border bg-card p-4 transition-colors hover:border-primary/30">
                    <div className="flex flex-col justify-between gap-2 lg:flex-row lg:items-start">
                      <div>
                        <div className="flex flex-wrap items-center gap-2">
                          <h3 className="font-semibold text-foreground">
                            {actionLabel}
                          </h3>
                          <Badge
                            variant="outline"
                            className={cn("text-xs font-medium", RESULT_STYLES[audit.result])}
                          >
                            {RESULT_LABELS[audit.result] || audit.result}
                          </Badge>
                        </div>

                        <div className="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
                          {/* Tenant info */}
                          <div className="flex items-center gap-1">
                            <Building2 className="size-3.5" />
                            <span>Tenant:</span>
                            {audit.tenantCode ? (
                              <span className="font-medium text-foreground font-mono bg-muted/60 px-1.5 py-0.5 rounded border border-border/50">
                                {audit.tenantCode}
                              </span>
                            ) : audit.tenantId ? (
                              <span className="font-medium text-foreground">
                                #{audit.tenantId}
                              </span>
                            ) : (
                              <span>Hệ thống chung</span>
                            )}
                          </div>

                          {/* Actor info */}
                          <div className="flex items-center gap-1">
                            <User className="size-3.5" />
                            <span>Thực hiện:</span>
                            <span className="font-medium text-foreground">
                              {audit.actorDisplayName ||
                                (audit.actorSystemAdminId
                                  ? `System Admin #${audit.actorSystemAdminId}`
                                  : "Hệ thống")}
                            </span>
                          </div>

                          {/* Target info */}
                          {audit.targetType && (
                            <div className="flex items-center gap-1">
                              <span>Đối tượng:</span>
                              <span className="font-medium text-foreground">
                                {audit.targetType}
                                {audit.targetId ? ` #${audit.targetId}` : ""}
                              </span>
                            </div>
                          )}
                        </div>
                      </div>

                      <time className="shrink-0 text-xs text-muted-foreground font-mono">
                        {formatDateTime(audit.occurredAt)}
                      </time>
                    </div>

                    {/* Status change if present */}
                    {(audit.previousStatus !== null && audit.previousStatus !== undefined ||
                      audit.newStatus !== null && audit.newStatus !== undefined) && (
                      <div className="mt-2.5 flex items-center gap-2 text-xs bg-muted/40 p-2 rounded-md border border-border/50">
                        <span className="text-muted-foreground">Trạng thái:</span>
                        <span className="line-through text-muted-foreground">
                          {audit.previousStatus !== null && audit.previousStatus !== undefined
                            ? TENANT_STATUS_LABELS[audit.previousStatus] ?? `Mã ${audit.previousStatus}`
                            : "—"}
                        </span>
                        <span>→</span>
                        <span className="font-medium text-foreground">
                          {audit.newStatus !== null && audit.newStatus !== undefined
                            ? TENANT_STATUS_LABELS[audit.newStatus] ?? `Mã ${audit.newStatus}`
                            : "—"}
                        </span>
                      </div>
                    )}

                    {/* Employee Type change if present */}
                    {(audit.previousEmployeeType !== null && audit.previousEmployeeType !== undefined ||
                      audit.newEmployeeType !== null && audit.newEmployeeType !== undefined) && (
                      <div className="mt-2.5 flex items-center gap-2 text-xs bg-muted/40 p-2 rounded-md border border-border/50">
                        <span className="text-muted-foreground">Vai trò quản lý:</span>
                        <span className="line-through text-muted-foreground">
                          {audit.previousEmployeeType === 1 ? "Manager" : "Nhân viên"}
                        </span>
                        <span>→</span>
                        <span className="font-medium text-primary">
                          {audit.newEmployeeType === 1 ? "Manager" : "Nhân viên"}
                        </span>
                      </div>
                    )}

                    {/* Failure code if any */}
                    {audit.failureCode && (
                      <p className="mt-2 text-xs text-destructive bg-destructive/10 px-2.5 py-1 rounded border border-destructive/20 inline-block font-mono">
                        Mã lỗi: {audit.failureCode}
                      </p>
                    )}

                    {/* Technical details details/summary */}
                    <details className="mt-3 text-xs text-muted-foreground">
                      <summary className="cursor-pointer select-none hover:text-foreground font-medium">
                        Chi tiết kỹ thuật
                      </summary>
                      <div className="mt-2 space-y-1.5 break-all pl-3 border-l-2 border-border text-xs font-mono">
                        <p>Audit ID: {audit.centralSecurityAuditId}</p>
                        {audit.tenantId && <p>Tenant ID: {audit.tenantId}</p>}
                        <div className="flex items-center gap-1.5">
                          <span>Correlation ID: {audit.correlationId}</span>
                          <button
                            type="button"
                            onClick={() => handleCopy(audit.correlationId, "Correlation ID")}
                            className="p-0.5 rounded hover:bg-muted text-muted-foreground hover:text-foreground"
                            title="Sao chép Correlation ID"
                          >
                            <Copy className="size-3" />
                          </button>
                        </div>
                        {audit.ipAddress && <p>IP Address: {audit.ipAddress}</p>}
                        {audit.userAgent && <p className="text-[11px] text-muted-foreground/80">User Agent: {audit.userAgent}</p>}
                      </div>
                    </details>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </CardContent>

      {!error && data.totalCount > 0 && (
        <div className="flex flex-col gap-3 border-t px-6 py-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-muted-foreground">
            {data.totalCount} hoạt động · Trang {data.page}/{Math.max(data.totalPages, 1)}
          </p>
          <div className="flex items-center gap-2">
            <Select
              value={String(pageSize)}
              onValueChange={(value) => {
                setPageSize(Number(value));
                setPage(1);
              }}
            >
              <SelectTrigger className="h-9 w-[120px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent showSearch={false}>
                {[10, 20, 50, 100].map((size) => (
                  <SelectItem key={size} value={String(size)}>
                    {size} / trang
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isLoading || page <= 1}
              onClick={() => setPage((p) => Math.max(p - 1, 1))}
            >
              <ChevronLeft className="size-4" /> Trước
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isLoading || page >= data.totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Sau <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      )}
    </Card>
  );
}
