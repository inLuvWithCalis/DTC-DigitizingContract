"use client";

import Link from "next/link";
import {
  Activity,
  AlertTriangle,
  ArrowRight,
  Building2,
  CheckCircle2,
  CircleGauge,
  Clock3,
  Database,
  HardDrive,
  LockKeyhole,
  Plus,
  Server,
  ShieldCheck,
  Users,
} from "lucide-react";
import { Header } from "@/components/ui/custom/header";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { useAuthStore } from "@/hooks/use-auth-store";
import { MOCK_TENANTS } from "@/services/tenant-management-mock";
import { MOCK_SYSTEM_ADMINS } from "@/services/system-admin-accounts-mock";
import { cn } from "@/lib/utils";

const tenantGrowth = [
  { month: "T2", value: 2 },
  { month: "T3", value: 3 },
  { month: "T4", value: 3 },
  { month: "T5", value: 4 },
  { month: "T6", value: 5 },
  { month: "T7", value: 6 },
];

const recentActivities = [
  {
    title: "Khởi tạo tenant Atlas Holdings",
    detail: "Database đang được cấp phát trên sql-prod-03.internal",
    time: "12 phút trước",
    icon: Plus,
    tone: "blue",
  },
  {
    title: "Khóa tenant Zenith Logistics",
    detail: "Thực hiện bởi Nguyễn Minh Anh · Lý do: yêu cầu từ khách hàng",
    time: "2 giờ trước",
    icon: LockKeyhole,
    tone: "rose",
  },
  {
    title: "Backup hoàn tất",
    detail: "6/6 tenant database đã được sao lưu thành công",
    time: "7 giờ trước",
    icon: Database,
    tone: "emerald",
  },
  {
    title: "Thay đổi quyền quản trị viên",
    detail: "audit.linh được gán vai trò Security Auditor",
    time: "1 ngày trước",
    icon: ShieldCheck,
    tone: "violet",
  },
];

const systemServices = [
  {
    name: "API Gateway",
    description: "api.econtract.internal",
    status: "Ổn định",
    latency: "42 ms",
  },
  {
    name: "Central Database",
    description: "sql-central-01.internal",
    status: "Ổn định",
    latency: "18 ms",
  },
  {
    name: "Tenant Databases",
    description: "5 ổn định · 1 cần theo dõi",
    status: "Cảnh báo",
    latency: "63 ms",
  },
  {
    name: "File Storage",
    description: "Object storage cluster",
    status: "Ổn định",
    latency: "55 ms",
  },
];

function formatStorage(megabytes: number) {
  if (megabytes < 1024) return `${megabytes} MB`;
  return `${(megabytes / 1024).toFixed(megabytes >= 10240 ? 0 : 1)} GB`;
}

function StatCard({
  title,
  value,
  description,
  icon: Icon,
  tone,
}: {
  title: string;
  value: string;
  description: string;
  icon: typeof Building2;
  tone: "blue" | "emerald" | "amber" | "violet";
}) {
  const tones = {
    blue: "bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-300",
    emerald:
      "bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-300",
    amber:
      "bg-amber-50 text-amber-600 dark:bg-amber-950/40 dark:text-amber-300",
    violet:
      "bg-violet-50 text-violet-600 dark:bg-violet-950/40 dark:text-violet-300",
  };

  return (
    <Card className="gap-0 py-0 shadow-sm">
      <CardContent className="flex items-start justify-between p-5">
        <div>
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          <p className="mt-2 text-2xl font-bold tracking-tight">{value}</p>
          <p className="mt-1 text-xs text-muted-foreground">{description}</p>
        </div>
        <div
          className={cn(
            "flex size-11 items-center justify-center rounded-xl",
            tones[tone],
          )}
        >
          <Icon className="size-5" />
        </div>
      </CardContent>
    </Card>
  );
}

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const activeTenants = MOCK_TENANTS.filter(
    (tenant) => tenant.status === "Active",
  ).length;
  const lockedTenants = MOCK_TENANTS.filter(
    (tenant) => tenant.status === "Locked",
  ).length;
  const provisioningTenants = MOCK_TENANTS.filter(
    (tenant) => tenant.status === "Provisioning",
  ).length;
  const activeAdmins = MOCK_SYSTEM_ADMINS.filter(
    (admin) => admin.status === "Active",
  ).length;
  const totalStorage = MOCK_TENANTS.reduce(
    (total, tenant) => total + tenant.storageUsedMb,
    0,
  );
  const totalStorageLimit = MOCK_TENANTS.reduce(
    (total, tenant) => total + tenant.storageLimitMb,
    0,
  );
  const storagePercent = Math.round((totalStorage / totalStorageLimit) * 100);
  const sortedStorageTenants = [...MOCK_TENANTS]
    .sort(
      (left, right) =>
        right.storageUsedMb / right.storageLimitMb -
        left.storageUsedMb / left.storageLimitMb,
    )
    .slice(0, 4);
  const maxGrowth = Math.max(...tenantGrowth.map((item) => item.value));

  return (
    <>
      <Header title="Tổng quan hệ thống" />

      <div className="grow overflow-y-auto bg-muted/20">
        <div className="mx-auto w-full max-w-[1500px] space-y-6 px-4 py-6 sm:px-6 lg:px-10 lg:py-8">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <div className="mb-2 flex items-center gap-2 text-sm font-medium text-primary">
                <ShieldCheck className="size-4" />
                System Administration
              </div>
              <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">
                Xin chào, {user?.fullName || "Quản trị viên"}
              </h1>
              <p className="mt-2 text-sm text-muted-foreground">
                Theo dõi sức khỏe hệ thống, tenant và hoạt động quản trị hôm
                nay.
              </p>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" asChild>
                <Link href="/administrators">
                  <Users />
                  Quản trị viên
                </Link>
              </Button>
              <Button asChild>
                <Link href="/tenants">
                  <Plus />
                  Tạo tenant
                </Link>
              </Button>
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <StatCard
              title="Tổng tenant"
              value={MOCK_TENANTS.length.toString()}
              description={`${activeTenants} đang hoạt động`}
              icon={Building2}
              tone="blue"
            />
            <StatCard
              title="Database cần chú ý"
              value="1"
              description={`${provisioningTenants} database đang khởi tạo`}
              icon={Database}
              tone="amber"
            />
            <StatCard
              title="Dung lượng hệ thống"
              value={formatStorage(totalStorage)}
              description={`${storagePercent}% tổng hạn mức`}
              icon={HardDrive}
              tone="violet"
            />
            <StatCard
              title="System Admin"
              value={activeAdmins.toString()}
              description={`${MOCK_SYSTEM_ADMINS.length} tài khoản được cấp`}
              icon={ShieldCheck}
              tone="emerald"
            />
          </div>

          <div className="grid gap-6 xl:grid-cols-[minmax(0,1.25fr)_minmax(380px,0.75fr)]">
            <Card className="shadow-sm">
              <CardHeader className="flex flex-row items-start justify-between border-b">
                <div>
                  <CardTitle>Tăng trưởng tenant</CardTitle>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Số tenant hoạt động trong 6 tháng gần nhất
                  </p>
                </div>
                <Badge variant="outline">+20% trong tháng</Badge>
              </CardHeader>
              <CardContent>
                <div className="flex h-64 items-end gap-4 pt-5 sm:gap-7">
                  {tenantGrowth.map((item, index) => (
                    <div
                      key={item.month}
                      className="flex h-full flex-1 flex-col items-center justify-end gap-2"
                    >
                      <span className="text-xs font-semibold">{item.value}</span>
                      <div
                        className={cn(
                          "w-full max-w-14 rounded-t-lg transition-all",
                          index === tenantGrowth.length - 1
                            ? "bg-primary"
                            : "bg-primary/20",
                        )}
                        style={{
                          height: `${Math.max(15, (item.value / maxGrowth) * 82)}%`,
                        }}
                      />
                      <span className="text-xs text-muted-foreground">
                        {item.month}
                      </span>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>

            <Card className="shadow-sm">
              <CardHeader className="border-b">
                <CardTitle>Trạng thái tenant</CardTitle>
                <p className="mt-1 text-sm text-muted-foreground">
                  Phân bố trạng thái hiện tại
                </p>
              </CardHeader>
              <CardContent className="flex flex-col items-center gap-6 sm:flex-row xl:flex-col 2xl:flex-row">
                <div
                  className="relative flex size-44 shrink-0 items-center justify-center rounded-full"
                  style={{
                    background: `conic-gradient(
                      var(--primary) 0 ${(activeTenants / MOCK_TENANTS.length) * 100}%,
                      #f43f5e ${(activeTenants / MOCK_TENANTS.length) * 100}% ${((activeTenants + lockedTenants) / MOCK_TENANTS.length) * 100}%,
                      #f59e0b ${((activeTenants + lockedTenants) / MOCK_TENANTS.length) * 100}% 100%
                    )`,
                  }}
                >
                  <div className="flex size-28 flex-col items-center justify-center rounded-full bg-card">
                    <span className="text-3xl font-bold">
                      {MOCK_TENANTS.length}
                    </span>
                    <span className="text-xs text-muted-foreground">
                      tenant
                    </span>
                  </div>
                </div>
                <div className="w-full space-y-3">
                  {[
                    {
                      label: "Đang hoạt động",
                      value: activeTenants,
                      color: "bg-primary",
                    },
                    {
                      label: "Đã khóa",
                      value: lockedTenants,
                      color: "bg-rose-500",
                    },
                    {
                      label: "Đang khởi tạo",
                      value: provisioningTenants,
                      color: "bg-amber-500",
                    },
                  ].map((item) => (
                    <div
                      key={item.label}
                      className="flex items-center justify-between rounded-lg border px-3 py-2.5"
                    >
                      <span className="flex items-center gap-2 text-sm">
                        <span
                          className={cn("size-2 rounded-full", item.color)}
                        />
                        {item.label}
                      </span>
                      <span className="font-semibold">{item.value}</span>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>

          <div className="grid gap-6 xl:grid-cols-2">
            <Card className="shadow-sm">
              <CardHeader className="flex flex-row items-center justify-between border-b">
                <div>
                  <CardTitle>Sức khỏe hệ thống</CardTitle>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Cập nhật lần cuối lúc 09:45
                  </p>
                </div>
                <Badge className="border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-50 dark:border-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-300">
                  <CheckCircle2 />
                  Hệ thống hoạt động
                </Badge>
              </CardHeader>
              <CardContent className="divide-y p-0">
                {systemServices.map((service) => (
                  <div
                    key={service.name}
                    className="flex items-center gap-3 px-6 py-4"
                  >
                    <div
                      className={cn(
                        "flex size-9 items-center justify-center rounded-lg",
                        service.status === "Ổn định"
                          ? "bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-300"
                          : "bg-amber-50 text-amber-600 dark:bg-amber-950/40 dark:text-amber-300",
                      )}
                    >
                      {service.name.includes("Database") ? (
                        <Database className="size-4" />
                      ) : (
                        <Server className="size-4" />
                      )}
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="font-medium">{service.name}</p>
                      <p className="truncate text-xs text-muted-foreground">
                        {service.description}
                      </p>
                    </div>
                    <div className="text-right">
                      <p
                        className={cn(
                          "text-xs font-semibold",
                          service.status === "Ổn định"
                            ? "text-emerald-600"
                            : "text-amber-600",
                        )}
                      >
                        {service.status}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {service.latency}
                      </p>
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>

            <Card className="shadow-sm">
              <CardHeader className="flex flex-row items-center justify-between border-b">
                <div>
                  <CardTitle>Sử dụng dung lượng</CardTitle>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Các tenant sử dụng nhiều nhất
                  </p>
                </div>
                <Button variant="ghost" size="sm" asChild>
                  <Link href="/tenants">
                    Xem tất cả
                    <ArrowRight />
                  </Link>
                </Button>
              </CardHeader>
              <CardContent className="space-y-5">
                {sortedStorageTenants.map((tenant) => {
                  const percent = Math.round(
                    (tenant.storageUsedMb / tenant.storageLimitMb) * 100,
                  );
                  return (
                    <div key={tenant.tenantId}>
                      <div className="flex items-center justify-between gap-4 text-sm">
                        <div className="min-w-0">
                          <p className="truncate font-medium">
                            {tenant.tenantName}
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {tenant.tenantCode}
                          </p>
                        </div>
                        <span
                          className={cn(
                            "shrink-0 text-xs font-medium",
                            percent >= 85 && "text-amber-600",
                          )}
                        >
                          {formatStorage(tenant.storageUsedMb)} /{" "}
                          {formatStorage(tenant.storageLimitMb)}
                        </span>
                      </div>
                      <Progress
                        value={percent}
                        className={cn(
                          "mt-2 h-1.5",
                          percent >= 85 && "[&>div]:bg-amber-500",
                        )}
                      />
                    </div>
                  );
                })}
              </CardContent>
            </Card>
          </div>

          <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_400px]">
            <Card className="shadow-sm">
              <CardHeader className="border-b">
                <CardTitle>Hoạt động quản trị gần đây</CardTitle>
                <p className="mt-1 text-sm text-muted-foreground">
                  Những thay đổi quan trọng trên toàn hệ thống
                </p>
              </CardHeader>
              <CardContent className="divide-y p-0">
                {recentActivities.map((activity) => {
                  const Icon = activity.icon;
                  const toneClasses = {
                    blue: "bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-300",
                    rose: "bg-rose-50 text-rose-600 dark:bg-rose-950/40 dark:text-rose-300",
                    emerald:
                      "bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-300",
                    violet:
                      "bg-violet-50 text-violet-600 dark:bg-violet-950/40 dark:text-violet-300",
                  };
                  return (
                    <div
                      key={activity.title}
                      className="flex items-start gap-3 px-6 py-4"
                    >
                      <div
                        className={cn(
                          "flex size-9 shrink-0 items-center justify-center rounded-lg",
                          toneClasses[
                            activity.tone as keyof typeof toneClasses
                          ],
                        )}
                      >
                        <Icon className="size-4" />
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="font-medium">{activity.title}</p>
                        <p className="mt-0.5 text-sm text-muted-foreground">
                          {activity.detail}
                        </p>
                      </div>
                      <span className="shrink-0 text-xs text-muted-foreground">
                        {activity.time}
                      </span>
                    </div>
                  );
                })}
              </CardContent>
            </Card>

            <div className="space-y-4">
              <Alert className="border-amber-200 bg-amber-50 text-amber-950 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-100">
                <AlertTriangle className="size-4" />
                <AlertTitle>Nova Retail dùng 90% dung lượng</AlertTitle>
                <AlertDescription>
                  Nên nâng hạn mức hoặc dọn dữ liệu trước khi database đầy.
                </AlertDescription>
              </Alert>
              <Alert>
                <Clock3 className="size-4" />
                <AlertTitle>Atlas Holdings đang khởi tạo</AlertTitle>
                <AlertDescription>
                  Provisioning database đang ở bước chạy migration.
                </AlertDescription>
              </Alert>
              <Card className="shadow-sm">
                <CardContent className="p-5">
                  <div className="flex items-center gap-3">
                    <div className="flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
                      <CircleGauge className="size-5" />
                    </div>
                    <div>
                      <p className="font-semibold">SLA hệ thống</p>
                      <p className="text-sm text-muted-foreground">
                        99,98% trong 30 ngày
                      </p>
                    </div>
                  </div>
                  <Progress value={99.98} className="mt-4 h-2" />
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
