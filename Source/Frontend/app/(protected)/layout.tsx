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
      <main className="grow flex flex-col overflow-hidden relative z-10">
        {children}
      </main>
    </div>
  );
}
