"use client";

import { useMemo } from "react";
import { Area, AreaChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Activity, BarChart3, Flame } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import { Badge } from "@/components/ui/badge";
import type { DashboardVolumePoint } from "@/services/dashboard-api";

interface ContractVolumeChartProps {
  data: DashboardVolumePoint[];
}

export function ContractVolumeChart({ data }: ContractVolumeChartProps) {
  const { total, peak } = useMemo(() => {
    if (!data || data.length === 0) return { total: 0, peak: null };
    const totalCount = data.reduce((acc, curr) => acc + curr.count, 0);
    const peakItem = data.reduce(
      (max, curr) => (curr.count > (max?.count ?? -1) ? curr : max),
      data[0],
    );
    return { total: totalCount, peak: peakItem };
  }, [data]);

  return (
    <Card className="rounded-2xl border bg-card shadow-sm">
      <CardHeader className="flex flex-col gap-2 pb-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <CardTitle className="flex items-center gap-2.5 text-base font-semibold">
            <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Activity className="size-4" />
            </div>
            <span>Lưu lượng hợp đồng</span>
          </CardTitle>
          <p className="mt-1 text-xs text-muted-foreground">
            Biến động số lượng hợp đồng mới theo từng chu kỳ thời gian
          </p>
        </div>

        {data.length > 0 && (
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="secondary" className="flex items-center gap-1 font-medium text-xs">
              <span>Tổng:</span>
              <span className="font-bold text-foreground">{total.toLocaleString("vi-VN")}</span>
            </Badge>
            {peak && peak.count > 0 && (
              <Badge
                variant="outline"
                className="flex items-center gap-1 text-xs text-amber-600 border-amber-500/30 bg-amber-500/10 dark:text-amber-400"
              >
                <Flame className="size-3" />
                <span>Đỉnh: {peak.period} ({peak.count})</span>
              </Badge>
            )}
          </div>
        )}
      </CardHeader>

      <CardContent className="pt-3">
        {data.length === 0 ? (
          <div className="flex h-72 flex-col items-center justify-center gap-2 text-center text-muted-foreground">
            <div className="flex size-12 items-center justify-center rounded-2xl bg-muted/60">
              <BarChart3 className="size-6 stroke-[1.5]" />
            </div>
            <p className="text-sm font-medium">Chưa có dữ liệu trong khoảng thời gian đã chọn.</p>
            <p className="text-xs text-muted-foreground">Thử mở rộng khoảng thời gian tìm kiếm.</p>
          </div>
        ) : (
          <ChartContainer
            config={{
              count: {
                label: "Hợp đồng",
                color: "var(--primary)",
              },
            }}
            className="h-72 w-full"
          >
            <AreaChart data={data} accessibilityLayer margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
              <defs>
                <linearGradient id="volumeAreaGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="var(--primary)" stopOpacity={0.35} />
                  <stop offset="95%" stopColor="var(--primary)" stopOpacity={0.02} />
                </linearGradient>
              </defs>
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
              <ChartTooltip
                content={
                  <ChartTooltipContent
                    nameKey="count"
                    labelFormatter={(label) => `Kỳ: ${label}`}
                  />
                }
              />
              <Area
                type="monotone"
                dataKey="count"
                stroke="var(--primary)"
                strokeWidth={2.5}
                fill="url(#volumeAreaGradient)"
                activeDot={{ r: 5, fill: "var(--primary)", strokeWidth: 2, stroke: "#fff" }}
              />
            </AreaChart>
          </ChartContainer>
        )}
      </CardContent>
    </Card>
  );
}
