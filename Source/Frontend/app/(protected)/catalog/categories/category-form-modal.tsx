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
  categoryApi,
  CategoryResponse,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from "@/services/catalog/category-api";

interface CategoryFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  item?: CategoryResponse | null;
  viewOnly?: boolean;
  initialParentId?: number | null;
}

export function CategoryFormModal({
  isOpen,
  onClose,
  onSuccess,
  item,
  viewOnly = false,
  initialParentId = null,
}: CategoryFormModalProps) {
  const isEditMode = !!item && !viewOnly;

  const [categoryName, setCategoryName] = useState("");
  const [categoryShortDesc, setCategoryShortDesc] = useState("");
  const [categoryOrder, setCategoryOrder] = useState("");
  const [categoryParentId, setCategoryParentId] = useState("");
  const [langId, setLangId] = useState("");
  const [image, setImage] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);

  const [parentOptions, setParentOptions] = useState<CategoryResponse[]>([]);
  const [isLoadingParents, setIsLoadingParents] = useState(false);

  useEffect(() => {
    if (isOpen) {
      const fetchParents = async () => {
        setIsLoadingParents(true);
        try {
          const res = await categoryApi.getList({ page: 1, pageSize: 1000 });

          const validParents = (res.items || []).filter(
            (c) => !item || c.categoryId !== item.categoryId,
          );
          setParentOptions(validParents);
        } catch (error) {
          console.error("Lỗi khi tải danh sách danh mục cha", error);
        } finally {
          setIsLoadingParents(false);
        }
      };

      fetchParents();
    }
  }, [isOpen, item]);

  useEffect(() => {
    if (isOpen) {
      setErrors({});
      if (item) {
        setCategoryName(item.categoryName || "");
        setCategoryShortDesc(item.categoryShortDesc || "");
        setCategoryOrder(
          item.categoryOrder !== undefined && item.categoryOrder !== null
            ? String(item.categoryOrder)
            : "",
        );
        setCategoryParentId(
          item.categoryParentId !== undefined && item.categoryParentId !== null
            ? String(item.categoryParentId)
            : "",
        );
        setLangId(
          item.langId !== undefined && item.langId !== null
            ? String(item.langId)
            : "",
        );
        setImage(item.image || "");
      } else {
        setCategoryName("");
        setCategoryShortDesc("");
        setCategoryOrder("");
        setCategoryParentId(initialParentId ? String(initialParentId) : "");
        setLangId("");
        setImage("");
      }
    }
  }, [isOpen, item, initialParentId]);

  const validate = () => {
    const newErrors: Record<string, string> = {};

    if (!categoryName.trim()) {
      newErrors.categoryName = "Vui lòng nhập tên danh mục";
    }
    if (categoryOrder && isNaN(Number(categoryOrder))) {
      newErrors.categoryOrder = "Thứ tự phải là số hợp lệ";
    }
    if (langId && isNaN(Number(langId))) {
      newErrors.langId = "ID Ngôn ngữ phải là số hợp lệ";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (viewOnly || !validate()) return;
    setIsSaving(true);

    try {
      const orderVal = categoryOrder ? Number(categoryOrder) : null;
      const parentIdVal =
        categoryParentId && categoryParentId !== "none"
          ? Number(categoryParentId)
          : null;
      const langIdVal = langId ? Number(langId) : null;

      if (isEditMode && item) {
        const payload: UpdateCategoryRequest = {
          categoryName: categoryName.trim(),
          categoryShortDesc: categoryShortDesc.trim() || null,
          categoryOrder: orderVal,
          categoryParentId: parentIdVal,
          langId: langIdVal,
          image: image.trim() || null,
        };
        await categoryApi.update(item.categoryId, payload);
        toast.success("Cập nhật danh mục thành công");
      } else {
        const payload: CreateCategoryRequest = {
          categoryName: categoryName.trim(),
          categoryShortDesc: categoryShortDesc.trim() || null,
          categoryOrder: orderVal,
          categoryParentId: parentIdVal,
          langId: langIdVal,
          image: image.trim() || null,
        };
        await categoryApi.create(payload);
        toast.success("Thêm danh mục mới thành công");
      }
      onSuccess();
      onClose();
    } catch (error: any) {
      const message =
        error?.response?.data?.message ||
        error?.message ||
        (isEditMode
          ? "Không thể cập nhật danh mục"
          : "Không thể thêm danh mục");
      toast.error(message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {viewOnly
              ? "Chi tiết danh mục"
              : isEditMode
                ? "Chỉnh sửa danh mục"
                : "Thêm danh mục mới"}
          </DialogTitle>
          <DialogDescription>
            {viewOnly
              ? "Xem thông tin chi tiết của danh mục."
              : isEditMode
                ? "Cập nhật thông tin danh mục trong hệ thống."
                : "Điền thông tin để tạo danh mục sản phẩm mới."}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid gap-2">
            <Label htmlFor="categoryName">
              Tên danh mục{" "}
              {!viewOnly && <span className="text-destructive">*</span>}
            </Label>
            <Input
              id="categoryName"
              placeholder="Ví dụ: Phần mềm, Phần cứng, Dịch vụ..."
              value={categoryName}
              onChange={(e) => setCategoryName(e.target.value)}
              disabled={viewOnly}
              aria-invalid={!!errors.categoryName}
              maxLength={500}
            />
            {errors.categoryName && (
              <p className="text-xs text-destructive">{errors.categoryName}</p>
            )}
          </div>

          <div className="grid gap-2">
            <Label htmlFor="categoryShortDesc">Mô tả ngắn</Label>
            <Textarea
              id="categoryShortDesc"
              placeholder="Nhập mô tả cho danh mục..."
              value={categoryShortDesc}
              onChange={(e) => setCategoryShortDesc(e.target.value)}
              disabled={viewOnly}
              rows={3}
              maxLength={1000}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="categoryOrder">Thứ tự sắp xếp</Label>
              <Input
                id="categoryOrder"
                type="number"
                placeholder="Ví dụ: 1"
                value={categoryOrder}
                onChange={(e) => setCategoryOrder(e.target.value)}
                disabled={viewOnly}
                aria-invalid={!!errors.categoryOrder}
              />
              {errors.categoryOrder && (
                <p className="text-xs text-destructive">
                  {errors.categoryOrder}
                </p>
              )}
            </div>

            <div className="grid gap-2">
              <Label>Danh mục cha</Label>
              <Select
                value={categoryParentId || "none"}
                onValueChange={(val) =>
                  setCategoryParentId(val === "none" ? "" : val)
                }
                disabled={viewOnly || isLoadingParents}
              >
                <SelectTrigger
                  className="w-full"
                  aria-invalid={!!errors.categoryParentId}
                >
                  <SelectValue
                    placeholder={
                      isLoadingParents ? "Đang tải..." : "-- Là danh mục gốc --"
                    }
                  />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem
                    value="none"
                    className="font-semibold text-primary"
                  >
                    -- Là danh mục gốc --
                  </SelectItem>
                  {parentOptions.map((cat) => (
                    <SelectItem
                      key={cat.categoryId}
                      value={String(cat.categoryId)}
                    >
                      {cat.categoryName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.categoryParentId && (
                <p className="text-xs text-destructive">
                  {errors.categoryParentId}
                </p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="langId">ID Ngôn ngữ</Label>
              <Input
                id="langId"
                type="number"
                placeholder="Ví dụ: 1"
                value={langId}
                onChange={(e) => setLangId(e.target.value)}
                disabled={viewOnly}
                aria-invalid={!!errors.langId}
              />
              {errors.langId && (
                <p className="text-xs text-destructive">{errors.langId}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="image">Hình ảnh / Icon URL</Label>
              <Input
                id="image"
                placeholder="Ví dụ: /icons/category.png"
                value={image}
                onChange={(e) => setImage(e.target.value)}
                disabled={viewOnly}
                maxLength={50}
              />
            </div>
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
