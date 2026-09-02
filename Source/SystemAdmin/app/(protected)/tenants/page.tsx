"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { Building2, Loader2, Plus, RefreshCw, Search } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { getApiErrorMessage } from "@/lib/api-error";
import {
  adminTenantsApi,
  TenantDatabaseMode,
  TenantResponseDto,
  TenantStatus,
} from "@/services/admin-tenants";

const STATUS_LABELS: Record<TenantStatus, string> = {
  [TenantStatus.Pending]: "Đang chờ",
  [TenantStatus.Provisioning]: "Đang khởi tạo",
  [TenantStatus.Active]: "Hoạt động",
  [TenantStatus.Failed]: "Lỗi khởi tạo",
  [TenantStatus.Suspended]: "Tạm khóa",
};

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
  const [keyword, setKeyword] = useState("");
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
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
    const normalized = keyword.trim().toLocaleLowerCase("vi");
    if (!normalized) return tenants;
    return tenants.filter(
      (tenant) =>
        tenant.tenantCode.toLocaleLowerCase("vi").includes(normalized) ||
        tenant.tenantName.toLocaleLowerCase("vi").includes(normalized) ||
        tenant.databaseName.toLocaleLowerCase("vi").includes(normalized),
    );
  }, [keyword, tenants]);

  const updateField = (field: keyof typeof initialForm, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
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
      toast.success("Đã tạo tenant và Manager đầu tiên.");
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể tạo tenant."));
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="space-y-6 p-4 md:p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            <Building2 className="size-6 text-primary" /> Quản lý tenant
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Dữ liệu lấy trực tiếp từ Central Database; không dùng dữ liệu mô phỏng.
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="size-4" /> Tạo tenant
        </Button>
      </div>

      <Card>
        <CardHeader className="gap-4 md:flex-row md:items-center md:justify-between">
          <CardTitle>Danh sách tenant ({tenants.length})</CardTitle>
          <div className="flex w-full gap-2 md:w-auto">
            <div className="relative flex-1 md:w-80">
              <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="Tìm mã, tên hoặc database..."
                className="pl-9"
              />
            </div>
            <Button variant="outline" size="icon" onClick={() => void loadTenants()}>
              <RefreshCw className={loading ? "size-4 animate-spin" : "size-4"} />
              <span className="sr-only">Tải lại</span>
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <div className="overflow-hidden rounded-lg border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Tenant</TableHead>
                  <TableHead>Database</TableHead>
                  <TableHead>Chế độ</TableHead>
                  <TableHead>Trạng thái</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  <TableRow><TableCell colSpan={4} className="h-32 text-center"><Loader2 className="mx-auto size-5 animate-spin" /></TableCell></TableRow>
                ) : filteredTenants.length === 0 ? (
                  <TableRow><TableCell colSpan={4} className="h-32 text-center text-muted-foreground">Không có tenant phù hợp.</TableCell></TableRow>
                ) : (
                  filteredTenants.map((tenant) => (
                    <TableRow key={tenant.tenantId}>
                      <TableCell>
                        <p className="font-medium">{tenant.tenantName}</p>
                        <p className="font-mono text-xs text-muted-foreground">{tenant.tenantCode}</p>
                      </TableCell>
                      <TableCell className="font-mono text-xs">{tenant.databaseName}</TableCell>
                      <TableCell>{tenant.databaseMode === TenantDatabaseMode.Dedicated ? "Dedicated" : "Shared"}</TableCell>
                      <TableCell><Badge variant={tenant.status === TenantStatus.Active ? "default" : tenant.status === TenantStatus.Failed ? "destructive" : "secondary"}>{STATUS_LABELS[tenant.status]}</Badge></TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <form onSubmit={handleCreate}>
            <DialogHeader>
              <DialogTitle>Tạo tenant mới</DialogTitle>
              <DialogDescription>Database, migration/seed và Manager đầu tiên được tạo trong cùng luồng provisioning.</DialogDescription>
            </DialogHeader>
            <div className="grid gap-4 py-5 sm:grid-cols-2">
              <Field label="Mã tenant" required value={form.tenantCode} onChange={(value) => updateField("tenantCode", value.replace(/[^a-zA-Z0-9-]/g, "").toLowerCase())} />
              <Field label="Tên tenant" required value={form.tenantName} onChange={(value) => updateField("tenantName", value)} />
              <Field label="Mã nhân viên Manager" value={form.employeeCode} onChange={(value) => updateField("employeeCode", value)} />
              <Field label="Tài khoản Manager" required value={form.employeeAccount} onChange={(value) => updateField("employeeAccount", value)} />
              <Field label="Họ tên Manager" required value={form.employeeFullName} onChange={(value) => updateField("employeeFullName", value)} />
              <Field label="Mật khẩu tạm" required type="password" minLength={12} value={form.employeePassword} onChange={(value) => updateField("employeePassword", value)} />
              <Field label="Số điện thoại" value={form.employeeMobile} onChange={(value) => updateField("employeeMobile", value)} />
              <Field label="Email" type="email" value={form.employeeEmail} onChange={(value) => updateField("employeeEmail", value)} />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCreateOpen(false)} disabled={creating}>Hủy</Button>
              <Button type="submit" disabled={creating}>
                {creating && <Loader2 className="size-4 animate-spin" />} Tạo tenant
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function Field({
  label,
  required = false,
  type = "text",
  minLength,
  value,
  onChange,
}: {
  label: string;
  required?: boolean;
  type?: string;
  minLength?: number;
  value: string;
  onChange: (value: string) => void;
}) {
  const id = `tenant-${label.toLowerCase().replaceAll(" ", "-")}`;
  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}{required && <span className="text-destructive"> *</span>}</Label>
      <Input id={id} type={type} required={required} minLength={minLength} value={value} onChange={(event) => onChange(event.target.value)} autoComplete={type === "password" ? "new-password" : undefined} />
    </div>
  );
}
