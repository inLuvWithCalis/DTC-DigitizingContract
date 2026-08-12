"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft,
  Copy,
  FileClock,
  Languages,
  Loader2,
  Pencil,
  ShieldAlert,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { ContractTemplateFormDialog } from "@/components/contract-templates/contract-template-form-dialog";
import {
  TemplateValidationStatusBadge,
  TemplateVersionStatusBadge,
} from "@/components/contract-templates/contract-template-status";
import { getContractTemplateErrorMessage } from "@/components/contract-templates/contract-template-utils";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Header } from "@/components/ui/custom/header";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { useAuthStore } from "@/hooks/use-auth-store";
import { formatDateTime } from "@/lib/format-date-time";
import { ContractLanguageMode } from "@/services/contract-api";
import {
  contractTemplateApi,
  getTemplateDocumentTypeLabel,
  TemplateVersionStatus,
  type ContractTemplateDetailResponse,
  type ContractTemplateVersionSummaryResponse,
} from "@/services/contract-template-api";
import { EmployeeType } from "@/services/employees-api";

export default function ContractTemplateDetailPage() {
  const params = useParams<{ templateId: string }>();
  const router = useRouter();
  const user = useAuthStore((state) => state.user);
  const canManage = user?.employeeType === EmployeeType.AdminOfficer;
  const templateId = Number(params.templateId);
  const [template, setTemplate] =
    useState<ContractTemplateDetailResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [copySource, setCopySource] =
    useState<ContractTemplateVersionSummaryResponse | null>(null);
  const [changeNote, setChangeNote] = useState("");
  const [isCopying, setIsCopying] = useState(false);

  const fetchTemplate = useCallback(async () => {
    if (!canManage || !Number.isInteger(templateId) || templateId <= 0) {
      setIsLoading(false);
      return;
    }
    try {
      setIsLoading(true);
      setTemplate(await contractTemplateApi.getById(templateId));
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }, [canManage, templateId]);

  useEffect(() => {
    void fetchTemplate();
  }, [fetchTemplate]);

  const versions = useMemo(
    () =>
      [...(template?.versions ?? [])].sort((a, b) => b.versionNo - a.versionNo),
    [template?.versions],
  );

  const handleCopy = async () => {
    if (!copySource) return;
    try {
      setIsCopying(true);
      const result = await contractTemplateApi.copyVersion(
        copySource.templateVersionId,
        {
          rowVersion: copySource.rowVersion,
          changeNote: changeNote.trim() || null,
        },
      );
      toast.success(
        `Đã tạo Version ${result.versionNo} ở trạng thái bản nháp.`,
      );
      setCopySource(null);
      setChangeNote("");
      router.push(
        `/admin/contract-templates/${templateId}/versions/${result.templateVersionId}`,
      );
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
      await fetchTemplate();
    } finally {
      setIsCopying(false);
    }
  };

  return (
    <>
      <Header title="Chi tiết mẫu hợp đồng" />
      <div className="grow overflow-y-auto p-3 sm:p-6 lg:p-10">
        <div className="mx-auto space-y-6">
          <Button asChild variant="ghost" className="-ml-3">
            <Link href="/admin/contract-templates">
              <ArrowLeft className="size-4" /> Danh sách mẫu
            </Link>
          </Button>

          {!canManage ? (
            <Alert variant="destructive">
              <ShieldAlert />
              <AlertTitle>Không có quyền truy cập</AlertTitle>
              <AlertDescription>
                Chỉ Admin Officer được quản trị mẫu hợp đồng.
              </AlertDescription>
            </Alert>
          ) : isLoading ? (
            <div className="space-y-4">
              <Skeleton className="h-44 w-full" />
              <Skeleton className="h-72 w-full" />
            </div>
          ) : !template ? (
            <Alert variant="destructive">
              <AlertTitle>Không tìm thấy mẫu hợp đồng</AlertTitle>
            </Alert>
          ) : (
            <>
              <Card>
                <CardHeader className="flex flex-row items-start justify-between gap-4">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <CardTitle className="text-xl">
                        {template.templateName}
                      </CardTitle>
                      <Badge variant="outline" className="font-mono">
                        {template.templateCode}
                      </Badge>
                    </div>
                    {template.templateNameEn && (
                      <p className="mt-2 text-sm text-muted-foreground">
                        {template.templateNameEn}
                      </p>
                    )}
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setIsEditOpen(true)}
                  >
                    <Pencil className="size-4" /> Chỉnh sửa
                  </Button>
                </CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                  <div>
                    <p className="text-xs text-muted-foreground">
                      Loại tài liệu
                    </p>
                    <p className="mt-1 font-medium">
                      {getTemplateDocumentTypeLabel(template.documentType)}
                    </p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Ngôn ngữ</p>
                    <p className="mt-1 inline-flex items-center gap-1.5 font-medium">
                      <Languages className="size-4" />
                      {template.languageMode === ContractLanguageMode.Bilingual
                        ? "Song ngữ"
                        : "Tiếng Việt"}
                    </p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">
                      Version phát hành
                    </p>
                    <p className="mt-1 font-medium">
                      {template.currentPublishedVersionId
                        ? `#${template.currentPublishedVersionId}`
                        : "Chưa có"}
                    </p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Cập nhật</p>
                    <p className="mt-1 font-medium">
                      {formatDateTime(
                        template.updatedDate || template.createdDate,
                      )}
                    </p>
                  </div>
                  {template.description && (
                    <div className="sm:col-span-2 lg:col-span-4">
                      <p className="text-xs text-muted-foreground">Mô tả</p>
                      <p className="mt-1 whitespace-pre-wrap text-sm">
                        {template.description}
                      </p>
                    </div>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <FileClock className="size-5 text-primary" /> Lịch sử phiên
                    bản
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  {versions.map((version) => {
                    const isCurrentPublished =
                      template.currentPublishedVersionId ===
                      version.templateVersionId;
                    return (
                      <div
                        key={version.templateVersionId}
                        className="flex flex-col justify-between gap-4 rounded-xl border p-4 md:flex-row md:items-center"
                      >
                        <div className="min-w-0">
                          <div className="flex flex-wrap items-center gap-2">
                            <p className="font-semibold">
                              Version {version.versionNo}
                            </p>
                            <TemplateVersionStatusBadge
                              status={version.status}
                            />
                            <TemplateValidationStatusBadge
                              status={version.validationStatus}
                            />
                            {isCurrentPublished && <Badge>Hiện hành</Badge>}
                          </div>
                          <p className="mt-2 text-sm text-muted-foreground">
                            {version.changeNote || "Không có ghi chú thay đổi"}
                          </p>
                          <p className="mt-1 text-xs text-muted-foreground">
                            Tạo lúc {formatDateTime(version.createdDate)}
                          </p>
                        </div>
                        <div className="flex shrink-0 flex-wrap gap-2">
                          <Button asChild variant="outline" size="sm">
                            <Link
                              href={`/admin/contract-templates/${templateId}/versions/${version.templateVersionId}`}
                            >
                              Mở workspace
                            </Link>
                          </Button>
                          {version.status === TemplateVersionStatus.Published &&
                            isCurrentPublished && (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => {
                                  setCopySource(version);
                                  setChangeNote("");
                                }}
                              >
                                <Copy className="size-4" /> Tạo bản nháp mới
                              </Button>
                            )}
                        </div>
                      </div>
                    );
                  })}
                  {versions.length === 0 && (
                    <p className="py-10 text-center text-sm text-muted-foreground">
                      Chưa có phiên bản.
                    </p>
                  )}
                </CardContent>
              </Card>
            </>
          )}
        </div>
      </div>

      <ContractTemplateFormDialog
        isOpen={isEditOpen}
        onClose={() => setIsEditOpen(false)}
        onSuccess={(updated) => setTemplate(updated)}
        template={template}
      />

      <Dialog
        open={Boolean(copySource)}
        onOpenChange={(open) => !open && setCopySource(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Tạo bản nháp mới</DialogTitle>
            <DialogDescription>
              Soft terms sẽ được sao chép; DOCX, validation và preview phải thực
              hiện lại.
            </DialogDescription>
          </DialogHeader>
          <Textarea
            value={changeNote}
            onChange={(event) => setChangeNote(event.target.value)}
            placeholder="Ghi chú thay đổi cho version mới"
          />
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setCopySource(null)}
              disabled={isCopying}
            >
              Hủy
            </Button>
            <Button onClick={handleCopy} disabled={isCopying}>
              {isCopying && <Loader2 className="size-4 animate-spin" />} Tạo bản
              nháp
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
