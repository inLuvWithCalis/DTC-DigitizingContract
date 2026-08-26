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
import { Textarea } from "@/components/ui/textarea";
import { Loader2 } from "lucide-react";
import { toast } from "@/components/ui/sonner";
import {
  customerApi,
  CustomerResponse,
  CreateCustomerRequest,
  UpdateCustomerRequest,
} from "@/services/customers-api";

interface CustomerFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (createdCustomer?: CustomerResponse) => void;
  item?: CustomerResponse | null;
  viewOnly?: boolean;
}

export function CustomerFormModal({
  isOpen,
  onClose,
  onSuccess,
  item,
  viewOnly = false,
}: CustomerFormModalProps) {
  const isEditMode = !!item && !viewOnly;

  const [customerCode, setCustomerCode] = useState("");
  const [customerFullName, setCustomerFullName] = useState("");
  const [customerCompany, setCustomerCompany] = useState("");
  const [customerEmail, setCustomerEmail] = useState("");
  const [customerMobile, setCustomerMobile] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [customerTaxCode, setCustomerTaxCode] = useState("");
  const [customerRepresentativeName, setCustomerRepresentativeName] =
    useState("");
  const [customerRepresentativeTitle, setCustomerRepresentativeTitle] =
    useState("");
  const [customerAddress, setCustomerAddress] = useState("");
  const [customerCity, setCustomerCity] = useState("");
  const [customerCountry, setCustomerCountry] = useState("");
  const [customerWebsite, setCustomerWebsite] = useState("");
  const [customerNotes, setCustomerNotes] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setErrors({});
      if (item) {
        setCustomerCode(item.customerCode || "");
        setCustomerFullName(item.customerFullName || "");
        setCustomerCompany(item.customerCompany || "");
        setCustomerEmail(item.customerEmail || "");
        setCustomerMobile(item.customerMobile || "");
        setCustomerPhone(item.customerPhone || "");
        setCustomerTaxCode(item.customerTaxCode || "");
        setCustomerRepresentativeName(item.customerRepresentativeName || "");
        setCustomerRepresentativeTitle(item.customerRepresentativeTitle || "");
        setCustomerAddress(item.customerAddress || "");
        setCustomerCity(item.customerCity || "");
        setCustomerCountry(item.customerCountry || "");
        setCustomerWebsite(item.customerWebsite || "");
        setCustomerNotes(item.customerNotes || "");
      } else {
        setCustomerCode("");
        setCustomerFullName("");
        setCustomerCompany("");
        setCustomerEmail("");
        setCustomerMobile("");
        setCustomerPhone("");
        setCustomerTaxCode("");
        setCustomerRepresentativeName("");
        setCustomerRepresentativeTitle("");
        setCustomerAddress("");
        setCustomerCity("");
        setCustomerCountry("");
        setCustomerWebsite("");
        setCustomerNotes("");
      }
    }
  }, [isOpen, item]);

  const validate = () => {
    const newErrors: Record<string, string> = {};
    if (!customerFullName.trim()) {
      newErrors.customerFullName = "Vui lòng nhập tên khách hàng / đối tác";
    }
    if (!customerEmail.trim()) {
      newErrors.customerEmail = "Vui lòng nhập email";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(customerEmail.trim())) {
      newErrors.customerEmail = "Email không hợp lệ";
    }
    if (!customerMobile.trim()) {
      newErrors.customerMobile = "Vui lòng nhập số di động";
    } else if (!/^[0-9+\-\s().]{8,15}$/.test(customerMobile.trim())) {
      newErrors.customerMobile = "Số di động không hợp lệ";
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (viewOnly || !validate()) return;
    setIsSaving(true);
    try {
      let createdCustomer: CustomerResponse | undefined;

      if (isEditMode && item) {
        const payload: UpdateCustomerRequest = {
          customerCode: customerCode.trim() || null,
          customerFullName: customerFullName.trim(),
          customerCompany: customerCompany.trim() || null,
          customerEmail: customerEmail.trim() || null,
          customerMobile: customerMobile.trim() || null,
          customerPhone: customerPhone.trim() || null,
          customerTaxCode: customerTaxCode.trim() || null,
          customerRepresentativeName:
            customerRepresentativeName.trim() || null,
          customerRepresentativeTitle:
            customerRepresentativeTitle.trim() || null,
          customerAddress: customerAddress.trim() || null,
          customerCity: customerCity.trim() || null,
          customerCountry: customerCountry.trim() || null,
          customerWebsite: customerWebsite.trim() || null,
          customerNotes: customerNotes.trim() || null,
        };
        await customerApi.update(item.customerId, payload);
        toast.success("Cập nhật thông tin khách hàng thành công");
      } else {
        const payload: CreateCustomerRequest = {
          customerCode: customerCode.trim() || null,
          customerFullName: customerFullName.trim(),
          customerCompany: customerCompany.trim() || null,
          customerEmail: customerEmail.trim() || null,
          customerMobile: customerMobile.trim() || null,
          customerPhone: customerPhone.trim() || null,
          customerTaxCode: customerTaxCode.trim() || null,
          customerRepresentativeName:
            customerRepresentativeName.trim() || null,
          customerRepresentativeTitle:
            customerRepresentativeTitle.trim() || null,
          customerAddress: customerAddress.trim() || null,
          customerCity: customerCity.trim() || null,
          customerCountry: customerCountry.trim() || null,
          customerWebsite: customerWebsite.trim() || null,
          customerNotes: customerNotes.trim() || null,
        };
        createdCustomer = await customerApi.create(payload);
        toast.success("Thêm khách hàng mới thành công");
      }
      onSuccess(createdCustomer);
      onClose();
    } catch (error: any) {
      const message =
        error?.response?.data?.message ||
        error?.message ||
        (isEditMode
          ? "Không thể cập nhật thông tin khách hàng"
          : "Không thể thêm khách hàng mới");
      toast.error(message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {viewOnly
              ? "Chi tiết khách hàng"
              : isEditMode
                ? "Chỉnh sửa khách hàng"
                : "Thêm khách hàng mới"}
          </DialogTitle>
          <DialogDescription>
            {viewOnly
              ? "Xem thông tin chi tiết của khách hàng / đối tác."
              : isEditMode
                ? "Cập nhật thông tin khách hàng trong hệ thống CRM."
                : "Điền thông tin để tạo hồ sơ khách hàng mới."}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="customerCode">Mã khách hàng</Label>
              <Input
                id="customerCode"
                placeholder="CUST-001 (Tùy chọn)"
                value={customerCode}
                onChange={(e) => setCustomerCode(e.target.value)}
                disabled={viewOnly}
                maxLength={30}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerTaxCode">Mã số thuế</Label>
              <Input
                id="customerTaxCode"
                placeholder="Ví dụ: 0101234567"
                value={customerTaxCode}
                onChange={(e) => setCustomerTaxCode(e.target.value)}
                disabled={viewOnly}
                maxLength={30}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="customerFullName">
                Tên khách hàng / Người đại diện{" "}
                {!viewOnly && <span className="text-destructive">*</span>}
              </Label>
              <Input
                id="customerFullName"
                placeholder="Nhập họ tên khách hàng..."
                value={customerFullName}
                onChange={(e) => setCustomerFullName(e.target.value)}
                disabled={viewOnly}
                aria-invalid={!!errors.customerFullName}
                maxLength={100}
              />
              {errors.customerFullName && (
                <p className="text-xs text-destructive">
                  {errors.customerFullName}
                </p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerCompany">Tên công ty / Tổ chức</Label>
              <Input
                id="customerCompany"
                placeholder="Nhập tên doanh nghiệp..."
                value={customerCompany}
                onChange={(e) => setCustomerCompany(e.target.value)}
                disabled={viewOnly}
                maxLength={1000}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="customerEmail">
                Email {!viewOnly && <span className="text-destructive">*</span>}
              </Label>
              <Input
                id="customerEmail"
                type="email"
                placeholder="email@company.com"
                value={customerEmail}
                onChange={(e) => setCustomerEmail(e.target.value)}
                disabled={viewOnly}
                aria-invalid={!!errors.customerEmail}
                maxLength={50}
              />
              {errors.customerEmail && (
                <p className="text-xs text-destructive">
                  {errors.customerEmail}
                </p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerMobile">
                Số di động{" "}
                {!viewOnly && <span className="text-destructive">*</span>}
              </Label>
              <Input
                id="customerMobile"
                placeholder="0912345678"
                value={customerMobile}
                onChange={(e) => setCustomerMobile(e.target.value)}
                disabled={viewOnly}
                aria-invalid={!!errors.customerMobile}
                maxLength={15}
              />
              {errors.customerMobile && (
                <p className="text-xs text-destructive">
                  {errors.customerMobile}
                </p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerPhone">Điện thoại cố định</Label>
              <Input
                id="customerPhone"
                placeholder="0243123456"
                value={customerPhone}
                onChange={(e) => setCustomerPhone(e.target.value)}
                disabled={viewOnly}
                maxLength={15}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="customerRepresentativeName">
                Người đại diện pháp luật
              </Label>
              <Input
                id="customerRepresentativeName"
                placeholder="Họ và tên người đại diện"
                value={customerRepresentativeName}
                onChange={(event) =>
                  setCustomerRepresentativeName(event.target.value)
                }
                disabled={viewOnly}
                maxLength={200}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerRepresentativeTitle">
                Chức danh người đại diện
              </Label>
              <Input
                id="customerRepresentativeTitle"
                placeholder="Ví dụ: Tổng giám đốc"
                value={customerRepresentativeTitle}
                onChange={(event) =>
                  setCustomerRepresentativeTitle(event.target.value)
                }
                disabled={viewOnly}
                maxLength={200}
              />
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="customerAddress">Địa chỉ</Label>
            <Input
              id="customerAddress"
              placeholder="Số nhà, đường phố, quận/huyện..."
              value={customerAddress}
              onChange={(e) => setCustomerAddress(e.target.value)}
              disabled={viewOnly}
              maxLength={2000}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="customerCity">Tỉnh / Thành phố</Label>
              <Input
                id="customerCity"
                placeholder="Hà Nội / TP.HCM..."
                value={customerCity}
                onChange={(e) => setCustomerCity(e.target.value)}
                disabled={viewOnly}
                maxLength={1000}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerCountry">Quốc gia</Label>
              <Input
                id="customerCountry"
                placeholder="Việt Nam"
                value={customerCountry}
                onChange={(e) => setCustomerCountry(e.target.value)}
                disabled={viewOnly}
                maxLength={200}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerWebsite">Website</Label>
              <Input
                id="customerWebsite"
                placeholder="https://company.com"
                value={customerWebsite}
                onChange={(e) => setCustomerWebsite(e.target.value)}
                disabled={viewOnly}
                maxLength={500}
              />
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="customerNotes">Ghi chú thêm</Label>
            <Textarea
              id="customerNotes"
              placeholder="Ghi chú nội bộ về khách hàng..."
              value={customerNotes}
              onChange={(e) => setCustomerNotes(e.target.value)}
              disabled={viewOnly}
              rows={3}
              maxLength={2000}
            />
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
