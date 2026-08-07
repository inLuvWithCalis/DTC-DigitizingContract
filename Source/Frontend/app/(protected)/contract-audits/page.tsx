"use client";

import { ScrollText, ShieldAlert } from "lucide-react";

import { ContractAuditLog } from "@/components/contracts/contract-audit-log";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Header } from "@/components/ui/custom/header";
import { useAuthStore } from "@/hooks/use-auth-store";
import { EmployeeType } from "@/services/employees-api";

export default function ContractAuditsPage() {
  const user = useAuthStore((state) => state.user);
  const canViewTenantAudits =
    user?.employeeType === EmployeeType.Manager ||
    user?.employeeType === EmployeeType.AdminOfficer;

  return (
    <>
      <Header title="Nhật ký hợp đồng" />
      <div className="grow overflow-y-auto p-3 sm:p-6 lg:p-10">
        <div className="mx-auto space-y-6">
          <div className="flex items-start gap-3">
            <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <ScrollText className="size-5" />
            </span>
            <div>
              <h1 className="text-2xl font-bold tracking-tight">
                Nhật ký hợp đồng
              </h1>
              <p className="mt-1 text-sm text-muted-foreground">
                Tra cứu hoạt động hợp đồng theo người thực hiện, hành động, kết
                quả và thời gian.
              </p>
            </div>
          </div>

          {canViewTenantAudits ? (
            <ContractAuditLog mode="tenant" />
          ) : (
            <Alert variant="destructive">
              <ShieldAlert />
              <AlertTitle>Không có quyền truy cập</AlertTitle>
              <AlertDescription>
                Chỉ Manager hoặc Admin Officer có thể xem nhật ký hợp đồng toàn
                đơn vị. Bạn vẫn có thể xem lịch sử trong từng hợp đồng mình phụ
                trách.
              </AlertDescription>
            </Alert>
          )}
        </div>
      </div>
    </>
  );
}
