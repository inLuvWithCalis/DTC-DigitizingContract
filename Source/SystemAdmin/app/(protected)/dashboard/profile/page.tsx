"use client";

import { useState } from "react";
import {
  BadgeCheck,
  CalendarClock,
  CheckCircle2,
  Mail,
  RefreshCw,
  ShieldCheck,
  User,
} from "lucide-react";
import { Header } from "@/components/ui/custom/header";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { authApi, UserProfileDto } from "@/services/auth-api";
import { useAuthStore } from "@/hooks/use-auth-store";

const profileItems = (user: UserProfileDto) => [
  {
    label: "Tên đăng nhập",
    value: user.username,
    icon: <User className="size-4 text-muted-foreground" />,
  },
  {
    label: "Họ và tên",
    value: user.fullName || "Chưa cập nhật",
    icon: <BadgeCheck className="size-4 text-muted-foreground" />,
  },
  {
    label: "Email",
    value: user.email || "Chưa cập nhật",
    icon: <Mail className="size-4 text-muted-foreground" />,
  },
  {
    label: "Mã quản trị viên",
    value: `#${user.systemAdminId}`,
    icon: <ShieldCheck className="size-4 text-muted-foreground" />,
  },
];

export default function SystemAdminProfilePage() {
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState("");

  const handleRefreshProfile = async () => {
    setIsRefreshing(true);
    setError("");

    try {
      const latestProfile = await authApi.getMe();
      setUser(latestProfile);
    } catch {
      setError("Không thể tải lại hồ sơ. Vui lòng đăng nhập lại hoặc thử sau.");
    } finally {
      setIsRefreshing(false);
    }
  };

  return (
    <>
      <Header title="Hồ sơ cá nhân" />

      <div className="grow overflow-y-auto">
        <div className="mx-auto w-full max-w-5xl px-6 py-8 lg:px-10">
          <div className="mb-8 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
            <div>
              <p className="text-sm font-medium text-primary">
                System Administrator
              </p>
              <h1 className="mt-2 text-3xl font-bold tracking-tight">
                Hồ sơ cá nhân
              </h1>


            </div>

            <Button onClick={handleRefreshProfile} disabled={isRefreshing}>
              <RefreshCw
                className={`size-4 ${isRefreshing ? "animate-spin" : ""}`}
              />
              Tải lại hồ sơ
            </Button>
          </div>

          {error && (
            <Alert variant="destructive" className="mb-6">
              <AlertTitle>Tải hồ sơ không thành công</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {!user ? (
            <Alert>
              <CalendarClock className="size-4" />
              <AlertTitle>Đang chờ dữ liệu hồ sơ</AlertTitle>
              <AlertDescription>
                Hệ thống đang xác thực phiên đăng nhập và lấy thông tin quản trị
                viên.
              </AlertDescription>
            </Alert>
          ) : (
            <div className="grid gap-6 lg:grid-cols-[320px_minmax(0,1fr)]">
              <Card className="overflow-hidden">
                <div className="h-24 bg-gradient-to-r from-primary to-primary/70" />
                <CardContent className="-mt-10 p-6">
                  <div className="flex size-20 items-center justify-center rounded-2xl border-4 border-card bg-primary text-3xl font-bold text-primary-foreground shadow-sm">
                    {user.username?.[0]?.toUpperCase() ?? "S"}
                  </div>

                  <div className="mt-5">
                    <h2 className="text-xl font-semibold">
                      {user.fullName || user.username}
                    </h2>
                    <p className="mt-1 text-sm text-muted-foreground">
                      {user.email || "Chưa cập nhật email"}
                    </p>
                  </div>

                  <div className="mt-5 flex items-center gap-2">
                    <Badge
                      variant={user.isActive ? "default" : "secondary"}
                      className="gap-1.5"
                    >
                      <CheckCircle2 className="size-3.5" />
                      {user.isActive ? "Đang hoạt động" : "Ngừng hoạt động"}
                    </Badge>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Thông tin tài khoản</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid gap-4 sm:grid-cols-2">
                    {profileItems(user).map((item) => (
                      <div
                        key={item.label}
                        className="rounded-xl border bg-muted/30 p-4"
                      >
                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                          {item.icon}
                          {item.label}
                        </div>
                        <p className="mt-2 break-words text-base font-semibold">
                          {item.value}
                        </p>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </div>
          )}
        </div>
      </div>
    </>
  );
}
