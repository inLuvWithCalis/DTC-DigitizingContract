"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { authApi } from "@/services/auth-api";
import { Spinner } from "@/components/ui/spinner";
import { useAuthStore } from "@/hooks/use-auth-store";
import { Sidebar } from "@/components/sidebar";
import { Header } from "@/components/ui/custom/header";

export default function ProtectedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const [isChecking, setIsChecking] = useState(true);
  const router = useRouter();
  const setUser = useAuthStore((state) => state.setUser);

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const userData = await authApi.getMe();
        setUser(userData);
      } catch (error) {
        setUser(null);
        router.replace("/?error=session_expired");
      } finally {
        setIsChecking(false);
      }
    };

    checkAuth();
  }, [router, setUser]);

  if (isChecking) {
    return (
      <div className="h-screen w-screen flex flex-col items-center justify-center bg-background">
        <Spinner text="Đang xác thực hệ thống..." />
      </div>
    );
  }

  return (
    <div className="flex flex-row h-screen w-screen overflow-hidden bg-background">
      <Sidebar />
      <main className="relative z-10 flex min-w-0 grow flex-col overflow-hidden">
        <div className="flex min-w-0 grow flex-col overflow-hidden">{children}</div>
        <footer className="z-20 mt-auto flex shrink-0 items-center justify-between border-t border-border bg-card/50 px-3 py-2.5 text-xs text-muted-foreground backdrop-blur-sm sm:px-6">
          <div className="truncate">Copyright &copy; 2026 DTC.,Ltd. All rights reserved.</div>
          <div className="flex items-center gap-4">
            <span className="hidden sm:inline">Version 1.0.0</span>
          </div>
        </footer>
      </main>
    </div>
  );
}
