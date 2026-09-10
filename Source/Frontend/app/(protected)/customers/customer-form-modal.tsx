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
import { CodeInput } from "@/components/ui/custom/code-input";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Building2,
  Globe,
  Loader2,
  Mail,
  MapPin,
  Phone,
  UserSquare2,
} from "lucide-react";
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
  const [customerFaxNumber, setCustomerFaxNumber] = useState("");
  const [customerTaxCode, setCustomerTaxCode] = useState("");
  const [customerRepresentativeName, setCustomerRepresentativeName] =
    useState("");
  const [customerRepresentativeTitle, setCustomerRepresentativeTitle] =
    useState("");
  const [customerContactPersonName, setCustomerContactPersonName] =
    useState("");
  const [customerContactPersonPhone, setCustomerContactPersonPhone] =
    useState("");
  const [customerContactPersonTitle, setCustomerContactPersonTitle] =
    useState("");
  const [customerBankAccountNumber, setCustomerBankAccountNumber] =
    useState("");
  const [customerBankName, setCustomerBankName] = useState("");
  const [customerAddress, setCustomerAddress] = useState("");
  const [customerCity, setCustomerCity] = useState("");
  const [customerCountry, setCustomerCountry] = useState("");
  const [customerWebsite, setCustomerWebsite] = useState("");
  const [customerNotes, setCustomerNotes] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (isOpen) {
      // The dialog keeps its form mounted, so each open intentionally starts
      // a fresh draft from the selected customer instead of stale local input.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setErrors({});
      if (item) {
        setCustomerCode(item.customerCode || "");
        setCustomerFullName(item.customerFullName || "");
        setCustomerCompany(item.customerCompany || "");
        setCustomerEmail(item.customerEmail || "");
        setCustomerMobile(item.customerMobile || "");
        setCustomerPhone(item.customerPhone || "");
        setCustomerFaxNumber(item.customerFaxNumber || "");
        setCustomerTaxCode(item.customerTaxCode || "");
        setCustomerRepresentativeName(item.customerRepresentativeName || "");
        setCustomerRepresentativeTitle(item.customerRepresentativeTitle || "");
        setCustomerContactPersonName(item.customerContactPersonName || "");
        setCustomerContactPersonPhone(item.customerContactPersonPhone || "");
        setCustomerContactPersonTitle(item.customerContactPersonTitle || "");
        setCustomerBankAccountNumber(item.customerBankAccountNumber || "");
        setCustomerBankName(item.customerBankName || "");
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
        setCustomerFaxNumber("");
        setCustomerTaxCode("");
        setCustomerRepresentativeName("");
        setCustomerRepresentativeTitle("");
        setCustomerContactPersonName("");
        setCustomerContactPersonPhone("");
        setCustomerContactPersonTitle("");
        setCustomerBankAccountNumber("");
        setCustomerBankName("");
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
          customerFaxNumber: customerFaxNumber.trim() || null,
          customerTaxCode: customerTaxCode.trim() || null,
          customerRepresentativeName:
            customerRepresentativeName.trim() || null,
          customerRepresentativeTitle:
            customerRepresentativeTitle.trim() || null,
          customerContactPersonName: customerContactPersonName.trim() || null,
          customerContactPersonPhone:
            customerContactPersonPhone.trim() || null,
          customerContactPersonTitle:
            customerContactPersonTitle.trim() || null,
          customerBankAccountNumber:
            customerBankAccountNumber.trim() || null,
          customerBankName: customerBankName.trim() || null,
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
          customerFaxNumber: customerFaxNumber.trim() || null,
          customerTaxCode: customerTaxCode.trim() || null,
          customerRepresentativeName:
            customerRepresentativeName.trim() || null,
          customerRepresentativeTitle:
            customerRepresentativeTitle.trim() || null,
          customerContactPersonName: customerContactPersonName.trim() || null,
          customerContactPersonPhone:
            customerContactPersonPhone.trim() || null,
          customerContactPersonTitle:
            customerContactPersonTitle.trim() || null,
          customerBankAccountNumber:
            customerBankAccountNumber.trim() || null,
          customerBankName: customerBankName.trim() || null,
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
    } catch (error: unknown) {
      const apiError = error as {
        response?: { data?: { message?: string } };
      };
      const message =
        apiError.response?.data?.message ||
        (error instanceof Error ? error.message : null) ||
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
      <DialogContent className="flex max-h-[90dvh] flex-col gap-0 overflow-hidden p-0 sm:max-w-3xl">
        <DialogHeader className="border-b border-border/50 px-6 py-5">
          <DialogTitle className="flex items-center gap-2 text-base font-semibold">
            <UserSquare2 className="size-5 text-primary" />
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

        <div className="grid gap-4 overflow-y-auto px-6 py-5 [&_label]:text-xs [&_label]:font-semibold [&_label]:uppercase [&_label]:text-muted-foreground [&_input]:h-9 [&_input:disabled]:cursor-default [&_input:disabled]:border-transparent [&_input:disabled]:bg-muted/40 [&_input:disabled]:font-medium [&_input:disabled]:text-foreground [&_input:disabled]:opacity-100 [&_input:disabled]:shadow-none [&_textarea:disabled]:cursor-default [&_textarea:disabled]:border-transparent [&_textarea:disabled]:bg-muted/40 [&_textarea:disabled]:font-medium [&_textarea:disabled]:text-foreground [&_textarea:disabled]:opacity-100 [&_textarea:disabled]:shadow-none">
          <div className="grid gap-2">
            <Label htmlFor="customerFullName">
              Tên khách hàng / Người liên hệ{" "}
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
            <div className="relative">
              <Building2 className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
              <Input
                id="customerCompany"
                className="pl-9"
                placeholder="Nhập tên doanh nghiệp..."
                value={customerCompany}
                onChange={(e) => setCustomerCompany(e.target.value)}
                disabled={viewOnly}
                maxLength={1000}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="customerCode">Mã KH</Label>
              <CodeInput
                id="customerCode"
                placeholder="CUST-001 (Tùy chọn)"
                value={customerCode}
                onValueChange={setCustomerCode}
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

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="customerBankAccountNumber">
                Số tài khoản ngân hàng
              </Label>
              <Input
                id="customerBankAccountNumber"
                placeholder="Nhập số tài khoản"
                value={customerBankAccountNumber}
                onChange={(event) =>
                  setCustomerBankAccountNumber(event.target.value)
                }
                disabled={viewOnly}
                maxLength={100}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerBankName">Tên ngân hàng</Label>
              <Input
                id="customerBankName"
                placeholder="Ví dụ: Ngân hàng TMCP Ngoại thương Việt Nam"
                value={customerBankName}
                onChange={(event) => setCustomerBankName(event.target.value)}
                disabled={viewOnly}
                maxLength={500}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div className="grid gap-2">
              <Label htmlFor="customerMobile">
                Số di động {!viewOnly && <span className="text-destructive">*</span>}
              </Label>
              <div className="relative">
                <Phone className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
                <Input
                  id="customerMobile"
                  className="pl-9"
                  placeholder="0912345678"
                  value={customerMobile}
                  onChange={(e) => setCustomerMobile(e.target.value)}
                  disabled={viewOnly}
                  aria-invalid={!!errors.customerMobile}
                  maxLength={15}
                />
              </div>
              {errors.customerMobile && (
                <p className="text-xs text-destructive">{errors.customerMobile}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerPhone">SĐT cố định</Label>
              <Input
                id="customerPhone"
                placeholder="0243123456"
                value={customerPhone}
                onChange={(e) => setCustomerPhone(e.target.value)}
                disabled={viewOnly}
                maxLength={15}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="customerFaxNumber">Fax</Label>
              <Input
                id="customerFaxNumber"
                placeholder="0243123457"
                value={customerFaxNumber}
                onChange={(event) => setCustomerFaxNumber(event.target.value)}
                disabled={viewOnly}
                maxLength={15}
              />
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="customerEmail">
              Email {!viewOnly && <span className="text-destructive">*</span>}
            </Label>
            <div className="relative">
              <Mail className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
              <Input
                id="customerEmail"
                type="email"
                className="pl-9"
                placeholder="email@company.com"
                value={customerEmail}
                onChange={(e) => setCustomerEmail(e.target.value)}
                disabled={viewOnly}
                aria-invalid={!!errors.customerEmail}
                maxLength={50}
              />
            </div>
            {errors.customerEmail && (
              <p className="text-xs text-destructive">{errors.customerEmail}</p>
            )}
          </div>

          <div className="grid gap-3 rounded-lg border bg-muted/20 p-4">
            <div>
              <p className="text-sm font-semibold">
                Người liên lạc / làm việc trực tiếp
              </p>
              <p className="text-xs text-muted-foreground">
                Đầu mối phối hợp công việc, không thay thế người đại diện pháp
                luật.
              </p>
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="grid gap-2 sm:col-span-2">
                <Label htmlFor="customerContactPersonName">Họ và tên</Label>
                <Input
                  id="customerContactPersonName"
                  placeholder="Họ và tên người liên hệ"
                  value={customerContactPersonName}
                  onChange={(event) =>
                    setCustomerContactPersonName(event.target.value)
                  }
                  disabled={viewOnly}
                  maxLength={200}
                />
              </div>
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="customerContactPersonTitle">Chức danh</Label>
                <Input
                  id="customerContactPersonTitle"
                  placeholder="Ví dụ: Trưởng phòng mua hàng"
                  value={customerContactPersonTitle}
                  onChange={(event) =>
                    setCustomerContactPersonTitle(event.target.value)
                  }
                  disabled={viewOnly}
                  maxLength={200}
                />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="customerContactPersonPhone">Số điện thoại</Label>
                <div className="relative">
                  <Phone className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
                  <Input
                    id="customerContactPersonPhone"
                    type="tel"
                    className="pl-9"
                    placeholder="0912345678"
                    value={customerContactPersonPhone}
                    onChange={(event) =>
                      setCustomerContactPersonPhone(event.target.value)
                    }
                    disabled={viewOnly}
                    maxLength={20}
                  />
                </div>
              </div>
            </div>
          </div>

          <div className="grid gap-3 rounded-lg border bg-muted/20 p-4">
            <div>
              <p className="text-sm font-semibold">Người đại diện pháp luật</p>
              <p className="text-xs text-muted-foreground">
                Đầu mối ký kết hợp đồng với công ty.
              </p>
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="customerRepresentativeName">Họ và tên</Label>
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
                <Label htmlFor="customerRepresentativeTitle">Chức danh</Label>
                <Input
                  id="customerRepresentativeTitle"
                  placeholder="Ví dụ: Giám đốc"
                  value={customerRepresentativeTitle}
                  onChange={(event) =>
                    setCustomerRepresentativeTitle(event.target.value)
                  }
                  disabled={viewOnly}
                  maxLength={200}
                />
              </div>
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="customerAddress">Địa chỉ</Label>
            <div className="relative">
              <MapPin className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
              <Input
                id="customerAddress"
                className="pl-9"
                placeholder="Số nhà, đường phố, quận/huyện..."
                value={customerAddress}
                onChange={(e) => setCustomerAddress(e.target.value)}
                disabled={viewOnly}
                maxLength={2000}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
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
          </div>

          <div className="grid gap-2">
            <Label htmlFor="customerWebsite">Website</Label>
            <div className="relative">
              <Globe className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
              <Input
                id="customerWebsite"
                className="pl-9"
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
              className="resize-none"
              placeholder="Ghi chú nội bộ về khách hàng..."
              value={customerNotes}
              onChange={(e) => setCustomerNotes(e.target.value)}
              disabled={viewOnly}
              rows={3}
              maxLength={2000}
            />
          </div>
        </div>

        <DialogFooter className="border-t border-border/50 bg-muted/20 px-6 py-4">
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
