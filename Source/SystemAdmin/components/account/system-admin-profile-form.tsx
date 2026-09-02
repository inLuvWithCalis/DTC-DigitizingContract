"use client";

import { FormEvent, useMemo, useState } from "react";
import {
  AtSign,
  CheckCircle2,
  FileBadge,
  Hash,
  Info,
  Mail,
  RefreshCw,
  RotateCcw,
  Save,
  Shield,
  ShieldAlert,
  User,
} from "lucide-react";
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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { getApiErrorCode, getApiErrorMessage } from "@/lib/api-error";
import { useAuthStore } from "@/hooks/use-auth-store";
import {
  profileApi,
  type SystemAdminProfile,
} from "@/services/profile-api";

interface SystemAdminProfileFormProps {
  profile: SystemAdminProfile;
  onSaved: (profile: SystemAdminProfile) => void;
  onReload: () => Promise<void>;
}

export function SystemAdminProfileForm({
  profile,
  onSaved,
  onReload,
}: SystemAdminProfileFormProps) {
  const initialFormState = useMemo(
    () => ({
      fullName: profile.fullName ?? "",
      email: profile.email ?? "",
    }),
    [profile],
  );

  const [form, setForm] = useState(initialFormState);
  const [isSaving, setIsSaving] = useState(false);
  const [isStale, setIsStale] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);

  // Check if form is dirty
  const isDirty = useMemo(() => {
    return (
      form.fullName !== initialFormState.fullName ||
      (form.email || "") !== (initialFormState.email || "")
    );
  }, [form, initialFormState]);

  const handleReset = () => {
    setForm(initialFormState);
    setError("");
    setSuccess("");
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsStale(false);

    if (!form.fullName.trim()) {
      setError("Họ và tên không được để trống.");
      return;
    }

    setIsSaving(true);
    try {
      const updated = await profileApi.updateProfile({
        fullName: form.fullName.trim(),
        email: form.email.trim() || null,
        rowVersion: profile.rowVersion,
      });
      onSaved(updated);
      if (user) {
        setUser({
          ...user,
          fullName: updated.fullName,
          email: updated.email,
          mustChangePassword: updated.mustChangePassword,
          passwordChangedAt: updated.passwordChangedAt,
        });
      }
      setSuccess("Hồ sơ Quản trị viên đã được cập nhật thành công.");
    } catch (requestError) {
      const stale = getApiErrorCode(requestError) === "StaleRowVersion";
      setIsStale(stale);
      setError(
        stale
          ? "Hồ sơ đã thay đổi ở một phiên khác. Vui lòng tải lại trước khi lưu."
          : getApiErrorMessage(requestError, "Không thể cập nhật hồ sơ."),
      );
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Card className="border-border/80 shadow-sm">
      <Tabs defaultValue="personal" className="w-full">
        <CardHeader className="pb-3 border-b border-border/60">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle className="text-lg font-semibold tracking-tight">
                Chi tiết tài khoản
              </CardTitle>
              <CardDescription className="text-xs text-muted-foreground mt-1">
                Xem và chỉnh sửa thông tin cá nhân hoặc thông tin quyền quản trị
              </CardDescription>
            </div>
            <TabsList className="grid w-full grid-cols-2 sm:w-auto">
              <TabsTrigger value="personal" className="text-xs sm:text-sm gap-2">
                <User className="size-4" />
                <span>Cá nhân & Liên hệ</span>
              </TabsTrigger>
              <TabsTrigger value="admin" className="text-xs sm:text-sm gap-2">
                <Shield className="size-4" />
                <span>Quản trị & Phân quyền</span>
              </TabsTrigger>
            </TabsList>
          </div>
        </CardHeader>

        <CardContent className="pt-6">
          {/* Form Alerts */}
          {(error || success) && (
            <Alert
              variant={error ? "destructive" : "default"}
              className={`mb-6 ${
                !error
                  ? "border-emerald-200 bg-emerald-50/80 text-emerald-900 dark:border-emerald-900/40 dark:bg-emerald-950/20 dark:text-emerald-300"
                  : ""
              }`}
            >
              <div className="flex items-center gap-2">
                {!error && (
                  <CheckCircle2 className="size-4 text-emerald-600 dark:text-emerald-400" />
                )}
                <AlertTitle>{error ? "Không thể lưu" : "Thành công"}</AlertTitle>
              </div>
              <AlertDescription className="text-xs sm:text-sm mt-1">
                {error || success}
              </AlertDescription>
            </Alert>
          )}

          {isStale && (
            <div className="mb-6 flex items-center justify-between rounded-xl border border-amber-300 bg-amber-50 p-4 dark:border-amber-900 dark:bg-amber-950/40">
              <div className="text-sm text-amber-800 dark:text-amber-300">
                Dữ liệu phiên của bạn đã cũ. Nhấn tải lại để cập nhật phiên bản mới nhất.
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={onReload}
                className="gap-2 border-amber-300 text-amber-900 hover:bg-amber-100 dark:border-amber-700 dark:text-amber-200"
              >
                <RefreshCw className="size-3.5" />
                Tải lại
              </Button>
            </div>
          )}

          {/* TAB 1: PERSONAL INFO */}
          <TabsContent value="personal" className="mt-0 space-y-6 outline-none">
            <form onSubmit={handleSubmit} className="space-y-6">
              <div className="grid gap-5 sm:grid-cols-2">
                <Field label="Họ và tên" required icon={<User className="size-4" />}>
                  <Input
                    value={form.fullName}
                    onChange={(event) =>
                      setForm((c) => ({ ...c, fullName: event.target.value }))
                    }
                    maxLength={200}
                    placeholder="Nhập họ và tên"
                    required
                  />
                </Field>

                <Field label="Email liên hệ" icon={<Mail className="size-4" />}>
                  <Input
                    type="email"
                    value={form.email}
                    onChange={(event) =>
                      setForm((c) => ({ ...c, email: event.target.value }))
                    }
                    maxLength={200}
                    placeholder="admin@dtctech.vn"
                  />
                </Field>
              </div>

              {/* FORM ACTIONS */}
              <div className="flex flex-col-reverse sm:flex-row items-center justify-between gap-3 border-t border-border/60 pt-4">
                <div className="flex items-center gap-2 text-xs text-muted-foreground">
                  {isDirty && (
                    <span className="flex items-center gap-1.5 font-medium text-amber-600 dark:text-amber-400">
                      <span className="size-2 rounded-full bg-amber-500 animate-pulse" />
                      Có thay đổi chưa lưu
                    </span>
                  )}
                </div>

                <div className="flex w-full sm:w-auto items-center justify-end gap-2.5">
                  {isDirty && (
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={handleReset}
                      disabled={isSaving}
                      className="gap-1.5"
                    >
                      <RotateCcw className="size-3.5" />
                      Đặt lại
                    </Button>
                  )}
                  <Button
                    type="submit"
                    disabled={isSaving || isStale || !isDirty}
                    className="min-w-32 gap-2 shadow-sm"
                  >
                    <Save className="size-4" />
                    {isSaving ? "Đang lưu..." : "Lưu thay đổi"}
                  </Button>
                </div>
              </div>
            </form>
          </TabsContent>

          {/* TAB 2: ADMIN & PERMISSIONS INFO */}
          <TabsContent value="admin" className="mt-0 space-y-6 outline-none">
            <div className="space-y-4">
              <div className="flex items-center gap-2 border-b border-border/40 pb-2">
                <Shield className="size-4 text-primary" />
                <h3 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                  Thông tin phân quyền & Hệ thống
                </h3>
              </div>

              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                <ReadonlyCard
                  label="Mã quản trị viên"
                  value={`#${profile.systemAdminId}`}
                  icon={<Hash className="size-4 text-primary" />}
                />
                <ReadonlyCard
                  label="Tên đăng nhập"
                  value={profile.username}
                  icon={<AtSign className="size-4 text-primary" />}
                />
                <ReadonlyCard
                  label="Vai trò hệ thống"
                  value={profile.roleName || "System Admin"}
                  icon={<Shield className="size-4 text-primary" />}
                  badge={
                    <Badge variant="secondary" className="text-xs">
                      {profile.roleName || "System Admin"}
                    </Badge>
                  }
                />
                <ReadonlyCard
                  label="Trạng thái tài khoản"
                  value={profile.isActive ? "Đang hoạt động" : "Ngừng hoạt động"}
                  icon={
                    profile.isActive ? (
                      <CheckCircle2 className="size-4 text-emerald-500" />
                    ) : (
                      <ShieldAlert className="size-4 text-destructive" />
                    )
                  }
                  badge={
                    <Badge
                      variant={profile.isActive ? "default" : "destructive"}
                      className="text-xs"
                    >
                      {profile.isActive ? "Đang hoạt động" : "Tạm khóa"}
                    </Badge>
                  }
                />
                <ReadonlyCard
                  label="Bảo mật mật khẩu"
                  value={
                    profile.mustChangePassword
                      ? "Cần đổi mật khẩu"
                      : "Mật khẩu an toàn"
                  }
                  icon={<FileBadge className="size-4 text-primary" />}
                  badge={
                    <Badge
                      variant={profile.mustChangePassword ? "destructive" : "secondary"}
                      className="text-xs"
                    >
                      {profile.mustChangePassword ? "Cần đổi" : "Hợp lệ"}
                    </Badge>
                  }
                />
              </div>
            </div>

            {/* NOTICE CALLOUT */}
            <div className="flex items-start gap-3 rounded-xl border border-blue-200/80 bg-blue-50/60 p-4 dark:border-blue-900/50 dark:bg-blue-950/20">
              <Info className="mt-0.5 size-5 shrink-0 text-blue-600 dark:text-blue-400" />
              <div className="space-y-1 text-xs sm:text-sm text-blue-950 dark:text-blue-200">
                <div className="font-semibold">Đặc quyền Quản trị tối cao (System Admin)</div>
                <p className="text-muted-foreground dark:text-blue-300/80 leading-relaxed">
                  Tài khoản System Admin sở hữu toàn quyền quản trị hạ tầng, doanh nghiệp thuê (tenants),
                  cấu hình bảo mật và phân quyền của toàn bộ hệ thống DTC. Vui lòng bảo vệ nghiêm ngặt
                  thông tin đăng nhập.
                </p>
              </div>
            </div>
          </TabsContent>
        </CardContent>
      </Tabs>
    </Card>
  );
}

function Field({
  label,
  required = false,
  icon,
  children,
}: {
  label: string;
  required?: boolean;
  icon?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <Label className="flex items-center gap-1.5 text-xs font-medium text-foreground">
        {icon && <span className="text-muted-foreground">{icon}</span>}
        <span>{label}</span>
        {required && <span className="text-destructive font-bold">*</span>}
      </Label>
      {children}
    </div>
  );
}

function ReadonlyCard({
  label,
  value,
  icon,
  badge,
}: {
  label: string;
  value: string;
  icon: React.ReactNode;
  badge?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col justify-between rounded-xl border border-border/70 bg-card/60 p-3.5 transition-all hover:bg-muted/30">
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          {icon}
          <span>{label}</span>
        </div>
        {badge}
      </div>
      <div className="mt-2 text-sm font-semibold text-foreground truncate" title={value}>
        {value}
      </div>
    </div>
  );
}
