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
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { useRouter } from "next/navigation";

export default function DashboardPage() {
  const router = useRouter();

  const handleLogout = () => {
    localStorage.removeItem("auth_token");
    router.push("/");
  };

  // Dữ liệu mẫu cho Thống kê Hợp đồng
  const stats = [
    {
      label: "Tổng Hợp đồng",
      value: "1,248",
      trend: "+12.5%",
      isPositive: true,
      icon: FileText,
      color: "text-indigo-600",
      bg: "bg-indigo-100",
      border: "group-hover:border-indigo-300",
    },
    {
      label: "Chờ Phê duyệt",
      value: "24",
      trend: "-2.4%",
      isPositive: true, // Giảm số lượng chờ là tốt
      icon: Clock,
      color: "text-amber-600",
      bg: "bg-amber-100",
      border: "group-hover:border-amber-300",
    },
    {
      label: "Sắp Hết hạn",
      value: "12",
      trend: "+4.1%",
      isPositive: false,
      icon: AlertCircle,
      color: "text-rose-600",
      bg: "bg-rose-100",
      border: "group-hover:border-rose-300",
    },
    {
      label: "Đã Thanh lý",
      value: "856",
      trend: "+8.2%",
      isPositive: true,
      icon: FileCheck2,
      color: "text-emerald-600",
      bg: "bg-emerald-100",
      border: "group-hover:border-emerald-300",
    },
  ];

  // Dữ liệu mẫu cho Hoạt động gần đây
  const recentActivities = [
    {
      action: "Tạo mới Hợp đồng Bán",
      details: "HĐ #HB-2024-001 - Công ty TNHH ABC",
      time: "2 giờ trước",
      icon: FilePlus,
      iconColor: "text-indigo-500",
      iconBg: "bg-indigo-50",
    },
    {
      action: "Phê duyệt Phụ lục",
      details: "Phụ lục #PL-01 cho HĐ #HB-2023-102",
      time: "4 giờ trước",
      icon: CheckCircle2,
      iconColor: "text-emerald-500",
      iconBg: "bg-emerald-50",
    },
    {
      action: "Cảnh báo Hết hạn",
      details: "Hợp đồng Mua #HM-092 sắp hết hiệu lực trong 5 ngày tới",
      time: "6 giờ trước",
      icon: AlertCircle,
      iconColor: "text-rose-500",
      iconBg: "bg-rose-50",
    },
  ];

  return (
    <div className="flex flex-row h-screen w-screen overflow-hidden bg-slate-50/50">
      <Sidebar />

      <main className="flex-grow flex flex-col overflow-hidden">
        {/* Top Bar */}
        <header className="bg-white/80 backdrop-blur-md border-b border-slate-200 sticky top-0 z-30 flex-shrink-0 transition-all">
          <div className="px-6 lg:px-8 h-16 flex items-center justify-between">
            <div className="hidden md:block">
              <h2 className="text-lg font-semibold text-slate-800 tracking-tight">
                Tổng quan Hệ thống
              </h2>
            </div>
            <div className="flex items-center gap-4 ml-auto">
              <button className="relative p-2 hover:bg-slate-100 rounded-full transition-colors text-slate-500 hover:text-slate-700">
                <Bell className="w-5 h-5" />
                {/* Chấm đỏ thông báo */}
                <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-rose-500 rounded-full border-2 border-white"></span>
              </button>

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="w-9 h-9 rounded-full bg-gradient-to-tr from-indigo-600 to-indigo-500 hover:opacity-90 text-white shadow-sm transition-transform hover:scale-105"
                  >
                    U
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48 rounded-xl">
                  <DropdownMenuItem className="cursor-pointer py-2.5">
                    <User className="mr-2 h-4 w-4 text-slate-500" />
                    <span className="font-medium text-slate-700">
                      Tài khoản
                    </span>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="cursor-pointer py-2.5 text-rose-600 focus:text-rose-600 focus:bg-rose-50"
                    onClick={handleLogout}
                  >
                    <span>Đăng xuất</span>
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </header>

        {/* Main Content Area */}
        <div className="flex-grow overflow-y-auto">
          {/* Hiệu ứng Fade-in và trượt lên nhẹ cho toàn bộ khối nội dung */}
          <div className="px-6 lg:px-10 py-8 animate-in fade-in slide-in-from-bottom-4 duration-700 fill-mode-both">
            {/* Welcome Section */}
            <div className="mb-8">
              <h1 className="text-3xl font-bold text-slate-900 mb-1.5 tracking-tight">
                Xin chào, Quản trị viên
              </h1>
              <p className="text-slate-500">
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
                    className={`group bg-white rounded-2xl border border-slate-200/60 p-6 shadow-sm hover:shadow-md transition-all duration-300 hover:-translate-y-1 cursor-default ${stat.border}`}
                  >
                    <div className="flex items-start justify-between mb-4">
                      <div>
                        <p className="text-slate-500 text-sm font-medium mb-1">
                          {stat.label}
                        </p>
                        <h3 className="text-3xl font-bold text-slate-800 tracking-tight">
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
                      <span className="text-slate-400 text-sm ml-1">
                        so với tháng trước
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Charts Section */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
              <div className="bg-white rounded-2xl border border-slate-200/60 p-6 shadow-sm hover:shadow-md transition-shadow duration-300">
                <h3 className="text-lg font-bold text-slate-800 mb-4 tracking-tight">
                  Lưu lượng Hợp đồng
                </h3>
                <div className="h-64 bg-slate-50/50 rounded-xl flex items-center justify-center border border-dashed border-slate-200">
                  <p className="text-slate-400 text-sm flex items-center gap-2">
                    <TrendingUp className="w-4 h-4" /> Khu vực nhúng Biểu đồ
                  </p>
                </div>
              </div>

              <div className="bg-white rounded-2xl border border-slate-200/60 p-6 shadow-sm hover:shadow-md transition-shadow duration-300">
                <h3 className="text-lg font-bold text-slate-800 mb-4 tracking-tight">
                  Tỷ lệ Trạng thái
                </h3>
                <div className="h-64 bg-slate-50/50 rounded-xl flex items-center justify-center border border-dashed border-slate-200">
                  <p className="text-slate-400 text-sm flex items-center gap-2">
                    <FileCheck2 className="w-4 h-4" /> Khu vực nhúng Biểu đồ
                    Tròn
                  </p>
                </div>
              </div>
            </div>

            {/* Recent Activity */}
            <div className="bg-white rounded-2xl border border-slate-200/60 p-6 shadow-sm">
              <h3 className="text-lg font-bold text-slate-800 mb-6 tracking-tight">
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
                          <p className="text-slate-800 font-semibold text-sm mb-0.5 group-hover:text-indigo-600 transition-colors">
                            {activity.action}
                          </p>
                          <p className="text-slate-500 text-sm">
                            {activity.details}
                          </p>
                        </div>
                      </div>
                      <span className="text-xs font-medium text-slate-400 whitespace-nowrap ml-4 bg-slate-50 px-2.5 py-1 rounded-full">
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
