"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { authApi } from "@/services/auth";
import { Spinner } from "@/components/ui/spinner";

export default function ProtectedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const [isChecking, setIsChecking] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const userData = await authApi.getMe();

        // TODO: Lưu data vào Global State (Zustand, Redux...) ở đây
        // setGlobalUser(userData);
      } catch (error) {
        // Nếu Session chết hoặc chưa login, API trả 401
        // Interceptor của bạn (nếu có giữ logic redirect) hoặc dòng này sẽ đá về Login
        router.push("/");
      } finally {
        setIsChecking(false);
      }
    };

    checkAuth();
  }, [router]);

  if (isChecking) {
    return (
      <div className="h-screen w-screen flex flex-col items-center justify-center bg-background">
        <Spinner text="Đang xác thực hệ thống..." />
      </div>
    );
  }

  return <>{children}</>;
}
