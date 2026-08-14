"use client";

import { useState, useEffect, useRef } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Loader2 } from "lucide-react";
import { toast } from "@/components/ui/sonner";

import {
  employeeApi,
  EmployeeResponse,
  EmployeeType,
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
  getEmployeeTypeLabel,
} from "@/services/employees-api";
import { departmentApi, DepartmentResponse } from "@/services/departments-api";
import { getApiErrorMessage, isStaleRowVersion } from "@/lib/api-error";

interface EmployeeFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  employee?: EmployeeResponse | null;
  viewOnly?: boolean;
}

const EMPLOYEE_TYPE_OPTIONS = Object.values(EmployeeType)
  .filter((v) => typeof v === "number" && v !== EmployeeType.Manager)
  .map((v) => ({
    label: getEmployeeTypeLabel(v as EmployeeType),
    value: String(v),
  }));

export function EmployeeFormModal({
  isOpen,
  onClose,
  onSuccess,
  employee,
  viewOnly = false,
}: EmployeeFormModalProps) {
  const modeCache = useRef({
    isCreateMode: !employee,
    isEditMode: !!employee && !viewOnly,
    isViewMode: !!employee && viewOnly,
  });

  if (isOpen) {
    modeCache.current = {
      isCreateMode: !employee,
      isEditMode: !!employee && !viewOnly,
      isViewMode: !!employee && viewOnly,
    };
  }

  const { isCreateMode, isEditMode, isViewMode } = modeCache.current;
  const [employeeCode, setEmployeeCode] = useState("");
  const [employeeAccount, setEmployeeAccount] = useState("");
  const [employeePassword, setEmployeePassword] = useState("");
  const [employeeFullName, setEmployeeFullName] = useState("");
  const [employeeMobile, setEmployeeMobile] = useState("");
  const [employeeEmail, setEmployeeEmail] = useState("");
  const [departmentId, setDepartmentId] = useState<string>("");
  const [employeeType, setEmployeeType] = useState<string>("");

  const [isSaving, setIsSaving] = useState(false);
  const [departments, setDepartments] = useState<DepartmentResponse[]>([]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isOpen) {
      departmentApi
        .getAll()
        .then((data) => {
          const list = Array.isArray(data) ? data : [];
          setDepartments(list);
        })
        .catch(() => setDepartments([]));
    }
  }, [isOpen]);

  useEffect(() => {
    if (isOpen && employee) {
      setEmployeeCode(employee.employeeCode || "");
      setEmployeeAccount(employee.employeeAccount || "");
      setEmployeePassword(""); // Không bao giờ load password về form
      setEmployeeFullName(employee.employeeFullName || "");
      setEmployeeMobile(employee.employeeMobile || "");
      setEmployeeEmail(employee.employeeEmail || "");
      setDepartmentId(
        employee.departmentId ? String(employee.departmentId) : "",
      );
      setEmployeeType(
        employee.employeeType ? String(employee.employeeType) : "",
      );
      setErrors({});
    } else if (isOpen && isCreateMode) {
      setEmployeeCode("");
      setEmployeeAccount("");
      setEmployeePassword("");
      setEmployeeFullName("");
      setEmployeeMobile("");
      setEmployeeEmail("");
      setDepartmentId("");
      setEmployeeType("");
      setErrors({});
    }
  }, [isOpen, employee, isCreateMode]);

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    // 1. CÁC TRƯỜNG LUÔN BẮT BUỘC (Cho cả Create và Edit)
    if (!employeeFullName.trim())
      newErrors.employeeFullName = "Họ và tên không được để trống";
    if (!employeeCode.trim())
      newErrors.employeeCode = "Mã nhân viên không được để trống";
    if (!employeeMobile.trim())
      newErrors.employeeMobile = "Số điện thoại không được để trống";

    if (!employeeEmail.trim()) {
      newErrors.employeeEmail = "Email không được để trống";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(employeeEmail)) {
      newErrors.employeeEmail = "Email không hợp lệ";
    }

    if (!departmentId) newErrors.departmentId = "Vui lòng chọn phòng ban";
    if (!employeeType) newErrors.employeeType = "Vui lòng chọn vai trò";

    // 2. CÁC TRƯỜNG CHỈ BẮT BUỘC KHI TẠO MỚI
    if (isCreateMode) {
      if (!employeeAccount.trim()) {
        newErrors.employeeAccount = "Tài khoản không được để trống";
      }
      if (!employeePassword.trim()) {
        newErrors.employeePassword = "Mật khẩu không được để trống";
      } else if (employeePassword.length < 6) {
        newErrors.employeePassword = "Mật khẩu tối thiểu 6 ký tự";
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    setIsSaving(true);
    try {
      if (isEditMode && employee) {
        // Cập nhật (Không gửi Account và Password lên)
        const updateData: UpdateEmployeeRequest = {
          employeeCode: employeeCode.trim() || null,
          employeeFullName: employeeFullName.trim(),
          employeeMobile: employeeMobile.trim() || null,
          employeeEmail: employeeEmail.trim() || null,
          departmentId: departmentId ? Number(departmentId) : null,
          employeeType: Number(employeeType),
          rowVersion: employee.rowVersion,
        };
        await employeeApi.update(employee.employeeId, updateData);
        toast.success("Cập nhật nhân viên thành công");
      } else {
        // Tạo mới
        const createData: CreateEmployeeRequest = {
          employeeCode: employeeCode.trim() || null,
          employeeAccount: employeeAccount.trim(),
          employeePassword: employeePassword,
          employeeFullName: employeeFullName.trim(),
          employeeMobile: employeeMobile.trim() || null,
          employeeEmail: employeeEmail.trim() || null,
          departmentId: departmentId ? Number(departmentId) : null,
          employeeType: Number(employeeType),
        };
        await employeeApi.create(createData);
        toast.success("Thêm nhân viên thành công");
      }
      onSuccess();
      onClose();
    } catch (error: any) {
      if (isStaleRowVersion(error)) {
        toast.error(
          "Dữ liệu nhân viên đã thay đổi. Danh sách sẽ được tải lại; vui lòng mở lại để sửa.",
        );
        onSuccess();
        onClose();
        return;
      }
      toast.error(
        getApiErrorMessage(
          error,
          isEditMode
            ? "Không thể cập nhật nhân viên"
            : "Không thể thêm nhân viên",
        ),
      );
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {isViewMode
              ? "Thông tin nhân viên"
              : isEditMode
                ? "Chỉnh sửa nhân viên"
                : "Thêm nhân viên mới"}
          </DialogTitle>
          <DialogDescription>
            {isViewMode
              ? "Xem chi tiết thông tin nhân viên."
              : isEditMode
                ? "Cập nhật thông tin nhân viên trong hệ thống."
                : "Điền thông tin để tạo tài khoản nhân viên mới."}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid gap-2">
            <Label htmlFor="employeeFullName">
              Họ và tên{" "}
              {!isViewMode && <span className="text-destructive">*</span>}
            </Label>
            <Input
              id="employeeFullName"
              placeholder="Nguyễn Văn A"
              value={employeeFullName}
              onChange={(e) => setEmployeeFullName(e.target.value)}
              maxLength={100}
              aria-invalid={!!errors.employeeFullName}
              disabled={isViewMode}
            />
            {errors.employeeFullName && (
              <p className="text-xs text-destructive">
                {errors.employeeFullName}
              </p>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="employeeAccount">
                Tài khoản{" "}
                {isCreateMode && <span className="text-destructive">*</span>}
              </Label>
              <Input
                id="employeeAccount"
                placeholder="nva"
                value={employeeAccount}
                onChange={(e) => setEmployeeAccount(e.target.value)}
                maxLength={50}
                aria-invalid={!!errors.employeeAccount}
                disabled={!isCreateMode} // View và Edit đều bị disable
              />
              {errors.employeeAccount && (
                <p className="text-xs text-destructive">
                  {errors.employeeAccount}
                </p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="employeeCode">
                Mã nhân viên{" "}
                {!isViewMode && <span className="text-destructive">*</span>}
              </Label>
              <Input
                id="employeeCode"
                placeholder="EMP-001"
                value={employeeCode}
                onChange={(e) => setEmployeeCode(e.target.value)}
                maxLength={30}
                disabled={isViewMode}
                aria-invalid={!!errors.employeeCode}
              />
              {errors.employeeCode && (
                <p className="text-xs text-destructive">
                  {errors.employeeCode}
                </p>
              )}
            </div>
          </div>

          {isCreateMode && (
            <div className="grid gap-2">
              <Label htmlFor="employeePassword">
                Mật khẩu <span className="text-destructive">*</span>
              </Label>
              <Input
                id="employeePassword"
                type="password"
                placeholder="••••••"
                value={employeePassword}
                onChange={(e) => setEmployeePassword(e.target.value)}
                maxLength={100}
                aria-invalid={!!errors.employeePassword}
              />
              {errors.employeePassword && (
                <p className="text-xs text-destructive">
                  {errors.employeePassword}
                </p>
              )}
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="employeeMobile">
                Số điện thoại{" "}
                {!isViewMode && <span className="text-destructive">*</span>}
              </Label>
              <Input
                id="employeeMobile"
                placeholder="0901 234 567"
                value={employeeMobile}
                onChange={(e) => setEmployeeMobile(e.target.value)}
                maxLength={20}
                disabled={isViewMode}
                aria-invalid={!!errors.employeeMobile}
              />
              {errors.employeeMobile && (
                <p className="text-xs text-destructive">
                  {errors.employeeMobile}
                </p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="employeeEmail">
                Email{" "}
                {!isViewMode && <span className="text-destructive">*</span>}
              </Label>
              <Input
                id="employeeEmail"
                type="email"
                placeholder="nva@company.com"
                value={employeeEmail}
                onChange={(e) => setEmployeeEmail(e.target.value)}
                maxLength={100}
                aria-invalid={!!errors.employeeEmail}
                disabled={isViewMode}
              />
              {errors.employeeEmail && (
                <p className="text-xs text-destructive">
                  {errors.employeeEmail}
                </p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label>
                Phòng ban{" "}
                {!isViewMode && <span className="text-destructive">*</span>}
              </Label>
              <Select
                value={departmentId}
                onValueChange={setDepartmentId}
                disabled={isViewMode}
              >
                <SelectTrigger
                  className={`w-full cursor-pointer ${
                    errors.departmentId
                      ? "border-destructive text-destructive focus:ring-destructive"
                      : ""
                  }`}
                >
                  <SelectValue placeholder="Chọn phòng ban" />
                </SelectTrigger>
                <SelectContent>
                  {departments.map((dept) => (
                    <SelectItem
                      key={dept.departmentId}
                      value={String(dept.departmentId)}
                    >
                      {dept.departmentName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.departmentId && (
                <p className="text-xs text-destructive">
                  {errors.departmentId}
                </p>
              )}
            </div>
            <div className="grid gap-2">
              <Label>
                Vai trò{" "}
                {!isViewMode && <span className="text-destructive">*</span>}
              </Label>
              <Select
                value={employeeType}
                onValueChange={setEmployeeType}
                disabled={isViewMode}
              >
                <SelectTrigger
                  className={`w-full cursor-pointer ${
                    errors.employeeType
                      ? "border-destructive text-destructive focus:ring-destructive"
                      : ""
                  }`}
                >
                  <SelectValue placeholder="Chọn vai trò" />
                </SelectTrigger>
                <SelectContent>
                  {EMPLOYEE_TYPE_OPTIONS.map((opt) => (
                    <SelectItem key={opt.value} value={opt.value}>
                      {opt.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.employeeType && (
                <p className="text-xs text-destructive">
                  {errors.employeeType}
                </p>
              )}
            </div>
          </div>
        </div>

        <DialogFooter>
          {isViewMode ? (
            <Button variant="outline" onClick={onClose}>
              Đóng
            </Button>
          ) : (
            <>
              <Button variant="outline" onClick={onClose} disabled={isSaving}>
                Hủy bỏ
              </Button>
              <Button onClick={handleSubmit} disabled={isSaving}>
                {isSaving && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                {isEditMode ? "Cập nhật" : "Tạo mới"}
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
