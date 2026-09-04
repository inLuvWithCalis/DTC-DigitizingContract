"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import {
  AlertCircle,
  Building,
  Clock,
  HelpCircle,
  LifeBuoy,
  RefreshCw,
  ShieldCheck,
} from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Header } from "@/components/ui/custom/header";
import { AccountSecurityCard } from "@/components/account/account-security-card";
import { SystemAdminProfileForm } from "@/components/account/system-admin-profile-form";
import { ProfileHeroBanner } from "@/components/account/profile-hero-banner";
import { getApiErrorMessage } from "@/lib/api-error";
import { profileApi, type SystemAdminProfile } from "@/services/profile-api";
import { useAuthStore } from "@/hooks/use-auth-store";

export default function SystemAdminProfilePage() {
  const [profile, setProfile] = useState<SystemAdminProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isReloading, setIsReloading] = useState(false);
  const [error, setError] = useState("");
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);

  const handleProfileChanged = useCallback(
    (updated: SystemAdminProfile) => {
      setProfile(updated);
      if (user) {
        setUser({
          ...user,
          fullName: updated.fullName,
          email: updated.email,
          imageUrl: updated.imageUrl,
          mustChangePassword: updated.mustChangePassword,
          passwordChangedAt: updated.passwordChangedAt,
        });
      }
    },
    [setUser, user],
  );

  const loadProfile = useCallback(async (isInitial = false) => {
    if (isInitial) {
      setIsLoading(true);
    } else {
      setIsReloading(true);
    }
    setError("");
    try {
      const data = await profileApi.getProfile();
      setProfile(data);
    } catch (requestError) {
      setError(
        getApiErrorMessage(
          requestError,
          "Không thể tải thông tin hồ sơ Quản trị viên.",
        ),
      );
    } finally {
      setIsLoading(false);
      setIsReloading(false);
    }
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void loadProfile(true), 0);
    return () => window.clearTimeout(timeoutId);
  }, [loadProfile]);

  return (
    <>
      <Header title="Hồ sơ quản trị viên" />
      <div className="grow overflow-y-auto bg-background/50">
        <div className="mx-auto w-full space-y-6 px-4 py-6 sm:px-6 lg:px-8">
          {isLoading ? (
            <ProfileSkeleton />
          ) : error ? (
            <div className="mx-auto max-w-2xl py-12">
              <Alert
                variant="destructive"
                className="border-destructive/40 shadow-sm"
              >
                <AlertCircle className="size-5" />
                <AlertTitle className="text-base font-semibold">
                  Không thể nạp dữ liệu hồ sơ
                </AlertTitle>
                <AlertDescription className="mt-2 space-y-4 text-sm">
                  <p>{error}</p>
                  <Button
                    variant="outline"
                    onClick={() => loadProfile(true)}
                    className="gap-2 bg-background hover:bg-muted"
                  >
                    <RefreshCw className="size-4" />
                    Thử lại
                  </Button>
                </AlertDescription>
              </Alert>
            </div>
          ) : profile ? (
            <div className="space-y-6">
              {/* HERO BANNER */}
              <ProfileHeroBanner
                profile={profile}
                isReloading={isReloading}
                onReload={() => loadProfile(false)}
                onProfileChanged={handleProfileChanged}
              />

              {/* MAIN CONTENT GRID */}
              <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px] xl:grid-cols-[minmax(0,1fr)_380px]">
                {/* Left Column: Profile Form (Personal + Admin Tabs) */}
                <div className="min-w-0 space-y-6">
                  <SystemAdminProfileForm
                    key={profile.rowVersion}
                    profile={profile}
                    onSaved={handleProfileChanged}
                    onReload={() => loadProfile(false)}
                  />
                </div>

                {/* Right Column: Security & System Meta Cards */}
                <div className="space-y-6">
                  <AccountSecurityCard
                    mustChangePassword={profile.mustChangePassword}
                    passwordChangedAt={profile.passwordChangedAt}
                  />

                  {/* System & Support Card */}
                  <Card className="border-border/80 shadow-sm gap-0">
                    <CardHeader className="pb-3">
                      <CardTitle className="flex items-center gap-2 text-sm font-semibold">
                        <LifeBuoy className="size-4 text-primary" />
                        <span>Hạ tầng & Quản trị</span>
                      </CardTitle>
                      <CardDescription className="text-xs">
                        Truy cập nhanh các phân hệ hệ thống
                      </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-3 pt-0 text-xs text-muted-foreground">
                      <div className="flex items-center justify-between rounded-lg bg-muted/40 p-2.5">
                        <span className="flex items-center gap-1.5 font-medium text-foreground">
                          <Clock className="size-3.5 text-muted-foreground" />
                          Trạng thái dữ liệu
                        </span>
                        <span className="font-mono text-[11px] text-muted-foreground">
                          Đồng bộ thời gian thực
                        </span>
                      </div>

                      <div className="space-y-2 pt-1">
                        <Button
                          asChild
                          variant="ghost"
                          size="sm"
                          className="w-full justify-start gap-2 text-xs font-normal hover:text-foreground"
                        >
                          <Link href="/tenants">
                            <Building className="size-3.5 text-primary" />
                            <span>Quản lý Doanh nghiệp (Tenants)</span>
                          </Link>
                        </Button>
                        <Button
                          asChild
                          variant="ghost"
                          size="sm"
                          className="w-full justify-start gap-2 text-xs font-normal hover:text-foreground"
                        >
                          <Link href="/audit-logs">
                            <ShieldCheck className="size-3.5 text-primary" />
                            <span>Nhật ký Kiểm toán Bảo mật</span>
                          </Link>
                        </Button>
                      </div>

                      <div className="rounded-xl border border-border/50 bg-muted/20 p-3 text-[11px] leading-relaxed text-muted-foreground">
                        <div className="flex items-center gap-1 font-medium text-foreground mb-1">
                          <HelpCircle className="size-3.5 text-primary" />
                          <span>Hạ tầng DTC Cloud</span>
                        </div>
                        Mọi thay đổi cấu hình bảo mật hoặc tài khoản System Admin đều được ghi lại trong chuỗi kiểm toán không thể xóa sửa.
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </div>
            </div>
          ) : null}
        </div>
      </div>
    </>
  );
}

function ProfileSkeleton() {
  return (
    <div className="space-y-6 animate-pulse">
      {/* Hero Banner Skeleton */}
      <div className="relative overflow-hidden rounded-2xl border border-border/70 bg-card p-6">
        <div className="h-28 w-full rounded-xl bg-muted/70" />
        <div className="flex flex-col sm:flex-row items-center sm:items-end gap-4 -mt-12 px-4">
          <Skeleton className="size-24 rounded-2xl border-4 border-card" />
          <div className="space-y-2 text-center sm:text-left">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-72" />
          </div>
        </div>
        <div className="mt-6 space-y-2">
          <Skeleton className="h-3 w-40" />
          <Skeleton className="h-2 w-full" />
        </div>
      </div>

      {/* Grid Skeleton */}
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px] xl:grid-cols-[minmax(0,1fr)_380px]">
        <div className="rounded-2xl border border-border/70 bg-card p-6 space-y-6">
          <div className="flex justify-between items-center pb-4 border-b border-border/60">
            <Skeleton className="h-5 w-32" />
            <Skeleton className="h-8 w-56 rounded-lg" />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Skeleton className="h-3 w-20" />
              <Skeleton className="h-9 w-full rounded-md" />
            </div>
            <div className="space-y-2">
              <Skeleton className="h-3 w-20" />
              <Skeleton className="h-9 w-full rounded-md" />
            </div>
          </div>
        </div>

        <div className="space-y-6">
          <div className="rounded-2xl border border-border/70 bg-card p-6 space-y-4">
            <Skeleton className="h-5 w-36" />
            <Skeleton className="h-16 w-full rounded-xl" />
            <Skeleton className="h-24 w-full rounded-xl" />
            <Skeleton className="h-9 w-full rounded-md" />
          </div>
        </div>
      </div>
    </div>
  );
}
