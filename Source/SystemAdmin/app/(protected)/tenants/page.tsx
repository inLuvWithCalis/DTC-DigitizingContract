"use client";

import { FormEvent, useMemo, useState } from "react";
import axios from "axios";
import {
  Activity,
  AlertTriangle,
  ArrowUpRight,
  Building2,
  Check,
  CheckCircle2,
  CircleGauge,
  Clock3,
  CloudCog,
  Copy,
  Database,
  Eye,
  HardDrive,
  KeyRound,
  Loader2,
  LockKeyhole,
  Mail,
  MapPin,
  Plus,
  RefreshCw,
  Search,
  Server,
  ShieldCheck,
  UnlockKeyhole,
  UserRound,
  Users,
} from "lucide-react";
import { Header } from "@/components/ui/custom/header";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Progress } from "@/components/ui/progress";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  adminTenantsApi,
  TenantDatabaseMode,
  TenantResponseDto,
  TenantStatus as TenantApiStatus,
} from "@/services/admin-tenants";
import {
  DatabaseHealth,
  MOCK_TENANTS,
  TenantManagementItem,
  TenantStatus,
} from "@/services/tenant-management-mock";
import { cn } from "@/lib/utils";

const TENANT_CODE_PATTERN = /^[a-z0-9-]{3,50}$/;
const DATABASE_PREFIX = "ContractManagement_Tenant_";
type StatusFilter = "All" | TenantStatus;

const statusConfig: Record<
  TenantStatus,
  { label: string; className: string; dotClassName: string }
> = {
  Active: {
    label: "Đang hoạt động",
    className:
      "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-300",
    dotClassName: "bg-emerald-500",
  },
  Locked: {
    label: "Đã khóa",
    className:
      "border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900 dark:bg-rose-950/50 dark:text-rose-300",
    dotClassName: "bg-rose-500",
  },
  Provisioning: {
    label: "Đang khởi tạo",
    className:
      "border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950/50 dark:text-amber-300",
    dotClassName: "bg-amber-500",
  },
};

const databaseHealthConfig: Record<
  DatabaseHealth,
  { label: string; className: string }
> = {
  Healthy: {
    label: "Ổn định",
    className: "text-emerald-600 dark:text-emerald-400",
  },
  Warning: {
    label: "Cần theo dõi",
    className: "text-amber-600 dark:text-amber-400",
  },
  Provisioning: {
    label: "Đang khởi tạo",
    className: "text-blue-600 dark:text-blue-400",
  },
};

function normalizeTenantCode(value: string) {
  return value
    .toLowerCase()
    .trimStart()
    .replace(/\s+/g, "-")
    .replace(/[^a-z0-9-]/g, "")
    .replace(/-{2,}/g, "-");
}

function getErrorMessage(error: unknown) {
  if (!axios.isAxiosError(error)) {
    return "Không thể tạo tenant. Vui lòng thử lại.";
  }

  const data = error.response?.data as
    | { message?: string; title?: string; errors?: Record<string, string[]> }
    | undefined;
  const validationMessage = data?.errors
    ? Object.values(data.errors).flat().join(" ")
    : undefined;

  return (
    validationMessage ||
    data?.message ||
    data?.title ||
    "Không thể tạo tenant. Vui lòng thử lại."
  );
}

function formatStorage(megabytes: number) {
  if (megabytes < 1024) return `${megabytes} MB`;
  return `${(megabytes / 1024).toFixed(megabytes >= 10240 ? 0 : 1)} GB`;
}

function storagePercent(tenant: TenantManagementItem) {
  if (!tenant.storageLimitMb) return 0;
  return Math.min(
    100,
    Math.round((tenant.storageUsedMb / tenant.storageLimitMb) * 100),
  );
}

function TenantStatusBadge({ status }: { status: TenantStatus }) {
  const config = statusConfig[status];

  return (
    <Badge
      variant="outline"
      className={cn("gap-1.5 whitespace-nowrap font-medium", config.className)}
    >
      <span className={cn("size-1.5 rounded-full", config.dotClassName)} />
      {config.label}
    </Badge>
  );
}

function DatabaseHealthLabel({ health }: { health: DatabaseHealth }) {
  const config = databaseHealthConfig[health];

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 text-xs font-medium",
        config.className,
      )}
    >
      {health === "Warning" ? (
        <AlertTriangle className="size-3.5" />
      ) : health === "Provisioning" ? (
        <RefreshCw className="size-3.5 animate-spin" />
      ) : (
        <CheckCircle2 className="size-3.5" />
      )}
      {config.label}
    </span>
  );
}

function MetricCard({
  title,
  value,
  description,
  icon: Icon,
  accent = "blue",
}: {
  title: string;
  value: string;
  description: string;
  icon: typeof Building2;
  accent?: "blue" | "emerald" | "amber" | "violet";
}) {
  const accentClasses = {
    blue: "bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-300",
    emerald:
      "bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-300",
    amber:
      "bg-amber-50 text-amber-600 dark:bg-amber-950/40 dark:text-amber-300",
    violet:
      "bg-violet-50 text-violet-600 dark:bg-violet-950/40 dark:text-violet-300",
  };

  return (
    <Card className="gap-0 py-0 shadow-sm">
      <CardContent className="flex items-start justify-between p-5">
        <div>
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          <p className="mt-2 text-2xl font-bold tracking-tight">{value}</p>
          <p className="mt-1 text-xs text-muted-foreground">{description}</p>
        </div>
        <div
          className={cn(
            "flex size-10 items-center justify-center rounded-xl",
            accentClasses[accent],
          )}
        >
          <Icon className="size-5" />
        </div>
      </CardContent>
    </Card>
  );
}

function CreateTenantPanel({
  onCreated,
}: {
  onCreated: (tenant: TenantManagementItem) => void;
}) {
  const [tenantCode, setTenantCode] = useState("");
  const [tenantName, setTenantName] = useState("");
  const [managerCode, setManagerCode] = useState("");
  const [managerAccount, setManagerAccount] = useState("");
  const [managerPassword, setManagerPassword] = useState("");
  const [managerFullName, setManagerFullName] = useState("");
  const [managerMobile, setManagerMobile] = useState("");
  const [managerEmail, setManagerEmail] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [createdTenant, setCreatedTenant] =
    useState<TenantResponseDto | null>(null);

  const normalizedCode = tenantCode.trim();
  const normalizedName = tenantName.trim();
  const codeIsInvalid =
    tenantCode.length > 0 && !TENANT_CODE_PATTERN.test(normalizedCode);
  const databasePreview = `${DATABASE_PREFIX}${normalizedCode || "tenant-code"}`;
  const canSubmit =
    !isSubmitting &&
    !codeIsInvalid &&
    TENANT_CODE_PATTERN.test(normalizedCode) &&
    normalizedName.length > 0 &&
    normalizedName.length <= 200 &&
    managerAccount.trim().length > 0 &&
    managerAccount.trim().length <= 50 &&
    managerPassword.length >= 6 &&
    managerPassword.length <= 100 &&
    managerFullName.trim().length > 0 &&
    managerFullName.trim().length <= 100;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setCreatedTenant(null);

    if (!TENANT_CODE_PATTERN.test(normalizedCode)) {
      setError(
        "Mã tenant phải có 3–50 ký tự, chỉ gồm chữ thường, số và dấu gạch ngang.",
      );
      return;
    }

    if (!normalizedName || normalizedName.length > 200) {
      setError(
        "Tên tenant là bắt buộc và không được vượt quá 200 ký tự.",
      );
      return;
    }

    if (
      !managerAccount.trim() ||
      managerAccount.trim().length > 50 ||
      managerPassword.length < 6 ||
      managerPassword.length > 100 ||
      !managerFullName.trim() ||
      managerFullName.trim().length > 100
    ) {
      setError("Vui lòng nhập đủ tài khoản, mật khẩu và họ tên của Manager đầu tiên.");
      return;
    }

    setIsSubmitting(true);
    try {
      const result = await adminTenantsApi.create({
        tenantCode: normalizedCode,
        tenantName: normalizedName,
        initialManager: {
          employeeCode: managerCode.trim() || null,
          employeeAccount: managerAccount.trim(),
          employeePassword: managerPassword,
          employeeFullName: managerFullName.trim(),
          employeeMobile: managerMobile.trim() || null,
          employeeEmail: managerEmail.trim() || null,
        },
      });
      setCreatedTenant(result);
      onCreated({
        tenantId: String(result.tenantId),
        tenantCode: result.tenantCode,
        tenantName: result.tenantName,
        status:
          result.status === TenantApiStatus.Active
            ? "Active"
            : result.status === TenantApiStatus.Suspended
              ? "Locked"
              : "Provisioning",
        plan: "Starter",
        databaseName: result.databaseName,
        databaseMode:
          result.databaseMode === TenantDatabaseMode.Shared
            ? "Shared"
            : "Dedicated",
        databaseHealth:
          result.status === TenantApiStatus.Active ? "Healthy" : "Provisioning",
        databaseServer: "Đang cập nhật",
        databaseVersion: "SQL Server 2022",
        region: "Southeast Asia",
        storageUsedMb: 0,
        storageLimitMb: 5120,
        totalUsers: 1,
        activeUsers: 0,
        contractCount: 0,
        ownerName: managerFullName.trim(),
        ownerEmail: managerEmail.trim() || "Chưa thiết lập",
        domain: `${result.tenantCode}.econtract.local`,
        createdAt: new Intl.DateTimeFormat("vi-VN").format(new Date()),
        lastActivityAt: "Vừa tạo",
        lastBackupAt: "Chưa có",
      });
      setTenantCode("");
      setTenantName("");
      setManagerCode("");
      setManagerAccount("");
      setManagerPassword("");
      setManagerFullName("");
      setManagerMobile("");
      setManagerEmail("");
    } catch (submitError) {
      setError(getErrorMessage(submitError));
    } finally {
      setManagerPassword("");
      setIsSubmitting(false);
    }
  };

  const handleCopyDatabaseName = async () => {
    if (!createdTenant?.databaseName || typeof navigator === "undefined") {
      return;
    }
    await navigator.clipboard.writeText(createdTenant.databaseName);
  };

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
      <Card className="shadow-sm">
        <CardHeader className="border-b">
          <div className="flex items-center gap-3">
            <div className="flex size-11 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <Building2 className="size-5" />
            </div>
            <div>
              <CardTitle>Khởi tạo tenant mới</CardTitle>
              <p className="mt-1 text-sm text-muted-foreground">
                Thông tin sẽ được gửi đến API và tự động tạo database.
              </p>
            </div>
          </div>
        </CardHeader>

        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid gap-5 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="tenantName">
                  Tên tenant <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="tenantName"
                  value={tenantName}
                  onChange={(event) => setTenantName(event.target.value)}
                  placeholder="Ví dụ: Công ty TNHH DTC"
                  maxLength={200}
                  disabled={isSubmitting}
                  required
                />
                <p className="text-xs text-muted-foreground">
                  Tên tổ chức hiển thị trong hệ thống, tối đa 200 ký tự.
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="tenantCode">
                  Tenant code <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="tenantCode"
                  value={tenantCode}
                  onChange={(event) =>
                    setTenantCode(normalizeTenantCode(event.target.value))
                  }
                  placeholder="Ví dụ: dtc-company"
                  minLength={3}
                  maxLength={50}
                  pattern="[a-z0-9\-]{3,50}"
                  aria-invalid={codeIsInvalid}
                  disabled={isSubmitting}
                  required
                />
                <p
                  className={cn(
                    "text-xs",
                    codeIsInvalid
                      ? "text-destructive"
                      : "text-muted-foreground",
                  )}
                >
                  3–50 ký tự; chỉ dùng chữ thường, số và dấu gạch ngang.
                </p>
              </div>
            </div>

            <div className="space-y-4 rounded-xl border p-4">
              <div>
                <p className="font-semibold">Manager đầu tiên</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  Tài khoản này được tạo cùng tenant và có quyền quản lý nhân viên.
                </p>
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="managerFullName">Họ và tên <span className="text-destructive">*</span></Label>
                  <Input id="managerFullName" value={managerFullName} onChange={(event) => setManagerFullName(event.target.value)} maxLength={100} disabled={isSubmitting} required />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="managerCode">Mã nhân viên</Label>
                  <Input id="managerCode" value={managerCode} onChange={(event) => setManagerCode(event.target.value)} maxLength={30} disabled={isSubmitting} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="managerAccount">Tài khoản <span className="text-destructive">*</span></Label>
                  <Input id="managerAccount" value={managerAccount} onChange={(event) => setManagerAccount(event.target.value)} maxLength={50} autoComplete="off" disabled={isSubmitting} required />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="managerPassword">Mật khẩu <span className="text-destructive">*</span></Label>
                  <Input id="managerPassword" type="password" value={managerPassword} onChange={(event) => setManagerPassword(event.target.value)} minLength={6} maxLength={100} autoComplete="new-password" disabled={isSubmitting} required />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="managerEmail">Email</Label>
                  <Input id="managerEmail" type="email" value={managerEmail} onChange={(event) => setManagerEmail(event.target.value)} maxLength={100} disabled={isSubmitting} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="managerMobile">Số điện thoại</Label>
                  <Input id="managerMobile" type="tel" value={managerMobile} onChange={(event) => setManagerMobile(event.target.value)} maxLength={15} disabled={isSubmitting} />
                </div>
              </div>
            </div>

            <div className="rounded-xl border bg-muted/40 p-4">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div className="min-w-0">
                  <p className="text-sm font-medium">Database dự kiến</p>
                  <p className="mt-1 break-all font-mono text-sm text-muted-foreground">
                    {databasePreview}
                  </p>
                </div>
                <Badge variant="outline">Dedicated</Badge>
              </div>
            </div>

            {error && (
              <Alert variant="destructive">
                <AlertTitle>Tạo tenant không thành công</AlertTitle>
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            {createdTenant && (
              <Alert className="border-emerald-200 bg-emerald-50 text-emerald-950 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-100">
                <CheckCircle2 className="size-4" />
                <AlertTitle>Đã tạo tenant thành công</AlertTitle>
                <AlertDescription className="mt-2">
                  <div className="space-y-3">
                    <p>
                      <span className="font-semibold">
                        {createdTenant.tenantName}
                      </span>{" "}
                      ({createdTenant.tenantCode}) đã được khởi tạo.
                    </p>
                    <div className="flex flex-col gap-2 rounded-lg border border-emerald-200 bg-white/70 p-3 text-sm dark:border-emerald-900 dark:bg-background/40 sm:flex-row sm:items-center sm:justify-between">
                      <span className="break-all font-mono">
                        {createdTenant.databaseName}
                      </span>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={handleCopyDatabaseName}
                      >
                        <Copy className="size-3.5" />
                        Sao chép
                      </Button>
                    </div>
                  </div>
                </AlertDescription>
              </Alert>
            )}

            <Separator />

            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-sm text-muted-foreground">
                Tenant code sẽ được gắn với tên database và không nên thay đổi.
              </p>
              <Button type="submit" disabled={!canSubmit}>
                {isSubmitting ? (
                  <Loader2 className="animate-spin" />
                ) : (
                  <Plus />
                )}
                {isSubmitting ? "Đang khởi tạo..." : "Tạo tenant"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <div className="space-y-6">
        <Card className="shadow-sm">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <CloudCog className="size-5 text-primary" />
              Quy trình tự động
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {[
              "Ghi nhận tenant vào database trung tâm.",
              "Cấp phát database và chạy migration.",
              "Tạo dữ liệu nền và tài khoản quản trị.",
              "Kiểm tra kết nối rồi kích hoạt tenant.",
            ].map((step, index) => (
              <div key={step} className="flex gap-3">
                <div className="flex size-7 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                  {index + 1}
                </div>
                <p className="pt-1 text-sm">{step}</p>
              </div>
            ))}
          </CardContent>
        </Card>

        <Alert>
          <ShieldCheck className="size-4" />
          <AlertTitle>Dữ liệu được cô lập</AlertTitle>
          <AlertDescription>
            Mỗi tenant sử dụng database riêng hoặc vùng dữ liệu được phân tách
            theo cấu hình hệ thống.
          </AlertDescription>
        </Alert>
      </div>
    </div>
  );
}

function TenantDetailSheet({
  tenant,
  open,
  onOpenChange,
  onRequestStatusChange,
}: {
  tenant?: TenantManagementItem;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onRequestStatusChange: (tenant: TenantManagementItem) => void;
}) {
  const [copied, setCopied] = useState(false);

  if (!tenant) return null;

  const percent = storagePercent(tenant);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(tenant.databaseName);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1600);
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full overflow-hidden p-0 sm:max-w-2xl">
        <SheetHeader className="border-b px-6 py-5">
          <div className="flex items-start gap-3 pr-8">
            <div className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <Building2 className="size-5" />
            </div>
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <SheetTitle className="text-lg">{tenant.tenantName}</SheetTitle>
                <TenantStatusBadge status={tenant.status} />
              </div>
              <SheetDescription className="mt-1">
                {tenant.tenantCode} · Gói {tenant.plan}
              </SheetDescription>
            </div>
          </div>
        </SheetHeader>

        <ScrollArea className="min-h-0 flex-1">
          <div className="space-y-6 p-6">
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
              <div className="rounded-xl border bg-muted/20 p-4">
                <Users className="size-4 text-muted-foreground" />
                <p className="mt-3 text-xl font-semibold">
                  {tenant.activeUsers}/{tenant.totalUsers}
                </p>
                <p className="text-xs text-muted-foreground">
                  Người dùng hoạt động
                </p>
              </div>
              <div className="rounded-xl border bg-muted/20 p-4">
                <KeyRound className="size-4 text-muted-foreground" />
                <p className="mt-3 text-xl font-semibold">
                  {tenant.contractCount.toLocaleString("vi-VN")}
                </p>
                <p className="text-xs text-muted-foreground">Hợp đồng</p>
              </div>
              <div className="col-span-2 rounded-xl border bg-muted/20 p-4 sm:col-span-1">
                <Activity className="size-4 text-muted-foreground" />
                <p className="mt-3 text-sm font-semibold">
                  {tenant.lastActivityAt}
                </p>
                <p className="text-xs text-muted-foreground">
                  Hoạt động gần nhất
                </p>
              </div>
            </div>

            <section className="space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="font-semibold">Thông tin database</h3>
                <DatabaseHealthLabel health={tenant.databaseHealth} />
              </div>
              <div className="rounded-xl border">
                <div className="flex items-center gap-3 border-b p-4">
                  <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-300">
                    <Database className="size-4" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-xs text-muted-foreground">
                      Database name
                    </p>
                    <p className="truncate font-mono text-sm font-medium">
                      {tenant.databaseName}
                    </p>
                  </div>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={handleCopy}
                    aria-label="Sao chép tên database"
                  >
                    {copied ? (
                      <Check className="size-4 text-emerald-600" />
                    ) : (
                      <Copy className="size-4" />
                    )}
                  </Button>
                </div>

                <div className="grid gap-0 sm:grid-cols-2">
                  <div className="border-b p-4 sm:border-r">
                    <p className="flex items-center gap-2 text-xs text-muted-foreground">
                      <Server className="size-3.5" /> Máy chủ
                    </p>
                    <p className="mt-1 text-sm font-medium">
                      {tenant.databaseServer}
                    </p>
                  </div>
                  <div className="border-b p-4">
                    <p className="flex items-center gap-2 text-xs text-muted-foreground">
                      <CloudCog className="size-3.5" /> Chế độ
                    </p>
                    <p className="mt-1 text-sm font-medium">
                      {tenant.databaseMode} · {tenant.databaseVersion}
                    </p>
                  </div>
                  <div className="p-4 sm:border-r">
                    <p className="flex items-center gap-2 text-xs text-muted-foreground">
                      <MapPin className="size-3.5" /> Khu vực
                    </p>
                    <p className="mt-1 text-sm font-medium">{tenant.region}</p>
                  </div>
                  <div className="p-4">
                    <p className="flex items-center gap-2 text-xs text-muted-foreground">
                      <Clock3 className="size-3.5" /> Sao lưu gần nhất
                    </p>
                    <p className="mt-1 text-sm font-medium">
                      {tenant.lastBackupAt}
                    </p>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border p-4">
                <div className="flex items-center justify-between text-sm">
                  <span className="flex items-center gap-2 font-medium">
                    <HardDrive className="size-4 text-muted-foreground" />
                    Dung lượng lưu trữ
                  </span>
                  <span className={cn(percent >= 85 && "text-amber-600")}>
                    {formatStorage(tenant.storageUsedMb)} /{" "}
                    {formatStorage(tenant.storageLimitMb)}
                  </span>
                </div>
                <Progress
                  value={percent}
                  className={cn("mt-3 h-2", percent >= 85 && "[&>div]:bg-amber-500")}
                />
                <p className="mt-2 text-xs text-muted-foreground">
                  Đã sử dụng {percent}% hạn mức của gói {tenant.plan}.
                </p>
              </div>
            </section>

            <section className="space-y-3">
              <h3 className="font-semibold">Thông tin vận hành</h3>
              <div className="rounded-xl border">
                {[
                  {
                    icon: UserRound,
                    label: "Người quản trị",
                    value: tenant.ownerName,
                  },
                  {
                    icon: Mail,
                    label: "Email",
                    value: tenant.ownerEmail,
                  },
                  {
                    icon: ArrowUpRight,
                    label: "Tên miền",
                    value: tenant.domain,
                  },
                  {
                    icon: Clock3,
                    label: "Ngày khởi tạo",
                    value: tenant.createdAt,
                  },
                ].map(({ icon: Icon, label, value }, index) => (
                  <div
                    key={label}
                    className={cn(
                      "flex items-center gap-3 px-4 py-3",
                      index > 0 && "border-t",
                    )}
                  >
                    <Icon className="size-4 shrink-0 text-muted-foreground" />
                    <span className="w-32 shrink-0 text-sm text-muted-foreground">
                      {label}
                    </span>
                    <span className="min-w-0 truncate text-sm font-medium">
                      {value}
                    </span>
                  </div>
                ))}
              </div>
            </section>

            <Alert>
              <CircleGauge className="size-4" />
              <AlertTitle>Mock UI quản trị</AlertTitle>
              <AlertDescription>
                Dữ liệu thống kê, dung lượng và thao tác trạng thái đang dùng
                mock. Có thể thay bằng API tenant management khi backend hoàn
                thiện.
              </AlertDescription>
            </Alert>
          </div>
        </ScrollArea>

        <SheetFooter className="border-t bg-background px-6 py-4">
          {tenant.status !== "Provisioning" && (
            <Button
              variant={tenant.status === "Active" ? "destructive" : "default"}
              onClick={() => onRequestStatusChange(tenant)}
            >
              {tenant.status === "Active" ? (
                <LockKeyhole />
              ) : (
                <UnlockKeyhole />
              )}
              {tenant.status === "Active"
                ? "Khóa tenant"
                : "Kích hoạt tenant"}
            </Button>
          )}
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}

export default function TenantsPage() {
  const [tenants, setTenants] =
    useState<TenantManagementItem[]>(MOCK_TENANTS);
  const [activeTab, setActiveTab] = useState("list");
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [selectedTenantId, setSelectedTenantId] = useState<string>();
  const [detailOpen, setDetailOpen] = useState(false);
  const [actionTarget, setActionTarget] =
    useState<TenantManagementItem | null>(null);
  const [actionMessage, setActionMessage] = useState("");

  const filteredTenants = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    return tenants.filter((tenant) => {
      const matchesSearch =
        !query ||
        tenant.tenantName.toLowerCase().includes(query) ||
        tenant.tenantCode.toLowerCase().includes(query) ||
        tenant.databaseName.toLowerCase().includes(query);
      const matchesStatus =
        statusFilter === "All" || tenant.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [searchQuery, statusFilter, tenants]);

  const selectedTenant = tenants.find(
    (tenant) => tenant.tenantId === selectedTenantId,
  );
  const activeCount = tenants.filter(
    (tenant) => tenant.status === "Active",
  ).length;
  const lockedCount = tenants.filter(
    (tenant) => tenant.status === "Locked",
  ).length;
  const totalStorage = tenants.reduce(
    (total, tenant) => total + tenant.storageUsedMb,
    0,
  );
  const warningCount = tenants.filter(
    (tenant) => tenant.databaseHealth === "Warning",
  ).length;

  const openTenantDetail = (tenant: TenantManagementItem) => {
    setSelectedTenantId(tenant.tenantId);
    setDetailOpen(true);
  };

  const handleStatusChange = () => {
    if (!actionTarget) return;

    const nextStatus: TenantStatus =
      actionTarget.status === "Active" ? "Locked" : "Active";
    setTenants((current) =>
      current.map((tenant) =>
        tenant.tenantId === actionTarget.tenantId
          ? {
              ...tenant,
              status: nextStatus,
              activeUsers:
                nextStatus === "Locked"
                  ? 0
                  : Math.max(1, tenant.activeUsers),
              lastActivityAt:
                nextStatus === "Locked" ? "Vừa bị khóa" : "Vừa kích hoạt",
            }
          : tenant,
      ),
    );
    setActionMessage(
      `${actionTarget.tenantName} đã ${
        nextStatus === "Locked" ? "được khóa" : "được kích hoạt"
      }.`,
    );
    setActionTarget(null);
  };

  const handleCreated = (tenant: TenantManagementItem) => {
    setTenants((current) => [
      tenant,
      ...current.filter((item) => item.tenantId !== tenant.tenantId),
    ]);
    setActionMessage(
      `${tenant.tenantName} đã được thêm vào danh sách quản lý.`,
    );
  };

  return (
    <>
      <Header title="Quản lý tenant" />

      <div className="grow overflow-y-auto bg-muted/20">
        <div className="mx-auto w-full max-w-[1500px] space-y-6 px-4 py-6 sm:px-6 lg:px-10 lg:py-8">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <div className="mb-2 flex items-center gap-2 text-sm font-medium text-primary">
                <ShieldCheck className="size-4" />
                System Administration
              </div>
              <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">
                Quản lý tenant
              </h1>
              <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
                Theo dõi tổ chức, trạng thái truy cập, tài nguyên database và
                hoạt động vận hành trên toàn hệ thống.
              </p>
            </div>
            <Button onClick={() => setActiveTab("create")}>
              <Plus />
              Tạo tenant mới
            </Button>
          </div>

          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <MetricCard
              title="Tổng tenant"
              value={tenants.length.toString()}
              description="Tất cả tổ chức trong hệ thống"
              icon={Building2}
            />
            <MetricCard
              title="Đang hoạt động"
              value={activeCount.toString()}
              description={`${lockedCount} tenant đang bị khóa`}
              icon={Activity}
              accent="emerald"
            />
            <MetricCard
              title="Dung lượng đã dùng"
              value={formatStorage(totalStorage)}
              description="Tổng dung lượng tất cả database"
              icon={HardDrive}
              accent="violet"
            />
            <MetricCard
              title="Cảnh báo database"
              value={warningCount.toString()}
              description={
                warningCount
                  ? "Có database gần chạm hạn mức"
                  : "Tất cả database ổn định"
              }
              icon={AlertTriangle}
              accent="amber"
            />
          </div>

          <Tabs value={activeTab} onValueChange={setActiveTab}>
            <TabsList className="grid w-full max-w-md grid-cols-2">
              <TabsTrigger value="list">Danh sách tenant</TabsTrigger>
              <TabsTrigger value="create">Tạo tenant</TabsTrigger>
            </TabsList>

            <TabsContent value="list" className="mt-4 space-y-4">
              {actionMessage && (
                <Alert className="border-emerald-200 bg-emerald-50 text-emerald-950 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-100">
                  <CheckCircle2 className="size-4" />
                  <AlertTitle>Đã cập nhật</AlertTitle>
                  <AlertDescription>{actionMessage}</AlertDescription>
                </Alert>
              )}

              <Card className="gap-0 overflow-hidden py-0 shadow-sm">
                <div className="flex flex-col gap-3 border-b p-4 lg:flex-row lg:items-center lg:justify-between">
                  <div>
                    <h2 className="font-semibold">Danh sách tổ chức</h2>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Hiển thị {filteredTenants.length} trên {tenants.length}{" "}
                      tenant
                    </p>
                  </div>
                  <div className="flex flex-col gap-2 sm:flex-row">
                    <div className="relative sm:w-72">
                      <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        value={searchQuery}
                        onChange={(event) => setSearchQuery(event.target.value)}
                        placeholder="Tìm tên, mã hoặc database..."
                        className="pl-9"
                      />
                    </div>
                    <Select
                      value={statusFilter}
                      onValueChange={(value) =>
                        setStatusFilter(value as StatusFilter)
                      }
                    >
                      <SelectTrigger className="w-full sm:w-48">
                        <SelectValue placeholder="Trạng thái" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="All">Tất cả trạng thái</SelectItem>
                        <SelectItem value="Active">Đang hoạt động</SelectItem>
                        <SelectItem value="Locked">Đã khóa</SelectItem>
                        <SelectItem value="Provisioning">
                          Đang khởi tạo
                        </SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="divide-y md:hidden">
                  {filteredTenants.map((tenant) => {
                    const percent = storagePercent(tenant);
                    return (
                      <div
                        key={tenant.tenantId}
                        role="button"
                        tabIndex={0}
                        onClick={() => openTenantDetail(tenant)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" || event.key === " ") {
                            event.preventDefault();
                            openTenantDetail(tenant);
                          }
                        }}
                        className="block w-full space-y-4 p-4 text-left transition-colors hover:bg-muted/40 active:bg-muted"
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="flex min-w-0 items-center gap-3">
                            <div className="flex size-10 shrink-0 items-center justify-center rounded-xl border bg-primary/5 text-sm font-bold text-primary">
                              {tenant.tenantName.charAt(0)}
                            </div>
                            <div className="min-w-0">
                              <p className="truncate font-medium">{tenant.tenantName}</p>
                              <p className="mt-0.5 truncate text-xs text-muted-foreground">
                                {tenant.tenantCode} · {tenant.plan}
                              </p>
                            </div>
                          </div>
                          <TenantStatusBadge status={tenant.status} />
                        </div>

                        <div className="rounded-lg border bg-muted/20 p-3">
                          <div className="flex min-w-0 items-center gap-2">
                            <Database className="size-4 shrink-0 text-muted-foreground" />
                            <span className="truncate font-mono text-xs">
                              {tenant.databaseName}
                            </span>
                          </div>
                          <div className="mt-2 flex items-center justify-between gap-3">
                            <DatabaseHealthLabel health={tenant.databaseHealth} />
                            <span className="text-xs text-muted-foreground">
                              {formatStorage(tenant.storageUsedMb)} · {percent}%
                            </span>
                          </div>
                          <Progress
                            value={percent}
                            className={cn(
                              "mt-2 h-1.5",
                              percent >= 85 && "[&>div]:bg-amber-500",
                            )}
                          />
                        </div>

                        <div className="grid grid-cols-2 gap-3 text-sm">
                          <div>
                            <p className="text-xs text-muted-foreground">Người dùng</p>
                            <p className="mt-1 font-medium">
                              {tenant.activeUsers}/{tenant.totalUsers} hoạt động
                            </p>
                          </div>
                          <div className="text-right">
                            <p className="text-xs text-muted-foreground">Hoạt động gần nhất</p>
                            <p className="mt-1 font-medium">{tenant.lastActivityAt}</p>
                          </div>
                        </div>
                      </div>
                    );
                  })}

                  {filteredTenants.length === 0 && (
                    <div className="flex min-h-56 flex-col items-center justify-center p-6 text-center">
                      <Search className="size-6 text-muted-foreground" />
                      <p className="mt-3 font-medium">Không tìm thấy tenant</p>
                      <p className="mt-1 text-sm text-muted-foreground">
                        Thử thay đổi từ khóa hoặc bộ lọc trạng thái.
                      </p>
                    </div>
                  )}
                </div>

                <div className="hidden overflow-x-auto md:block">
                  <Table className="min-w-[1080px]">
                    <TableHeader>
                      <TableRow className="bg-muted/40 hover:bg-muted/40">
                        <TableHead className="w-[290px]">
                          Tenant / Tổ chức
                        </TableHead>
                        <TableHead>Trạng thái</TableHead>
                        <TableHead className="w-[260px]">Database</TableHead>
                        <TableHead className="w-[190px]">Dung lượng</TableHead>
                        <TableHead>Người dùng</TableHead>
                        <TableHead>Hoạt động</TableHead>
                        <TableHead className="text-right">Thao tác</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {filteredTenants.map((tenant) => {
                        const percent = storagePercent(tenant);
                        return (
                          <TableRow
                            key={tenant.tenantId}
                            className="cursor-pointer"
                            onClick={() => openTenantDetail(tenant)}
                          >
                            <TableCell>
                              <div className="flex items-center gap-3">
                                <div className="flex size-10 shrink-0 items-center justify-center rounded-xl border bg-primary/5 text-sm font-bold text-primary">
                                  {tenant.tenantName.charAt(0)}
                                </div>
                                <div className="min-w-0">
                                  <p className="truncate font-medium">
                                    {tenant.tenantName}
                                  </p>
                                  <p className="mt-0.5 text-xs text-muted-foreground">
                                    {tenant.tenantCode} · {tenant.plan}
                                  </p>
                                </div>
                              </div>
                            </TableCell>
                            <TableCell>
                              <TenantStatusBadge status={tenant.status} />
                            </TableCell>
                            <TableCell>
                              <div className="flex items-start gap-2">
                                <Database className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
                                <div className="min-w-0">
                                  <p
                                    className="max-w-[210px] truncate font-mono text-xs"
                                    title={tenant.databaseName}
                                  >
                                    {tenant.databaseName}
                                  </p>
                                  <div className="mt-1">
                                    <DatabaseHealthLabel
                                      health={tenant.databaseHealth}
                                    />
                                  </div>
                                </div>
                              </div>
                            </TableCell>
                            <TableCell>
                              <div className="flex items-center justify-between text-xs">
                                <span>
                                  {formatStorage(tenant.storageUsedMb)}
                                </span>
                                <span
                                  className={cn(
                                    "text-muted-foreground",
                                    percent >= 85 && "text-amber-600",
                                  )}
                                >
                                  {percent}%
                                </span>
                              </div>
                              <Progress
                                value={percent}
                                className={cn(
                                  "mt-2 h-1.5",
                                  percent >= 85 && "[&>div]:bg-amber-500",
                                )}
                              />
                            </TableCell>
                            <TableCell>
                              <p className="font-medium">
                                {tenant.activeUsers}/{tenant.totalUsers}
                              </p>
                              <p className="text-xs text-muted-foreground">
                                đang hoạt động
                              </p>
                            </TableCell>
                            <TableCell>
                              <p className="text-sm">{tenant.lastActivityAt}</p>
                              <p className="text-xs text-muted-foreground">
                                Tạo {tenant.createdAt}
                              </p>
                            </TableCell>
                            <TableCell className="text-right">
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={(event) => {
                                  event.stopPropagation();
                                  openTenantDetail(tenant);
                                }}
                              >
                                <Eye />
                                Chi tiết
                              </Button>
                            </TableCell>
                          </TableRow>
                        );
                      })}

                      {filteredTenants.length === 0 && (
                        <TableRow>
                          <TableCell colSpan={7} className="h-56 text-center">
                            <div className="mx-auto flex max-w-sm flex-col items-center">
                              <div className="flex size-12 items-center justify-center rounded-full bg-muted">
                                <Search className="size-5 text-muted-foreground" />
                              </div>
                              <p className="mt-4 font-medium">
                                Không tìm thấy tenant
                              </p>
                              <p className="mt-1 text-sm text-muted-foreground">
                                Thử thay đổi từ khóa hoặc bộ lọc trạng thái.
                              </p>
                            </div>
                          </TableCell>
                        </TableRow>
                      )}
                    </TableBody>
                  </Table>
                </div>
              </Card>
            </TabsContent>

            <TabsContent value="create" className="mt-4">
              <CreateTenantPanel onCreated={handleCreated} />
            </TabsContent>
          </Tabs>
        </div>
      </div>

      <TenantDetailSheet
        tenant={selectedTenant}
        open={detailOpen}
        onOpenChange={setDetailOpen}
        onRequestStatusChange={setActionTarget}
      />

      <AlertDialog
        open={Boolean(actionTarget)}
        onOpenChange={(open) => !open && setActionTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {actionTarget?.status === "Active"
                ? "Khóa tenant này?"
                : "Kích hoạt tenant này?"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {actionTarget?.status === "Active"
                ? `Người dùng của ${actionTarget?.tenantName} sẽ không thể đăng nhập. Database và dữ liệu vẫn được giữ nguyên.`
                : `${actionTarget?.tenantName} sẽ có thể đăng nhập và tiếp tục sử dụng hệ thống.`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Hủy</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleStatusChange}
              className={cn(
                actionTarget?.status === "Active" &&
                  "bg-destructive text-white hover:bg-destructive/90",
              )}
            >
              {actionTarget?.status === "Active" ? (
                <LockKeyhole />
              ) : (
                <UnlockKeyhole />
              )}
              {actionTarget?.status === "Active"
                ? "Xác nhận khóa"
                : "Xác nhận kích hoạt"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
