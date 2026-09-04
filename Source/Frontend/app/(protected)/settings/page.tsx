"use client";

import { useCallback, useEffect, useState } from "react";
import { AlertCircle, RefreshCw, Settings } from "lucide-react";
import { PreferencesForm } from "@/components/account/preferences-form";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Header } from "@/components/ui/custom/header";
import { Skeleton } from "@/components/ui/skeleton";
import { getApiErrorMessage } from "@/lib/api-error";
import {
  preferencesApi,
  type EmployeePreferences,
} from "@/services/preferences-api";

export default function SettingsPage() {
  const [preferences, setPreferences] =
    useState<EmployeePreferences | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  const loadPreferences = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      setPreferences(await preferencesApi.get());
    } catch (requestError) {
      setError(
        getApiErrorMessage(
          requestError,
          "Không thể tải tùy chọn cá nhân.",
        ),
      );
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void loadPreferences(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [loadPreferences]);

  return (
    <>
      <Header title="Cấu hình cá nhân" />
      <div className="grow overflow-y-auto bg-background/50">
        <div className="mx-auto w-full max-w-6xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
          <div>
            <h1 className="flex items-center gap-2 text-2xl font-bold tracking-tight">
              <Settings className="size-6 text-primary" />
              Cấu hình cá nhân
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Điều chỉnh điểm bắt đầu và cách hiển thị phù hợp với cách bạn làm việc.
            </p>
          </div>

          {isLoading ? (
            <SettingsSkeleton />
          ) : error || !preferences ? (
            <Alert variant="destructive">
              <AlertCircle className="size-5" />
              <AlertTitle>Không thể tải cấu hình</AlertTitle>
              <AlertDescription className="mt-2 space-y-4">
                <p>{error || "Không nhận được dữ liệu cấu hình."}</p>
                <Button
                  variant="outline"
                  className="bg-background"
                  onClick={() => void loadPreferences()}
                >
                  <RefreshCw className="size-4" />
                  Thử lại
                </Button>
              </AlertDescription>
            </Alert>
          ) : (
            <PreferencesForm
              key={preferences.rowVersion}
              preferences={preferences}
              onSaved={setPreferences}
              onReload={loadPreferences}
            />
          )}
        </div>
      </div>
    </>
  );
}

function SettingsSkeleton() {
  return (
    <Card>
      <CardHeader className="space-y-2">
        <Skeleton className="h-6 w-44" />
        <Skeleton className="h-4 w-full max-w-xl" />
      </CardHeader>
      <CardContent className="grid gap-6 lg:grid-cols-3">
        {[0, 1, 2].map((item) => (
          <div key={item} className="space-y-2">
            <Skeleton className="h-4 w-36" />
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-3 w-4/5" />
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
