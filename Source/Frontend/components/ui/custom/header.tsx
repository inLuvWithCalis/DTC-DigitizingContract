"use client";

import { Bell, LogOut, Settings, User } from "lucide-react";
import { ThemeToggle } from "@/components/theme-toggle";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/hooks/use-auth-store";
import { authApi } from "@/services/auth-api";

export function Header({ title }: { title: string }) {
  const router = useRouter();
  const user = useAuthStore((state) => state.user);
  const { logout } = useAuthStore();

  const handleLogout = async () => {
    logout();
    router.push("/");
    try {
      await authApi.logout();
    } catch (error) {
      console.error("Lỗi khi gọi API logout:", error);
    }
  };

  return (
    <header className="bg-card/80 backdrop-blur-md border-b border-border sticky top-0 z-30 flex-shrink-0 transition-all">
      <div className="px-6 lg:px-8 h-16 flex items-center justify-between">
        <div className="hidden md:block">
          <h2 className="text-lg font-semibold text-foreground tracking-tight">
            {title}
          </h2>
        </div>
        <div className="flex items-center gap-3 ml-auto">
          <ThemeToggle />
          <button className="relative p-2 hover:bg-accent rounded-full transition-colors text-muted-foreground hover:text-foreground">
            <Bell className="w-5 h-5" />
            <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-rose-500 rounded-full border-2 border-card"></span>
          </button>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="default"
                size="icon"
                className="w-9 h-9 rounded-full bg-gradient-to-tr from-primary to-primary/80 hover:opacity-90 text-primary-foreground shadow-sm transition-transform hover:scale-105"
              >
                {user?.employeeFullName?.[0]?.toUpperCase() ?? ""}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="center"
              side="bottom"
              className="w-56 rounded-xl"
            >
              <DropdownMenuLabel className="font-normal px-2 py-2.5">
                <div className="flex flex-col space-y-1.5">
                  <p className="text-sm font-semibold leading-none text-foreground truncate">
                    {user?.employeeFullName || "Người dùng"}
                  </p>
                  <p className="text-xs leading-none text-muted-foreground truncate">
                    {user?.employeeEmail || "Chưa cập nhật email"}
                  </p>
                </div>
              </DropdownMenuLabel>

              <DropdownMenuSeparator />

              <DropdownMenuItem className="cursor-pointer py-2">
                <User className="mr-2 h-4 w-4 text-muted-foreground" />
                <span className="font-medium">Hồ sơ cá nhân</span>
              </DropdownMenuItem>
              <DropdownMenuItem className="cursor-pointer py-2">
                <Settings className="mr-2 h-4 w-4 text-muted-foreground" />
                <span className="font-medium">Đổi mật khẩu</span>
              </DropdownMenuItem>

              <DropdownMenuSeparator />

              <DropdownMenuItem
                className="cursor-pointer py-2 text-destructive focus:text-destructive dark:focus:bg-destructive/10 focus:bg-destructive/10"
                onClick={handleLogout}
              >
                <LogOut className="mr-2 h-4 w-4" />
                <span className="font-medium">Đăng xuất</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  );
}
