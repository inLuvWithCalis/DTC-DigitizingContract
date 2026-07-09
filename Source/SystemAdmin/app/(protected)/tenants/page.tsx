"use client";

import { FormEvent, useState } from "react";
import axios from "axios";
import { Building2, CheckCircle2, Database, Loader2, Plus } from "lucide-react";
import { Header } from "@/components/ui/custom/header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { adminTenantsApi, TenantResponseDto } from "@/services/admin-tenants";

const TENANT_CODE_PATTERN = /^[a-z0-9-]{3,50}$/;

function getErrorMessage(error: unknown) {
  if (!axios.isAxiosError(error)) return "Không thể tạo tenant. Vui lòng thử lại.";
  const data = error.response?.data as { message?: string; title?: string; errors?: Record<string, string[]> } | undefined;
  const validationMessage = data?.errors ? Object.values(data.errors).flat().join(" ") : undefined;
  return validationMessage || data?.message || data?.title || "Không thể tạo tenant. Vui lòng thử lại.";
}

export default function TenantsPage() {
  const [tenantCode, setTenantCode] = useState("");
  const [tenantName, setTenantName] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [createdTenant, setCreatedTenant] = useState<TenantResponseDto | null>(null);
  const codeIsInvalid = tenantCode.length > 0 && !TENANT_CODE_PATTERN.test(tenantCode);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setCreatedTenant(null);
    const normalizedCode = tenantCode.trim();
    const normalizedName = tenantName.trim();
    if (!TENANT_CODE_PATTERN.test(normalizedCode)) {
      setError("Mã tenant phải có 3–50 ký tự, chỉ gồm chữ thường, số và dấu gạch ngang.");
      return;
    }
    if (!normalizedName || normalizedName.length > 200) {
      setError("Tên tenant là bắt buộc và không được vượt quá 200 ký tự.");
      return;
    }
    setIsSubmitting(true);
    try {
      const result = await adminTenantsApi.create({ tenantCode: normalizedCode, tenantName: normalizedName });
      setCreatedTenant(result);
      setTenantCode("");
      setTenantName("");
    } catch (submitError) {
      setError(getErrorMessage(submitError));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <Header title="Quản lý tenant" />
      <div className="grow overflow-y-auto">
        <div className="mx-auto w-full max-w-5xl px-6 py-8 lg:px-10">
          <div className="mb-8">
            <h1 className="text-3xl font-bold tracking-tight">Tạo tenant mới</h1>
            <p className="mt-2 text-muted-foreground">Khởi tạo tổ chức và cơ sở dữ liệu riêng để bắt đầu sử dụng hệ thống.</p>
          </div>
          <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
            <form onSubmit={handleSubmit} className="rounded-2xl border bg-card p-6 shadow-sm lg:p-8">
              <div className="mb-7 flex items-center gap-3">
                <div className="flex size-11 items-center justify-center rounded-xl bg-primary/10 text-primary"><Building2 className="size-5" /></div>
                <div><h2 className="font-semibold">Thông tin tenant</h2><p className="text-sm text-muted-foreground">Nhập đầy đủ hai trường bên dưới.</p></div>
              </div>
              <div className="space-y-6">
                <div className="space-y-2">
                  <Label htmlFor="tenantName">Tên tenant <span className="text-destructive">*</span></Label>
                  <Input id="tenantName" value={tenantName} onChange={(event) => setTenantName(event.target.value)} placeholder="Ví dụ: Công ty TNHH DTC" maxLength={200} disabled={isSubmitting} required />
                  <p className="text-xs text-muted-foreground">Tên tổ chức hiển thị trong hệ thống.</p>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="tenantCode">Mã tenant <span className="text-destructive">*</span></Label>
                  <Input id="tenantCode" value={tenantCode} onChange={(event) => setTenantCode(event.target.value.toLowerCase().replace(/\s+/g, "-"))} placeholder="Ví dụ: dtc-company" minLength={3} maxLength={50} pattern="[a-z0-9\\-]{3,50}" aria-invalid={codeIsInvalid} disabled={isSubmitting} required />
                  <p className={codeIsInvalid ? "text-xs text-destructive" : "text-xs text-muted-foreground"}>3–50 ký tự; chỉ dùng chữ thường, số và dấu gạch ngang.</p>
                </div>
                {error && <Alert variant="destructive"><AlertTitle>Tạo tenant không thành công</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
                {createdTenant && <Alert className="border-emerald-200 bg-emerald-50 text-emerald-900 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-100"><CheckCircle2 /><AlertTitle>Đã tạo tenant thành công</AlertTitle><AlertDescription>{createdTenant.tenantName} ({createdTenant.tenantCode}) — database: {createdTenant.databaseName}</AlertDescription></Alert>}
                <div className="flex justify-end border-t pt-6">
                  <Button type="submit" disabled={isSubmitting || codeIsInvalid || !tenantCode || !tenantName.trim()}>{isSubmitting ? <Loader2 className="animate-spin" /> : <Plus />}{isSubmitting ? "Đang khởi tạo..." : "Tạo tenant"}</Button>
                </div>
              </div>
            </form>
            <aside className="h-fit rounded-2xl border bg-muted/30 p-6">
              <Database className="mb-4 size-8 text-primary" />
              <h2 className="font-semibold">Điều gì sẽ được tạo?</h2>
              <p className="mt-2 text-sm leading-6 text-muted-foreground">Hệ thống sẽ lưu tenant vào cơ sở dữ liệu trung tâm, khởi tạo database riêng và chạy dữ liệu nền cần thiết. Quá trình này có thể mất một chút thời gian.</p>
            </aside>
          </div>
        </div>
      </div>
    </>
  );
}
