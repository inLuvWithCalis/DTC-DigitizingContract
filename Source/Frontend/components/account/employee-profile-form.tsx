"use client";

import { FormEvent, useMemo, useState } from "react";
import {
  AtSign,
  Briefcase,
  Building2,
  Calendar,
  CheckCircle2,
  Compass,
  FileBadge,
  Heart,
  Info,
  Mail,
  MapPin,
  Phone,
  RefreshCw,
  RotateCcw,
  Save,
  Shield,
  Smartphone,
  User,
  Users,
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import { getApiErrorMessage, isStaleRowVersion } from "@/lib/api-error";
import { useAuthStore } from "@/hooks/use-auth-store";
import {
  profileApi,
  type EmployeeProfile,
  type UpdateEmployeeProfileRequest,
} from "@/services/profile-api";
import {
  getEmployeeStatusLabel,
  getEmployeeTypeLabel,
} from "@/services/employees-api";

interface EmployeeProfileFormProps {
  profile: EmployeeProfile;
  onSaved: (profile: EmployeeProfile) => void;
  onReload: () => Promise<void>;
}

export function EmployeeProfileForm({
  profile,
  onSaved,
  onReload,
}: EmployeeProfileFormProps) {
  const initialFormState = useMemo<UpdateEmployeeProfileRequest>(
    () => ({
      fullName: profile.fullName ?? "",
      birthDate: toDateInput(profile.birthDate),
      gender: profile.gender ?? "",
      maritalStatus: profile.maritalStatus ?? "",
      mobile: profile.mobile ?? "",
      phone: profile.phone ?? "",
      email: profile.email ?? "",
      address: profile.address ?? "",
      rowVersion: profile.rowVersion,
    }),
    [profile],
  );

  const [form, setForm] =
    useState<UpdateEmployeeProfileRequest>(initialFormState);
  const [isSaving, setIsSaving] = useState(false);
  const [isStale, setIsStale] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const setUser = useAuthStore((state) => state.setUser);
  const user = useAuthStore((state) => state.user);

  // Check if form is dirty (user modified values)
  const isDirty = useMemo(() => {
    return (
      form.fullName !== initialFormState.fullName ||
      (form.birthDate || "") !== (initialFormState.birthDate || "") ||
      (form.gender || "") !== (initialFormState.gender || "") ||
      (form.maritalStatus || "") !== (initialFormState.maritalStatus || "") ||
      (form.mobile || "") !== (initialFormState.mobile || "") ||
      (form.phone || "") !== (initialFormState.phone || "") ||
      (form.email || "") !== (initialFormState.email || "") ||
      (form.address || "") !== (initialFormState.address || "")
    );
  }, [form, initialFormState]);

  const setField = <K extends keyof UpdateEmployeeProfileRequest>(
    field: K,
    value: UpdateEmployeeProfileRequest[K],
  ) => {
    setForm((current) => ({ ...current, [field]: value }));
    if (error) setError("");
    if (success) setSuccess("");
  };

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
        ...form,
        fullName: form.fullName.trim(),
        birthDate: form.birthDate || null,
        gender: form.gender?.trim() || null,
        maritalStatus: form.maritalStatus?.trim() || null,
        mobile: form.mobile?.trim() || null,
        phone: form.phone?.trim() || null,
        email: form.email?.trim() || null,
        address: form.address?.trim() || null,
      });
      setForm((current) => ({ ...current, rowVersion: updated.rowVersion }));
      onSaved(updated);
      if (user) {
        setUser({
          ...user,
          fullName: updated.fullName,
          mustChangePassword: updated.mustChangePassword,
          passwordChangedAt: updated.passwordChangedAt,
        });
      }
      setSuccess("Hồ sơ cá nhân đã được cập nhật thành công.");
    } catch (requestError) {
      const stale = isStaleRowVersion(requestError);
      setIsStale(stale);
      setError(
        stale
          ? "Hồ sơ đã thay đổi ở một phiên làm việc khác. Vui lòng tải lại trang trước khi lưu."
          : getApiErrorMessage(
              requestError,
              "Không thể cập nhật hồ sơ cá nhân.",
            ),
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
                Chi tiết hồ sơ
              </CardTitle>
              <CardDescription className="text-xs text-muted-foreground mt-1">
                Xem và cập nhật thông tin cá nhân hoặc xem quyền hạn công việc
              </CardDescription>
            </div>
            <TabsList className="grid w-full grid-cols-2 sm:w-auto">
              <TabsTrigger
                value="personal"
                className="text-xs sm:text-sm gap-2"
              >
                <User className="size-4" />
                <span>Cá nhân & Liên hệ</span>
              </TabsTrigger>
              <TabsTrigger value="work" className="text-xs sm:text-sm gap-2">
                <Briefcase className="size-4" />
                <span>Công việc & Phân quyền</span>
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
                <AlertTitle>
                  {error ? "Không thể lưu" : "Thành công"}
                </AlertTitle>
              </div>
              <AlertDescription className="text-xs sm:text-sm mt-1">
                {error || success}
              </AlertDescription>
            </Alert>
          )}

          {isStale && (
            <div className="mb-6 flex items-center justify-between rounded-xl border border-amber-300 bg-amber-50 p-4 dark:border-amber-900 dark:bg-amber-950/40">
              <div className="text-sm text-amber-800 dark:text-amber-300">
                Dữ liệu phiên của bạn đã cũ. Nhấn tải lại để cập nhật phiên bản
                mới nhất.
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

          {/* TAB 1: PERSONAL & CONTACT INFORMATION */}
          <TabsContent value="personal" className="mt-0 space-y-6 outline-none">
            <form onSubmit={handleSubmit} className="space-y-6">
              {/* SECTION: BASIC INFO */}
              <div className="space-y-4">
                <div className="flex items-center gap-2 border-b border-border/40 pb-2">
                  <User className="size-4 text-primary" />
                  <h3 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                    Thông tin cơ bản
                  </h3>
                </div>

                <div className="grid gap-5 sm:grid-cols-2">
                  <Field
                    label="Họ và tên"
                    required
                    icon={<User className="size-4" />}
                  >
                    <Input
                      value={form.fullName}
                      onChange={(event) =>
                        setField("fullName", event.target.value)
                      }
                      maxLength={100}
                      placeholder="Nhập họ và tên đầy đủ"
                      required
                    />
                  </Field>

                  <Field
                    label="Ngày sinh"
                    icon={<Calendar className="size-4" />}
                  >
                    <Input
                      type="date"
                      value={form.birthDate ?? ""}
                      onChange={(event) =>
                        setField("birthDate", event.target.value)
                      }
                    />
                  </Field>

                  <Field label="Giới tính" icon={<Users className="size-4" />}>
                    <Select
                      value={form.gender ?? "NONE"}
                      onValueChange={(val) =>
                        setField("gender", val === "NONE" ? null : val)
                      }
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder="Chọn giới tính" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="NONE">Chưa xác định</SelectItem>
                        <SelectItem value="M">Nam</SelectItem>
                        <SelectItem value="F">Nữ</SelectItem>
                        <SelectItem value="O">Khác</SelectItem>
                      </SelectContent>
                    </Select>
                  </Field>

                  <Field
                    label="Tình trạng hôn nhân"
                    icon={<Heart className="size-4" />}
                  >
                    <Select
                      value={form.maritalStatus ?? "NONE"}
                      onValueChange={(val) =>
                        setField("maritalStatus", val === "NONE" ? null : val)
                      }
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder="Chọn tình trạng" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="NONE">Chưa xác định</SelectItem>
                        <SelectItem value="S">Độc thân</SelectItem>
                        <SelectItem value="M">Đã kết hôn</SelectItem>
                        <SelectItem value="O">Khác</SelectItem>
                      </SelectContent>
                    </Select>
                  </Field>
                </div>
              </div>

              {/* SECTION: CONTACT INFO */}
              <div className="space-y-4 pt-2">
                <div className="flex items-center gap-2 border-b border-border/40 pb-2">
                  <Smartphone className="size-4 text-primary" />
                  <h3 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                    Thông tin liên hệ
                  </h3>
                </div>

                <div className="grid gap-5 sm:grid-cols-2">
                  <Field
                    label="Email liên hệ"
                    icon={<Mail className="size-4" />}
                  >
                    <Input
                      type="email"
                      value={form.email ?? ""}
                      onChange={(event) =>
                        setField("email", event.target.value)
                      }
                      maxLength={100}
                      placeholder="example@company.com"
                    />
                  </Field>

                  <Field
                    label="Số di động"
                    icon={<Smartphone className="size-4" />}
                  >
                    <Input
                      type="tel"
                      value={form.mobile ?? ""}
                      onChange={(event) =>
                        setField("mobile", event.target.value)
                      }
                      maxLength={15}
                      placeholder="09xx xxx xxx"
                    />
                  </Field>

                  <Field
                    label="Điện thoại bàn / nội bộ"
                    icon={<Phone className="size-4" />}
                  >
                    <Input
                      type="tel"
                      value={form.phone ?? ""}
                      onChange={(event) =>
                        setField("phone", event.target.value)
                      }
                      maxLength={15}
                      placeholder="024 xxx xxxx"
                    />
                  </Field>

                  <div className="sm:col-span-2">
                    <Field
                      label="Địa chỉ liên hệ / Thường trú"
                      icon={<MapPin className="size-4" />}
                    >
                      <Textarea
                        value={form.address ?? ""}
                        onChange={(event) =>
                          setField("address", event.target.value)
                        }
                        maxLength={500}
                        rows={3}
                        placeholder="Số nhà, tên đường, phường/xã, quận/huyện, tỉnh/thành phố..."
                        className="resize-none"
                      />
                    </Field>
                  </div>
                </div>
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

          {/* TAB 2: WORK & ORGANIZATION INFO (READONLY) */}
          <TabsContent value="work" className="mt-0 space-y-6 outline-none">
            <div className="space-y-4">
              <div className="flex items-center gap-2 border-b border-border/40 pb-2">
                <Building2 className="size-4 text-primary" />
                <h3 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                  Thông tin nhân sự & Hệ thống
                </h3>
              </div>

              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                <ReadonlyCard
                  label="Mã nhân viên"
                  value={profile.employeeCode || "Chưa cấp"}
                  icon={<FileBadge className="size-4 text-primary" />}
                />
                <ReadonlyCard
                  label="Tài khoản đăng nhập"
                  value={profile.account || "Chưa cập nhật"}
                  icon={<AtSign className="size-4 text-primary" />}
                />
                <ReadonlyCard
                  label="Phòng ban"
                  value={profile.departmentName || "Chưa phân công"}
                  icon={<Building2 className="size-4 text-primary" />}
                />
                <ReadonlyCard
                  label="Chức danh"
                  value={profile.titleName || "Chưa cập nhật"}
                  icon={<Briefcase className="size-4 text-primary" />}
                />
                <ReadonlyCard
                  label="Vai trò hệ thống"
                  value={profile.roleName || "Chưa cập nhật"}
                  icon={<Shield className="size-4 text-primary" />}
                  badge={
                    <Badge variant="secondary" className="text-xs">
                      {profile.roleName || "N/A"}
                    </Badge>
                  }
                />
                <ReadonlyCard
                  label="Loại nhân viên"
                  value={getEmployeeTypeLabel(profile.employeeType)}
                  icon={<Users className="size-4 text-primary" />}
                />
                <ReadonlyCard
                  label="Trang mặc định"
                  value={profile.defaultPage || "Mặc định hệ thống"}
                  icon={<Compass className="size-4 text-primary" />}
                />
                <ReadonlyCard
                  label="Trạng thái tài khoản"
                  value={getEmployeeStatusLabel(profile.status)}
                  icon={<CheckCircle2 className="size-4 text-emerald-500" />}
                  badge={
                    <Badge
                      variant={profile.status === 1 ? "default" : "destructive"}
                      className="text-xs"
                    >
                      {getEmployeeStatusLabel(profile.status)}
                    </Badge>
                  }
                />
              </div>
            </div>

            {/* NOTICE CALLOUT */}
            <div className="flex items-start gap-3 rounded-xl border border-blue-200/80 bg-blue-50/60 p-4 dark:border-blue-900/50 dark:bg-blue-950/20">
              <Info className="mt-0.5 size-5 shrink-0 text-blue-600 dark:text-blue-400" />
              <div className="space-y-1 text-xs sm:text-sm text-blue-950 dark:text-blue-200">
                <div className="font-semibold">Lưu ý quản trị tổ chức</div>
                <p className="text-muted-foreground dark:text-blue-300/80 leading-relaxed">
                  Các thông tin về mã nhân viên, phòng ban, chức danh, phân loại
                  và quyền hạn hệ thống được quản trị tập trung bởi Phòng Nhân
                  sự & Ban Quản trị hệ thống. Nếu cần điều chỉnh, vui lòng gửi
                  yêu cầu đến quản trị viên phụ trách.
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
      <div
        className="mt-2 text-sm font-semibold text-foreground truncate"
        title={value}
      >
        {value}
      </div>
    </div>
  );
}

function toDateInput(value: string | null) {
  if (!value) return null;
  return value.slice(0, 10);
}
