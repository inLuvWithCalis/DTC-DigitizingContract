"use client";

import { useCallback, useEffect, useState } from "react";
import { Activity, Database, FileStack, HardDrive, KeyRound, RefreshCw, Server, TimerReset } from "lucide-react";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Header } from "@/components/ui/custom/header";
import { Skeleton } from "@/components/ui/skeleton";
import { getApiErrorMessage } from "@/lib/api-error";
import { systemHealthApi, type SystemHealthResponse } from "@/services/system-health-api";

const POLL_INTERVAL_MS = 30_000;

function statusVariant(status: string): "default" | "secondary" | "destructive" {
  if (status === "Healthy") return "default";
  if (status === "Degraded") return "secondary";
  return "destructive";
}

function formatBytes(value: number | null) {
  if (value == null) return "Không xác định";
  const units = ["B", "KB", "MB", "GB", "TB"];
  let amount = value;
  let unit = 0;
  while (amount >= 1024 && unit < units.length - 1) { amount /= 1024; unit += 1; }
  return `${amount.toLocaleString("vi-VN", { maximumFractionDigits: 1 })} ${units[unit]}`;
}

function formatUptime(seconds: number) {
  const days = Math.floor(seconds / 86400);
  const hours = Math.floor((seconds % 86400) / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return `${days ? `${days} ngày ` : ""}${hours} giờ ${minutes} phút`;
}

export default function SystemHealthPage() {
  const [data, setData] = useState<SystemHealthResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadHealth = useCallback(async (showLoading = false) => {
    if (showLoading) setIsLoading(true);
    try {
      setData(await systemHealthApi.get());
      setError(null);
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, "Không thể tải trạng thái hệ thống."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    queueMicrotask(() => void loadHealth(true));
    const poll = () => { if (document.visibilityState === "visible") void loadHealth(); };
    const interval = window.setInterval(poll, POLL_INTERVAL_MS);
    document.addEventListener("visibilitychange", poll);
    return () => { window.clearInterval(interval); document.removeEventListener("visibilitychange", poll); };
  }, [loadHealth]);

  const checks = data ? [
    { title: "Central Database", icon: Database, status: data.centralDatabase.status, details: data.centralDatabase.code || "Kết nối bình thường" },
    { title: "Private storage", icon: HardDrive, status: data.privateStorage.status, details: `${data.privateStorage.writable ? "Có quyền ghi" : "Không thể ghi"} · Trống ${formatBytes(data.privateStorage.availableFreeSpaceBytes)}` },
    { title: "PDF renderer", icon: FileStack, status: data.pdfRenderer.status, details: data.pdfRenderer.mode },
    { title: "OTP delivery", icon: KeyRound, status: data.otpDelivery.status, details: `${data.otpDelivery.providerMode} · Backlog: ${data.otpDelivery.backlogCount ?? "chưa thu thập"}` },
    { title: "Session store", icon: TimerReset, status: data.sessionStore.status, details: data.sessionStore.mode },
    { title: "API runtime", icon: Server, status: data.api.status, details: `v${data.api.version} · ${formatUptime(data.api.uptimeSeconds)}` },
  ] : [];

  return <>
    <Header title="Giám sát hệ thống" />
    <div className="grow overflow-y-auto">
      <main className="space-y-6 p-3 sm:p-6 lg:p-10">
        <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div><div className="flex items-center gap-3"><span className="flex size-11 items-center justify-center rounded-xl bg-primary/10 text-primary"><Activity className="size-5" /></span><div><h1 className="text-2xl font-bold tracking-tight">Tình trạng hệ thống</h1><p className="text-sm text-muted-foreground">Thông tin vận hành đã lược bỏ đường dẫn, connection string và secrets.</p></div></div></div>
          <Button variant="outline" onClick={() => void loadHealth(true)} disabled={isLoading}><RefreshCw className={isLoading ? "animate-spin" : undefined} /> Kiểm tra lại</Button>
        </div>

        {error && <Alert variant="destructive"><AlertDescription>{error}</AlertDescription></Alert>}
        {isLoading && !data ? <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{Array.from({ length: 6 }, (_, index) => <Skeleton key={index} className="h-36 rounded-2xl" />)}</div> : data ? <>
          <Card className="rounded-2xl"><CardContent className="flex flex-col justify-between gap-4 pt-6 sm:flex-row sm:items-center"><div><p className="text-sm text-muted-foreground">Trạng thái tổng thể</p><p className="mt-1 text-2xl font-bold">{data.status}</p><p className="mt-1 text-xs text-muted-foreground">Cập nhật {new Date(data.generatedAt).toLocaleString("vi-VN")} · tự động mỗi 30 giây khi tab đang mở</p></div><div className="flex items-center gap-3"><Badge variant={statusVariant(data.status)} className="px-3 py-1">{data.status}</Badge><div className="rounded-xl bg-muted px-4 py-2 text-center"><p className="text-xs text-muted-foreground">Tenant lỗi</p><p className="text-xl font-bold">{data.failedTenantCount ?? "—"}</p></div></div></CardContent></Card>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{checks.map((check) => { const Icon = check.icon; return <Card key={check.title} className="rounded-2xl"><CardHeader className="flex-row items-center justify-between space-y-0 pb-3"><span className="flex items-center gap-2"><Icon className="size-4 text-primary" /><CardTitle className="text-base">{check.title}</CardTitle></span><Badge variant={statusVariant(check.status)}>{check.status}</Badge></CardHeader><CardContent><p className="text-sm text-muted-foreground">{check.details}</p></CardContent></Card>; })}</div>
        </> : null}
      </main>
    </div>
  </>;
}
