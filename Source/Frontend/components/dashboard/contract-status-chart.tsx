"use client";

import { useMemo } from "react";
import { Cell, Pie, PieChart, ResponsiveContainer } from "recharts";
import { PieChart as PieChartIcon } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import { Badge } from "@/components/ui/badge";
import type { DashboardStatusPoint } from "@/services/dashboard-api";

interface ContractStatusChartProps {
  data: DashboardStatusPoint[];
}

const statusConfig: Record<string, { label: string; color: string }> = {
  Draft: { label: "Bản nháp", color: "#64748b" },
  Negotiating: { label: "Đang đàm phán", color: "#8b5cf6" },
  PendingApproval: { label: "Chờ phê duyệt", color: "#f59e0b" },
  PendingSignature: { label: "Chờ ký số", color: "#06b6d4" },
  Signed: { label: "Đã ký kết", color: "#10b981" },
  Completed: { label: "Đã hoàn thành", color: "#3b82f6" },
  Rejected: { label: "Bị từ chối", color: "#ef4444" },
  Cancelled: { label: "Đã hủy bỏ", color: "#78716c" },
};

export function ContractStatusChart({ data }: ContractStatusChartProps) {
  const { chartData, totalCount } = useMemo(() => {
    const total = data.reduce((sum, item) => sum + item.count, 0);
    const mapped = data.map((item, index) => {
      const cfg = statusConfig[item.status] ?? {
        label: item.status,
        color: `hsl(${((index * 45) % 360)}, 70%, 50%)`,
      };
      const percentage = total > 0 ? ((item.count / total) * 100).toFixed(1) : "0";
      return {
        ...item,
        label: cfg.label,
        fill: cfg.color,
        percentage,
      };
    });
    return { chartData: mapped, totalCount: total };
  }, [data]);

  return (
    <Card className="rounded-2xl border bg-card shadow-sm">
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <div>
          <CardTitle className="flex items-center gap-2.5 text-base font-semibold">
            <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <PieChartIcon className="size-4" />
            </div>
            <span>Phân bố trạng thái</span>
          </CardTitle>
          <p className="mt-1 text-xs text-muted-foreground">
            Tỷ trọng các hợp đồng theo từng giai đoạn xử lý
          </p>
        </div>

        {totalCount > 0 && (
          <Badge variant="secondary" className="text-xs font-semibold">
            {totalCount.toLocaleString("vi-VN")} hợp đồng
          </Badge>
        )}
      </CardHeader>

      <CardContent className="pt-3">
        {data.length === 0 ? (
          <div className="flex h-72 flex-col items-center justify-center gap-2 text-center text-muted-foreground">
            <div className="flex size-12 items-center justify-center rounded-2xl bg-muted/60">
              <PieChartIcon className="size-6 stroke-[1.5]" />
            </div>
            <p className="text-sm font-medium">Chưa có dữ liệu trạng thái.</p>
            <p className="text-xs text-muted-foreground">Các trạng thái sẽ hiển thị khi có hợp đồng phát sinh.</p>
          </div>
        ) : (
          <div className="grid gap-6 sm:grid-cols-[1fr_220px] lg:grid-cols-[1fr_260px] items-center">
            {/* Donut Chart */}
            <div className="relative flex items-center justify-center">
              <ChartContainer config={{ count: { label: "Hợp đồng" } }} className="h-64 w-full">
                <PieChart accessibilityLayer>
                  <ChartTooltip
                    content={
                      <ChartTooltipContent
                        nameKey="label"
                        formatter={(value, name, item) => (
                          <div className="flex items-center gap-2">
                            <span className="font-semibold">{Number(value).toLocaleString("vi-VN")} HĐ</span>
                            <span className="text-muted-foreground">
                              ({(item.payload as { percentage?: string })?.percentage}%)
                            </span>
                          </div>
                        )}
                      />
                    }
                  />
                  <Pie
                    data={chartData}
                    dataKey="count"
                    nameKey="label"
                    innerRadius={56}
                    outerRadius={86}
                    paddingAngle={3}
                    cornerRadius={4}
                    stroke="var(--card)"
                    strokeWidth={2}
                  >
                    {chartData.map((entry) => (
                      <Cell key={entry.status} fill={entry.fill} />
                    ))}
                  </Pie>
                </PieChart>
              </ChartContainer>

              {/* Center Total overlay */}
              <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center text-center">
                <span className="text-xs font-medium text-muted-foreground">Tổng số</span>
                <span className="text-2xl font-bold tracking-tight text-foreground">
                  {totalCount.toLocaleString("vi-VN")}
                </span>
                <span className="text-[10px] text-muted-foreground">hợp đồng</span>
              </div>
            </div>

            {/* Status Breakdown Legend with progress bars */}
            <div className="flex flex-col gap-2.5 max-h-64 overflow-y-auto pr-1">
              {chartData.map((item) => (
                <div key={item.status} className="group flex flex-col gap-1 text-xs">
                  <div className="flex items-center justify-between gap-2">
                    <div className="flex min-w-0 items-center gap-2">
                      <span
                        className="size-2.5 shrink-0 rounded-full transition-transform group-hover:scale-125"
                        style={{ backgroundColor: item.fill }}
                      />
                      <span className="truncate font-medium text-foreground">{item.label}</span>
                    </div>
                    <div className="flex items-center gap-1.5 shrink-0">
                      <span className="font-semibold text-foreground">
                        {item.count.toLocaleString("vi-VN")}
                      </span>
                      <span className="text-[11px] text-muted-foreground">
                        ({item.percentage}%)
                      </span>
                    </div>
                  </div>
                  {/* Mini progress bar */}
                  <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted/60">
                    <div
                      className="h-full rounded-full transition-all duration-500"
                      style={{
                        width: `${item.percentage}%`,
                        backgroundColor: item.fill,
                      }}
                    />
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
