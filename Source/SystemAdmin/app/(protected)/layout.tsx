"use client";

import { useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { authApi } from "@/services/auth-api";
import { Spinner } from "@/components/ui/spinner";
import { useAuthStore } from "@/hooks/use-auth-store";
import { Sidebar } from "@/components/sidebar";

export default function ProtectedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const [isChecking, setIsChecking] = useState(true);
  const router = useRouter();
  const pathname = usePathname();
  const setUser = useAuthStore((state) => state.setUser);
  const user = useAuthStore((state) => state.user);

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const userData = await authApi.getMe();
        setUser(userData);
        if (
          userData.mustChangePassword &&
          pathname !== "/profile" &&
          pathname !== "/change-password"
        ) {
          router.replace("/change-password?required=1");
        }
      } catch {
        setUser(null);
        router.replace("/?error=session_expired");
      } finally {
        setIsChecking(false);
      }
    };

    checkAuth();
  }, [pathname, router, setUser]);

  if (isChecking) {
    return (
      <div className="h-screen w-screen flex flex-col items-center justify-center bg-background">
        <Spinner text="Đang xác thực hệ thống..." />
      </div>
    );
  }

  if (
    user?.mustChangePassword &&
    pathname !== "/profile" &&
    pathname !== "/change-password"
  ) {
    return (
      <div className="h-screen w-screen flex flex-col items-center justify-center bg-background">
        <Spinner text="Đang chuyển tới màn đổi mật khẩu..." />
      </div>
    );
  }

  return (
    <div className="flex flex-row h-screen w-screen overflow-hidden bg-background">
      <Sidebar />
      <main className="relative z-10 flex min-w-0 grow flex-col overflow-hidden">
        {children}
      </main>
    </div>
  );
}
