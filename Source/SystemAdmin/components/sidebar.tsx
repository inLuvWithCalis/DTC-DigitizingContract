"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  Building2,
  ChevronLeft,
  FileText,
  HeartPulse,
  LogOut,
  Menu,
  ScrollText,
  Settings,
  User,
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
import { useSidebar } from "./sidebar-context";
import { useAuthStore } from "@/hooks/use-auth-store";
import { authApi } from "@/services/auth-api";
import { cn } from "@/lib/utils";

const navItems = [
  {
    label: "Quản lý tenant",
    icon: Building2,
    href: "/tenants",
    disabled: false,
  },
  {
    label: "Giám sát hệ thống",
    icon: HeartPulse,
    href: "/system-health",
    disabled: true,
  },
  {
    label: "Nhật ký bảo mật",
    icon: ScrollText,
    href: "/audit-logs",
    disabled: false,
  },
  {
    label: "Cấu hình",
    icon: Settings,
    href: "/settings",
    disabled: true,
  },
];

export function Sidebar() {
  const { isExpanded, setIsExpanded } = useSidebar();
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const { user, logout } = useAuthStore();
  const router = useRouter();
  const pathname = usePathname();

  const handleLogout = async () => {
    logout();
    router.push("/");
    try {
      await authApi.logout();
    } catch {
      // Phiên phía client đã được xóa; API có thể không còn session hợp lệ.
    }
  };

  const renderNavigation = (mobile = false) => (
    <nav className="flex-1 space-y-1.5 overflow-y-auto px-3 py-4">
      {/* <p
        className={cn(
          "mb-2 px-3 text-[10px] font-semibold uppercase tracking-[0.16em] text-muted-foreground",
          !mobile && !isExpanded && "sr-only",
        )}
      >
        Quản trị hệ thống
      </p> */}

      {navItems.map((item) => {
        const Icon = item.icon;
        const isActive = pathname.startsWith(item.href);
        const showLabel = mobile || isExpanded;
        const commonClassName = cn(
          "group relative flex min-h-10 items-center rounded-xl text-sm transition-colors",
          showLabel ? "gap-3 px-3" : "justify-center px-0",
          item.disabled
            ? "cursor-not-allowed text-muted-foreground/55"
            : isActive
              ? "bg-primary/10 font-semibold text-primary"
              : "text-muted-foreground hover:bg-accent hover:text-foreground",
        );

        const content = (
          <>
            {isActive && !item.disabled && (
              <span className="absolute left-0 top-1/2 h-5 w-1 -translate-y-1/2 rounded-r-full bg-primary" />
            )}
            <Icon
              className={cn(
                "size-5 shrink-0",
                isActive && !item.disabled
                  ? "text-primary"
                  : "text-muted-foreground/75",
              )}
            />
            {showLabel && (
              <span className="min-w-0 flex-1 truncate">{item.label}</span>
            )}
            {showLabel && item.disabled && (
              <span className="rounded-full border bg-muted/50 px-2 py-0.5 text-[10px] font-medium text-muted-foreground">
                Sắp có
              </span>
            )}
          </>
        );

        if (item.disabled) {
          return (
            <div
              key={item.href}
              className={commonClassName}
              aria-disabled="true"
              title={showLabel ? undefined : `${item.label} — Sắp có`}
            >
              {content}
            </div>
          );
        }

        return (
          <Link
            key={item.href}
            href={item.href}
            className={commonClassName}
            title={showLabel ? undefined : item.label}
            onClick={() => setIsMobileOpen(false)}
          >
            {content}
          </Link>
        );
      })}
    </nav>
  );

  const renderAccountMenu = (mobile = false) => (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          className={cn(
            "flex w-full items-center rounded-xl p-2 text-left transition-colors hover:bg-accent focus:outline-none",
            mobile || isExpanded ? "gap-3" : "justify-center",
          )}
        >
          <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary text-sm font-bold text-primary-foreground">
            {user?.username?.[0]?.toUpperCase() ?? "S"}
          </div>
          {(mobile || isExpanded) && (
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold">
                {user?.fullName || "System Administrator"}
              </p>
              <p className="truncate text-xs text-muted-foreground">
                {user?.username || "sysadmin"}
              </p>
            </div>
          )}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align={mobile || isExpanded ? "end" : "start"}
        side={mobile ? "top" : "right"}
        className="w-60 rounded-xl"
      >
        <DropdownMenuItem asChild className="cursor-pointer py-2.5">
          <Link href="/dashboard/profile">
            <User className="mr-2 size-4 text-muted-foreground" />
            Hồ sơ cá nhân
          </Link>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          className="cursor-pointer py-2.5 text-destructive focus:bg-destructive/10 focus:text-destructive"
          onClick={handleLogout}
        >
          <LogOut className="mr-2 size-4" />
          Đăng xuất
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );

  return (
    <>
      <button
        onClick={() => setIsMobileOpen(true)}
        className="fixed left-4 top-3 z-40 rounded-xl border bg-card p-2 text-muted-foreground shadow-sm transition-colors hover:bg-accent lg:hidden"
        aria-label="Mở menu"
      >
        <Menu className="size-5" />
      </button>

      <aside
        className={cn(
          "relative z-20 hidden h-screen shrink-0 flex-col border-r bg-card transition-[width] duration-300 lg:flex",
          isExpanded ? "w-64" : "w-20",
        )}
      >
        <div
          className={cn(
            "flex h-16 items-center border-b px-4",
            isExpanded ? "justify-between" : "justify-center",
          )}
        >
          {isExpanded && (
            <Link href="/tenants" className="flex min-w-0 items-center gap-2.5">
              <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-sm shadow-primary/20">
                <FileText className="size-4" />
              </div>
              <div className="min-w-0">
                <p className="truncate text-sm font-bold">eContract Hub</p>
                <p className="truncate text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                  System Admin
                </p>
              </div>
            </Link>
          )}

          <Button
            variant="ghost"
            size="icon"
            onClick={() => setIsExpanded(!isExpanded)}
            className="size-8 shrink-0 rounded-lg text-muted-foreground"
            aria-label={isExpanded ? "Thu gọn menu" : "Mở rộng menu"}
          >
            <ChevronLeft
              className={cn(
                "size-5 transition-transform",
                !isExpanded && "rotate-180",
              )}
            />
          </Button>
        </div>

        {renderNavigation()}

        <div className="border-t p-3">{renderAccountMenu()}</div>
      </aside>

      {isMobileOpen && (
        <>
          <button
            className="fixed inset-0 z-50 bg-black/45 backdrop-blur-sm lg:hidden"
            onClick={() => setIsMobileOpen(false)}
            aria-label="Đóng menu"
          />
          <aside className="fixed inset-y-0 left-0 z-50 flex w-[290px] flex-col bg-card shadow-2xl lg:hidden">
            <div className="flex h-16 items-center justify-between border-b px-4">
              <Link
                href="/tenants"
                className="flex min-w-0 items-center gap-2.5"
              >
                <div className="flex size-9 items-center justify-center rounded-xl bg-primary text-primary-foreground">
                  <FileText className="size-4" />
                </div>
                <div>
                  <p className="text-sm font-bold">eContract Hub</p>
                  <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                    System Admin
                  </p>
                </div>
              </Link>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setIsMobileOpen(false)}
                aria-label="Đóng menu"
              >
                <X className="size-5" />
              </Button>
            </div>

            {renderNavigation(true)}

            <div className="border-t p-3">{renderAccountMenu(true)}</div>
          </aside>
        </>
      )}
    </>
  );
}
