"use client";
import { Sidebar } from "@/components/sidebar";
import {
  Bell,
  User,
  FileText,
  Clock,
  AlertCircle,
  FileCheck2,
  FilePlus,
  CheckCircle2,
  TrendingUp,
  TrendingDown,
} from "lucide-react";
import { Header } from "@/components/ui/custom/header";

export default function DashboardPage() {
  const stats = [
    {
      label: "Tổng Hợp đồng",
      value: "1,248",
      trend: "+12.5%",
      isPositive: true,
      icon: FileText,
      color: "text-indigo-600 dark:text-indigo-400",
      bg: "bg-indigo-100 dark:bg-indigo-500/15",
      border:
        "group-hover:border-indigo-300 dark:group-hover:border-indigo-500/40",
    },
    {
      label: "Chờ Phê duyệt",
      value: "24",
      trend: "-2.4%",
      isPositive: true, // Giảm số lượng chờ là tốt
      icon: Clock,
      color: "text-amber-600 dark:text-amber-400",
      bg: "bg-amber-100 dark:bg-amber-500/15",
      border:
        "group-hover:border-amber-300 dark:group-hover:border-amber-500/40",
    },
    {
      label: "Sắp Hết hạn",
      value: "12",
      trend: "+4.1%",
      isPositive: false,
      icon: AlertCircle,
      color: "text-rose-600 dark:text-rose-400",
      bg: "bg-rose-100 dark:bg-rose-500/15",
      border: "group-hover:border-rose-300 dark:group-hover:border-rose-500/40",
    },
    {
      label: "Đã Thanh lý",
      value: "856",
      trend: "+8.2%",
      isPositive: true,
      icon: FileCheck2,
      color: "text-emerald-600 dark:text-emerald-400",
      bg: "bg-emerald-100 dark:bg-emerald-500/15",
      border:
        "group-hover:border-emerald-300 dark:group-hover:border-emerald-500/40",
    },
  ];

  // Dữ liệu mẫu cho Hoạt động gần đây
  const recentActivities = [
    {
      action: "Tạo mới Hợp đồng Bán",
      details: "HĐ #HB-2024-001 - Công ty TNHH ABC",
      time: "2 giờ trước",
      icon: FilePlus,
      iconColor: "text-indigo-500 dark:text-indigo-400",
      iconBg: "bg-indigo-50 dark:bg-indigo-500/15",
    },
    {
      action: "Phê duyệt Phụ lục",
      details: "Phụ lục #PL-01 cho HĐ #HB-2023-102",
      time: "4 giờ trước",
      icon: CheckCircle2,
      iconColor: "text-emerald-500 dark:text-emerald-400",
      iconBg: "bg-emerald-50 dark:bg-emerald-500/15",
    },
    {
      action: "Cảnh báo Hết hạn",
      details: "Hợp đồng Mua #HM-092 sắp hết hiệu lực trong 5 ngày tới",
      time: "6 giờ trước",
      icon: AlertCircle,
      iconColor: "text-rose-500 dark:text-rose-400",
      iconBg: "bg-rose-50 dark:bg-rose-500/15",
    },
  ];

  return (
    <div className="flex flex-row h-screen w-screen overflow-hidden bg-background">
      <Sidebar />

      <main className="grow flex flex-col overflow-hidden">
        {/* Top Bar */}
        <Header title="Dashboard" />
        {/* Main Content Area */}
        <div className="grow overflow-y-auto">
          {/* Hiệu ứng Fade-in và trượt lên nhẹ cho toàn bộ khối nội dung */}
          <div className="px-6 lg:px-10 py-8 animate-in fade-in slide-in-from-bottom-4 duration-700 fill-mode-both">
            {/* Welcome Section */}
            <div className="mb-8">
              <h1 className="text-3xl font-bold text-foreground mb-1.5 tracking-tight">
                Xin chào, Quản trị viên
              </h1>
              <p className="text-muted-foreground">
                Dưới đây là tình hình số hóa hợp đồng hôm nay.
              </p>
            </div>

            {/* Stats Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5 mb-8">
              {stats.map((stat, i) => {
                const Icon = stat.icon;
                return (
                  <div
                    key={i}
                    // Animation Hover: Trượt lên trên 2px, đổi viền, tăng bóng đổ
                    className={`group bg-card rounded-2xl border border-border p-6 shadow-sm hover:shadow-md transition-all duration-300 hover:-translate-y-1 cursor-default ${stat.border}`}
                  >
                    <div className="flex items-start justify-between mb-4">
                      <div>
                        <p className="text-muted-foreground text-sm font-medium mb-1">
                          {stat.label}
                        </p>
                        <h3 className="text-3xl font-bold text-foreground tracking-tight">
                          {stat.value}
                        </h3>
                      </div>
                      {/* Icon Container với hiệu ứng scale khi hover card */}
                      <div
                        className={`w-12 h-12 rounded-xl flex items-center justify-center transition-transform duration-300 group-hover:scale-110 ${stat.bg}`}
                      >
                        <Icon className={`w-6 h-6 ${stat.color}`} />
                      </div>
                    </div>

                    <div className="flex items-center gap-1.5 mt-2">
                      {stat.isPositive ? (
                        <TrendingUp className="w-4 h-4 text-emerald-500" />
                      ) : (
                        <TrendingDown className="w-4 h-4 text-rose-500" />
                      )}
                      <span
                        className={`text-sm font-semibold ${
                          stat.isPositive ? "text-emerald-600" : "text-rose-600"
                        }`}
                      >
                        {stat.trend}
                      </span>
                      <span className="text-muted-foreground text-sm ml-1">
                        so với tháng trước
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Charts Section */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
              <div className="bg-card rounded-2xl border border-border p-6 shadow-sm hover:shadow-md transition-shadow duration-300">
                <h3 className="text-lg font-bold text-foreground mb-4 tracking-tight">
                  Lưu lượng Hợp đồng
                </h3>
                <div className="h-64 bg-muted/30 rounded-xl flex items-center justify-center border border-dashed border-border">
                  <p className="text-muted-foreground text-sm flex items-center gap-2">
                    <TrendingUp className="w-4 h-4" /> Khu vực nhúng Biểu đồ
                  </p>
                </div>
              </div>

              <div className="bg-card rounded-2xl border border-border p-6 shadow-sm hover:shadow-md transition-shadow duration-300">
                <h3 className="text-lg font-bold text-foreground mb-4 tracking-tight">
                  Tỷ lệ Trạng thái
                </h3>
                <div className="h-64 bg-muted/30 rounded-xl flex items-center justify-center border border-dashed border-border">
                  <p className="text-muted-foreground text-sm flex items-center gap-2">
                    <FileCheck2 className="w-4 h-4" /> Khu vực nhúng Biểu đồ
                    Tròn
                  </p>
                </div>
              </div>
            </div>

            {/* Recent Activity */}
            <div className="bg-card rounded-2xl border border-border p-6 shadow-sm">
              <h3 className="text-lg font-bold text-foreground mb-6 tracking-tight">
                Hoạt động gần đây
              </h3>
              <div className="space-y-6">
                {recentActivities.map((activity, i) => {
                  const ActivityIcon = activity.icon;
                  return (
                    <div
                      key={i}
                      // Hiệu ứng dịch nhẹ sang phải khi hover từng dòng
                      className="group flex items-start justify-between relative transition-transform duration-200 hover:translate-x-1"
                    >
                      <div className="flex gap-4 items-start">
                        <div
                          className={`mt-0.5 w-10 h-10 rounded-full flex items-center justify-center shrink-0 ${activity.iconBg}`}
                        >
                          <ActivityIcon
                            className={`w-5 h-5 ${activity.iconColor}`}
                          />
                        </div>
                        <div>
                          <p className="text-foreground font-semibold text-sm mb-0.5 group-hover:text-primary transition-colors">
                            {activity.action}
                          </p>
                          <p className="text-muted-foreground text-sm">
                            {activity.details}
                          </p>
                        </div>
                      </div>
                      <span className="text-xs font-medium text-muted-foreground whitespace-nowrap ml-4 bg-muted/50 px-2.5 py-1 rounded-full">
                        {activity.time}
                      </span>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
