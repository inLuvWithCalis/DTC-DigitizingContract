"use client";

import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { ContractLanguageMode } from "@/services/contract-api";
import {
  contractTemplateApi,
  type ContractTemplateDetailResponse,
} from "@/services/contract-template-api";
import { getContractTemplateErrorMessage } from "./contract-template-utils";

interface ContractTemplateFormDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (template: ContractTemplateDetailResponse) => void;
  template?: ContractTemplateDetailResponse | null;
}

export function ContractTemplateFormDialog({
  isOpen,
  onClose,
  onSuccess,
  template,
}: ContractTemplateFormDialogProps) {
  const isEdit = Boolean(template);
  const [templateCode, setTemplateCode] = useState("");
  const [templateName, setTemplateName] = useState("");
  const [templateNameEn, setTemplateNameEn] = useState("");
  const [languageMode, setLanguageMode] = useState<ContractLanguageMode>(
    ContractLanguageMode.Vietnamese,
  );
  const [description, setDescription] = useState("");
  const [initialChangeNote, setInitialChangeNote] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    setTemplateCode(template?.templateCode ?? "");
    setTemplateName(template?.templateName ?? "");
    setTemplateNameEn(template?.templateNameEn ?? "");
    setLanguageMode(
      template?.languageMode ?? ContractLanguageMode.Vietnamese,
    );
    setDescription(template?.description ?? "");
    setInitialChangeNote("");
  }, [isOpen, template]);

  const handleSubmit = async () => {
    if (!templateName.trim()) {
      toast.error("Vui lòng nhập tên mẫu hợp đồng.");
      return;
    }
    if (!isEdit && !templateCode.trim()) {
      toast.error("Vui lòng nhập mã mẫu hợp đồng.");
      return;
    }
    if (
      languageMode === ContractLanguageMode.Bilingual &&
      !templateNameEn.trim()
    ) {
      toast.error("Mẫu song ngữ cần có tên tiếng Anh.");
      return;
    }

    try {
      setIsSaving(true);
      const result = template
        ? await contractTemplateApi.update(template.templateId, {
            templateName: templateName.trim(),
            templateNameEn: templateNameEn.trim() || null,
            description: description.trim() || null,
            rowVersion: template.rowVersion,
          })
        : await contractTemplateApi.create({
            templateCode: templateCode.trim(),
            templateName: templateName.trim(),
            templateNameEn: templateNameEn.trim() || null,
            languageMode,
            description: description.trim() || null,
            initialChangeNote: initialChangeNote.trim() || null,
          });

      toast.success(isEdit ? "Đã cập nhật mẫu hợp đồng." : "Đã tạo mẫu hợp đồng.");
      onSuccess(result);
      onClose();
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {isEdit ? "Chỉnh sửa mẫu hợp đồng" : "Tạo mẫu hợp đồng"}
          </DialogTitle>
          <DialogDescription>
            {isEdit
              ? "Chỉ tên và mô tả có thể thay đổi sau khi tạo."
              : "Hệ thống sẽ đồng thời tạo Version 1 ở trạng thái bản nháp."}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="template-code">
              Mã mẫu <span className="text-destructive">*</span>
            </Label>
            <Input
              id="template-code"
              value={templateCode}
              onChange={(event) => setTemplateCode(event.target.value)}
              disabled={isEdit}
              placeholder="VD: SOFTWARE-SUPPLY-VI"
            />
          </div>
          <div className="space-y-2">
            <Label>
              Ngôn ngữ <span className="text-destructive">*</span>
            </Label>
            <Select
              value={String(languageMode)}
              onValueChange={(value) =>
                setLanguageMode(Number(value) as ContractLanguageMode)
              }
              disabled={isEdit}
            >
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={String(ContractLanguageMode.Vietnamese)}>
                  Tiếng Việt
                </SelectItem>
                <SelectItem value={String(ContractLanguageMode.Bilingual)}>
                  Song ngữ
                </SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="template-name">
              Tên mẫu <span className="text-destructive">*</span>
            </Label>
            <Input
              id="template-name"
              value={templateName}
              onChange={(event) => setTemplateName(event.target.value)}
              placeholder="Tên mẫu hợp đồng"
            />
          </div>
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="template-name-en">
              Tên tiếng Anh
              {languageMode === ContractLanguageMode.Bilingual && (
                <span className="text-destructive"> *</span>
              )}
            </Label>
            <Input
              id="template-name-en"
              value={templateNameEn}
              onChange={(event) => setTemplateNameEn(event.target.value)}
              placeholder="English template name"
            />
          </div>
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="template-description">Mô tả</Label>
            <Textarea
              id="template-description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              className="min-h-24"
            />
          </div>
          {!isEdit && (
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="initial-change-note">Ghi chú Version 1</Label>
              <Textarea
                id="initial-change-note"
                value={initialChangeNote}
                onChange={(event) => setInitialChangeNote(event.target.value)}
                placeholder="Nội dung khởi tạo bản nháp đầu tiên"
              />
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isSaving}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} disabled={isSaving}>
            {isSaving && <Loader2 className="size-4 animate-spin" />}
            {isEdit ? "Lưu thay đổi" : "Tạo mẫu"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
