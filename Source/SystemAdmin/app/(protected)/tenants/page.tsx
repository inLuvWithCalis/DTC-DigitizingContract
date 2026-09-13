"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import {
  Building2,
  CheckCircle2,
  Copy,
  Database,
  Eye,
  EyeOff,
  Layers,
  Loader2,
  Lock,
  Plus,
  RefreshCw,
  Server,
  ShieldCheck,
  Sparkles,
  User,
  Users,
} from "lucide-react";
import { toast } from "sonner";
import { ColumnDef } from "@tanstack/react-table";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Header } from "@/components/ui/custom/header";
import { DataTable } from "@/components/ui/custom/data-table";
import { SelectFilter } from "@/components/ui/custom/select-filter";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import {
  SummaryCardItem,
  SummaryCards,
} from "@/components/ui/custom/summary-cards";
import { getApiErrorMessage } from "@/lib/api-error";
import { cn } from "@/lib/utils";
import {
  adminTenantsApi,
  TenantDatabaseMode,
  TenantResponseDto,
  TenantStatus,
} from "@/services/admin-tenants";

const TENANT_STATUS_OPTIONS = [
  { label: "Tất cả trạng thái", value: "All" },
  { label: "Đang hoạt động", value: String(TenantStatus.Active) },
  { label: "Đang khởi tạo", value: String(TenantStatus.Provisioning) },
  { label: "Tạm khóa", value: String(TenantStatus.Suspended) },
  { label: "Lỗi khởi tạo", value: String(TenantStatus.Failed) },
  { label: "Đang chờ", value: String(TenantStatus.Pending) },
];

const DATABASE_MODE_OPTIONS = [
  { label: "Tất cả chế độ DB", value: "All" },
  { label: "Dedicated", value: String(TenantDatabaseMode.Dedicated) },
  { label: "Shared", value: String(TenantDatabaseMode.Shared) },
];

function TenantStatusBadge({ status }: { status: TenantStatus }) {
  const config: Record<TenantStatus, { label: string; className: string }> = {
    [TenantStatus.Active]: {
      label: "Đang hoạt động",
      className:
        "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-500/10 dark:text-emerald-400 dark:border-emerald-500/20",
    },
    [TenantStatus.Provisioning]: {
      label: "Đang khởi tạo",
      className:
        "bg-cyan-50 text-cyan-700 border-cyan-200 dark:bg-cyan-500/10 dark:text-cyan-400 dark:border-cyan-500/20",
    },
    [TenantStatus.Suspended]: {
      label: "Tạm khóa",
      className:
        "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-500/10 dark:text-amber-400 dark:border-amber-500/20",
    },
    [TenantStatus.Failed]: {
      label: "Lỗi khởi tạo",
      className:
        "bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-500/10 dark:text-rose-400 dark:border-rose-500/20",
    },
    [TenantStatus.Pending]: {
      label: "Đang chờ",
      className:
        "bg-slate-50 text-slate-700 border-slate-200 dark:bg-slate-500/10 dark:text-slate-400 dark:border-slate-500/20",
    },
  };

  const item = config[status] ?? {
    label: "Không xác định",
    className: "bg-secondary text-secondary-foreground border-border",
  };

  return (
    <Badge variant="outline" className={cn("font-medium", item.className)}>
      {item.label}
    </Badge>
  );
}

const initialForm = {
  tenantCode: "",
  tenantName: "",
  employeeCode: "",
  employeeAccount: "",
  employeePassword: "",
  employeeFullName: "",
  employeeMobile: "",
  employeeEmail: "",
};

export default function TenantsPage() {
  const [tenants, setTenants] = useState<TenantResponseDto[]>([]);
  const [filterStatus, setFilterStatus] = useState<string>("All");
  const [filterDbMode, setFilterDbMode] = useState<string>("All");
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [viewingTenant, setViewingTenant] = useState<TenantResponseDto | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [form, setForm] = useState(initialForm);

  const loadTenants = useCallback(async () => {
    setLoading(true);
    try {
      setTenants(await adminTenantsApi.getAll());
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể tải danh sách tenant."));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    queueMicrotask(() => void loadTenants());
  }, [loadTenants]);

  const filteredTenants = useMemo(() => {
    return tenants.filter((tenant) => {
      if (filterStatus !== "All" && String(tenant.status) !== filterStatus) {
        return false;
      }
      if (filterDbMode !== "All" && String(tenant.databaseMode) !== filterDbMode) {
        return false;
      }
      return true;
    });
  }, [tenants, filterStatus, filterDbMode]);

  const updateField = (field: keyof typeof initialForm, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
  };

  const handleCopy = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    toast.success(`Đã sao chép ${label}: ${text}`);
  };

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault();
    setCreating(true);
    try {
      const created = await adminTenantsApi.create({
        tenantCode: form.tenantCode.trim().toLowerCase(),
        tenantName: form.tenantName.trim(),
        initialManager: {
          employeeCode: form.employeeCode.trim() || null,
          employeeAccount: form.employeeAccount.trim(),
          employeePassword: form.employeePassword,
          employeeFullName: form.employeeFullName.trim(),
          employeeMobile: form.employeeMobile.trim() || null,
          employeeEmail: form.employeeEmail.trim() || null,
        },
      });
      setTenants((current) => [created, ...current]);
      setForm(initialForm);
      setCreateOpen(false);
      toast.success("Đã tạo tenant và Manager đầu tiên thành công.");
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể tạo tenant."));
    } finally {
      setCreating(false);
    }
  };

  // Summary cards matching customers page style
  const summaryItems: SummaryCardItem[] = useMemo(
    () => [
      {
        title: "Tổng số Tenant",
        value: tenants.length,
        icon: <Building2 className="w-6 h-6" />,
        iconWrapperClassName: "bg-primary/10 text-primary",
      },
      {
        title: "Đang hoạt động",
        value: tenants.filter((c) => c.status === TenantStatus.Active).length,
        icon: <CheckCircle2 className="w-6 h-6" />,
        iconWrapperClassName:
          "bg-emerald-500/10 text-emerald-600 dark:text-emerald-500",
      },
      {
        title: "Dedicated Database",
        value: tenants.filter(
          (c) => c.databaseMode === TenantDatabaseMode.Dedicated,
        ).length,
        icon: <Server className="w-6 h-6" />,
        iconWrapperClassName:
          "bg-blue-500/10 text-blue-600 dark:text-blue-500",
      },
      {
        title: "Tạm khóa / Lỗi",
        value: tenants.filter(
          (c) =>
            c.status === TenantStatus.Suspended ||
            c.status === TenantStatus.Failed,
        ).length,
        icon: <Lock className="w-6 h-6" />,
        iconWrapperClassName:
          "bg-amber-500/10 text-amber-600 dark:text-amber-500",
      },
    ],
    [tenants],
  );

  const columns = useMemo<ColumnDef<TenantResponseDto>[]>(
    () => [
      {
        id: "select",
        header: ({ table }) => (
          <div className="flex justify-center">
            <Checkbox
              checked={
                table.getIsAllPageRowsSelected() ||
                (table.getIsSomePageRowsSelected() && "indeterminate")
              }
              onCheckedChange={(value) =>
                table.toggleAllPageRowsSelected(!!value)
              }
              aria-label="Select all"
              className="translate-y-[2px]"
            />
          </div>
        ),
        cell: ({ row }) => (
          <div
            className="flex justify-center"
            onClick={(e) => e.stopPropagation()}
          >
            <Checkbox
              checked={row.getIsSelected()}
              onCheckedChange={(value) => row.toggleSelected(!!value)}
              aria-label="Select row"
              className="translate-y-[2px]"
            />
          </div>
        ),
        enableSorting: false,
      },
      {
        id: "tenantInfo",
        accessorFn: (row) =>
          `${row.tenantName} ${row.tenantCode} ${row.databaseName}`,
        header: "Tổ chức / Tenant",
        cell: ({ row }) => {
          const item = row.original;
          return (
            <div className="max-w-[320px] flex items-center gap-3">
              <div className="w-9 h-9 rounded-full bg-primary/10 flex items-center justify-center shrink-0 text-primary font-bold text-sm">
                {item.tenantName ? item.tenantName.charAt(0).toUpperCase() : <Building2 className="w-4.5 h-4.5" />}
              </div>
              <div className="min-w-0">
                <span className="font-medium text-foreground block truncate">
                  {item.tenantName}
                </span>
                <div className="flex items-center gap-1.5 mt-0.5">
                  <span className="text-xs font-mono text-muted-foreground bg-muted/80 px-1.5 py-0.2 rounded border border-border/50 truncate">
                    {item.tenantCode}
                  </span>
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleCopy(item.tenantCode, "mã tenant");
                    }}
                    className="text-muted-foreground hover:text-foreground transition-colors p-0.5 rounded hover:bg-muted"
                    title="Sao chép mã tenant"
                  >
                    <Copy className="w-3 h-3" />
                  </button>
                </div>
              </div>
            </div>
          );
        },
      },
      {
        accessorKey: "databaseName",
        header: "Cơ sở dữ liệu",
        cell: ({ row }) => {
          const dbName = row.original.databaseName;
          return (
            <div className="flex items-center gap-2 min-w-[160px]">
              <Database className="w-3.5 h-3.5 text-muted-foreground shrink-0" />
              <span className="font-mono text-xs text-foreground truncate">
                {dbName}
              </span>
            </div>
          );
        },
      },
      {
        accessorKey: "databaseMode",
        header: () => <div className="text-center">Chế độ DB</div>,
        cell: ({ row }) => {
          const mode = row.original.databaseMode;
          const isDedicated = mode === TenantDatabaseMode.Dedicated;
          return (
            <div className="flex justify-center min-w-[110px]">
              {isDedicated ? (
                <Badge
                  variant="outline"
                  className="gap-1.5 py-0.5 px-2 font-medium text-xs border-violet-500/30 bg-violet-500/10 text-violet-700 dark:text-violet-300"
                >
                  <Server className="w-3 h-3" />
                  <span>Dedicated</span>
                </Badge>
              ) : (
                <Badge
                  variant="outline"
                  className="gap-1.5 py-0.5 px-2 font-medium text-xs border-border bg-muted/60 text-muted-foreground"
                >
                  <Layers className="w-3 h-3" />
                  <span>Shared</span>
                </Badge>
              )}
            </div>
          );
        },
      },
      {
        accessorKey: "status",
        header: () => <div className="text-center">Trạng thái</div>,
        cell: ({ row }) => {
          return (
            <div className="flex justify-center min-w-[130px]">
              <TenantStatusBadge status={row.original.status} />
            </div>
          );
        },
      },
      {
        id: "action",
        header: () => <div className="text-right pr-4">Thao tác</div>,
        cell: ({ row }) => {
          const item = row.original;
          return (
            <SplitActionMenu
              primaryLabel="Chi tiết"
              primaryIcon={<Eye className="w-4 h-4" />}
              onPrimaryClick={() => setViewingTenant(item)}
              menuItems={[
                {
                  label: "Sao chép mã",
                  icon: <Copy className="w-4 h-4" />,
                  onClick: () => handleCopy(item.tenantCode, "mã tenant"),
                },
                {
                  label: "Sao chép tên DB",
                  icon: <Database className="w-4 h-4" />,
                  onClick: () => handleCopy(item.databaseName, "tên DB"),
                },
              ]}
            />
          );
        },
      },
    ],
    [],
  );

  const CustomFilters = (
    <>
      <SelectFilter
        value={filterStatus}
        onChange={(val) => setFilterStatus(val)}
        options={TENANT_STATUS_OPTIONS}
        placeholder="Trạng thái"
      />
      <SelectFilter
        value={filterDbMode}
        onChange={(val) => setFilterDbMode(val)}
        options={DATABASE_MODE_OPTIONS}
        placeholder="Chế độ DB"
      />
    </>
  );

  return (
    <>
      <Header title="Quản lý Tenant" />

      <div className="grow overflow-y-auto p-2 lg:p-10 space-y-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Quản lý Tenant
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              Quản lý danh sách các tổ chức thuê bao trong hệ thống từ Central Database
            </p>
          </div>

          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => void loadTenants()}
              disabled={loading}
              className="h-9 gap-1.5"
            >
              <RefreshCw className={cn("w-4 h-4", loading && "animate-spin")} />
              <span>Làm mới</span>
            </Button>
            <Button
              onClick={() => {
                setForm(initialForm);
                setCreateOpen(true);
              }}
              className="h-9"
            >
              <Plus className="w-4 h-4 mr-2" /> Thêm mới
            </Button>
          </div>
        </div>

        <SummaryCards items={summaryItems} isLoading={loading} />

        <Card className="border-border shadow-sm bg-card min-h-[500px] flex flex-col gap-0 p-0">
          <CardContent className="p-4 flex flex-col justify-between flex-1 pb-0">
            <DataTable
              columns={columns}
              data={filteredTenants}
              searchKey="tenantInfo"
              searchPlaceholder="Tìm kiếm tên, mã tenant, database..."
              filterSlot={CustomFilters}
              isLoading={loading}
              onRowClick={(row) => setViewingTenant(row)}
              mobileCardRenderer={(row, { isSelected }) => {
                const item = row.original;
                return (
                  <div
                    className={`rounded-xl border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40 ${
                      isSelected
                        ? "border-primary/40 bg-primary/5"
                        : "border-border"
                    }`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider block font-mono">
                          {item.tenantCode || "CHƯA CÓ MÃ"}
                        </span>
                        <h3 className="text-base font-bold text-foreground mt-0.5">
                          {item.tenantName || "Chưa có tên"}
                        </h3>
                      </div>
                      <TenantStatusBadge status={item.status} />
                    </div>

                    <div className="grid grid-cols-2 gap-2 text-sm text-muted-foreground bg-muted/40 p-2.5 rounded-lg border border-border/50 my-2">
                      <div>
                        <span className="text-xs text-muted-foreground block">
                          Cơ sở dữ liệu
                        </span>
                        <span className="font-medium text-foreground block truncate font-mono text-xs">
                          {item.databaseName}
                        </span>
                      </div>
                      <div>
                        <span className="text-xs text-muted-foreground block">
                          Chế độ
                        </span>
                        <span className="font-medium text-primary block text-xs">
                          {item.databaseMode === TenantDatabaseMode.Dedicated
                            ? "Dedicated"
                            : "Shared"}
                        </span>
                      </div>
                    </div>
                  </div>
                );
              }}
            />
          </CardContent>
        </Card>
      </div>

      {/* View Details Dialog */}
      <Dialog open={Boolean(viewingTenant)} onOpenChange={(open) => !open && setViewingTenant(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary font-bold">
                <Building2 className="w-5 h-5" />
              </div>
              <div>
                <DialogTitle>{viewingTenant?.tenantName}</DialogTitle>
                <DialogDescription className="font-mono text-xs">
                  {viewingTenant?.tenantCode}
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          {viewingTenant && (
            <div className="space-y-4 py-3 text-sm">
              <div className="grid grid-cols-2 gap-3 p-3 rounded-lg bg-muted/40 border border-border/60">
                <div>
                  <span className="text-xs text-muted-foreground block">ID Định danh</span>
                  <span className="font-semibold text-foreground">#{viewingTenant.tenantId}</span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Trạng thái</span>
                  <div className="mt-1">
                    <TenantStatusBadge status={viewingTenant.status} />
                  </div>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Tên Database</span>
                  <span className="font-mono text-xs font-semibold text-foreground block truncate">
                    {viewingTenant.databaseName}
                  </span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Mô hình Database</span>
                  <span className="font-medium text-primary block">
                    {viewingTenant.databaseMode === TenantDatabaseMode.Dedicated
                      ? "Dedicated DB (Riêng)"
                      : "Shared DB (Dùng chung)"}
                  </span>
                </div>
              </div>
            </div>
          )}

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                if (viewingTenant) {
                  handleCopy(viewingTenant.tenantCode, "mã tenant");
                }
              }}
              className="gap-1.5"
            >
              <Copy className="w-4 h-4" />
              Sao chép mã
            </Button>
            <Button onClick={() => setViewingTenant(null)}>Đóng</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Create Tenant Form Modal */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-2xl">
          <form onSubmit={handleCreate}>
            <DialogHeader>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary">
                  <Building2 className="w-5 h-5" />
                </div>
                <div>
                  <DialogTitle>Tạo tenant mới</DialogTitle>
                  <DialogDescription>
                    Khởi tạo cơ sở dữ liệu, schema và tài khoản Manager trong cùng một quy trình
                  </DialogDescription>
                </div>
              </div>
            </DialogHeader>

            <div className="py-5 space-y-5">
              {/* Section 1: Tenant Information */}
              <div className="space-y-3">
                <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                  <Building2 className="w-4 h-4 text-primary" />
                  <span>1. Thông tin tổ chức (Tenant)</span>
                </div>

                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label htmlFor="tenant-code" className="text-xs">
                      Mã tenant <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      id="tenant-code"
                      required
                      placeholder="vd: fpt-telecom, vinfast"
                      value={form.tenantCode}
                      onChange={(event) =>
                        updateField(
                          "tenantCode",
                          event.target.value
                            .replace(/[^a-zA-Z0-9-]/g, "")
                            .toLowerCase(),
                        )
                      }
                      className="font-mono text-sm"
                    />
                    <p className="text-[11px] text-muted-foreground">
                      Dùng làm định danh và tiền tố database (chữ thường, số, dấu gạch ngang).
                    </p>
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="tenant-name" className="text-xs">
                      Tên tổ chức / Doanh nghiệp <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      id="tenant-name"
                      required
                      placeholder="vd: Công ty Cổ phần Công nghệ FPT"
                      value={form.tenantName}
                      onChange={(event) => updateField("tenantName", event.target.value)}
                      className="text-sm"
                    />
                  </div>
                </div>
              </div>

              {/* Section 2: Initial Manager Account */}
              <div className="space-y-3 border-t border-border pt-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                    <ShieldCheck className="w-4 h-4 text-primary" />
                    <span>2. Quản trị viên khởi tạo (Initial Manager)</span>
                  </div>
                  <Badge variant="outline" className="text-[10px]">
                    Quyền Manager cao nhất
                  </Badge>
                </div>

                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label htmlFor="employee-fullname" className="text-xs">
                      Họ và tên Manager <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      id="employee-fullname"
                      required
                      placeholder="vd: Nguyễn Văn Quản Trị"
                      value={form.employeeFullName}
                      onChange={(event) =>
                        updateField("employeeFullName", event.target.value)
                      }
                      className="text-sm"
                    />
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="employee-account" className="text-xs">
                      Tài khoản đăng nhập <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      id="employee-account"
                      required
                      placeholder="vd: manager_admin"
                      value={form.employeeAccount}
                      onChange={(event) =>
                        updateField("employeeAccount", event.target.value)
                      }
                      className="text-sm font-mono"
                    />
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="employee-password" className="text-xs">
                      Mật khẩu tạm <span className="text-destructive">*</span>
                    </Label>
                    <div className="relative">
                      <Input
                        id="employee-password"
                        required
                        type={showPassword ? "text" : "password"}
                        placeholder="••••••••"
                        value={form.employeePassword}
                        onChange={(event) =>
                          updateField("employeePassword", event.target.value)
                        }
                        className="text-sm pr-9"
                        autoComplete="new-password"
                      />
                      <button
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors p-1"
                      >
                        {showPassword ? (
                          <EyeOff className="w-4 h-4" />
                        ) : (
                          <Eye className="w-4 h-4" />
                        )}
                      </button>
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="employee-code" className="text-xs">
                      Mã nhân viên (tùy chọn)
                    </Label>
                    <Input
                      id="employee-code"
                      placeholder="vd: EMP-001"
                      value={form.employeeCode}
                      onChange={(event) =>
                        updateField("employeeCode", event.target.value)
                      }
                      className="text-sm font-mono"
                    />
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="employee-email" className="text-xs">
                      Email liên hệ
                    </Label>
                    <Input
                      id="employee-email"
                      type="email"
                      placeholder="admin@doanhnghiep.com"
                      value={form.employeeEmail}
                      onChange={(event) =>
                        updateField("employeeEmail", event.target.value)
                      }
                      className="text-sm"
                    />
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="employee-mobile" className="text-xs">
                      Số điện thoại
                    </Label>
                    <Input
                      id="employee-mobile"
                      placeholder="0987654321"
                      value={form.employeeMobile}
                      onChange={(event) =>
                        updateField("employeeMobile", event.target.value)
                      }
                      className="text-sm"
                    />
                  </div>
                </div>
              </div>
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setCreateOpen(false)}
                disabled={creating}
              >
                Hủy
              </Button>
              <Button type="submit" disabled={creating}>
                {creating && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                {creating ? "Đang khởi tạo..." : "Xác nhận tạo tenant"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}
