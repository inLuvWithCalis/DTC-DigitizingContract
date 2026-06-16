"use client";

import { useState, useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  X,
  LayoutDashboard,
  FileSignature,
  ShoppingCart,
  Users,
  Settings,
  LogOut,
  User,
  ChevronLeft,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useSidebar } from "./sidebar-context";

export function Sidebar() {
  const { isExpanded, setIsExpanded } = useSidebar();
  const [isMobileOpen, setIsMobileOpen] = useState(false);

  const router = useRouter();
  const pathname = usePathname(); // Lấy URL hiện tại để xử lý trạng thái Active

  // Đóng sidebar mobile khi chuyển trang
  useEffect(() => {
    setIsMobileOpen(false);
  }, [pathname]);

  const navItems = [
    { label: "Tổng quan", icon: LayoutDashboard, href: "/dashboard" },
    { label: "Hợp đồng Bán", icon: FileSignature, href: "/dashboard/sales" },
    { label: "Hợp đồng Mua", icon: ShoppingCart, href: "/dashboard/purchases" },
    { label: "Đối tác", icon: Users, href: "/dashboard/partners" },
    { label: "Cấu hình hệ thống", icon: Settings, href: "/dashboard/settings" },
  ];

  const handleLogout = () => {
    localStorage.removeItem("auth_token");
    router.push("/");
  };

  // Component render Menu Item để dùng chung cho cả Desktop và Mobile
  const renderNavItems = () => (
    <nav className="flex-1 px-3 py-4 space-y-1.5">
      {navItems.map((item, i) => {
        const Icon = item.icon;
        // Xử lý Active: Nếu là trang chủ (/dashboard) thì khớp hoàn toàn, còn lại dùng startsWith để active cả trang con
        const isActive =
          item.href === "/dashboard"
            ? pathname === "/dashboard"
            : pathname?.startsWith(item.href);

        return (
          <a
            key={i}
            href={item.href}
            className={`flex items-center gap-3 px-3 py-2.5 rounded-xl transition-all duration-200 group relative ${
              isActive
                ? "bg-indigo-50 text-indigo-700 font-semibold shadow-sm"
                : "text-slate-600 hover:bg-slate-100/80 hover:text-slate-900"
            }`}
          >
            {/* Thanh viền đánh dấu active bên trái */}
            {isActive && (
              <div className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-5 bg-indigo-600 rounded-r-full" />
            )}

            <Icon
              className={`h-5 w-5 flex-shrink-0 transition-colors ${
                isActive
                  ? "text-indigo-600"
                  : "text-slate-400 group-hover:text-slate-600"
              }`}
            />
            {/* Fix lỗi text bị bóp méo khi thu gọn bằng truncate và opacity */}
            <span
              className={`text-sm whitespace-nowrap transition-all duration-300 ${isExpanded ? "opacity-100 translate-x-0" : "opacity-0 -translate-x-2 hidden lg:block overflow-hidden w-0"}`}
            >
              {item.label}
            </span>
          </a>
        );
      })}
    </nav>
  );

  return (
    <>
      {/* Mobile Close Button */}
      {isMobileOpen && (
        <div className="fixed top-4 left-4 z-50 lg:hidden">
          <Button
            variant="outline"
            size="icon"
            onClick={() => setIsMobileOpen(false)}
            className="border-slate-200 bg-white hover:bg-slate-100 shadow-sm rounded-xl"
          >
            <X className="h-5 w-5 text-slate-600" />
          </Button>
        </div>
      )}

      {/* Sidebar Desktop - Collapsible with smooth width animation */}
      {/* Lưu ý: Tailwind chuẩn không có w-18, mình đổi sang w-20 (80px) để nó render chuẩn */}
      <aside
        className={`relative hidden lg:flex h-screen bg-white border-r border-slate-200 flex-col flex-shrink-0 transition-all duration-300 ease-in-out z-20 ${
          isExpanded ? "w-64" : "w-20"
        }`}
      >
        <div
          className={`h-16 border-b border-slate-200 p-4 flex items-center transition-all duration-300 ${
            isExpanded ? "justify-between" : "justify-center"
          }`}
        >
          {isExpanded && (
            <div className="flex items-center gap-2.5 overflow-hidden animate-in fade-in duration-300">
              <div className="w-8 h-8 bg-indigo-600 rounded-lg flex items-center justify-center flex-shrink-0 shadow-sm shadow-indigo-200">
                <svg
                  className="w-4 h-4 text-white"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.5"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
              </div>
              <div className="min-w-0">
                <h1 className="text-sm font-bold leading-tight text-slate-900 truncate">
                  eContract Hub
                </h1>
                <p className="text-[11px] font-medium text-slate-500 uppercase tracking-wider truncate">
                  Quản lý
                </p>
              </div>
            </div>
          )}
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setIsExpanded(!isExpanded)}
            className="text-slate-400 hover:text-slate-600 hover:bg-slate-100 h-8 w-8 rounded-lg flex-shrink-0"
          >
            <ChevronLeft
              className={`h-5 w-5 transition-transform duration-300 ${isExpanded ? "" : "rotate-180"}`}
            />
          </Button>
        </div>

        {/* Navigation Items (Desktop) */}
        {renderNavItems()}

        {/* User Section (Desktop) */}
        <div className="border-t border-slate-200 p-3">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                className={`w-full flex items-center gap-3 px-2 py-2 rounded-xl hover:bg-slate-50 transition-colors ${
                  isExpanded ? "justify-start" : "justify-center"
                }`}
              >
                <div className="w-9 h-9 rounded-full bg-gradient-to-tr from-indigo-600 to-indigo-500 flex items-center justify-center text-sm font-bold flex-shrink-0 text-white shadow-sm">
                  U
                </div>
                {isExpanded && (
                  <div className="flex-1 text-left min-w-0 animate-in fade-in">
                    <p className="text-sm font-semibold text-slate-900 truncate">
                      Quản trị viên
                    </p>
                    <p className="text-xs text-slate-500 truncate">
                      admin@econtract.vn
                    </p>
                  </div>
                )}
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="end"
              side="right"
              sideOffset={12}
              className="w-56 rounded-xl"
            >
              <DropdownMenuItem className="cursor-pointer py-2">
                <User className="mr-2 h-4 w-4 text-slate-500" />
                <span className="font-medium">Hồ sơ cá nhân</span>
              </DropdownMenuItem>
              <DropdownMenuItem className="cursor-pointer py-2">
                <Settings className="mr-2 h-4 w-4 text-slate-500" />
                <span className="font-medium">Đổi mật khẩu</span>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="cursor-pointer py-2 text-rose-600 focus:text-rose-600 focus:bg-rose-50"
                onClick={handleLogout}
              >
                <LogOut className="mr-2 h-4 w-4" />
                <span className="font-medium">Đăng xuất</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </aside>

      {/* Mobile Overlay Sidebar */}
      {isMobileOpen && (
        <>
          <div
            className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm z-30 lg:hidden animate-in fade-in"
            onClick={() => setIsMobileOpen(false)}
          />
          <aside className="fixed left-0 top-0 h-screen w-72 bg-white z-40 flex flex-col overflow-y-auto animate-in slide-in-from-left duration-300 shadow-2xl">
            <div className="h-16 border-b border-slate-200 p-4 flex items-center justify-between">
              <div className="flex items-center gap-2.5">
                <div className="w-8 h-8 bg-indigo-600 rounded-lg flex items-center justify-center flex-shrink-0 shadow-sm shadow-indigo-200">
                  <svg
                    className="w-4 h-4 text-white"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                    />
                  </svg>
                </div>
                <div>
                  <h1 className="text-sm font-bold leading-tight text-slate-900">
                    eContract Hub
                  </h1>
                  <p className="text-[11px] font-medium text-slate-500 uppercase tracking-wider">
                    Quản lý
                  </p>
                </div>
              </div>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setIsMobileOpen(false)}
                className="text-slate-400 hover:text-slate-600 hover:bg-slate-100 h-8 w-8 rounded-lg flex-shrink-0"
              >
                <X className="h-5 w-5" />
              </Button>
            </div>

            {/* Navigation Items (Mobile) */}
            {renderNavItems()}

            {/* User Section (Mobile) */}
            <div className="border-t border-slate-200 p-4">
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <button className="w-full flex items-center gap-3 px-2 py-2 rounded-xl hover:bg-slate-50 transition-colors justify-start">
                    <div className="w-10 h-10 rounded-full bg-gradient-to-tr from-indigo-600 to-indigo-500 flex items-center justify-center text-sm font-bold flex-shrink-0 text-white shadow-sm">
                      U
                    </div>
                    <div className="flex-1 text-left min-w-0">
                      <p className="text-sm font-semibold text-slate-900 truncate">
                        Quản trị viên
                      </p>
                      <p className="text-xs text-slate-500 truncate">
                        admin@econtract.vn
                      </p>
                    </div>
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent
                  align="end"
                  side="top"
                  className="w-64 rounded-xl"
                >
                  <DropdownMenuItem className="cursor-pointer py-2.5">
                    <User className="mr-2 h-4 w-4 text-slate-500" />
                    <span className="font-medium">Hồ sơ cá nhân</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem className="cursor-pointer py-2.5">
                    <Settings className="mr-2 h-4 w-4 text-slate-500" />
                    <span className="font-medium">Đổi mật khẩu</span>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="cursor-pointer py-2.5 text-rose-600 focus:text-rose-600 focus:bg-rose-50"
                    onClick={handleLogout}
                  >
                    <LogOut className="mr-2 h-4 w-4" />
                    <span className="font-medium">Đăng xuất</span>
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </aside>
        </>
      )}

      {/* Mobile Menu Trigger in Top Bar */}
      <button
        onClick={() => setIsMobileOpen(!isMobileOpen)}
        className="fixed top-3 left-4 z-20 p-2 lg:hidden hover:bg-slate-100 rounded-xl transition-colors text-slate-600 bg-white shadow-sm border border-slate-200"
      >
        <svg
          className="h-5 w-5"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M4 6h16M4 12h16M4 18h16"
          />
        </svg>
      </button>
    </>
  );
}
