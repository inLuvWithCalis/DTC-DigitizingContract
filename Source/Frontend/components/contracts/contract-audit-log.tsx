"use client";

import { useCallback, useEffect, useState } from "react";
import {
  AlertCircle,
  Bot,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  History,
  RefreshCw,
  Search,
  SlidersHorizontal,
  UserRound,
} from "lucide-react";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  DateRangeFilter,
  type DateRange,
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
import { formatDateTime } from "@/lib/format-date-time";
import { cn } from "@/lib/utils";
import {
  CONTRACT_AUDIT_ACTION_LABELS,
  CONTRACT_AUDIT_ACTION_TYPES,
  CONTRACT_AUDIT_ACTOR_LABELS,
  CONTRACT_AUDIT_ACTOR_TYPES,
  CONTRACT_AUDIT_RESULT_LABELS,
  CONTRACT_AUDIT_RESULTS,
  contractAuditApi,
  type ContractAuditActionType,
  type ContractAuditActorType,
  type ContractAuditFilterRequest,
  type ContractAuditResponse,
  type ContractAuditResult,
} from "@/services/contract-audit-api";
import {
  ContractStatus,
  getContractStatusLabel,
} from "@/services/contract-api";

interface ContractAuditLogProps {
  contractId?: number;
  versionId?: number;
  mode?: "contract" | "tenant";
}

interface AuditFilters {
  contractId: string;
  versionId: string;
  actorType: ContractAuditActorType | "all";
  actionType: ContractAuditActionType | "all";
  result: ContractAuditResult | "all";
  dateRange: DateRange;
}

const EMPTY_DATE_RANGE: DateRange = { from: undefined, to: undefined };

const createInitialFilters = (versionId?: number): AuditFilters => ({
  contractId: "",
  versionId: versionId ? String(versionId) : "",
  actorType: "all",
  actionType: "all",
  result: "all",
  dateRange: EMPTY_DATE_RANGE,
});

const RESULT_STYLES: Record<ContractAuditResult, string> = {
  Succeeded:
    "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-300",
  Failed:
    "border-red-200 bg-red-50 text-red-700 dark:border-red-900 dark:bg-red-950/50 dark:text-red-300",
  Denied:
    "border-orange-200 bg-orange-50 text-orange-700 dark:border-orange-900 dark:bg-orange-950/50 dark:text-orange-300",
  RateLimited:
    "border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950/50 dark:text-amber-300",
  ConcurrencyConflict:
    "border-violet-200 bg-violet-50 text-violet-700 dark:border-violet-900 dark:bg-violet-950/50 dark:text-violet-300",
};

const FIELD_LABELS: Record<string, string> = {
  Status: "Trạng thái",
  ResponsibleEmployeeId: "Nhân viên phụ trách",
  CurrentVersionId: "Phiên bản hiện tại",
  ContractName: "Tên hợp đồng",
  EffectiveDate: "Ngày hiệu lực",
  ExpireDate: "Ngày hết hạn",
  CurrencyCode: "Đơn vị tiền tệ",
  TotalAmount: "Tổng giá trị",
  ItemCount: "Số hạng mục",
  TermCount: "Số điều khoản",
  SourceVersionId: "Phiên bản nguồn",
  NewVersionId: "Phiên bản mới",
  SourceVersionLocked: "Khóa phiên bản nguồn",
  Source: "Nguồn",
  Target: "Đích",
  TermId: "Điều khoản",
  ParentCommentId: "Bình luận cha",
  State: "Trạng thái",
  VerificationPhoneId: "Số xác minh",
  PhoneSource: "Nguồn số điện thoại",
  LinkId: "Link truy cập",
  LinkState: "Trạng thái link",
  ExpiresAt: "Hết hạn lúc",
  CustomerOtpChallengeId: "Mã thử thách OTP",
  ChallengeState: "Trạng thái OTP",
  FailedAttemptCount: "Số lần nhập sai",
  CustomerAccessSessionId: "Phiên khách hàng",
  SessionState: "Trạng thái phiên",
  IdleExpiresAt: "Hết hạn do không hoạt động",
  HardExpiresAt: "Hết hạn hoàn toàn",
  RevocationReasonCode: "Mã lý do thu hồi",
};

const SUBJECT_LABELS: Record<string, string> = {
  Contract: "Hợp đồng",
  ContractVersion: "Phiên bản",
  NegotiationComment: "Bình luận đàm phán",
  CustomerAccessLink: "Link khách hàng",
  CustomerOtpChallenge: "Yêu cầu OTP",
  CustomerAccessSession: "Phiên khách hàng",
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

const formatFieldLabel = (key: string) =>
  FIELD_LABELS[key] ?? key.replace(/([a-z0-9])([A-Z])/g, "$1 $2");

const formatAuditValue = (key: string, value: unknown): string => {
  if (value === null || value === undefined || value === "") return "—";
  if (key === "Status" && !Number.isNaN(Number(value))) {
    return getContractStatusLabel(Number(value) as ContractStatus);
  }
  if (typeof value === "boolean") return value ? "Có" : "Không";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
};

const getErrorMessage = (error: unknown) => {
  const apiError = error as {
    response?: { status?: number; data?: { message?: string } };
    message?: string;
  };

  if (apiError.response?.status === 403) {
    return "Bạn không có quyền xem phạm vi nhật ký này.";
  }

  return (
    apiError.response?.data?.message ??
    apiError.message ??
    "Không thể tải lịch sử hoạt động."
  );
};

function AuditChanges({ audit }: { audit: ContractAuditResponse }) {
  const previousValues = audit.previousValues ?? {};
  const newValues = audit.newValues ?? {};
  const keys = Array.from(
    new Set([...Object.keys(previousValues), ...Object.keys(newValues)]),
  );

  if (keys.length === 0) return null;

  return (
    <div className="mt-3 overflow-hidden rounded-lg border bg-muted/20">
      {keys.map((key) => (
        <div
          key={key}
          className="grid gap-1 border-b px-3 py-2 text-xs last:border-b-0 sm:grid-cols-[minmax(130px,0.7fr)_1fr_auto_1fr] sm:items-center"
        >
          <span className="font-medium text-foreground">
            {formatFieldLabel(key)}
          </span>
          <span className="break-all text-muted-foreground">
            {formatAuditValue(key, previousValues[key])}
          </span>
          <span className="hidden text-muted-foreground sm:inline">→</span>
          <span className="break-all font-medium text-foreground">
            {formatAuditValue(key, newValues[key])}
          </span>
        </div>
      ))}
    </div>
  );
}

function AuditActor({ audit }: { audit: ContractAuditResponse }) {
  const label = CONTRACT_AUDIT_ACTOR_LABELS[audit.actorType];
  const actorName = audit.actorDisplayName?.trim() || label;
  const actorId =
    audit.actorType === "Employee"
      ? audit.actorEmployeeId
      : audit.actorType === "Customer"
        ? audit.actorCustomerAccessSessionId
        : null;

  if (audit.actorType === "System") {
    return (
      <span className="inline-flex items-center gap-1.5">
        <Bot className="size-3.5" /> {label}
      </span>
    );
  }

  return (
    <span className="inline-flex items-center gap-1.5">
      <UserRound className="size-3.5" />
      {actorName}
      {actorId && (
        <span>
          ({audit.actorType === "Employee" ? "NV" : "Phiên"} #{actorId})
        </span>
      )}
    </span>
  );
}

export function ContractAuditLog({
  contractId,
  versionId,
  mode = contractId ? "contract" : "tenant",
}: ContractAuditLogProps) {
  const [draftFilters, setDraftFilters] = useState<AuditFilters>(() =>
    createInitialFilters(versionId),
  );
  const [appliedFilters, setAppliedFilters] = useState<AuditFilters>(() =>
    createInitialFilters(versionId),
  );
  const [items, setItems] = useState<ContractAuditResponse[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filterError, setFilterError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const activeFilterCount = [
    mode === "tenant" && appliedFilters.contractId,
    appliedFilters.versionId,
    appliedFilters.actorType !== "all",
    appliedFilters.actionType !== "all",
    appliedFilters.result !== "all",
    appliedFilters.dateRange.from || appliedFilters.dateRange.to,
  ].filter(Boolean).length;

  const loadAudits = useCallback(async () => {
    const params: ContractAuditFilterRequest = {
      contractId: contractId ?? toPositiveInteger(appliedFilters.contractId),
      versionId: toPositiveInteger(appliedFilters.versionId),
      actorType:
        appliedFilters.actorType === "all"
          ? undefined
          : appliedFilters.actorType,
      actionType:
        appliedFilters.actionType === "all"
          ? undefined
          : appliedFilters.actionType,
      result:
        appliedFilters.result === "all" ? undefined : appliedFilters.result,
      fromUtc: toStartOfDayUtc(appliedFilters.dateRange.from),
      toUtc: toEndOfDayUtc(appliedFilters.dateRange.to),
      page,
      pageSize,
    };

    try {
      setIsLoading(true);
      setError(null);
      const response = await contractAuditApi.getList(params);
      setItems(response.items);
      setTotalCount(response.totalCount);
      setTotalPages(response.totalPages);
    } catch (loadError) {
      setItems([]);
      setTotalCount(0);
      setTotalPages(0);
      setError(getErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }, [appliedFilters, contractId, page, pageSize]);

  useEffect(() => {
    void loadAudits();
  }, [loadAudits, refreshKey]);

  const applyFilters = () => {
    if (
      mode === "tenant" &&
      draftFilters.contractId.trim() &&
      !toPositiveInteger(draftFilters.contractId)
    ) {
      setFilterError("Mã hợp đồng phải là số nguyên dương.");
      return;
    }

    if (
      draftFilters.versionId.trim() &&
      !toPositiveInteger(draftFilters.versionId)
    ) {
      setFilterError("Mã phiên bản phải là số nguyên dương.");
      return;
    }

    setFilterError(null);
    setPage(1);
    setAppliedFilters(draftFilters);
    setIsFiltersOpen(false);
  };

  const clearFilters = () => {
    const initial = createInitialFilters(versionId);
    setDraftFilters(initial);
    setAppliedFilters(initial);
    setFilterError(null);
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
              Lịch sử hoạt động
            </CardTitle>
            <CardDescription className="mt-1.5">
              {mode === "contract"
                ? "Theo dõi các thay đổi và thao tác đã xảy ra trên hợp đồng."
                : "Theo dõi hoạt động hợp đồng trong toàn bộ đơn vị."}
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
            <div
              className={`grid gap-3 md:grid-cols-2 ${
                mode === "tenant" ? "xl:grid-cols-4" : "xl:grid-cols-3"
              }`}
            >
              {mode === "tenant" && (
                <div className="space-y-1.5">
                  <Label htmlFor="audit-contract-id">Mã hợp đồng</Label>
                  <Input
                    id="audit-contract-id"
                    type="number"
                    min={1}
                    maxLength={0}
                    value={draftFilters.contractId}
                    onChange={(event) =>
                      setDraftFilters((current) => ({
                        ...current,
                        contractId: event.target.value,
                      }))
                    }
                    placeholder="Tất cả hợp đồng"
                  />
                </div>
              )}

              <div className="space-y-1.5">
                <Label htmlFor="audit-version-id">Mã phiên bản</Label>
                <Input
                  id="audit-version-id"
                  type="number"
                  min={1}
                  maxLength={0}
                  value={draftFilters.versionId}
                  onChange={(event) =>
                    setDraftFilters((current) => ({
                      ...current,
                      versionId: event.target.value,
                    }))
                  }
                  placeholder="Tất cả phiên bản"
                />
              </div>

              <div className="space-y-1.5">
                <Label>Người thực hiện</Label>
                <Select
                  value={draftFilters.actorType}
                  onValueChange={(value) =>
                    setDraftFilters((current) => ({
                      ...current,
                      actorType: value as AuditFilters["actorType"],
                    }))
                  }
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tất cả</SelectItem>
                    {CONTRACT_AUDIT_ACTOR_TYPES.map((actorType) => (
                      <SelectItem key={actorType} value={actorType}>
                        {CONTRACT_AUDIT_ACTOR_LABELS[actorType]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label>Kết quả</Label>
                <Select
                  value={draftFilters.result}
                  onValueChange={(value) =>
                    setDraftFilters((current) => ({
                      ...current,
                      result: value as AuditFilters["result"],
                    }))
                  }
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tất cả</SelectItem>
                    {CONTRACT_AUDIT_RESULTS.map((result) => (
                      <SelectItem key={result} value={result}>
                        {CONTRACT_AUDIT_RESULT_LABELS[result]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="mt-2 grid grid-cols-2 gap-3 space-y-1.5">
              <div className="space-y-1.5">
                <Label>Hành động</Label>
                <Select
                  value={draftFilters.actionType}
                  onValueChange={(value) =>
                    setDraftFilters((current) => ({
                      ...current,
                      actionType: value as AuditFilters["actionType"],
                    }))
                  }
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tất cả hành động</SelectItem>
                    {CONTRACT_AUDIT_ACTION_TYPES.map((actionType) => (
                      <SelectItem key={actionType} value={actionType}>
                        {CONTRACT_AUDIT_ACTION_LABELS[actionType]}
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
                    setDraftFilters((current) => ({ ...current, dateRange }))
                  }
                  buttonClassName="bg-white"
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
        ) : items.length === 0 ? (
          <div className="py-12 text-center">
            <History className="mx-auto size-10 text-muted-foreground/50" />
            <p className="mt-3 font-medium">Chưa có hoạt động phù hợp</p>
            <p className="mt-1 text-sm text-muted-foreground">
              Thử thay đổi bộ lọc hoặc làm mới dữ liệu.
            </p>
          </div>
        ) : (
          <div className="space-y-0">
            {items.map((audit, index) => (
              <article
                key={audit.contractAuditId}
                className="relative grid grid-cols-[36px_minmax(0,1fr)] gap-3 pb-6 last:pb-0"
              >
                {index < items.length - 1 && (
                  <span className="absolute bottom-0 left-[17px] top-9 w-px bg-border" />
                )}
                <span className="z-10 flex size-9 items-center justify-center rounded-full border bg-background text-primary shadow-sm">
                  <History className="size-4" />
                </span>

                <div className="min-w-0 rounded-lg border p-4">
                  <div className="flex flex-col justify-between gap-2 lg:flex-row lg:items-start">
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="font-semibold">
                          {CONTRACT_AUDIT_ACTION_LABELS[audit.actionType]}
                        </h3>
                        <Badge
                          variant="outline"
                          className={RESULT_STYLES[audit.result]}
                        >
                          {CONTRACT_AUDIT_RESULT_LABELS[audit.result]}
                        </Badge>
                      </div>
                      <div className="mt-1.5 flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
                        <AuditActor audit={audit} />
                        {audit.versionId && (
                          <span>Phiên bản #{audit.versionId}</span>
                        )}
                        <span>
                          {SUBJECT_LABELS[audit.subjectType] ??
                            audit.subjectType}
                          {" #"}
                          {audit.subjectId}
                        </span>
                      </div>
                    </div>
                    <time className="shrink-0 text-xs text-muted-foreground">
                      {formatDateTime(audit.occurredAt)}
                    </time>
                  </div>

                  {audit.reason && (
                    <p className="mt-3 rounded-md bg-muted/50 px-3 py-2 text-sm">
                      <span className="font-medium">Lý do: </span>
                      {audit.reason}
                    </p>
                  )}

                  {audit.failureCode && (
                    <p className="mt-2 text-xs text-destructive">
                      Mã lỗi: {audit.failureCode}
                    </p>
                  )}

                  <AuditChanges audit={audit} />

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

      {!error && totalCount > 0 && (
        <div className="flex flex-col gap-3 border-t px-6 py-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-muted-foreground">
            {totalCount} hoạt động · Trang {page}/{Math.max(totalPages, 1)}
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
                setPage((value) => Math.min(totalPages, value + 1))
              }
              disabled={page >= totalPages || isLoading}
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
