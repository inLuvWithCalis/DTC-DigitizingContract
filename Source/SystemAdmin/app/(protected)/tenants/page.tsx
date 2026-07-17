"use client";

import { FormEvent, useMemo, useState } from "react";
import axios from "axios";
import {
  ArrowRight,
  Building2,
  CheckCircle2,
  Copy,
  Database,
  Loader2,
  Plus,
  ServerCog,
  ShieldCheck,
  Sparkles,
} from "lucide-react";
import { Header } from "@/components/ui/custom/header";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { adminTenantsApi, TenantResponseDto } from "@/services/admin-tenants";

const TENANT_CODE_PATTERN = /^[a-z0-9-]{3,50}$/;
const DATABASE_PREFIX = "ContractManagement_Tenant_";

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

function normalizeTenantCode(value: string) {
  return value
    .toLowerCase()
    .trimStart()
    .replace(/\s+/g, "-")
    .replace(/[^a-z0-9-]/g, "")
    .replace(/-{2,}/g, "-");
}

export default function TenantsPage() {
  const [tenantCode, setTenantCode] = useState("");
  const [tenantName, setTenantName] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [createdTenant, setCreatedTenant] =
    useState<TenantResponseDto | null>(null);

  const normalizedCode = tenantCode.trim();
  const normalizedName = tenantName.trim();
  const codeIsInvalid =
    tenantCode.length > 0 && !TENANT_CODE_PATTERN.test(normalizedCode);
  const databasePreview = useMemo(
    () => `${DATABASE_PREFIX}${normalizedCode || "tenant-code"}`,
    [normalizedCode],
  );
  const canSubmit =
    !isSubmitting &&
    !codeIsInvalid &&
    TENANT_CODE_PATTERN.test(normalizedCode) &&
    normalizedName.length > 0 &&
    normalizedName.length <= 200;

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
      setError("Tên tenant là bắt buộc và không được vượt quá 200 ký tự.");
      return;
    }

    setIsSubmitting(true);
    try {
      const result = await adminTenantsApi.create({
        tenantCode: normalizedCode,
        tenantName: normalizedName,
      });
      setCreatedTenant(result);
      setTenantCode("");
      setTenantName("");
    } catch (submitError) {
      setError(getErrorMessage(submitError));
    } finally {
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
    <>
      <Header title="Quản lý tenant" />

      <div className="grow overflow-y-auto bg-muted/20">
        <div className="mx-auto w-full max-w-6xl px-6 py-8 lg:px-10">
          <div className="mb-8 overflow-hidden rounded-3xl border bg-card shadow-sm">
            <div className="relative p-6 lg:p-8">
              <div className="absolute right-0 top-0 h-36 w-36 rounded-bl-full bg-primary/10" />
              <div className="absolute right-16 top-10 h-20 w-20 rounded-full bg-primary/10 blur-2xl" />

              <div className="relative flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
                <div className="max-w-2xl">

                  <h1 className="text-3xl font-bold tracking-tight lg:text-4xl">
                    Tạo tenant mới
                  </h1>
                  <p className="mt-3 text-base leading-7 text-muted-foreground">
                    Khởi tạo tổ chức mới, ghi nhận vào cơ sở dữ liệu trung tâm
                    và tự động chuẩn bị database riêng để tenant bắt đầu sử
                    dụng hệ thống.
                  </p>
                </div>

                <div className="grid gap-3 sm:grid-cols-3 lg:w-[420px]">
                  <div className="rounded-2xl border bg-background/70 p-4">
                    <Database className="mb-3 size-5 text-primary" />
                    <p className="text-sm font-medium">Dedicated DB</p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Database riêng
                    </p>
                  </div>
                  <div className="rounded-2xl border bg-background/70 p-4">
                    <ShieldCheck className="mb-3 size-5 text-primary" />
                    <p className="text-sm font-medium">Isolated</p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Tách dữ liệu
                    </p>
                  </div>
                  <div className="rounded-2xl border bg-background/70 p-4">
                    <ServerCog className="mb-3 size-5 text-primary" />
                    <p className="text-sm font-medium">Auto seed</p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Dữ liệu nền
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
            <Card className="shadow-sm">
              <CardHeader className="space-y-1">
                <div className="flex items-center gap-3">
                  <div className="flex size-11 items-center justify-center rounded-xl bg-primary/10 text-primary">
                    <Building2 className="size-5" />
                  </div>
                  <div>
                    <CardTitle>Thông tin tenant</CardTitle>
                    <p className="text-sm text-muted-foreground">
                      Nhập tên tổ chức và mã định danh dùng cho hệ thống.
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
                        Tên tổ chức hiển thị trong hệ thống. Tối đa 200 ký tự.
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
                        pattern="[a-z0-9\\-]{3,50}"
                        aria-invalid={codeIsInvalid}
                        disabled={isSubmitting}
                        required
                      />
                      <p
                        className={
                          codeIsInvalid
                            ? "text-xs text-destructive"
                            : "text-xs text-muted-foreground"
                        }
                      >
                        3–50 ký tự; chỉ dùng chữ thường, số và dấu gạch ngang.
                      </p>
                    </div>
                  </div>

                  <div className="rounded-2xl border bg-muted/40 p-4">
                    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                      <div>
                        <p className="text-sm font-medium">
                          Database dự kiến
                        </p>
                        <p className="mt-1 break-all font-mono text-sm text-muted-foreground">
                          {databasePreview}
                        </p>
                      </div>
                      <Badge variant="outline">Preview</Badge>
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
                          <div className="flex flex-col gap-2 rounded-xl border border-emerald-200 bg-white/70 p-3 text-sm dark:border-emerald-900 dark:bg-background/40 sm:flex-row sm:items-center sm:justify-between">
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
                              Copy
                            </Button>
                          </div>
                        </div>
                      </AlertDescription>
                    </Alert>
                  )}

                  <Separator />

                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <p className="text-sm text-muted-foreground">
                      Kiểm tra kỹ tenant code trước khi tạo vì mã này sẽ gắn với
                      database.
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
                    <Database className="size-5 text-primary" />
                    Quy trình khởi tạo
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  {[
                    "Lưu thông tin tenant vào database trung tâm.",
                    "Tạo database riêng theo tenant code.",
                    "Chạy migration và dữ liệu nền ban đầu.",
                    "Kích hoạt tenant để đăng nhập sử dụng.",
                  ].map((step, index) => (
                    <div key={step} className="flex gap-3">
                      <div className="mt-0.5 flex size-7 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                        {index + 1}
                      </div>
                      <div>
                        <p className="text-sm font-medium">{step}</p>
                        {index < 3 && (
                          <ArrowRight className="mt-3 size-4 rotate-90 text-muted-foreground" />
                        )}
                      </div>
                    </div>
                  ))}
                </CardContent>
              </Card>

              <Card className="border-dashed shadow-sm">
                <CardContent className="p-6">
                  <h3 className="font-semibold">Gợi ý đặt tenant code</h3>
                  <ul className="mt-3 space-y-2 text-sm text-muted-foreground">
                    <li>• Dùng tên ngắn, dễ nhận diện: `dtc`, `abc-company`.</li>
                    <li>• Không dùng dấu tiếng Việt hoặc ký tự đặc biệt.</li>
                    <li>• Không nên đổi mã sau khi tenant đã được tạo.</li>
                  </ul>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
