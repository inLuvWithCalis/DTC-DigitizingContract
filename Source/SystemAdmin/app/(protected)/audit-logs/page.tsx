"use client";

import { ShieldCheck } from "lucide-react";

import { CentralSecurityAuditLog } from "@/components/security-audits/central-security-audit-log";
import { Header } from "@/components/ui/custom/header";

export default function CentralSecurityAuditsPage() {
  return (
    <>
      <Header title="Nhật ký bảo mật" />
      <div className="grow overflow-y-auto p-3 sm:p-6 lg:p-10">
        <div className="mx-auto space-y-6">
          <div className="flex items-start gap-3">
            <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <ShieldCheck className="size-5" />
            </span>
            <div>
              <h1 className="text-2xl font-bold tracking-tight">
                Nhật ký bảo mật trung tâm
              </h1>
              <p className="mt-1 text-sm text-muted-foreground">
                Theo dõi đăng nhập, từ chối truy cập, tạo tenant và thay đổi vai trò Manager trong toàn hệ thống.
              </p>
            </div>
          </div>

          <CentralSecurityAuditLog />
        </div>
      </div>
    </>
  );
}
