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
  CircleAlert,
  Download,
  FileArchive,
  FileImage,
  FileSpreadsheet,
  FileText,
  Loader2,
  Paperclip,
  Plus,
  RotateCcw,
  Trash2,
  UploadCloud,
  X,
} from "lucide-react";
import { getApiErrorMessage } from "@/lib/api-error";
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

type UploadQueueStatus = "pending" | "uploading" | "success" | "error";

interface UploadQueueItem {
  id: string;
  file: File;
  documentType: ContractDocumentType;
  status: UploadQueueStatus;
  progress: number;
  errorMessage?: string;
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
    name: attachment.contractFileName || `Tài liệu #${attachment.attachmentId}`,
    documentType: attachment.documentType,
    documentTypeName:
      documentTypeLabel(attachment.documentType) || attachment.documentTypeName,
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
  const [uploadQueue, setUploadQueue] = useState<UploadQueueItem[]>([]);
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

  const addFilesToQueue = (files: File[]) => {
    if (!canManage || files.length === 0) return;

    const invalid = {
      empty: 0,
      unsupported: 0,
      oversized: 0,
    };
    const accepted = files.flatMap<UploadQueueItem>((file, index) => {
      if (file.size === 0) {
        invalid.empty += 1;
        return [];
      }
      if (!ACCEPTED_EXTENSIONS.includes(getExtension(file.name))) {
        invalid.unsupported += 1;
        return [];
      }
      if (file.size > MAX_FILE_SIZE) {
        invalid.oversized += 1;
        return [];
      }

      return [
        {
          id: `${Date.now()}-${index}-${file.name}-${file.lastModified}`,
          file,
          documentType: 99,
          status: "pending",
          progress: 0,
        },
      ];
    });

    if (accepted.length > 0) {
      setUploadQueue((current) => [...current, ...accepted]);
    }
    if (invalid.empty > 0) {
      toast.error(`${invalid.empty} file bị bỏ qua vì không có nội dung.`);
    }
    if (invalid.unsupported > 0) {
      toast.error(`${invalid.unsupported} file có định dạng chưa hỗ trợ.`);
    }
    if (invalid.oversized > 0) {
      toast.error(`${invalid.oversized} file vượt quá dung lượng 10 MB.`);
    }
  };

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    addFilesToQueue(Array.from(event.target.files ?? []));
    event.target.value = "";
  };

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
    if (!canManage) return;
    addFilesToQueue(Array.from(event.dataTransfer.files));
  };

  const updateQueueItem = (
    itemId: string,
    update: Partial<Omit<UploadQueueItem, "id" | "file">>,
  ) => {
    setUploadQueue((current) =>
      current.map((item) =>
        item.id === itemId ? { ...item, ...update } : item,
      ),
    );
  };

  const uploadQueueItem = async (item: UploadQueueItem) => {
    updateQueueItem(item.id, {
      status: "uploading",
      progress: 1,
      errorMessage: undefined,
    });
    try {
      if (mockMode) {
        const newAttachment: ContractAttachmentItem = {
          id: Date.now() + Math.floor(Math.random() * 1000),
          name: item.file.name,
          size: item.file.size,
          documentType: item.documentType,
          documentTypeName: documentTypeLabel(item.documentType),
          uploadedAt: new Date().toISOString(),
          uploadedBy: "Bạn",
        };
        setAttachments((current) => [newAttachment, ...current]);
      } else {
        const response = await contractAttachmentsApi.upload(
          contractId,
          item.file,
          item.documentType,
          (progress) => updateQueueItem(item.id, { progress }),
        );
        setAttachments((current) => [
          {
            id: response.attachmentId,
            name: response.contractFileName || item.file.name,
            size: item.file.size,
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

      updateQueueItem(item.id, { status: "success", progress: 100 });
      return true;
    } catch (error) {
      updateQueueItem(item.id, {
        status: "error",
        progress: 0,
        errorMessage: getApiErrorMessage(
          error,
          "Không thể tải file lên. Vui lòng thử lại.",
        ),
      });
      return false;
    }
  };

  const handleUpload = async () => {
    if (!canManage || isUploading) return;
    const pendingItems = uploadQueue.filter(
      (item) => item.status === "pending" || item.status === "error",
    );
    if (pendingItems.length === 0) {
      toast.error("Vui lòng chọn file cần đính kèm.");
      return;
    }

    setIsUploading(true);
    const results = await Promise.all(pendingItems.map(uploadQueueItem));
    setIsUploading(false);

    const successCount = results.filter(Boolean).length;
    if (successCount > 0) {
      toast.success(
        mockMode
          ? `Đã thêm ${successCount} tài liệu vào bản xem thử.`
          : `Đã tải lên ${successCount}/${pendingItems.length} tài liệu.`,
      );
    }
    if (successCount < pendingItems.length) {
      toast.error(
        `${pendingItems.length - successCount} file tải lên thất bại. Bạn có thể thử lại từng file.`,
      );
    }
  };

  const retryUpload = async (item: UploadQueueItem) => {
    if (isUploading) return;
    setIsUploading(true);
    const success = await uploadQueueItem(item);
    setIsUploading(false);
    if (success) toast.success(`Đã tải lên ${item.file.name}.`);
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

  const handleDownload = async (attachment: ContractAttachmentItem) => {
    if (mockMode) {
      toast.info("Bản xem thử chưa có file thật để tải xuống.");
      return;
    }
    try {
      const blob = await contractAttachmentsApi.download(contractId, attachment.id);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = attachment.name;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể tải tài liệu."));
    }
  };

  return (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
      <Card className="overflow-hidden gap-0">
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
              <p className="mt-1 text-sm text-muted-foreground">{loadError}</p>
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
                          {documentTypeLabel(attachment.documentType)}
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
            multiple
            className="hidden"
            accept=".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg,.zip"
            onChange={handleFileChange}
            disabled={!canManage || isUploading}
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
              if (canManage && (event.key === "Enter" || event.key === " ")) {
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
            <p className="mt-3 text-sm font-semibold">
              Kéo thả một hoặc nhiều file vào đây
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              hoặc bấm để chọn từ máy tính
            </p>
            <p className="mt-3 text-[11px] text-muted-foreground">
              PDF, Word, Excel, ảnh hoặc ZIP · Tối đa 10 MB
            </p>
          </div>

          {uploadQueue.length > 0 && (
            <div className="space-y-3">
              <div className="flex items-center justify-between gap-2">
                <p className="text-sm font-medium">
                  Danh sách tải lên ({uploadQueue.length})
                </p>
                {uploadQueue.some((item) => item.status === "success") && (
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={isUploading}
                    onClick={() =>
                      setUploadQueue((current) =>
                        current.filter((item) => item.status !== "success"),
                      )
                    }
                  >
                    Dọn file đã xong
                  </Button>
                )}
              </div>

              <div className="max-h-[420px] space-y-3 overflow-y-auto pr-1">
                {uploadQueue.map((item) => {
                  const FileIcon = getFileIcon(item.file.name);
                  const isComplete = item.status === "success";
                  const isFailed = item.status === "error";
                  const isItemUploading = item.status === "uploading";

                  return (
                    <div
                      key={item.id}
                      className={`rounded-xl border p-3 ${
                        isFailed
                          ? "border-destructive/40 bg-destructive/5"
                          : isComplete
                            ? "border-emerald-500/30 bg-emerald-500/5"
                            : "bg-muted/30"
                      }`}
                    >
                      <div className="flex items-start gap-3">
                        <div
                          className={`flex size-9 shrink-0 items-center justify-center rounded-lg ${
                            isFailed
                              ? "bg-destructive/10 text-destructive"
                              : isComplete
                                ? "bg-emerald-500/10 text-emerald-600"
                                : "bg-primary/10 text-primary"
                          }`}
                        >
                          {isFailed ? (
                            <CircleAlert className="size-4" />
                          ) : isComplete ? (
                            <CheckCircle2 className="size-4" />
                          ) : isItemUploading ? (
                            <Loader2 className="size-4 animate-spin" />
                          ) : (
                            <FileIcon className="size-4" />
                          )}
                        </div>
                        <div className="min-w-0 flex-1">
                          <p
                            className="truncate text-sm font-medium"
                            title={item.file.name}
                          >
                            {item.file.name}
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {formatFileSize(item.file.size)}
                          </p>
                        </div>
                        {isFailed && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="size-8"
                            title="Thử tải lại"
                            disabled={isUploading}
                            onClick={() => void retryUpload(item)}
                          >
                            <RotateCcw className="size-4" />
                          </Button>
                        )}
                        {!isItemUploading && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="size-8"
                            title="Bỏ khỏi danh sách"
                            disabled={isUploading}
                            onClick={() =>
                              setUploadQueue((current) =>
                                current.filter(
                                  (queueItem) => queueItem.id !== item.id,
                                ),
                              )
                            }
                          >
                            <X className="size-4" />
                          </Button>
                        )}
                      </div>

                      <div className="mt-3">
                        <Select
                          value={String(item.documentType)}
                          disabled={isUploading || isComplete}
                          onValueChange={(value) =>
                            updateQueueItem(item.id, {
                              documentType: Number(
                                value,
                              ) as ContractDocumentType,
                            })
                          }
                        >
                          <SelectTrigger className="w-full bg-background">
                            <SelectValue placeholder="Chọn loại chứng từ" />
                          </SelectTrigger>
                          <SelectContent>
                            {CONTRACT_DOCUMENT_TYPES.map((type) => (
                              <SelectItem
                                key={type.value}
                                value={String(type.value)}
                              >
                                {type.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>

                      {(isItemUploading || isComplete) && (
                        <div className="mt-3 space-y-1">
                          <Progress value={item.progress} className="h-1" />
                          <p className="text-right text-[11px] text-muted-foreground">
                            {isComplete ? "Đã tải lên" : `${item.progress}%`}
                          </p>
                        </div>
                      )}
                      {item.errorMessage && (
                        <p className="mt-2 text-xs text-destructive">
                          {item.errorMessage}
                        </p>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          <Button
            className="w-full"
            disabled={
              !canManage ||
              isUploading ||
              !uploadQueue.some(
                (item) => item.status === "pending" || item.status === "error",
              )
            }
            onClick={() => void handleUpload()}
          >
            {isUploading ? (
              <Loader2 className="size-4 animate-spin" />
            ) : (
              <UploadCloud className="size-4" />
            )}
            {isUploading
              ? "Đang tải lên..."
              : `Tải ${uploadQueue.filter((item) => item.status === "pending" || item.status === "error").length} file`}
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
