"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  BriefcaseBusiness,
  Building2,
  ChevronLeft,
  FileSignature,
  FileText,
  FolderTree,
  LayoutDashboard,
  LogOut,
  Menu,
  Package,
  ScrollText,
  Settings,
  Tags,
  User,
  Users,
  X,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useAuthStore } from "@/hooks/use-auth-store";
import { EmployeeType } from "@/services/employees-api";
import { useSidebar } from "./sidebar-context";

interface NavItem {
  label: string;
  icon: typeof LayoutDashboard;
  href: string;
  allowedEmployeeTypes?: EmployeeType[];
}

const navItems: NavItem[] = [
  { label: "Tổng quan", icon: LayoutDashboard, href: "/dashboard" },
  { label: "Hợp đồng", icon: FileText, href: "/contracts" },
  {
    label: "Nhật ký hợp đồng",
    icon: ScrollText,
    href: "/contract-audits",
    allowedEmployeeTypes: [EmployeeType.Manager, EmployeeType.AdminOfficer],
  },
  { label: "Báo giá", icon: FileSignature, href: "/quotations" },
  { label: "Khách hàng", icon: Users, href: "/customers" },
  { label: "Danh mục", icon: FolderTree, href: "/catalog/categories" },
  { label: "Sản phẩm", icon: Package, href: "/catalog/products" },
  { label: "Dịch vụ", icon: BriefcaseBusiness, href: "/catalog/services" },
  { label: "Loại dịch vụ", icon: Tags, href: "/catalog/service-types" },
  { label: "Nhân viên", icon: User, href: "/admin/employees" },
  { label: "Phòng ban", icon: Building2, href: "/admin/departments" },
  { label: "Cấu hình", icon: Settings, href: "/dashboard/settings" },
];

export function Sidebar() {
  const { isExpanded, setIsExpanded } = useSidebar();
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const { user, logout } = useAuthStore();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    setIsMobileOpen(false);
  }, [pathname]);

  const handleLogout = () => {
    logout();
    router.push("/");
  };

  const renderNavItems = () => (
    <nav className="flex-1 px-3 py-4 space-y-1.5 flex flex-col items-stretch overflow-y-auto">
      {navItems
        .filter(
          (item) =>
            !item.allowedEmployeeTypes ||
            (user?.employeeType !== null &&
              user?.employeeType !== undefined &&
              item.allowedEmployeeTypes.includes(user.employeeType)),
        )
        .map((item) => {
          const Icon = item.icon;
          const isActive =
            item.href === "/dashboard"
              ? pathname === "/dashboard"
              : pathname?.startsWith(item.href);

          return (
            <Link
              key={item.href}
              href={item.href}
              className={`flex items-center py-2.5 rounded-xl transition-all duration-200 group relative ${
                isExpanded ? "gap-3 px-3" : "justify-center px-0 lg:px-0 px-3"
              } ${
                isActive
                  ? "bg-primary/10 text-primary font-semibold shadow-sm"
                  : "text-muted-foreground hover:bg-accent hover:text-foreground"
              } ${isMobileOpen && "justify-start gap-5"}`}
            >
              {isActive && (
                <div className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-5 bg-primary rounded-r-full" />
              )}

              <Icon
                className={`h-5 w-5 flex-shrink-0 transition-colors ${
                  isActive
                    ? "text-primary"
                    : "text-muted-foreground/70 group-hover:text-foreground"
                }`}
              />
              <span
                className={`text-sm whitespace-nowrap transition-all duration-300 lg:block ${
                  isExpanded
                    ? "opacity-100 translate-x-0"
                    : "lg:opacity-0 lg:-translate-x-2 lg:hidden lg:w-0"
                } opacity-100 translate-x-0 block`}
              >
                {item.label}
              </span>
            </Link>
          );
        })}
    </nav>
  );

  return (
    <>
      <button
        onClick={() => setIsMobileOpen(true)}
        className="fixed top-3 left-4 z-40 p-2 lg:hidden hover:bg-accent rounded-xl transition-colors text-muted-foreground bg-card shadow-sm border border-border"
      >
        <Menu className="h-5 w-5" />
      </button>

      <aside
        className={`relative hidden lg:flex h-screen bg-card border-r border-border flex-col flex-shrink-0 transition-all duration-300 ease-in-out z-20 ${
          isExpanded ? "w-64" : "w-20"
        }`}
      >
        <div
          className={`h-16 border-b border-border p-4 flex items-center transition-all duration-300 ${
            isExpanded ? "justify-between" : "justify-center"
          }`}
        >
          {isExpanded && (
            <div className="flex items-center gap-2.5 overflow-hidden animate-in fade-in duration-300">
              <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center flex-shrink-0 shadow-sm shadow-primary/20">
                <FileText className="w-4 h-4 text-primary-foreground" />
              </div>
              <div className="min-w-0">
                <h1 className="text-sm font-bold leading-tight text-foreground truncate">
                  eContract Hub
                </h1>
                <p className="text-[11px] font-medium text-muted-foreground uppercase tracking-wider truncate">
                  Quản lý
                </p>
              </div>
            </div>
          )}
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setIsExpanded(!isExpanded)}
            className="text-muted-foreground hover:text-foreground hover:bg-accent h-8 w-8 rounded-lg flex-shrink-0"
          >
            <ChevronLeft
              className={`h-5 w-5 transition-transform duration-300 ${
                isExpanded ? "" : "rotate-180"
              }`}
            />
          </Button>
        </div>

        {renderNavItems()}
      </aside>

      {isMobileOpen && (
        <>
          <button
            type="button"
            aria-label="Đóng menu"
            className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 lg:hidden animate-in fade-in duration-200"
            onClick={() => setIsMobileOpen(false)}
          />

          <aside className="fixed left-0 top-0 h-screen w-[280px] bg-card z-50 flex flex-col animate-in slide-in-from-left duration-300 shadow-2xl lg:hidden">
            <div className="h-16 border-b border-border p-4 flex items-center justify-between shrink-0">
              <div className="flex items-center gap-2.5">
                <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center flex-shrink-0 shadow-sm shadow-primary/20">
                  <FileText className="w-4 h-4 text-primary-foreground" />
                </div>
                <div>
                  <h1 className="text-sm font-bold leading-tight text-foreground">
                    eContract Hub
                  </h1>
                  <p className="text-[11px] font-medium text-muted-foreground uppercase tracking-wider">
                    Quản lý
                  </p>
                </div>
              </div>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setIsMobileOpen(false)}
                className="text-muted-foreground hover:text-foreground hover:bg-accent h-8 w-8 rounded-lg flex-shrink-0"
              >
                <X className="h-5 w-5" />
              </Button>
            </div>

            {renderNavItems()}

            <div className="border-t border-border p-4 shrink-0">
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <button className="w-full flex items-center gap-3 px-2 py-2 rounded-xl hover:bg-accent transition-colors justify-start focus:outline-none">
                    <div className="w-10 h-10 rounded-full bg-gradient-to-tr from-primary to-primary/80 flex items-center justify-center text-sm font-bold flex-shrink-0 text-primary-foreground shadow-sm">
                      {user?.employeeFullName?.[0]?.toUpperCase() ?? "U"}
                    </div>
                    <div className="flex-1 text-left min-w-0">
                      <p className="text-sm font-semibold text-foreground truncate">
                        {user?.employeeFullName || "Người dùng"}
                      </p>
                      <p className="text-xs text-muted-foreground truncate">
                        {user?.employeeEmail || "Chưa cập nhật email"}
                      </p>
                    </div>
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent
                  align="end"
                  side="top"
                  className="w-64 rounded-xl z-60"
                >
                  <DropdownMenuItem asChild className="cursor-pointer py-2.5">
                    <Link href="/dashboard/profile">
                      <User className="mr-2 h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">Hồ sơ cá nhân</span>
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem className="cursor-pointer py-2.5">
                    <Settings className="mr-2 h-4 w-4 text-muted-foreground" />
                    <span className="font-medium">Đổi mật khẩu</span>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="cursor-pointer py-2.5 text-destructive focus:text-destructive dark:focus:bg-destructive/10 focus:bg-destructive/10"
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
    </>
  );
}
