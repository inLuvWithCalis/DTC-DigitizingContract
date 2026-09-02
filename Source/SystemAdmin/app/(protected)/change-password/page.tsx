"use client";

import Link from "next/link";
import {
  ArrowLeft,
  CheckCircle2,
  HelpCircle,
  Shield,
  ShieldAlert,
  User,
} from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Header } from "@/components/ui/custom/header";
import { ChangePasswordForm } from "@/components/account/change-password-form";
import { useAuthStore } from "@/hooks/use-auth-store";

export default function ChangePasswordPage() {
  const user = useAuthStore((state) => state.user);
  const mustChangePassword = Boolean(user?.mustChangePassword);

  const initials = user?.fullName
    ? user.fullName
        .trim()
        .split(/\s+/)
        .map((p) => p[0])
        .join("")
        .slice(0, 2)
        .toUpperCase()
    : "SA";

  return (
    <>
      <Header title="Đổi mật khẩu" />
      <div className="grow overflow-y-auto bg-background/50">
        <div className="mx-auto w-full max-w-6xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
          {/* TOP BREADCRUMB / ACTION BAR */}
          {!mustChangePassword && (
            <div className="flex items-center justify-between">
              <Button
                asChild
                variant="ghost"
                size="sm"
                className="gap-2 text-xs text-muted-foreground hover:text-foreground -ml-2"
              >
                <Link href="/profile">
                  <ArrowLeft className="size-3.5" />
                  <span>Quay lại Hồ sơ quản trị</span>
                </Link>
              </Button>
            </div>
          )}

          {/* MANDATORY ALERT BANNER */}
          {mustChangePassword && (
            <Alert
              variant="destructive"
              className="border-amber-400 bg-amber-500/10 text-amber-900 dark:border-amber-600 dark:bg-amber-950/40 dark:text-amber-200 shadow-sm"
            >
              <div className="flex items-start gap-3">
                <ShieldAlert className="size-5 shrink-0 text-amber-600 dark:text-amber-400 mt-0.5" />
                <div className="space-y-1">
                  <AlertTitle className="text-base font-semibold text-amber-900 dark:text-amber-200">
                    Yêu cầu bắt buộc đổi mật khẩu
                  </AlertTitle>
                  <AlertDescription className="text-xs sm:text-sm text-amber-800 dark:text-amber-300 leading-relaxed">
                    Đây là mật khẩu bootstrap khởi tạo hoặc vừa được đặt lại. Để
                    đảm bảo an toàn tối đa cho hệ thống quản trị trung tâm, bạn
                    cần thiết lập mật khẩu mới trước khi tiếp tục quản lý hệ
                    thống.
                  </AlertDescription>
                </div>
              </div>
            </Alert>
          )}

          {/* MAIN 2-COLUMN GRID */}
          <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px] xl:grid-cols-[minmax(0,1fr)_360px]">
            {/* Left Column: Change Password Form */}
            <div className="min-w-0">
              <ChangePasswordForm mustChangePassword={mustChangePassword} />
            </div>

            {/* Right Column: Account Info & Security Policies */}
            <div className="space-y-6">
              {/* Current Account Card */}
              <Card className="border-border/80 shadow-sm gap-0">
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm font-semibold flex items-center gap-2">
                    <User className="size-4 text-primary" />
                    <span>Tài khoản đang đổi</span>
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-3 pt-0">
                  <div className="flex items-center gap-3 rounded-xl bg-muted/40 p-3">
                    <Avatar className="size-11 rounded-xl bg-primary text-primary-foreground font-semibold">
                      <AvatarFallback className="rounded-xl bg-primary text-primary-foreground">
                        {initials}
                      </AvatarFallback>
                    </Avatar>
                    <div className="min-w-0 space-y-0.5">
                      <div className="text-sm font-semibold text-foreground truncate">
                        {user?.fullName || "System Admin"}
                      </div>
                      <div className="text-xs text-muted-foreground truncate">
                        @{user?.username || "admin"}
                      </div>
                    </div>
                  </div>

                  <div className="space-y-2 text-xs">
                    <div className="flex items-center justify-between text-muted-foreground">
                      <span>Vai trò hệ thống:</span>
                      <Badge
                        variant="secondary"
                        className="text-xs font-normal"
                      >
                        System Admin
                      </Badge>
                    </div>
                    <div className="flex items-center justify-between text-muted-foreground">
                      <span>Lần đổi gần nhất:</span>
                      <span className="font-medium text-foreground">
                        {formatDateTime(user?.passwordChangedAt)}
                      </span>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Security Policy Card */}
              <Card className="border-border/80 shadow-sm gap-0">
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm font-semibold flex items-center gap-2">
                    <Shield className="size-4 text-primary" />
                    <span>Chính sách an toàn Quản trị</span>
                  </CardTitle>
                  <CardDescription className="text-xs">
                    Các lưu ý quan trọng khi đổi mật khẩu System Admin
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 pt-0 text-xs text-muted-foreground">
                  <div className="flex items-start gap-2.5">
                    <CheckCircle2 className="size-4 shrink-0 text-emerald-500 mt-0.5" />
                    <span>
                      Tự động kết thúc mọi phiên đăng nhập cũ trên các thiết bị
                      khác sau khi đổi thành công.
                    </span>
                  </div>
                  <div className="flex items-start gap-2.5">
                    <CheckCircle2 className="size-4 shrink-0 text-emerald-500 mt-0.5" />
                    <span>
                      Mật khẩu mới không được trùng với mật khẩu hiện tại.
                    </span>
                  </div>
                  <div className="flex items-start gap-2.5">
                    <CheckCircle2 className="size-4 shrink-0 text-emerald-500 mt-0.5" />
                    <span>
                      Độ dài tối thiểu 12 ký tự để phòng chống các hình thức dò
                      quét mật khẩu.
                    </span>
                  </div>

                  {/* <div className="rounded-xl border border-border/50 bg-muted/20 p-3 text-[11px] leading-relaxed mt-2">
                    <div className="flex items-center gap-1 font-medium text-foreground mb-1">
                      <HelpCircle className="size-3.5 text-primary" />
                      <span>Quên mật khẩu Quản trị?</span>
                    </div>
                    Trường hợp khẩn cấp, cần can thiệp qua quy trình bootstrap recovery từ máy chủ hạ tầng gốc.
                  </div> */}
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

function formatDateTime(value: string | null | undefined) {
  if (!value) return "Chưa có thông tin";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Chưa có thông tin";

  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(date);
}
