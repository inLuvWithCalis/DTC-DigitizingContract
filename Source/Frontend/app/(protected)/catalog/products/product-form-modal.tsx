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
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Loader2, Package, FileText, Globe } from "lucide-react";
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
  onSuccess: (createdProduct?: ProductResponse) => void;
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

  const [activeTab, setActiveTab] = useState("basic");

  // Basic Info
  const [productCode, setProductCode] = useState("");
  const [productName, setProductName] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [productPrice, setProductPrice] = useState("");
  const [productOrder, setProductOrder] = useState("1");
  const [langId, setLangId] = useState("");
  const [productTags, setProductTags] = useState("");
  const [productShortDesc, setProductShortDesc] = useState("");

  // Details & Features
  const [productDetails, setProductDetails] = useState("");
  const [productFeatures, setProductFeatures] = useState("");
  const [productBenefit, setProductBenefit] = useState("");
  const [productSmallImage, setProductSmallImage] = useState("");
  const [productLargeImage, setProductLargeImage] = useState("");

  // SEO & Metadata
  const [titleBrowser, setTitleBrowser] = useState("");
  const [metaKeyword, setMetaKeyword] = useState("");
  const [metaDescription, setMetaDescription] = useState("");

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setErrors({});
      setActiveTab("basic");
      if (item) {
        setProductCode(item.productCode || "");
        setProductName(item.productName || "");
        setCategoryId(
          item.categoryId !== undefined && item.categoryId !== null
            ? String(item.categoryId)
            : "",
        );
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
        setLangId(
          item.langId !== undefined && item.langId !== null
            ? String(item.langId)
            : "",
        );
        setProductTags(item.productTags || "");
        setProductShortDesc(item.productShortDesc || "");
        setProductDetails(item.productDetails || "");
        setProductFeatures(item.productFeatures || "");
        setProductBenefit(item.productBenefit || "");
        setProductSmallImage(item.productSmallImage || "");
        setProductLargeImage(item.productLargeImage || "");
        setTitleBrowser(item.titleBrowser || "");
        setMetaKeyword(item.metaKeyword || "");
        setMetaDescription(item.metaDescription || "");
      } else {
        setProductCode("");
        setProductName("");
        setCategoryId("");
        setProductPrice("");
        setProductOrder("1");
        setLangId("");
        setProductTags("");
        setProductShortDesc("");
        setProductDetails("");
        setProductFeatures("");
        setProductBenefit("");
        setProductSmallImage("");
        setProductLargeImage("");
        setTitleBrowser("");
        setMetaKeyword("");
        setMetaDescription("");
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
      newErrors.productPrice = "Đơn giá phải là số hợp lệ";
    }
    if (categoryId && isNaN(Number(categoryId))) {
      newErrors.categoryId = "ID Danh mục phải là số";
    }
    if (langId && isNaN(Number(langId))) {
      newErrors.langId = "ID Ngôn ngữ phải là số";
    }

    setErrors(newErrors);

    if (
      newErrors.productCode ||
      newErrors.productName ||
      newErrors.productPrice ||
      newErrors.categoryId ||
      newErrors.langId
    ) {
      setActiveTab("basic");
    }

    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (viewOnly || !validate()) return;
    setIsSaving(true);
    try {
      let createdProduct: ProductResponse | undefined;

      const priceVal = productPrice ? Number(productPrice) : null;
      const orderVal = productOrder ? Number(productOrder) : 1;
      const categoryIdVal = categoryId ? Number(categoryId) : null;
      const langIdVal = langId ? Number(langId) : null;

      const commonPayload = {
        productCode: productCode.trim() || null,
        productName: productName.trim(),
        categoryId: categoryIdVal,
        productShortDesc: productShortDesc.trim() || null,
        productDetails: productDetails.trim() || null,
        productFeatures: productFeatures.trim() || null,
        productBenefit: productBenefit.trim() || null,
        productPrice: priceVal,
        productSmallImage: productSmallImage.trim() || null,
        productLargeImage: productLargeImage.trim() || null,
        langId: langIdVal,
        productOrder: orderVal,
        productTags: productTags.trim() || null,
        titleBrowser: titleBrowser.trim() || null,
        metaKeyword: metaKeyword.trim() || null,
        metaDescription: metaDescription.trim() || null,
      };

      if (isEditMode && item) {
        await productApi.update(item.productId, commonPayload);
        toast.success("Cập nhật sản phẩm thành công");
      } else {
        createdProduct = await productApi.create(commonPayload);
        toast.success("Thêm sản phẩm mới thành công");
      }
      onSuccess(createdProduct);
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
      <DialogContent className="sm:max-w-3xl max-h-[90vh] overflow-y-auto">
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
              ? "Xem toàn bộ thông tin chi tiết của sản phẩm."
              : isEditMode
                ? "Cập nhật đầy đủ thông tin sản phẩm theo DTO."
                : "Điền thông tin chi tiết để tạo sản phẩm mới."}
          </DialogDescription>
        </DialogHeader>

        <Tabs
          value={activeTab}
          onValueChange={setActiveTab}
          className="w-full mt-2"
        >
          <TabsList className="grid w-full grid-cols-1 sm:grid-cols-3 h-auto gap-1 sm:gap-0">
            <TabsTrigger
              value="basic"
              className="flex items-center justify-start sm:justify-center gap-2"
            >
              <Package className="w-4 h-4" />
              <span>Thông tin cơ bản</span>
            </TabsTrigger>
            <TabsTrigger
              value="details"
              className="flex items-center justify-start sm:justify-center gap-2"
            >
              <FileText className="w-4 h-4" />
              <span>Chi tiết & Đặc điểm</span>
            </TabsTrigger>
            <TabsTrigger
              value="seo"
              className="flex items-center justify-start sm:justify-center gap-2"
            >
              <Globe className="w-4 h-4" />
              <span>SEO & Metadata</span>
            </TabsTrigger>
          </TabsList>

          {/* TAB 1: THÔNG TIN CƠ BẢN */}
          <TabsContent value="basic" className="space-y-4 py-3">
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
                  aria-invalid={!!errors.productCode}
                  maxLength={50}
                />
                {errors.productCode && (
                  <p className="text-xs text-destructive">
                    {errors.productCode}
                  </p>
                )}
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

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
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
                  <p className="text-xs text-destructive">
                    {errors.productPrice}
                  </p>
                )}
              </div>

              <div className="grid gap-2">
                <Label htmlFor="categoryId">ID Danh mục</Label>
                <Input
                  id="categoryId"
                  type="number"
                  placeholder="Ví dụ: 10"
                  value={categoryId}
                  onChange={(e) => setCategoryId(e.target.value)}
                  disabled={viewOnly}
                  aria-invalid={!!errors.categoryId}
                />
                {errors.categoryId && (
                  <p className="text-xs text-destructive">
                    {errors.categoryId}
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

            <div className="grid gap-2">
              <Label htmlFor="productTags">Từ khóa / Tags</Label>
              <Input
                id="productTags"
                placeholder="chuyen-doi-so, crm, contract..."
                value={productTags}
                onChange={(e) => setProductTags(e.target.value)}
                disabled={viewOnly}
                maxLength={500}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="productShortDesc">Mô tả ngắn</Label>
              <Textarea
                id="productShortDesc"
                placeholder="Mô tả tóm tắt sản phẩm (hiển thị trên danh sách)..."
                value={productShortDesc}
                onChange={(e) => setProductShortDesc(e.target.value)}
                disabled={viewOnly}
                rows={3}
                maxLength={2000}
              />
            </div>
          </TabsContent>

          {/* TAB 2: CHI TIẾT & ĐẶC ĐIỂM */}
          <TabsContent value="details" className="space-y-4 py-3">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label htmlFor="productSmallImage">
                  Hình ảnh nhỏ (URL / Path)
                </Label>
                <Input
                  id="productSmallImage"
                  placeholder="https://example.com/small.jpg"
                  value={productSmallImage}
                  onChange={(e) => setProductSmallImage(e.target.value)}
                  disabled={viewOnly}
                  maxLength={500}
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor="productLargeImage">
                  Hình ảnh lớn (URL / Path)
                </Label>
                <Input
                  id="productLargeImage"
                  placeholder="https://example.com/large.jpg"
                  value={productLargeImage}
                  onChange={(e) => setProductLargeImage(e.target.value)}
                  disabled={viewOnly}
                  maxLength={500}
                />
              </div>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="productDetails">Chi tiết sản phẩm</Label>
              <Textarea
                id="productDetails"
                placeholder="Nội dung giới thiệu chi tiết về sản phẩm..."
                value={productDetails}
                onChange={(e) => setProductDetails(e.target.value)}
                disabled={viewOnly}
                rows={4}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="productFeatures">Tính năng nổi bật</Label>
              <Textarea
                id="productFeatures"
                placeholder="Liệt kê các tính năng nổi bật của sản phẩm..."
                value={productFeatures}
                onChange={(e) => setProductFeatures(e.target.value)}
                disabled={viewOnly}
                rows={3}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="productBenefit">Lợi ích mang lại</Label>
              <Textarea
                id="productBenefit"
                placeholder="Liệt kê các lợi ích khi sử dụng sản phẩm..."
                value={productBenefit}
                onChange={(e) => setProductBenefit(e.target.value)}
                disabled={viewOnly}
                rows={3}
              />
            </div>
          </TabsContent>

          {/* TAB 3: SEO & METADATA */}
          <TabsContent value="seo" className="space-y-4 py-3">
            <div className="grid gap-2">
              <Label htmlFor="titleBrowser">
                Tiêu đề trình duyệt (Title Browser)
              </Label>
              <Input
                id="titleBrowser"
                placeholder="Tiêu đề hiển thị trên thanh tab trình duyệt / Google..."
                value={titleBrowser}
                onChange={(e) => setTitleBrowser(e.target.value)}
                disabled={viewOnly}
                maxLength={255}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="metaKeyword">Từ khóa SEO (Meta Keyword)</Label>
              <Input
                id="metaKeyword"
                placeholder="từ khóa 1, từ khóa 2..."
                value={metaKeyword}
                onChange={(e) => setMetaKeyword(e.target.value)}
                disabled={viewOnly}
                maxLength={500}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="metaDescription">
                Mô tả SEO (Meta Description)
              </Label>
              <Textarea
                id="metaDescription"
                placeholder="Mô tả chuẩn SEO hiển thị trên kết quả tìm kiếm..."
                value={metaDescription}
                onChange={(e) => setMetaDescription(e.target.value)}
                disabled={viewOnly}
                rows={4}
                maxLength={1000}
              />
            </div>
          </TabsContent>
        </Tabs>

        <DialogFooter className="mt-4 pt-4 border-t">
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
