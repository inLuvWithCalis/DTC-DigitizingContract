"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { LayoutDashboard, Palette, RefreshCw, Rows3, Save } from "lucide-react";
import { useTheme } from "next-themes";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "@/components/ui/sonner";
import { useAuthStore } from "@/hooks/use-auth-store";
import { getApiErrorMessage, isStaleRowVersion } from "@/lib/api-error";
import {
  getStoredTablePageSize,
  preferencesApi,
  storeTablePageSize,
  TABLE_PAGE_SIZE_OPTIONS,
  type EmployeePreferences,
} from "@/services/preferences-api";

type ThemePreference = "system" | "light" | "dark";

const THEME_OPTIONS: Array<{ value: ThemePreference; label: string }> = [
  { value: "system", label: "Theo hệ thống" },
  { value: "light", label: "Sáng" },
  { value: "dark", label: "Tối" },
];

const normalizeTheme = (value: string | undefined): ThemePreference =>
  value === "light" || value === "dark" ? value : "system";

interface PreferencesFormProps {
  preferences: EmployeePreferences;
  onSaved: (preferences: EmployeePreferences) => void;
  onReload: () => Promise<void>;
}

export function PreferencesForm({
  preferences,
  onSaved,
  onReload,
}: PreferencesFormProps) {
  const { theme, setTheme } = useTheme();
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const [mounted, setMounted] = useState(false);
  const [defaultPage, setDefaultPage] = useState(preferences.defaultPage);
  const [themePreference, setThemePreference] =
    useState<ThemePreference>("system");
  const [savedTheme, setSavedTheme] = useState<ThemePreference>("system");
  const [pageSize, setPageSize] = useState(20);
  const [savedPageSize, setSavedPageSize] = useState(20);
  const [isSaving, setIsSaving] = useState(false);
  const [isStale, setIsStale] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      const currentTheme = normalizeTheme(theme);
      const currentPageSize = getStoredTablePageSize() ?? 20;
      setThemePreference(currentTheme);
      setSavedTheme(currentTheme);
      setPageSize(currentPageSize);
      setSavedPageSize(currentPageSize);
      setMounted(true);
    }, 0);
    return () => window.clearTimeout(timeoutId);
  }, [theme]);

  const isDirty = useMemo(
    () =>
      defaultPage !== preferences.defaultPage ||
      themePreference !== savedTheme ||
      pageSize !== savedPageSize,
    [
      defaultPage,
      pageSize,
      preferences.defaultPage,
      savedPageSize,
      savedTheme,
      themePreference,
    ],
  );

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");
    setIsStale(false);
    setIsSaving(true);

    try {
      if (defaultPage !== preferences.defaultPage) {
        const updated = await preferencesApi.update({
          defaultPage,
          rowVersion: preferences.rowVersion,
        });
        onSaved(updated);
        if (user) {
          setUser({ ...user, defaultPage: updated.defaultPage });
        }
      }

      storeTablePageSize(pageSize);
      setTheme(themePreference);
      setSavedPageSize(pageSize);
      setSavedTheme(themePreference);
      toast.success("Đã lưu tùy chọn cá nhân.");
    } catch (requestError) {
      const stale = isStaleRowVersion(requestError);
      setIsStale(stale);
      setError(
        stale
          ? "Tùy chọn đã thay đổi ở một phiên khác. Vui lòng tải lại trước khi lưu."
          : getApiErrorMessage(requestError, "Không thể lưu tùy chọn cá nhân."),
      );
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && (
        <Alert variant="destructive">
          <AlertTitle>Không thể lưu cấu hình</AlertTitle>
          <AlertDescription className="mt-2 space-y-3">
            <p>{error}</p>
            {isStale && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="bg-background"
                onClick={() => void onReload()}
              >
                <RefreshCw className="size-4" />
                Tải dữ liệu mới
              </Button>
            )}
          </AlertDescription>
        </Alert>
      )}

      <Card className="border-border/80 shadow-sm">
        <CardHeader>
          <CardTitle>Tùy chọn hiển thị</CardTitle>
          <CardDescription>
            Trang mặc định được lưu theo tài khoản. Giao diện và số dòng được lưu
            riêng trên trình duyệt này.
          </CardDescription>
        </CardHeader>
        <CardContent className="grid gap-6 lg:grid-cols-3">
          <div className="space-y-2">
            <Label htmlFor="default-page" className="flex items-center gap-2">
              <LayoutDashboard className="size-4 text-primary" />
              Trang sau khi đăng nhập
            </Label>
            <Select value={defaultPage} onValueChange={setDefaultPage}>
              <SelectTrigger id="default-page" className="w-full bg-background">
                <SelectValue placeholder="Chọn trang mặc định" />
              </SelectTrigger>
              <SelectContent showSearch={false}>
                {preferences.availableLandingPages.map((option) => (
                  <SelectItem key={option.path} value={option.path}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">
              Chỉ các trang tài khoản của bạn được phép truy cập mới xuất hiện.
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="theme" className="flex items-center gap-2">
              <Palette className="size-4 text-primary" />
              Giao diện
            </Label>
            <Select
              value={themePreference}
              onValueChange={(value) =>
                setThemePreference(value as ThemePreference)
              }
              disabled={!mounted}
            >
              <SelectTrigger id="theme" className="w-full bg-background">
                <SelectValue placeholder="Chọn giao diện" />
              </SelectTrigger>
              <SelectContent showSearch={false}>
                {THEME_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">
              Dùng cơ chế theme hiện có và không gửi lựa chọn này lên server.
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="page-size" className="flex items-center gap-2">
              <Rows3 className="size-4 text-primary" />
              Số dòng mặc định
            </Label>
            <Select
              value={String(pageSize)}
              onValueChange={(value) => setPageSize(Number(value))}
              disabled={!mounted}
            >
              <SelectTrigger id="page-size" className="w-full bg-background">
                <SelectValue />
              </SelectTrigger>
              <SelectContent showSearch={false}>
                {TABLE_PAGE_SIZE_OPTIONS.map((option) => (
                  <SelectItem key={option} value={String(option)}>
                    {option} dòng
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">
              Áp dụng cho các bảng phân trang server trên trình duyệt này.
            </p>
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button type="submit" disabled={!mounted || !isDirty || isSaving}>
          {isSaving ? (
            <RefreshCw className="size-4 animate-spin" />
          ) : (
            <Save className="size-4" />
          )}
          Lưu tùy chọn
        </Button>
      </div>
    </form>
  );
}
