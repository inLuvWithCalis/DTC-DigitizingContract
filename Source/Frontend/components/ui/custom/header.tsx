"use client";

import { Bell, User } from "lucide-react";
import { ThemeToggle } from "@/components/theme-toggle";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useRouter } from "next/navigation";

export function Header() {
  const router = useRouter();

  const handleLogout = () => {
    router.push("/");
  };

  return (
    <header className="bg-card/80 backdrop-blur-md border-b border-border sticky top-0 z-30 flex-shrink-0 transition-all">
      <div className="px-6 lg:px-8 h-16 flex items-center justify-between">
        <div className="hidden md:block">
          <h2 className="text-lg font-semibold text-foreground tracking-tight">
            Tổng quan Hệ thống
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
                variant="ghost"
                size="icon"
                className="w-9 h-9 rounded-full bg-gradient-to-tr from-primary to-primary/80 hover:opacity-90 text-primary-foreground shadow-sm transition-transform hover:scale-105"
              >
                U
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48 rounded-xl">
              <DropdownMenuItem className="cursor-pointer py-2.5">
                <User className="mr-2 h-4 w-4 text-muted-foreground " />
                <span className="font-medium">Tài khoản</span>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="cursor-pointer py-2.5 text-rose-600 focus:text-rose-600 dark:focus:bg-rose-500/10 focus:bg-rose-50"
                onClick={handleLogout}
              >
                <span>Đăng xuất</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  );
}
