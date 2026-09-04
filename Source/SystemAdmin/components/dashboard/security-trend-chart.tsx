"use client";

import { useMemo } from "react";
import { Bar, BarChart, CartesianGrid, Legend, XAxis, YAxis } from "recharts";
import { KeyRound, ShieldAlert, ShieldCheck } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import { Badge } from "@/components/ui/badge";
import type { CentralSecurityTrend } from "@/services/admin-dashboard-api";

interface SecurityTrendChartProps {
  data: CentralSecurityTrend[];
}

export function SecurityTrendChart({ data }: SecurityTrendChartProps) {
  const { totalDenied, totalLoginFailures } = useMemo(() => {
    if (!data || data.length === 0) return { totalDenied: 0, totalLoginFailures: 0 };
    return {
      totalDenied: data.reduce((sum, item) => sum + item.deniedCount, 0),
      totalLoginFailures: data.reduce((sum, item) => sum + item.loginFailureCount, 0),
    };
  }, [data]);

  return (
    <Card className="rounded-2xl border bg-card shadow-sm">
      <CardHeader className="flex flex-col gap-2 pb-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <CardTitle className="flex items-center gap-2.5 text-base font-semibold">
            <div className="flex size-8 items-center justify-center rounded-lg bg-rose-500/10 text-rose-600 dark:bg-rose-500/15 dark:text-rose-400">
              <ShieldAlert className="size-4" />
            </div>
            <span>Xu hướng an ninh & bảo mật</span>
          </CardTitle>
          <p className="mt-1 text-xs text-muted-foreground">
            Theo dõi tần suất các truy cập bị từ chối và lượt đăng nhập thất bại
          </p>
        </div>

        {data.length > 0 && (
          <div className="flex flex-wrap items-center gap-2">
            <Badge
              variant="outline"
              className="flex items-center gap-1 text-xs border-amber-500/30 bg-amber-500/10 text-amber-600 dark:text-amber-400"
            >
              <span>Từ chối:</span>
              <span className="font-bold">{totalDenied.toLocaleString("vi-VN")}</span>
            </Badge>
            <Badge
              variant="outline"
              className="flex items-center gap-1 text-xs border-rose-500/30 bg-rose-500/10 text-rose-600 dark:text-rose-400"
            >
              <KeyRound className="size-3" />
              <span>Đăng nhập lỗi:</span>
              <span className="font-bold">{totalLoginFailures.toLocaleString("vi-VN")}</span>
            </Badge>
          </div>
        )}
      </CardHeader>

      <CardContent className="pt-3">
        {data.length === 0 ? (
          <div className="flex h-72 flex-col items-center justify-center gap-2 text-center text-muted-foreground">
            <div className="flex size-12 items-center justify-center rounded-2xl bg-emerald-500/10 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-400">
              <ShieldCheck className="size-6" />
            </div>
            <p className="text-sm font-semibold text-foreground">
              Không phát hiện sự kiện bất thường
            </p>
            <p className="text-xs text-muted-foreground">
              Không có sự kiện từ chối hoặc đăng nhập lỗi trong khoảng thời gian đã chọn.
            </p>
          </div>
        ) : (
          <ChartContainer
            className="h-72 w-full"
            config={{
              deniedCount: {
                label: "Từ chối truy cập",
                color: "#f59e0b",
              },
              loginFailureCount: {
                label: "Đăng nhập lỗi",
                color: "#ef4444",
              },
            }}
          >
            <BarChart data={data} accessibilityLayer margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
              <CartesianGrid vertical={false} strokeDasharray="3 3" className="stroke-border/60" />
              <XAxis
                dataKey="period"
                tickLine={false}
                axisLine={false}
                minTickGap={20}
                className="text-xs fill-muted-foreground"
              />
              <YAxis
                allowDecimals={false}
                tickLine={false}
                axisLine={false}
                className="text-xs fill-muted-foreground"
                width={35}
              />
              <ChartTooltip content={<ChartTooltipContent />} />
              <Legend
                verticalAlign="top"
                align="right"
                wrapperStyle={{ paddingBottom: "10px", fontSize: "12px" }}
              />
              <Bar
                dataKey="deniedCount"
                fill="var(--color-deniedCount)"
                radius={[6, 6, 0, 0]}
                maxBarSize={40}
              />
              <Bar
                dataKey="loginFailureCount"
                fill="var(--color-loginFailureCount)"
                radius={[6, 6, 0, 0]}
                maxBarSize={40}
              />
            </BarChart>
          </ChartContainer>
        )}
      </CardContent>
    </Card>
  );
}
