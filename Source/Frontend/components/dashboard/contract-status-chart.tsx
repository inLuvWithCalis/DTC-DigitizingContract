"use client";

import { Cell, Pie, PieChart } from "recharts";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import type { DashboardStatusPoint } from "@/services/dashboard-api";

const labels: Record<string, string> = {
  Draft: "Nháp",
  Negotiating: "Đàm phán",
  PendingApproval: "Chờ duyệt",
  PendingSignature: "Chờ ký",
  Signed: "Đã ký",
  Completed: "Hoàn thành",
  Cancelled: "Đã hủy",
  Rejected: "Từ chối",
};
const colors = ["var(--chart-1)", "var(--chart-2)", "var(--chart-3)", "var(--chart-4)", "var(--chart-5)", "#64748b", "#ef4444", "#f97316"];

export function ContractStatusChart({ data }: { data: DashboardStatusPoint[] }) {
  const chartData = data.map((item, index) => ({ ...item, label: labels[item.status] ?? item.status, fill: colors[index % colors.length] }));
  return (
    <Card className="rounded-2xl">
      <CardHeader><CardTitle className="text-base">Phân bố trạng thái</CardTitle></CardHeader>
      <CardContent className="grid gap-4 sm:grid-cols-[minmax(0,1fr)_180px]">
        {data.length === 0 ? (
          <div className="flex h-64 items-center justify-center text-sm text-muted-foreground sm:col-span-2">Chưa có dữ liệu trạng thái.</div>
        ) : (
          <>
            <ChartContainer config={{ count: { label: "Hợp đồng" } }} className="h-64 w-full">
              <PieChart accessibilityLayer>
                <ChartTooltip content={<ChartTooltipContent nameKey="label" />} />
                <Pie data={chartData} dataKey="count" nameKey="label" innerRadius={52} outerRadius={82} paddingAngle={2}>
                  {chartData.map((item) => <Cell key={item.status} fill={item.fill} />)}
                </Pie>
              </PieChart>
            </ChartContainer>
            <div className="flex flex-col justify-center gap-2">
              {chartData.map((item) => (
                <div key={item.status} className="flex items-center justify-between gap-3 text-sm">
                  <span className="flex min-w-0 items-center gap-2"><span className="size-2.5 shrink-0 rounded-full" style={{ backgroundColor: item.fill }} /><span className="truncate">{item.label}</span></span>
                  <span className="font-semibold">{item.count}</span>
                </div>
              ))}
            </div>
          </>
        )}
      </CardContent>
    </Card>
  );
}
