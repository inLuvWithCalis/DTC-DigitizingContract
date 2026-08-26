"use client";

import { useCallback, useEffect, useState } from "react";
import { Building2, Loader2, RefreshCw, Save, ShieldCheck } from "lucide-react";

import { PermissionGuard } from "@/components/auth/permission-guard";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Header } from "@/components/ui/custom/header";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { toast } from "@/components/ui/sonner";
import { getApiErrorMessage, isStaleRowVersion } from "@/lib/api-error";
import { RBAC_PERMISSIONS } from "@/lib/rbac";
import {
  tenantLegalProfileApi,
  type TenantLegalProfileResponse,
  type UpsertTenantLegalProfileRequest,
} from "@/services/tenant-legal-profile-api";

const EMPTY_FORM: UpsertTenantLegalProfileRequest = {
  legalEntityName: "",
  taxCode: "",
  address: "",
  representativeName: "",
  representativeTitle: "",
  phoneNumber: "",
  faxNumber: "",
  bankAccountNumber: "",
  bankName: "",
  rowVersion: null,
};

function LegalProfilePageContent() {
  const [form, setForm] = useState<UpsertTenantLegalProfileRequest>(EMPTY_FORM);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  const loadProfile = useCallback(async () => {
    setIsLoading(true);
    try {
      const profile = await tenantLegalProfileApi.get();
      setForm(profile ? toForm(profile) : EMPTY_FORM);
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "Không thể tải hồ sơ pháp lý doanh nghiệp."),
      );
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadProfile();
  }, [loadProfile]);

  const updateField = (
    field: keyof UpsertTenantLegalProfileRequest,
    value: string,
  ) => setForm((current) => ({ ...current, [field]: value }));

  const save = async () => {
    const requiredValues = [
      form.legalEntityName,
      form.taxCode,
      form.address,
      form.representativeName,
      form.representativeTitle,
    ];
    if (requiredValues.some((value) => !value.trim())) {
      toast.error("Vui lòng nhập đầy đủ các trường bắt buộc.");
      return;
    }

    setIsSaving(true);
    try {
      const saved = await tenantLegalProfileApi.upsert({
        ...form,
        legalEntityName: form.legalEntityName.trim(),
        taxCode: form.taxCode.trim(),
        address: form.address.trim(),
        representativeName: form.representativeName.trim(),
        representativeTitle: form.representativeTitle.trim(),
        phoneNumber: form.phoneNumber?.trim() || null,
        faxNumber: form.faxNumber?.trim() || null,
        bankAccountNumber: form.bankAccountNumber?.trim() || null,
        bankName: form.bankName?.trim() || null,
      });
      setForm(toForm(saved));
      toast.success("Đã lưu hồ sơ pháp lý doanh nghiệp.");
    } catch (error) {
      if (isStaleRowVersion(error)) {
        toast.error("Dữ liệu đã được người khác cập nhật. Đang tải lại bản mới nhất.");
        await loadProfile();
      } else {
        toast.error(getApiErrorMessage(error, "Không thể lưu hồ sơ pháp lý."));
      }
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <>
      <Header />
      <div className="grow overflow-y-auto p-3 sm:p-6 lg:p-10">
        <div className="mx-auto max-w-5xl space-y-6">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <div className="flex items-center gap-2">
                <Building2 className="size-6 text-primary" />
                <h1 className="text-2xl font-bold tracking-tight">
                  Hồ sơ pháp lý doanh nghiệp
                </h1>
              </div>
              <p className="mt-1 text-sm text-muted-foreground">
                Dữ liệu này được đóng băng vào từng phiên bản hợp đồng khi gửi duyệt.
              </p>
            </div>
            <div className="flex gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => void loadProfile()}
                disabled={isLoading || isSaving}
              >
                <RefreshCw className="size-4" />
                Tải lại
              </Button>
              <Button
                type="button"
                onClick={() => void save()}
                disabled={isLoading || isSaving}
              >
                {isSaving ? (
                  <Loader2 className="size-4 animate-spin" />
                ) : (
                  <Save className="size-4" />
                )}
                Lưu thay đổi
              </Button>
            </div>
          </div>

          <Card>
            <CardHeader className="border-b">
              <CardTitle className="flex items-center gap-2 text-lg">
                <ShieldCheck className="size-5 text-primary" />
                Thông tin bên cung cấp
              </CardTitle>
            </CardHeader>
            <CardContent className="grid gap-5 pt-6 sm:grid-cols-2">
              {isLoading ? (
                Array.from({ length: 9 }, (_, index) => (
                  <Skeleton key={index} className="h-20 w-full" />
                ))
              ) : (
                <>
                  <Field
                    id="legalEntityName"
                    label="Tên pháp nhân"
                    value={form.legalEntityName}
                    maxLength={500}
                    onChange={(value) => updateField("legalEntityName", value)}
                  />
                  <Field
                    id="taxCode"
                    label="Mã số thuế"
                    value={form.taxCode}
                    maxLength={50}
                    onChange={(value) => updateField("taxCode", value)}
                  />
                  <Field
                    id="representativeName"
                    label="Người đại diện"
                    value={form.representativeName}
                    maxLength={200}
                    onChange={(value) => updateField("representativeName", value)}
                  />
                  <Field
                    id="representativeTitle"
                    label="Chức danh người đại diện"
                    value={form.representativeTitle}
                    maxLength={200}
                    onChange={(value) => updateField("representativeTitle", value)}
                  />
                  <Field
                    id="phoneNumber"
                    label="Điện thoại"
                    value={form.phoneNumber ?? ""}
                    maxLength={30}
                    required={false}
                    onChange={(value) => updateField("phoneNumber", value)}
                  />
                  <Field
                    id="faxNumber"
                    label="Fax"
                    value={form.faxNumber ?? ""}
                    maxLength={30}
                    required={false}
                    onChange={(value) => updateField("faxNumber", value)}
                  />
                  <Field
                    id="bankAccountNumber"
                    label="Số tài khoản ngân hàng"
                    value={form.bankAccountNumber ?? ""}
                    maxLength={100}
                    required={false}
                    onChange={(value) => updateField("bankAccountNumber", value)}
                  />
                  <Field
                    id="bankName"
                    label="Tên ngân hàng"
                    value={form.bankName ?? ""}
                    maxLength={500}
                    required={false}
                    onChange={(value) => updateField("bankName", value)}
                  />
                  <div className="grid gap-2 sm:col-span-2">
                    <RequiredLabel htmlFor="address">Địa chỉ</RequiredLabel>
                    <Textarea
                      id="address"
                      value={form.address}
                      onChange={(event) => updateField("address", event.target.value)}
                      maxLength={2000}
                      rows={4}
                      placeholder="Địa chỉ đăng ký kinh doanh"
                    />
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </>
  );
}

function Field({
  id,
  label,
  value,
  maxLength,
  required = true,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  maxLength: number;
  required?: boolean;
  onChange: (value: string) => void;
}) {
  return (
    <div className="grid gap-2">
      {required ? (
        <RequiredLabel htmlFor={id}>{label}</RequiredLabel>
      ) : (
        <Label htmlFor={id}>{label}</Label>
      )}
      <Input
        id={id}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        maxLength={maxLength}
      />
    </div>
  );
}

function RequiredLabel({
  htmlFor,
  children,
}: {
  htmlFor: string;
  children: React.ReactNode;
}) {
  return (
    <Label htmlFor={htmlFor}>
      {children} <span className="text-destructive">*</span>
    </Label>
  );
}

function toForm(
  profile: TenantLegalProfileResponse,
): UpsertTenantLegalProfileRequest {
  return {
    legalEntityName: profile.legalEntityName,
    taxCode: profile.taxCode,
    address: profile.address,
    representativeName: profile.representativeName,
    representativeTitle: profile.representativeTitle,
    phoneNumber: profile.phoneNumber ?? "",
    faxNumber: profile.faxNumber ?? "",
    bankAccountNumber: profile.bankAccountNumber ?? "",
    bankName: profile.bankName ?? "",
    rowVersion: profile.rowVersion,
  };
}

export default function LegalProfilePage() {
  return (
    <PermissionGuard permission={RBAC_PERMISSIONS.tenantLegalProfileManage}>
      <LegalProfilePageContent />
    </PermissionGuard>
  );
}
