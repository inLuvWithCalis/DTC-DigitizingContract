"use client";

import { FormEvent, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  AlertCircle,
  ArrowLeft,
  Check,
  CheckCircle2,
  Eye,
  EyeOff,
  KeyRound,
  Lock,
  ShieldCheck,
  Sparkles,
  X,
} from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Progress } from "@/components/ui/progress";
import { getApiErrorMessage } from "@/lib/api-error";
import { useAuthStore } from "@/hooks/use-auth-store";
import { profileApi } from "@/services/profile-api";

interface ChangePasswordFormProps {
  mustChangePassword?: boolean;
}

export function ChangePasswordForm({
  mustChangePassword = false,
}: ChangePasswordFormProps) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const logout = useAuthStore((state) => state.logout);
  const router = useRouter();

  // Password rules validation
  const rules = useMemo(() => {
    const hasMinLength = newPassword.length >= 12;
    const hasUpperAndLower =
      /[a-z]/.test(newPassword) && /[A-Z]/.test(newPassword);
    const hasNumber = /[0-9]/.test(newPassword);
    const hasSpecial = /[^A-Za-z0-9]/.test(newPassword);
    const isDifferent =
      Boolean(newPassword) &&
      Boolean(currentPassword) &&
      newPassword !== currentPassword;
    const isMatching =
      Boolean(confirmPassword) && newPassword === confirmPassword;

    let score = 0;
    if (newPassword.length >= 8) score += 1;
    if (hasMinLength) score += 1;
    if (hasUpperAndLower) score += 1;
    if (hasNumber) score += 1;
    if (hasSpecial) score += 1;

    let strengthLabel = "Chưa nhập";
    let strengthColor = "bg-muted";
    let strengthTextColor = "text-muted-foreground";
    let strengthPercentage = 0;

    if (newPassword.length > 0) {
      if (score <= 2) {
        strengthLabel = "Yếu";
        strengthColor = "bg-rose-500";
        strengthTextColor = "text-rose-600 dark:text-rose-400";
        strengthPercentage = 25;
      } else if (score === 3) {
        strengthLabel = "Trung bình";
        strengthColor = "bg-amber-500";
        strengthTextColor = "text-amber-600 dark:text-amber-400";
        strengthPercentage = 50;
      } else if (score === 4) {
        strengthLabel = "Khá mạnh";
        strengthColor = "bg-blue-500";
        strengthTextColor = "text-blue-600 dark:text-blue-400";
        strengthPercentage = 75;
      } else {
        strengthLabel = "Rất mạnh";
        strengthColor = "bg-emerald-500";
        strengthTextColor = "text-emerald-600 dark:text-emerald-400";
        strengthPercentage = 100;
      }
    }

    return {
      hasMinLength,
      hasUpperAndLower,
      hasNumber,
      hasSpecial,
      isDifferent,
      isMatching,
      score,
      strengthLabel,
      strengthColor,
      strengthTextColor,
      strengthPercentage,
      isValid: hasMinLength && isMatching && (isDifferent || !currentPassword),
    };
  }, [newPassword, currentPassword, confirmPassword]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");

    if (newPassword.length < 12) {
      setError("Mật khẩu mới phải có ít nhất 12 ký tự.");
      return;
    }
    if (newPassword === currentPassword) {
      setError("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
      return;
    }
    if (newPassword !== confirmPassword) {
      setError("Xác nhận mật khẩu mới không khớp.");
      return;
    }

    setIsSaving(true);
    try {
      await profileApi.changePassword({ currentPassword, newPassword });
      logout();
      router.replace("/?password_changed=1");
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, "Không thể đổi mật khẩu."));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Card className="border-border/80 shadow-sm gap-0">
      <CardHeader className="border-b border-border/60 pb-4">
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <CardTitle className="flex items-center gap-2.5 text-base sm:text-lg font-semibold tracking-tight">
              <div className="flex size-9 items-center justify-center rounded-lg bg-primary/10 text-primary dark:bg-primary/20">
                <KeyRound className="size-5" />
              </div>
              <span>Thiết lập mật khẩu mới</span>
            </CardTitle>
            <CardDescription className="text-xs text-muted-foreground">
              Nhập mật khẩu hiện tại và tạo mật khẩu mới an toàn theo tiêu chuẩn
            </CardDescription>
          </div>
        </div>
      </CardHeader>

      <CardContent className="pt-6">
        <form
          id="change-password-form"
          className="space-y-5"
          onSubmit={handleSubmit}
        >
          {error && (
            <Alert
              variant="destructive"
              className="border-destructive/40 shadow-sm"
            >
              <AlertCircle className="size-4" />
              <AlertTitle className="text-sm font-semibold">
                Không thể đổi mật khẩu
              </AlertTitle>
              <AlertDescription className="text-xs sm:text-sm mt-1">
                {error}
              </AlertDescription>
            </Alert>
          )}

          {/* Current Password Field */}
          <PasswordField
            label="Mật khẩu hiện tại"
            value={currentPassword}
            placeholder="Nhập mật khẩu hiện tại của bạn"
            visible={showCurrent}
            onVisibleChange={() => setShowCurrent((value) => !value)}
            onChange={setCurrentPassword}
          />

          {/* New Password Field */}
          <div className="space-y-2">
            <PasswordField
              label="Mật khẩu mới"
              value={newPassword}
              placeholder="Nhập mật khẩu mới (tối thiểu 12 ký tự)"
              visible={showNew}
              onVisibleChange={() => setShowNew((value) => !value)}
              onChange={setNewPassword}
            />

            {/* Password Strength Meter */}
            {newPassword.length > 0 && (
              <div className="space-y-1.5 rounded-lg border border-border/60 bg-muted/20 p-3">
                <div className="flex items-center justify-between text-xs">
                  <span className="flex items-center gap-1.5 text-muted-foreground font-medium">
                    <Sparkles className="size-3.5 text-primary" />
                    Độ mạnh mật khẩu:
                  </span>
                  <span className={`font-semibold ${rules.strengthTextColor}`}>
                    {rules.strengthLabel}
                  </span>
                </div>
                <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                  <div
                    className={`h-full transition-all duration-300 ${rules.strengthColor}`}
                    style={{ width: `${rules.strengthPercentage}%` }}
                  />
                </div>
              </div>
            )}
          </div>

          {/* Confirm Password Field */}
          <div className="space-y-1.5">
            <PasswordField
              label="Xác nhận mật khẩu mới"
              value={confirmPassword}
              placeholder="Nhập lại mật khẩu mới vừa tạo"
              visible={showConfirm}
              onVisibleChange={() => setShowConfirm((value) => !value)}
              onChange={setConfirmPassword}
            />
            {confirmPassword.length > 0 && (
              <div className="flex items-center gap-1.5 text-xs pt-1">
                {rules.isMatching ? (
                  <span className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400 font-medium">
                    <CheckCircle2 className="size-3.5" />
                    Mật khẩu xác nhận trùng khớp
                  </span>
                ) : (
                  <span className="flex items-center gap-1 text-rose-600 dark:text-rose-400 font-medium">
                    <X className="size-3.5" />
                    Mật khẩu xác nhận chưa khớp
                  </span>
                )}
              </div>
            )}
          </div>

          {/* Real-time Checklist */}
          <div className="rounded-xl border border-border/60 bg-muted/30 p-3.5 space-y-2.5 mb-10">
            <div className="text-xs font-semibold text-foreground">
              Tiêu chuẩn an toàn mật khẩu:
            </div>
            <div className="grid gap-2 sm:grid-cols-2 text-xs">
              <RuleItem
                satisfied={rules.hasMinLength}
                label="Độ dài từ 12 ký tự trở lên"
              />
              <RuleItem
                satisfied={rules.hasUpperAndLower}
                label="Bao gồm chữ hoa và chữ thường"
              />
              <RuleItem
                satisfied={rules.hasNumber}
                label="Chứa ít nhất một chữ số (0-9)"
              />
              <RuleItem
                satisfied={rules.isDifferent}
                label="Khác với mật khẩu hiện tại"
              />
            </div>
          </div>
        </form>
      </CardContent>

      <CardFooter className="flex flex-col-reverse sm:flex-row items-center justify-end gap-3 border-t border-border/60 pt-4">
        <Button
          form="change-password-form"
          type="submit"
          disabled={isSaving || !rules.hasMinLength || !rules.isMatching}
          className="w-full sm:w-auto min-w-44 gap-2 shadow-sm font-medium"
        >
          <ShieldCheck className="size-4" />
          {isSaving ? "Đang xử lý..." : "Đổi mật khẩu và đăng xuất"}
        </Button>
      </CardFooter>
    </Card>
  );
}

function PasswordField({
  label,
  value,
  placeholder,
  visible,
  onVisibleChange,
  onChange,
}: {
  label: string;
  value: string;
  placeholder?: string;
  visible: boolean;
  onVisibleChange: () => void;
  onChange: (value: string) => void;
}) {
  return (
    <div className="space-y-1.5">
      <Label className="flex items-center gap-1.5 text-xs font-medium text-foreground">
        <Lock className="size-3.5 text-muted-foreground" />
        <span>{label}</span>
        <span className="text-destructive font-bold">*</span>
      </Label>
      <div className="flex items-center gap-2">
        <Input
          type={visible ? "text" : "password"}
          value={value}
          placeholder={placeholder}
          onChange={(event) => onChange(event.target.value)}
          maxLength={64}
          required
          className="pr-10"
        />
        <Button
          type="button"
          variant="ghost"
          size="icon"
          onClick={onVisibleChange}
          aria-label={visible ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
        >
          {visible ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
        </Button>
      </div>
    </div>
  );
}

function RuleItem({ satisfied, label }: { satisfied: boolean; label: string }) {
  return (
    <div
      className={`flex items-center gap-2 transition-colors ${
        satisfied
          ? "text-emerald-600 dark:text-emerald-400 font-medium"
          : "text-muted-foreground"
      }`}
    >
      <div
        className={`flex size-4 shrink-0 items-center justify-center rounded-full text-[10px] ${
          satisfied
            ? "bg-emerald-500/15 text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400"
            : "bg-muted text-muted-foreground"
        }`}
      >
        {satisfied ? (
          <Check className="size-2.5 stroke-[3]" />
        ) : (
          <span className="size-1 rounded-full bg-current" />
        )}
      </div>
      <span>{label}</span>
    </div>
  );
}
