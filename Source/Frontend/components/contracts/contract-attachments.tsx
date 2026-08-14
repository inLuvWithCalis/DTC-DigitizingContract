"use client";

import {
  ChangeEvent,
  DragEvent,
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";
import {
  CheckCircle2,
  Download,
  FileArchive,
  FileImage,
  FileSpreadsheet,
  FileText,
  Loader2,
  Paperclip,
  Plus,
  Trash2,
  UploadCloud,
  X,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  CONTRACT_DOCUMENT_TYPES,
  ContractAttachmentResponse,
  ContractDocumentType,
  contractAttachmentsApi,
} from "@/services/contract-attachments-api";

const MAX_FILE_SIZE = 10 * 1024 * 1024;
const ACCEPTED_EXTENSIONS = [
  "pdf",
  "doc",
  "docx",
  "xls",
  "xlsx",
  "png",
  "jpg",
  "jpeg",
  "zip",
];

export interface ContractAttachmentItem {
  id: number;
  name: string;
  size?: number;
  documentType: ContractDocumentType;
  documentTypeName: string;
  uploadedAt?: string;
  uploadedBy?: string;
  downloadUrl?: string;
}

interface ContractAttachmentsProps {
  contractId: number;
  initialAttachments?: ContractAttachmentItem[];
  mockMode?: boolean;
  canManage?: boolean;
}

function getExtension(fileName: string) {
  return fileName.split(".").pop()?.toLowerCase() || "";
}

function formatFileSize(size?: number) {
  if (!size) return "Không rõ dung lượng";
  if (size < 1024 * 1024) return `${Math.ceil(size / 1024)} KB`;
  return `${(size / (1024 * 1024)).toFixed(1)} MB`;
}

function getFileIcon(fileName: string) {
  const extension = getExtension(fileName);
  if (["png", "jpg", "jpeg"].includes(extension)) return FileImage;
  if (["xls", "xlsx"].includes(extension)) return FileSpreadsheet;
  if (extension === "zip") return FileArchive;
  return FileText;
}

function documentTypeLabel(value: ContractDocumentType) {
  return (
    CONTRACT_DOCUMENT_TYPES.find((type) => type.value === value)?.label ||
    "Tài liệu khác"
  );
}

function mapAttachment(
  attachment: ContractAttachmentResponse,
): ContractAttachmentItem {
  return {
    id: attachment.attachmentId,
    name:
      attachment.contractFileName ||
      `Tài liệu #${attachment.attachmentId}`,
    documentType: attachment.documentType,
    documentTypeName:
      documentTypeLabel(attachment.documentType) ||
      attachment.documentTypeName,
    uploadedAt: attachment.uploadDate,
    uploadedBy: attachment.uploadEmployeeId
      ? `Nhân viên #${attachment.uploadEmployeeId}`
      : undefined,
    downloadUrl: attachment.contractFilePath,
  };
}

export function ContractAttachments({
  contractId,
  initialAttachments = [],
  mockMode = true,
  canManage = true,
}: ContractAttachmentsProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [attachments, setAttachments] =
    useState<ContractAttachmentItem[]>(initialAttachments);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [documentType, setDocumentType] = useState<ContractDocumentType>(99);
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [isLoadingAttachments, setIsLoadingAttachments] = useState(!mockMode);
  const [loadError, setLoadError] = useState<string | null>(null);

  const loadAttachments = useCallback(async () => {
    if (mockMode) return;

    setIsLoadingAttachments(true);
    setLoadError(null);

    try {
      const response = await contractAttachmentsApi.getAll(contractId);
      setAttachments(response.map(mapAttachment));
    } catch {
      setLoadError("Không thể tải danh sách chứng từ.");
    } finally {
      setIsLoadingAttachments(false);
    }
  }, [contractId, mockMode]);

  useEffect(() => {
    void loadAttachments();
  }, [loadAttachments]);

  const validateAndSelectFile = (file?: File) => {
    if (!canManage) return;
    if (!file) return;

    if (file.size === 0) {
      toast.error("File đang trống (0 byte). Vui lòng chọn file có nội dung.");
      return;
    }

    const extension = getExtension(file.name);
    if (!ACCEPTED_EXTENSIONS.includes(extension)) {
      toast.error("Định dạng file chưa được hỗ trợ.");
      return;
    }
    if (file.size > MAX_FILE_SIZE) {
      toast.error("File vượt quá dung lượng tối đa 10 MB.");
      return;
    }

    setSelectedFile(file);
  };

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    validateAndSelectFile(event.target.files?.[0]);
    event.target.value = "";
  };

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
    if (!canManage) return;
    validateAndSelectFile(event.dataTransfer.files?.[0]);
  };

  const handleUpload = async () => {
    if (!canManage) return;
    if (!selectedFile) {
      toast.error("Vui lòng chọn file cần đính kèm.");
      return;
    }

    setIsUploading(true);
    try {
      if (mockMode) {
        const newAttachment: ContractAttachmentItem = {
          id: Date.now(),
          name: selectedFile.name,
          size: selectedFile.size,
          documentType,
          documentTypeName: documentTypeLabel(documentType),
          uploadedAt: new Date().toISOString(),
          uploadedBy: "Bạn",
        };
        setAttachments((current) => [newAttachment, ...current]);
      } else {
        const response = await contractAttachmentsApi.upload(
          contractId,
          selectedFile,
          documentType,
        );
        setAttachments((current) => [
          {
            id: response.attachmentId,
            name: response.contractFileName || selectedFile.name,
            size: selectedFile.size,
            documentType: response.documentType,
            documentTypeName: response.documentTypeName,
            uploadedAt: response.uploadDate,
            uploadedBy: response.uploadEmployeeId
              ? `Nhân viên #${response.uploadEmployeeId}`
              : undefined,
            downloadUrl: response.contractFilePath,
          },
          ...current,
        ]);
      }

      setSelectedFile(null);
      setDocumentType(99);
      toast.success(
        mockMode
          ? "Đã thêm tài liệu vào bản xem thử."
          : "Đính kèm tài liệu thành công.",
      );
    } catch {
      toast.error("Không thể tải file lên. Vui lòng thử lại.");
    } finally {
      setIsUploading(false);
    }
  };

  const handleDelete = async (attachment: ContractAttachmentItem) => {
    if (!canManage) return;
    setDeletingId(attachment.id);
    try {
      if (!mockMode) {
        await contractAttachmentsApi.delete(contractId, attachment.id);
      }
      setAttachments((current) =>
        current.filter((item) => item.id !== attachment.id),
      );
      toast.success(mockMode ? "Đã xóa khỏi bản xem thử." : "Đã xóa tài liệu.");
    } catch {
      toast.error("Không thể xóa tài liệu. Vui lòng thử lại.");
    } finally {
      setDeletingId(null);
    }
  };

  const handleDownload = (attachment: ContractAttachmentItem) => {
    if (!attachment.downloadUrl || mockMode) {
      toast.info("Bản xem thử chưa có file thật để tải xuống.");
      return;
    }
    window.open(attachment.downloadUrl, "_blank", "noopener,noreferrer");
  };

  return (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
      <Card className="overflow-hidden">
        <CardHeader className="border-b">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Paperclip className="size-5 text-primary" />
                Tài liệu hợp đồng
              </CardTitle>
              <p className="mt-1 text-sm text-muted-foreground">
                {attachments.length} file đang được lưu trong hồ sơ
              </p>
            </div>
            {mockMode && (
              <Badge
                variant="outline"
                className="border-amber-200 bg-amber-50 text-amber-700"
              >
                Chế độ xem thử
              </Badge>
            )}
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {isLoadingAttachments ? (
            <div className="flex items-center justify-center gap-2 px-6 py-16 text-sm text-muted-foreground">
              <Loader2 className="size-5 animate-spin text-primary" />
              Đang tải danh sách chứng từ...
            </div>
          ) : loadError ? (
            <div className="px-6 py-16 text-center">
              <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-destructive/10 text-destructive">
                <FileText className="size-7" />
              </div>
              <p className="mt-4 font-semibold">Không thể tải chứng từ</p>
              <p className="mt-1 text-sm text-muted-foreground">
                {loadError}
              </p>
              <Button
                variant="outline"
                className="mt-5"
                onClick={() => void loadAttachments()}
              >
                Thử lại
              </Button>
            </div>
          ) : attachments.length === 0 ? (
            <div className="px-6 py-16 text-center">
              <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-primary/10 text-primary">
                <FileText className="size-7" />
              </div>
              <p className="mt-4 font-semibold">Chưa có tài liệu đính kèm</p>
              <p className="mx-auto mt-1 max-w-sm text-sm text-muted-foreground">
                Thêm hợp đồng đã ký, biên bản nghiệm thu, hóa đơn hoặc tài liệu
                liên quan.
              </p>
              <Button
                className="mt-5"
                disabled={!canManage}
                onClick={() => inputRef.current?.click()}
              >
                <Plus className="size-4" />
                Chọn file đầu tiên
              </Button>
            </div>
          ) : (
            <div className="divide-y">
              {attachments.map((attachment) => {
                const FileIcon = getFileIcon(attachment.name);
                return (
                  <div
                    key={attachment.id}
                    className="group flex flex-col gap-4 p-5 transition-colors hover:bg-muted/30 sm:flex-row sm:items-center"
                  >
                    <div className="flex min-w-0 flex-1 items-start gap-3">
                      <div className="flex size-11 shrink-0 items-center justify-center rounded-xl border bg-background text-primary shadow-sm">
                        <FileIcon className="size-5" />
                      </div>
                      <div className="min-w-0">
                        <p
                          className="truncate font-medium"
                          title={attachment.name}
                        >
                          {attachment.name}
                        </p>
                        <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-muted-foreground">
                          <span>{formatFileSize(attachment.size)}</span>
                          <span>•</span>
                          <span>
                            {attachment.uploadedBy || "Không rõ người tải"}
                          </span>
                          {attachment.uploadedAt && (
                            <>
                              <span>•</span>
                              <span>
                                {new Date(attachment.uploadedAt).toLocaleString(
                                  "vi-VN",
                                )}
                              </span>
                            </>
                          )}
                        </div>
                        <Badge variant="secondary" className="mt-2 font-normal">
                          {attachment.documentTypeName}
                        </Badge>
                      </div>
                    </div>
                    <div className="flex shrink-0 items-center gap-2 self-end sm:self-center">
                      <Button
                        variant="outline"
                        size="icon"
                        title="Tải xuống"
                        onClick={() => handleDownload(attachment)}
                      >
                        <Download className="size-4" />
                      </Button>
                      <Button
                        variant="outline"
                        size="icon"
                        title="Xóa tài liệu"
                        className="text-destructive hover:bg-destructive/10 hover:text-destructive"
                        disabled={!canManage || deletingId === attachment.id}
                        onClick={() => handleDelete(attachment)}
                      >
                        {deletingId === attachment.id ? (
                          <Loader2 className="size-4 animate-spin" />
                        ) : (
                          <Trash2 className="size-4" />
                        )}
                      </Button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      <Card className="h-fit xl:sticky xl:top-4">
        <CardHeader>
          <CardTitle className="text-lg">Thêm tài liệu</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <input
            ref={inputRef}
            type="file"
            className="hidden"
            accept=".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg,.zip"
            onChange={handleFileChange}
            disabled={!canManage}
          />

          <div
            role="button"
            tabIndex={canManage ? 0 : -1}
            aria-disabled={!canManage}
            className={`rounded-2xl border-2 border-dashed p-6 text-center transition-colors ${
              isDragging
                ? "border-primary bg-primary/5"
                : "border-border hover:border-primary/50 hover:bg-muted/30"
            }`}
            onClick={() => canManage && inputRef.current?.click()}
            onKeyDown={(event) => {
              if (
                canManage &&
                (event.key === "Enter" || event.key === " ")
              ) {
                inputRef.current?.click();
              }
            }}
            onDragEnter={(event) => {
              event.preventDefault();
              setIsDragging(true);
            }}
            onDragOver={(event) => event.preventDefault()}
            onDragLeave={() => setIsDragging(false)}
            onDrop={handleDrop}
          >
            <div className="mx-auto flex size-12 items-center justify-center rounded-2xl bg-primary/10 text-primary">
              <UploadCloud className="size-6" />
            </div>
            <p className="mt-3 text-sm font-semibold">Kéo thả file vào đây</p>
            <p className="mt-1 text-xs text-muted-foreground">
              hoặc bấm để chọn từ máy tính
            </p>
            <p className="mt-3 text-[11px] text-muted-foreground">
              PDF, Word, Excel, ảnh hoặc ZIP · Tối đa 10 MB
            </p>
          </div>

          {selectedFile && (
            <div className="rounded-xl border bg-muted/30 p-3">
              <div className="flex items-center gap-3">
                <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-emerald-500/10 text-emerald-600">
                  <CheckCircle2 className="size-4" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">
                    {selectedFile.name}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {formatFileSize(selectedFile.size)}
                  </p>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-8"
                  onClick={() => setSelectedFile(null)}
                >
                  <X className="size-4" />
                </Button>
              </div>
              <Progress value={100} className="mt-3 h-1" />
            </div>
          )}

          <div className="space-y-2">
            <p className="text-sm font-medium">Loại chứng từ</p>
            <Select
              value={String(documentType)}
              disabled={!canManage}
              onValueChange={(value) =>
                setDocumentType(Number(value) as ContractDocumentType)
              }
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Chọn loại chứng từ" />
              </SelectTrigger>
              <SelectContent>
                {CONTRACT_DOCUMENT_TYPES.map((type) => (
                  <SelectItem key={type.value} value={String(type.value)}>
                    {type.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <Button
            className="w-full"
            disabled={!canManage || !selectedFile || isUploading}
            onClick={handleUpload}
          >
            {isUploading ? (
              <Loader2 className="size-4 animate-spin" />
            ) : (
              <UploadCloud className="size-4" />
            )}
            {isUploading ? "Đang tải lên..." : "Đính kèm tài liệu"}
          </Button>

          <p className="text-center text-xs leading-5 text-muted-foreground">
            File được phân loại để dễ theo dõi tiến độ hoàn thiện hồ sơ hợp
            đồng.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

// --- ContractDocuments: wrapper dùng cho tab "Chứng từ" ---
import { ContractDetailResponse } from "@/services/contract-api";

export function ContractDocuments({
  contract,
  canManage,
}: {
  contract: ContractDetailResponse;
  canManage: boolean;
}) {
  return (
    <ContractAttachments
      contractId={contract.contractId}
      canManage={canManage}
      mockMode={false}
    />
  );
}
