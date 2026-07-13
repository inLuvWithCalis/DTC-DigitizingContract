"use client";

import { useState, useEffect } from "react";
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
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import {
  serviceTypeApi,
  ServiceTypeResponse,
  CreateServiceTypeRequest,
  UpdateServiceTypeRequest,
} from "@/services/catalog/service-types-api";

interface ServiceTypeFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  item?: ServiceTypeResponse | null;
  viewOnly?: boolean;
}

export function ServiceTypeFormModal({
  isOpen,
  onClose,
  onSuccess,
  item,
  viewOnly = false,
}: ServiceTypeFormModalProps) {
  const isEditMode = !!item && !viewOnly;

  const [serviceTypeName, setServiceTypeName] = useState("");
  const [langId, setLangId] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setErrors({});
      if (item) {
        setServiceTypeName(item.serviceTypeName || "");
        setLangId(
          item.langId !== undefined && item.langId !== null
            ? String(item.langId)
            : "",
        );
      } else {
        setServiceTypeName("");
        setLangId("");
      }
    }
  }, [isOpen, item]);

  const validate = () => {
    const newErrors: Record<string, string> = {};
    if (!serviceTypeName.trim()) {
      newErrors.serviceTypeName = "Vui lòng nhập tên loại dịch vụ";
    }
    if (langId && isNaN(Number(langId))) {
      newErrors.langId = "ID Ngôn ngữ phải là số";
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (viewOnly || !validate()) return;
    setIsSaving(true);
    try {
      const langIdVal = langId ? Number(langId) : null;

      if (isEditMode && item) {
        const payload: UpdateServiceTypeRequest = {
          serviceTypeName: serviceTypeName.trim(),
          langId: langIdVal,
        };
        await serviceTypeApi.update(item.serviceTypeId, payload);
        toast.success("Cập nhật loại dịch vụ thành công");
      } else {
        const payload: CreateServiceTypeRequest = {
          serviceTypeName: serviceTypeName.trim(),
          langId: langIdVal,
        };
        await serviceTypeApi.create(payload);
        toast.success("Thêm loại dịch vụ mới thành công");
      }
      onSuccess();
      onClose();
    } catch (error: any) {
      const message =
        error?.response?.data?.message ||
        error?.message ||
        (isEditMode
          ? "Không thể cập nhật loại dịch vụ"
          : "Không thể thêm loại dịch vụ");
      toast.error(message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {viewOnly
              ? "Chi tiết loại dịch vụ"
              : isEditMode
                ? "Chỉnh sửa loại dịch vụ"
                : "Thêm loại dịch vụ mới"}
          </DialogTitle>
          <DialogDescription>
            {viewOnly
              ? "Xem thông tin chi tiết của loại dịch vụ."
              : isEditMode
                ? "Cập nhật thông tin loại dịch vụ trong hệ thống."
                : "Điền thông tin để tạo loại dịch vụ mới."}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid gap-2">
            <Label htmlFor="serviceTypeName">
              Tên loại dịch vụ{" "}
              {!viewOnly && <span className="text-destructive">*</span>}
            </Label>
            <Input
              id="serviceTypeName"
              placeholder="Ví dụ: Tư vấn, Triển khai, Hosting..."
              value={serviceTypeName}
              onChange={(e) => setServiceTypeName(e.target.value)}
              disabled={viewOnly}
              aria-invalid={!!errors.serviceTypeName}
              maxLength={200}
            />
            {errors.serviceTypeName && (
              <p className="text-xs text-destructive">
                {errors.serviceTypeName}
              </p>
            )}
          </div>

          <div className="grid gap-2">
            <Label htmlFor="langId">ID Ngôn ngữ</Label>
            <Input
              id="langId"
              type="number"
              placeholder="Ví dụ: 1 (VN)"
              value={langId}
              onChange={(e) => setLangId(e.target.value)}
              disabled={viewOnly}
              aria-invalid={!!errors.langId}
            />
            {errors.langId && (
              <p className="text-xs text-destructive">{errors.langId}</p>
            )}
          </div>
        </div>

        <DialogFooter>
          {viewOnly ? (
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
