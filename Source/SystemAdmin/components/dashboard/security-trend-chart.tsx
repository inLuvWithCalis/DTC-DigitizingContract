"use client";

import { Bar, BarChart, CartesianGrid, Legend, XAxis, YAxis } from "recharts";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import type { CentralSecurityTrend } from "@/services/admin-dashboard-api";

export function SecurityTrendChart({ data }: { data: CentralSecurityTrend[] }) {
  return <Card className="rounded-2xl"><CardHeader><CardTitle className="text-base">Xu hướng bảo mật</CardTitle></CardHeader><CardContent>{data.length === 0 ? <div className="flex h-64 items-center justify-center text-sm text-muted-foreground">Không có sự kiện từ chối hoặc đăng nhập lỗi.</div> : <ChartContainer className="h-64 w-full" config={{ deniedCount: { label: "Từ chối", color: "var(--chart-4)" }, loginFailureCount: { label: "Đăng nhập lỗi", color: "var(--chart-1)" } }}><BarChart data={data} accessibilityLayer><CartesianGrid vertical={false} /><XAxis dataKey="period" tickLine={false} axisLine={false} minTickGap={24} /><YAxis allowDecimals={false} tickLine={false} axisLine={false} width={28} /><ChartTooltip content={<ChartTooltipContent />} /><Legend /><Bar dataKey="deniedCount" fill="var(--color-deniedCount)" radius={[4, 4, 0, 0]} /><Bar dataKey="loginFailureCount" fill="var(--color-loginFailureCount)" radius={[4, 4, 0, 0]} /></BarChart></ChartContainer>}</CardContent></Card>;
}
