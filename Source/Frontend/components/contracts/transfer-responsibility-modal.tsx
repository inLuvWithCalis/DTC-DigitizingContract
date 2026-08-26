"use client";

import { useEffect, useState } from "react";
import { UserCog } from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  employeeApi,
  type EmployeeDirectoryResponse,
} from "@/services/employees-api";
import { contractApi, ContractDetailResponse } from "@/services/contract-api";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { showConfirmToast } from "@/components/ui/custom/confirm-toast";
import { useRouter } from "next/navigation";

interface TransferResponsibilityModalProps {
  isOpen: boolean;
  onClose: () => void;
  contractId: number;
  rowVersion: string;
  currentEmployeeId?: number | null;
  currentEmployeeName?: string | null;
  onSuccess: (updatedContract: ContractDetailResponse) => void;
}

export function TransferResponsibilityModal({
  isOpen,
  onClose,
  contractId,
  rowVersion,
  currentEmployeeId,
  currentEmployeeName,
  onSuccess,
}: TransferResponsibilityModalProps) {
  const router = useRouter();

  const [employees, setEmployees] = useState<EmployeeDirectoryResponse[]>([]);
  const [isLoadingEmployees, setIsLoadingEmployees] = useState(false);
  const [newEmployeeId, setNewEmployeeId] = useState<string>("");
  const [employeeSearch, setEmployeeSearch] = useState("");
  const [reason, setReason] = useState<string>(
    "Bàn giao công việc quản lý hợp đồng",
  );
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (isOpen) {
      const timeoutId = window.setTimeout(() => {
      const fetchEmployees = async () => {
        setIsLoadingEmployees(true);
        try {
          const res = await employeeApi.searchDirectory({
            page: 1,
            pageSize: 50,
            keyword: employeeSearch.trim() || undefined,
          });
          setEmployees(res.items);
        } catch (error) {
          console.error("Lỗi khi tải danh sách nhân viên:", error);
          toast.error("Không thể tải danh sách nhân viên.");
        } finally {
          setIsLoadingEmployees(false);
        }
      };
      void fetchEmployees();
      }, 350);
      return () => window.clearTimeout(timeoutId);
    }
  }, [employeeSearch, isOpen]);

  const handleTransfer = () => {
    if (!newEmployeeId) {
      toast.error("Vui lòng chọn người phụ trách mới.");
      return;
    }
    if (!reason.trim()) {
      toast.error("Vui lòng nhập lý do chuyển giao.");
      return;
    }

    const selectedEmpName =
      employees.find((e) => e.employeeId === Number(newEmployeeId))
        ?.employeeFullName || "Nhân viên đã chọn";

    showConfirmToast({
      title: "Xác nhận chuyển giao",
      description: `Bạn có chắc chắn muốn chuyển giao quyền phụ trách hợp đồng này cho "${selectedEmpName}"?`,
      confirmLabel: "Chuyển giao",
      cancelLabel: "Hủy",
      onConfirm: async () => {
        setIsSubmitting(true);
        try {
          await contractApi.transferResponsibility(contractId, {
            newResponsibleEmployeeId: Number(newEmployeeId),
            reason: reason.trim(),
            rowVersion: rowVersion,
          });

          const updatedContractData: any =
            await contractApi.getDetail(contractId);
          const updatedContract = updatedContractData.data
            ? updatedContractData.data
            : updatedContractData;

          onSuccess(updatedContract);
          onClose();
          router.push("/contracts");
        } catch (error: any) {
          console.error("Lỗi chuyển giao người phụ trách:", error);
          const data = error?.response?.data.message;
          toast.error(data);
        } finally {
          setIsSubmitting(false);
        }
      },
    });
  };

  const formContent = (
    <div className="space-y-4 py-2 text-left">
      {currentEmployeeName && (
        <div className="rounded-lg border bg-muted/40 p-3 text-sm text-foreground">
          <span className="text-muted-foreground">
            Người phụ trách hiện tại:{" "}
          </span>
          <strong className="text-foreground">{currentEmployeeName}</strong>
        </div>
      )}

      <div className="space-y-2 text-foreground">
        <Label>
          Người phụ trách mới <span className="text-red-500">*</span>
        </Label>
        <Input
          value={employeeSearch}
          onChange={(event) => setEmployeeSearch(event.target.value)}
          placeholder="Tìm theo tên, mã hoặc phòng ban..."
        />
        <Select value={newEmployeeId} onValueChange={setNewEmployeeId}>
          <SelectTrigger className="w-full">
            <SelectValue
              placeholder={
                isLoadingEmployees
                  ? "Đang tải danh sách..."
                  : "Chọn nhân viên tiếp nhận"
              }
            />
          </SelectTrigger>
          <SelectContent>
            {employees
              .filter((emp) => emp.employeeId !== currentEmployeeId)
              .map((emp) => (
                <SelectItem key={emp.employeeId} value={String(emp.employeeId)}>
                  {emp.employeeFullName || `Nhân viên #${emp.employeeId}`}
                  {emp.employeeCode ? ` • ${emp.employeeCode}` : ""}
                  {emp.departmentName ? ` • ${emp.departmentName}` : ""}
                </SelectItem>
              ))}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-2 text-foreground">
        <Label>
          Lý do chuyển giao <span className="text-red-500">*</span>
        </Label>
        <Textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Nhập lý do chuyển giao..."
          className="min-h-20"
        />
      </div>
    </div>
  );

  return (
    <ConfirmDialog
      isOpen={isOpen}
      onClose={onClose}
      onConfirm={handleTransfer}
      title="Chuyển giao người phụ trách"
      description={formContent}
      icon={<UserCog className="size-5 text-primary" />}
      confirmText="Xác nhận chuyển giao"
      cancelText="Hủy"
      isLoading={isSubmitting}
    />
  );
}
