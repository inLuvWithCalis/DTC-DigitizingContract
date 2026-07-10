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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import {
  productApi,
  ProductResponse,
  CreateProductRequest,
  UpdateProductRequest,
} from "@/services/catalog/products-api";

interface ProductFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  item?: ProductResponse | null;
  viewOnly?: boolean;
}

export function ProductFormModal({
  isOpen,
  onClose,
  onSuccess,
  item,
  viewOnly = false,
}: ProductFormModalProps) {
  const isEditMode = !!item && !viewOnly;

  const [productCode, setProductCode] = useState("");
  const [productName, setProductName] = useState("");
  const [productShortDesc, setProductShortDesc] = useState("");
  const [productPrice, setProductPrice] = useState("");
  const [productOrder, setProductOrder] = useState("1");
  const [productTags, setProductTags] = useState("");
  const [productDetails, setProductDetails] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setErrors({});
      if (item) {
        setProductCode(item.productCode || "");
        setProductName(item.productName || "");
        setProductShortDesc(item.productShortDesc || "");
        setProductPrice(
          item.productPrice !== undefined && item.productPrice !== null
            ? String(item.productPrice)
            : "",
        );
        setProductOrder(
          item.productOrder !== undefined && item.productOrder !== null
            ? String(item.productOrder)
            : "1",
        );
        setProductTags(item.productTags || "");
        setProductDetails(item.productDetails || "");
      } else {
        setProductCode("");
        setProductName("");
        setProductShortDesc("");
        setProductPrice("");
        setProductOrder("1");
        setProductTags("");
        setProductDetails("");
      }
    }
  }, [isOpen, item]);

  const validate = () => {
    const newErrors: Record<string, string> = {};
    if (productCode.trim() === "") {
      newErrors.productCode = "Vui lòng nhập mã sản phẩm";
    }
    if (!productName.trim()) {
      newErrors.productName = "Vui lòng nhập tên sản phẩm";
    }
    if (productPrice && isNaN(Number(productPrice))) {
      newErrors.productPrice = "Đơn giá phải là số";
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (viewOnly || !validate()) return;
    setIsSaving(true);
    try {
      const priceVal = productPrice ? Number(productPrice) : null;
      const orderVal = productOrder ? Number(productOrder) : 1;

      if (isEditMode && item) {
        const payload: UpdateProductRequest = {
          productCode: productCode || null,
          productName: productName.trim(),
          productShortDesc: productShortDesc || null,
          productPrice: priceVal,
          productOrder: orderVal,
          productTags: productTags || null,
          productDetails: productDetails || null,
        };
        await productApi.update(item.productId, payload);
        toast.success("Cập nhật sản phẩm thành công");
      } else {
        const payload: CreateProductRequest = {
          productCode: productCode || null,
          productName: productName.trim(),
          productShortDesc: productShortDesc || null,
          productPrice: priceVal,
          productOrder: orderVal,
          productTags: productTags || null,
          productDetails: productDetails || null,
        };
        await productApi.create(payload);
        toast.success("Thêm sản phẩm mới thành công");
      }
      onSuccess();
      onClose();
    } catch (error: any) {
      const message =
        error?.response?.data?.message ||
        error?.message ||
        (isEditMode
          ? "Không thể cập nhật sản phẩm"
          : "Không thể thêm sản phẩm");
      toast.error(message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {viewOnly
              ? "Chi tiết sản phẩm"
              : isEditMode
                ? "Chỉnh sửa sản phẩm"
                : "Thêm sản phẩm mới"}
          </DialogTitle>
          <DialogDescription>
            {viewOnly
              ? "Xem thông tin chi tiết của sản phẩm."
              : isEditMode
                ? "Cập nhật thông tin sản phẩm trong hệ thống."
                : "Điền thông tin để tạo sản phẩm mới."}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="productCode">
                Mã sản phẩm{" "}
                {!viewOnly && <span className="text-destructive">*</span>}
              </Label>
              <Input
                id="productCode"
                placeholder="PROD-001"
                value={productCode}
                onChange={(e) => setProductCode(e.target.value)}
                disabled={viewOnly}
                maxLength={20}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="productOrder">Thứ tự hiển thị</Label>
              <Input
                id="productOrder"
                type="number"
                placeholder="1"
                value={productOrder}
                onChange={(e) => setProductOrder(e.target.value)}
                disabled={viewOnly}
              />
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="productName">
              Tên sản phẩm{" "}
              {!viewOnly && <span className="text-destructive">*</span>}
            </Label>
            <Input
              id="productName"
              placeholder="Nhập tên sản phẩm..."
              value={productName}
              onChange={(e) => setProductName(e.target.value)}
              disabled={viewOnly}
              aria-invalid={!!errors.productName}
              maxLength={500}
            />
            {errors.productName && (
              <p className="text-xs text-destructive">{errors.productName}</p>
            )}
          </div>

          <div className="grid gap-2">
            <Label htmlFor="productPrice">Đơn giá (VNĐ)</Label>
            <Input
              id="productPrice"
              type="number"
              placeholder="Ví dụ: 1500000"
              value={productPrice}
              onChange={(e) => setProductPrice(e.target.value)}
              disabled={viewOnly}
              aria-invalid={!!errors.productPrice}
            />
            {errors.productPrice && (
              <p className="text-xs text-destructive">{errors.productPrice}</p>
            )}
          </div>

          <div className="grid gap-2">
            <Label htmlFor="productShortDesc">Mô tả ngắn</Label>
            <Textarea
              id="productShortDesc"
              placeholder="Mô tả tóm tắt sản phẩm..."
              value={productShortDesc}
              onChange={(e) => setProductShortDesc(e.target.value)}
              disabled={viewOnly}
              rows={3}
              maxLength={2000}
            />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="productTags">Từ khóa / Tags</Label>
            <Input
              id="productTags"
              placeholder="tag1, tag2..."
              value={productTags}
              onChange={(e) => setProductTags(e.target.value)}
              disabled={viewOnly}
              maxLength={500}
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
