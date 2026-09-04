"use client";

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import type { DashboardVolumePoint } from "@/services/dashboard-api";

export function ContractVolumeChart({ data }: { data: DashboardVolumePoint[] }) {
  return (
    <Card className="rounded-2xl">
      <CardHeader><CardTitle className="text-base">Lưu lượng hợp đồng</CardTitle></CardHeader>
      <CardContent>
        {data.length === 0 ? (
          <div className="flex h-64 items-center justify-center text-sm text-muted-foreground">Chưa có hợp đồng trong khoảng đã chọn.</div>
        ) : (
          <ChartContainer config={{ count: { label: "Hợp đồng", color: "var(--chart-1)" } }} className="h-64 w-full">
            <BarChart data={data} accessibilityLayer>
              <CartesianGrid vertical={false} />
              <XAxis dataKey="period" tickLine={false} axisLine={false} minTickGap={24} />
              <YAxis allowDecimals={false} tickLine={false} axisLine={false} width={28} />
              <ChartTooltip content={<ChartTooltipContent />} />
              <Bar dataKey="count" fill="var(--color-count)" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ChartContainer>
        )}
      </CardContent>
    </Card>
  );
}
