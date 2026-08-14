"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Activity,
  AlertCircle,
  ArrowRight,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  History,
  PencilLine,
  RefreshCw,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  UserCheck,
  UserRound,
} from "lucide-react";

import { PermissionGuard } from "@/components/auth/permission-guard";
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
  DateRangeFilter,
  type DateRange,
} from "@/components/ui/custom/date-range-filter";
import { Header } from "@/components/ui/custom/header";
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
import { RBAC_PERMISSIONS } from "@/lib/rbac";
import { cn } from "@/lib/utils";
import {
  employeeApi,
  getEmployeeStatusLabel,
  getEmployeeTypeLabel,
  type EmployeeDirectoryResponse,
} from "@/services/employees-api";
import {
  SECURITY_AUDIT_ACTIONS,
  SECURITY_AUDIT_RESULTS,
  securityAuditApi,
  type SecurityAuditPagedResult,
  type TenantSecurityAuditResponse,
} from "@/services/security-audits-api";

interface SecurityAuditFilters {
  action: string;
  result: string;
  actorEmployeeId: string;
  dateRange: DateRange;
}

const createInitialFilters = (): SecurityAuditFilters => ({
  action: "all",
  result: "all",
  actorEmployeeId: "",
  dateRange: { from: undefined, to: undefined },
});

const ACTION_LABELS: Record<string, string> = {
  AccessDenied: "Từ chối truy cập",
  EmployeeCreated: "Tạo nhân viên",
  EmployeeRoleChanged: "Thay đổi vai trò nhân viên",
  EmployeeStatusChanged: "Thay đổi trạng thái nhân viên",
  EmployeePasswordReset: "Đặt lại mật khẩu nhân viên",
  ManagerRoleChanged: "Thay đổi quyền quản lý",
};

const RESULT_LABELS: Record<string, string> = {
  Success: "Thành công",
  Denied: "Bị từ chối",
  Failed: "Thất bại",
};

const RESULT_STYLES: Record<string, string> = {
  Success:
    "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-300",
  Denied:
    "border-orange-200 bg-orange-50 text-orange-700 dark:border-orange-900 dark:bg-orange-950/50 dark:text-orange-300",
  Failed:
    "border-red-200 bg-red-50 text-red-700 dark:border-red-900 dark:bg-red-950/50 dark:text-red-300",
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

function SecurityAuditChanges({
  audit,
}: {
  audit: TenantSecurityAuditResponse;
}) {
  const roleChanged =
    audit.previousEmployeeType !== null &&
    audit.previousEmployeeType !== undefined &&
    audit.newEmployeeType !== null &&
    audit.newEmployeeType !== undefined;
  const statusChanged =
    audit.previousStatus !== null &&
    audit.previousStatus !== undefined &&
    audit.newStatus !== null &&
    audit.newStatus !== undefined;

  if (!roleChanged && !statusChanged) return null;

  return (
    <div className="mt-3 space-y-2.5 rounded-lg border border-border/70 bg-muted/30 p-3 text-xs">
      {roleChanged && (
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border/50 pb-2.5 last:border-b-0 last:pb-0">
          <div className="flex items-center gap-1.5 font-medium text-foreground">
            <UserCheck className="size-3.5 text-primary" />
            <span>Thay đổi vai trò:</span>
          </div>
          <div className="flex items-center gap-1.5">
            <Badge
              variant="outline"
              className="bg-background/80 font-normal text-muted-foreground line-through"
            >
              {getEmployeeTypeLabel(audit.previousEmployeeType)}
            </Badge>
            <ArrowRight className="size-3.5 text-muted-foreground shrink-0" />
            <Badge
              variant="secondary"
              className="border border-primary/20 bg-primary/10 font-semibold text-primary"
            >
              {getEmployeeTypeLabel(audit.newEmployeeType)}
            </Badge>
          </div>
        </div>
      )}
      {statusChanged && (
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-1.5 font-medium text-foreground">
            <Activity className="size-3.5 text-primary" />
            <span>Thay đổi trạng thái:</span>
          </div>
          <div className="flex items-center gap-1.5">
            <Badge
              variant="outline"
              className="bg-background/80 font-normal text-muted-foreground line-through"
            >
              {getEmployeeStatusLabel(audit.previousStatus)}
            </Badge>
            <ArrowRight className="size-3.5 text-muted-foreground shrink-0" />
            <Badge
              variant="secondary"
              className={cn(
                "border font-semibold",
                audit.newStatus === 1
                  ? "border-emerald-500/20 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300"
                  : "border-rose-500/20 bg-rose-500/10 text-rose-700 dark:text-rose-300",
              )}
            >
              {getEmployeeStatusLabel(audit.newStatus)}
            </Badge>
          </div>
        </div>
      )}
    </div>
  );
}

function SecurityAuditContent() {
  const [draftFilters, setDraftFilters] =
    useState<SecurityAuditFilters>(createInitialFilters);
  const [appliedFilters, setAppliedFilters] =
    useState<SecurityAuditFilters>(createInitialFilters);
  const [data, setData] = useState<
    SecurityAuditPagedResult<TenantSecurityAuditResponse>
  >({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
  });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [refreshKey, setRefreshKey] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [employees, setEmployees] = useState<EmployeeDirectoryResponse[]>([]);

  const employeeNames = useMemo(
    () =>
      new Map(
        employees.map((employee) => [
          employee.employeeId,
          employee.employeeFullName,
        ]),
      ),
    [employees],
  );
  const activeFilterCount = [
    appliedFilters.action !== "all",
    appliedFilters.result !== "all",
    appliedFilters.actorEmployeeId,
    appliedFilters.dateRange.from || appliedFilters.dateRange.to,
  ].filter(Boolean).length;

  useEffect(() => {
    employeeApi
      .getDirectory()
      .then(setEmployees)
      .catch(() => setEmployees([]));
  }, []);

  const loadAudits = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      const response = await securityAuditApi.getList({
        page,
        pageSize,
        action:
          appliedFilters.action === "all" ? undefined : appliedFilters.action,
        result:
          appliedFilters.result === "all" ? undefined : appliedFilters.result,
        actorEmployeeId: appliedFilters.actorEmployeeId
          ? Number(appliedFilters.actorEmployeeId)
          : undefined,
        fromUtc: toStartOfDayUtc(appliedFilters.dateRange.from),
        toUtc: toEndOfDayUtc(appliedFilters.dateRange.to),
      });
      setData(response);
    } catch (loadError) {
      setData({
        items: [],
        totalCount: 0,
        page,
        pageSize,
        totalPages: 0,
      });
      setError(getApiErrorMessage(loadError, "Không thể tải nhật ký bảo mật."));
    } finally {
      setIsLoading(false);
    }
  }, [appliedFilters, page, pageSize]);

  useEffect(() => {
    void loadAudits();
  }, [loadAudits, refreshKey]);

  const applyFilters = () => {
    setPage(1);
    setAppliedFilters(draftFilters);
    setIsFiltersOpen(false);
  };

  const clearFilters = () => {
    const initial = createInitialFilters();
    setDraftFilters(initial);
    setAppliedFilters(initial);
    setPage(1);
    setIsFiltersOpen(false);
  };

  return (
    <Card className="gap-0 overflow-hidden py-0">
      <CardHeader className="border-b py-5">
        <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
          <div>
            <CardTitle className="flex items-center gap-2">
              <History className="size-5 text-primary" />
              Lịch sử bảo mật
            </CardTitle>
            <CardDescription className="mt-1.5">
              Theo dõi các lần từ chối truy cập và thay đổi nhạy cảm trong đơn
              vị.
            </CardDescription>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setRefreshKey((value) => value + 1)}
            disabled={isLoading}
          >
            <RefreshCw className={cn("size-4", isLoading && "animate-spin")} />
            Làm mới
          </Button>
        </div>
      </CardHeader>

      <Collapsible open={isFiltersOpen} onOpenChange={setIsFiltersOpen}>
        <div className="flex items-center justify-between gap-3 px-6 py-3">
          <CollapsibleTrigger asChild>
            <Button
              type="button"
              variant="ghost"
              className="-ml-3 flex-1 justify-start sm:flex-none"
            >
              <SlidersHorizontal className="size-4" />
              Bộ lọc
              {activeFilterCount > 0 && (
                <Badge className="min-w-5 rounded-full px-1.5">
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
                  onValueChange={(action) =>
                    setDraftFilters((current) => ({ ...current, action }))
                  }
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent showSearch={false}>
                    <SelectItem value="all">Tất cả hành động</SelectItem>
                    {SECURITY_AUDIT_ACTIONS.map((action) => (
                      <SelectItem key={action} value={action}>
                        {ACTION_LABELS[action] ?? action}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label>Kết quả</Label>
                <Select
                  value={draftFilters.result}
                  onValueChange={(result) =>
                    setDraftFilters((current) => ({ ...current, result }))
                  }
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent showSearch={false}>
                    <SelectItem value="all">Tất cả kết quả</SelectItem>
                    {SECURITY_AUDIT_RESULTS.map((result) => (
                      <SelectItem key={result} value={result}>
                        {RESULT_LABELS[result] ?? result}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="security-audit-actor">ID người thực hiện</Label>
                <Input
                  id="security-audit-actor"
                  inputMode="numeric"
                  value={draftFilters.actorEmployeeId}
                  onChange={(event) =>
                    setDraftFilters((current) => ({
                      ...current,
                      actorEmployeeId: event.target.value.replace(/\D/g, ""),
                    }))
                  }
                  placeholder="Ví dụ: 12"
                />
              </div>
            </div>

            <div className="mt-3 space-y-1.5">
              <Label>Khoảng thời gian</Label>
              <DateRangeFilter
                dateRange={draftFilters.dateRange}
                onChange={(dateRange) =>
                  setDraftFilters((current) => ({ ...current, dateRange }))
                }
                buttonClassName="bg-white"
              />
            </div>

            <div className="mt-4 flex flex-wrap justify-end gap-2">
              <Button type="button" variant="ghost" onClick={clearFilters}>
                Xóa bộ lọc
              </Button>
              <Button type="button" onClick={applyFilters}>
                <Search className="size-4" /> Áp dụng
              </Button>
            </div>
          </CardContent>
        </CollapsibleContent>
      </Collapsible>

      <CardContent className="py-5">
        {error ? (
          <Alert variant="destructive">
            <AlertCircle />
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
            <ShieldCheck className="mx-auto size-10 text-muted-foreground/50" />
            <p className="mt-3 font-medium">Chưa có hoạt động phù hợp</p>
            <p className="mt-1 text-sm text-muted-foreground">
              Thử thay đổi bộ lọc hoặc làm mới dữ liệu.
            </p>
          </div>
        ) : (
          <div className="space-y-0">
            {data.items.map((audit, index) => (
              <article
                key={audit.authorizationAuditId}
                className="relative grid grid-cols-[36px_minmax(0,1fr)] gap-3 pb-6 last:pb-0"
              >
                {index < data.items.length - 1 && (
                  <span className="absolute bottom-0 left-[17px] top-9 w-px bg-border" />
                )}
                <span className="z-10 flex size-9 items-center justify-center rounded-full border bg-background text-primary shadow-sm">
                  <ShieldCheck className="size-4" />
                </span>

                <div className="min-w-0 rounded-lg border p-4">
                  <div className="flex flex-col justify-between gap-2 lg:flex-row lg:items-start">
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="font-semibold">
                          {ACTION_LABELS[audit.action] ?? audit.action}
                        </h3>
                        <Badge
                          variant="outline"
                          className={RESULT_STYLES[audit.result]}
                        >
                          {RESULT_LABELS[audit.result] ?? audit.result}
                        </Badge>
                      </div>
                      <div className="mt-1.5 flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
                        <span className="inline-flex items-center gap-1.5">
                          <UserRound className="size-3.5" />
                          {audit.actorEmployeeId
                            ? employeeNames.get(audit.actorEmployeeId) ||
                              `Nhân viên #${audit.actorEmployeeId}`
                            : audit.actorType}
                        </span>
                        <span className="inline-flex items-center gap-0">
                          <PencilLine className="size-3.5 text-muted-foreground shrink-0" />
                          <ArrowRight className="size-3.5 text-muted-foreground shrink-0" />
                        </span>
                        <span className="inline-flex items-center gap-1.5">
                          <UserRound className="size-3.5" />

                          {audit.targetId
                            ? ` ${employeeNames.get(parseInt(audit.targetId))}`
                            : ""}
                        </span>
                      </div>
                    </div>
                    <time className="shrink-0 text-xs text-muted-foreground">
                      {formatDateTime(audit.occurredAt)}
                    </time>
                  </div>

                  {audit.failureCode && (
                    <p className="mt-3 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                      <span className="font-medium">Mã lỗi: </span>
                      {audit.failureCode}
                    </p>
                  )}

                  <SecurityAuditChanges audit={audit} />

                  <details className="mt-3 text-xs text-muted-foreground">
                    <summary className="cursor-pointer select-none hover:text-foreground">
                      Chi tiết kỹ thuật
                    </summary>
                    <div className="mt-2 space-y-1 break-all pl-3">
                      <p>Correlation ID: {audit.correlationId}</p>
                      {audit.ipAddress && <p>IP: {audit.ipAddress}</p>}
                      {audit.userAgent && <p>User agent: {audit.userAgent}</p>}
                    </div>
                  </details>
                </div>
              </article>
            ))}
          </div>
        )}
      </CardContent>

      {!error && data.totalCount > 0 && (
        <div className="flex flex-col gap-3 border-t px-6 py-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-muted-foreground">
            {data.totalCount} hoạt động · Trang {data.page}/
            {Math.max(data.totalPages, 1)}
          </p>
          <div className="flex items-center gap-2">
            <Select
              value={String(pageSize)}
              onValueChange={(value) => {
                setPageSize(Number(value));
                setPage(1);
              }}
            >
              <SelectTrigger className="h-9">
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
              size="icon"
              onClick={() => setPage((value) => Math.max(1, value - 1))}
              disabled={page <= 1 || isLoading}
              aria-label="Trang trước"
            >
              <ChevronLeft className="size-4" />
            </Button>
            <Button
              type="button"
              variant="outline"
              size="icon"
              onClick={() =>
                setPage((value) => Math.min(data.totalPages, value + 1))
              }
              disabled={page >= data.totalPages || isLoading}
              aria-label="Trang sau"
            >
              <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      )}
    </Card>
  );
}

export default function SecurityAuditsPage() {
  return (
    <>
      <Header title="Nhật ký bảo mật" />
      <div className="grow overflow-y-auto p-3 sm:p-6 lg:p-10">
        <div className="mx-auto space-y-6">
          <div className="flex items-start gap-3">
            <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <ShieldCheck className="size-5" />
            </span>
            <div>
              <h1 className="text-2xl font-bold tracking-tight">
                Nhật ký bảo mật
              </h1>
              <p className="mt-1 text-sm text-muted-foreground">
                Tra cứu hoạt động bảo mật theo người thực hiện, hành động, kết
                quả và thời gian.
              </p>
            </div>
          </div>

          <PermissionGuard
            permission={RBAC_PERMISSIONS.securityAuditReadTenant}
          >
            <SecurityAuditContent />
          </PermissionGuard>
        </div>
      </div>
    </>
  );
}
