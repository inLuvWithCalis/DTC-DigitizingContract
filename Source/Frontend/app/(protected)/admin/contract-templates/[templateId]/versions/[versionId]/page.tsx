"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import {
  Archive,
  ArrowLeft,
  Braces,
  CheckCircle2,
  Download,
  Eye,
  FileText,
  Info,
  ListChecks,
  Loader2,
  RefreshCw,
  Send,
  ShieldAlert,
  Upload,
  XCircle,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { ContractTemplatePlaceholderCatalog } from "@/components/contract-templates/contract-template-placeholder-catalog";
import {
  TemplateValidationStatusBadge,
  TemplateVersionStatusBadge,
} from "@/components/contract-templates/contract-template-status";
import { ContractTemplateTermsEditor } from "@/components/contract-templates/contract-template-terms-editor";
import {
  downloadBlob,
  getContractTemplateErrorMessage,
  parseValidationMessages,
} from "@/components/contract-templates/contract-template-utils";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { Header } from "@/components/ui/custom/header";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { usePermission } from "@/hooks/use-permission";
import { RBAC_PERMISSIONS } from "@/lib/rbac";
import { formatDateTime } from "@/lib/format-date-time";
import { ContractLanguageMode } from "@/services/contract-api";
import {
  contractTemplateApi,
  TemplateValidationStatus,
  TemplateVersionStatus,
  type ContractTemplateDetailResponse,
  type ContractTemplateVersionDetailResponse,
} from "@/services/contract-template-api";

const MAX_DOCX_SIZE = 10 * 1024 * 1024;
const DOCX_MIME =
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
const TAB_VALUES = [
  "overview",
  "terms",
  "document",
  "placeholders",
  "preview",
] as const;
type WorkspaceTab = (typeof TAB_VALUES)[number];

const isWorkspaceTab = (value: string): value is WorkspaceTab =>
  TAB_VALUES.includes(value as WorkspaceTab);

function RequirementRow({
  met,
  children,
}: {
  met: boolean;
  children: React.ReactNode;
}) {
  return (
    <li className="flex items-start gap-2 text-sm">
      {met ? (
        <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-emerald-600" />
      ) : (
        <XCircle className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
      )}
      <span className={met ? "" : "text-muted-foreground"}>{children}</span>
    </li>
  );
}

export default function ContractTemplateVersionWorkspacePage() {
  const params = useParams<{ templateId: string; versionId: string }>();
  const { can } = usePermission();
  const canManage = can(RBAC_PERMISSIONS.templateManage);
  const templateId = Number(params.templateId);
  const versionId = Number(params.versionId);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [template, setTemplate] =
    useState<ContractTemplateDetailResponse | null>(null);
  const [version, setVersion] =
    useState<ContractTemplateVersionDetailResponse | null>(null);
  const [activeTab, setActiveTab] = useState<WorkspaceTab>("overview");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [isDownloadingDocx, setIsDownloadingDocx] = useState(false);
  const [isOpeningPdf, setIsOpeningPdf] = useState(false);
  const [isPublishOpen, setIsPublishOpen] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [isRetireOpen, setIsRetireOpen] = useState(false);
  const [isRetiring, setIsRetiring] = useState(false);

  const fetchWorkspace = useCallback(
    async (showLoading = true) => {
      if (
        !canManage ||
        !Number.isInteger(templateId) ||
        !Number.isInteger(versionId)
      ) {
        setIsLoading(false);
        return;
      }
      try {
        if (showLoading) setIsLoading(true);
        else setIsRefreshing(true);
        const [templateResponse, versionResponse] = await Promise.all([
          contractTemplateApi.getById(templateId),
          contractTemplateApi.getVersion(versionId),
        ]);
        if (versionResponse.templateId !== templateId) {
          throw new Error("Phiên bản không thuộc mẫu hợp đồng này.");
        }
        setTemplate(templateResponse);
        setVersion(versionResponse);
      } catch (error) {
        toast.error(
          getContractTemplateErrorMessage(
            error,
            "Không thể tải workspace mẫu hợp đồng.",
          ),
        );
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [canManage, templateId, versionId],
  );

  useEffect(() => {
    const hash = window.location.hash.replace("#", "");
    if (isWorkspaceTab(hash)) setActiveTab(hash);
    void fetchWorkspace();
  }, [fetchWorkspace]);

  const changeTab = (value: string) => {
    if (!isWorkspaceTab(value)) return;
    setActiveTab(value);
    window.history.replaceState(
      null,
      "",
      `${window.location.pathname}${window.location.search}#${value}`,
    );
  };

  const selectDocument = (file: File | null) => {
    if (!file) {
      setSelectedFile(null);
      return;
    }
    if (
      !file.name.toLocaleLowerCase().endsWith(".docx") &&
      file.type !== DOCX_MIME
    ) {
      toast.error("Chỉ chấp nhận file DOCX.");
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }
    if (file.size > MAX_DOCX_SIZE) {
      toast.error("File DOCX không được vượt quá 10 MB.");
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }
    setSelectedFile(file);
  };

  const uploadDocument = async () => {
    if (!version || !selectedFile) return;
    try {
      setIsUploading(true);
      const result = await contractTemplateApi.uploadDocument(
        version.templateVersionId,
        selectedFile,
        version.rowVersion,
      );
      setVersion(result);
      setSelectedFile(null);
      if (fileInputRef.current) fileInputRef.current.value = "";
      if (result.validationStatus === TemplateValidationStatus.Valid) {
        toast.success("Đã upload và kiểm tra DOCX thành công.");
      } else {
        toast.warning(
          "Đã upload DOCX nhưng tài liệu chưa hợp lệ. Vui lòng xem kết quả kiểm tra.",
        );
      }
      await fetchWorkspace(false);
    } catch (error) {
      toast.error(
        getContractTemplateErrorMessage(
          error,
          "Không thể upload tài liệu DOCX.",
        ),
      );
      await fetchWorkspace(false);
    } finally {
      setIsUploading(false);
    }
  };

  const generatePreview = async () => {
    if (!version) return;
    try {
      setIsGenerating(true);
      const result = await contractTemplateApi.generatePreview(
        version.templateVersionId,
        {
          versionRowVersion: version.rowVersion,
        },
      );
      toast.success(
        result.isReused
          ? "Preview hiện tại vẫn còn hợp lệ."
          : "Đã tạo preview mới.",
      );
      await fetchWorkspace(false);
    } catch (error) {
      toast.error(
        getContractTemplateErrorMessage(error, "Không thể tạo preview."),
      );
      await fetchWorkspace(false);
    } finally {
      setIsGenerating(false);
    }
  };

  const downloadPreview = async () => {
    if (!version) return;
    try {
      setIsDownloadingDocx(true);
      const blob = await contractTemplateApi.downloadPreview(
        version.templateVersionId,
      );
      downloadBlob(
        blob,
        `${version.templateCode}-v${version.versionNo}-preview.docx`,
      );
    } catch (error) {
      toast.error(
        getContractTemplateErrorMessage(error, "Không thể tải preview DOCX."),
      );
    } finally {
      setIsDownloadingDocx(false);
    }
  };

  const openPublishedPdf = async () => {
    if (!version || isOpeningPdf) return;

    const previewWindow = window.open("about:blank", "_blank");
    if (!previewWindow) {
      toast.error(
        "Trình duyệt đã chặn tab xem trước. Vui lòng cho phép cửa sổ bật lên.",
      );
      return;
    }

    previewWindow.opener = null;
    previewWindow.document.title = "Đang tải PDF phát hành...";
    previewWindow.document.body.textContent = "Đang tải bản PDF phát hành...";

    try {
      setIsOpeningPdf(true);
      const blob = await contractTemplateApi.downloadPublishedPreviewPdf(
        version.templateVersionId,
      );
      const pdfUrl = URL.createObjectURL(
        blob.type === "application/pdf"
          ? blob
          : new Blob([blob], { type: "application/pdf" }),
      );
      previewWindow.location.replace(pdfUrl);
      window.setTimeout(() => URL.revokeObjectURL(pdfUrl), 60_000);
    } catch (error) {
      previewWindow.close();
      toast.error(
        getContractTemplateErrorMessage(
          error,
          "Không thể mở bản PDF phát hành.",
        ),
      );
    } finally {
      setIsOpeningPdf(false);
    }
  };

  const publishVersion = async () => {
    if (!version) return;
    try {
      setIsPublishing(true);
      const result = await contractTemplateApi.publish(
        version.templateVersionId,
        {
          versionRowVersion: version.rowVersion,
        },
      );
      setVersion(result);
      setIsPublishOpen(false);
      toast.success(`Đã phát hành Version ${result.versionNo}.`);
      await fetchWorkspace(false);
    } catch (error) {
      toast.error(
        getContractTemplateErrorMessage(
          error,
          "Không thể phát hành phiên bản.",
        ),
      );
      await fetchWorkspace(false);
    } finally {
      setIsPublishing(false);
    }
  };

  const retireVersion = async () => {
    if (!version) return;
    try {
      setIsRetiring(true);
      const result = await contractTemplateApi.retire(
        version.templateVersionId,
        {
          versionRowVersion: version.rowVersion,
        },
      );
      setVersion(result);
      setIsRetireOpen(false);
      toast.success(`Đã ngừng sử dụng Version ${result.versionNo}.`);
      await fetchWorkspace(false);
    } catch (error) {
      toast.error(
        getContractTemplateErrorMessage(
          error,
          "Không thể ngừng sử dụng phiên bản.",
        ),
      );
      await fetchWorkspace(false);
    } finally {
      setIsRetiring(false);
    }
  };

  const isDraft = version?.status === TemplateVersionStatus.Draft;
  const hasDocument = Boolean(version?.documentFileId);
  const isDocumentValid =
    version?.validationStatus === TemplateValidationStatus.Valid;
  const hasPreview = Boolean(version?.previewFileId);
  const hasTerms = Boolean(version?.terms.length);
  const canGeneratePreview = Boolean(isDraft && hasDocument && isDocumentValid);
  const canPublish = Boolean(canGeneratePreview && hasPreview && hasTerms);
  const templateHasDraft = Boolean(
    template?.versions.some(
      (candidate) => candidate.status === TemplateVersionStatus.Draft,
    ),
  );
  const latestRetiredVersionId = [...(template?.versions ?? [])]
    .filter(
      (candidate) => candidate.status === TemplateVersionStatus.Retired,
    )
    .sort((left, right) => right.versionNo - left.versionNo)[0]
    ?.templateVersionId;
  const canCreateDraftFromRetired = Boolean(
    version?.status === TemplateVersionStatus.Retired &&
      !template?.currentPublishedVersionId &&
      !templateHasDraft &&
      version.templateVersionId === latestRetiredVersionId,
  );
  const validationMessages = parseValidationMessages(
    version?.validationMessage,
  );

  return (
    <>
      <Header title="Workspace mẫu hợp đồng" />
      <div className="grow overflow-y-auto p-3 sm:p-6 lg:p-10">
        <div className="mx-auto space-y-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <Button asChild variant="ghost" className="-ml-3">
              <Link href={`/admin/contract-templates/${templateId}`}>
                <ArrowLeft className="size-4" /> Chi tiết mẫu
              </Link>
            </Button>
            {canManage && version && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => fetchWorkspace(false)}
                disabled={isRefreshing}
              >
                <RefreshCw
                  className={`size-4 ${isRefreshing ? "animate-spin" : ""}`}
                />{" "}
                Làm mới
              </Button>
            )}
          </div>

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
              <Skeleton className="h-32 w-full" />
              <Skeleton className="h-96 w-full" />
            </div>
          ) : !template || !version ? (
            <Alert variant="destructive">
              <AlertTitle>Không tìm thấy phiên bản mẫu hợp đồng</AlertTitle>
            </Alert>
          ) : (
            <>
              <Card className="py-0">
                <CardContent className="flex flex-col justify-between gap-5 p-5 sm:flex-row sm:items-start sm:p-6">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h1 className="text-xl font-bold sm:text-2xl">
                        {template.templateName}
                      </h1>
                      <Badge variant="outline" className="font-mono">
                        {template.templateCode}
                      </Badge>
                    </div>
                    <div className="mt-3 flex flex-wrap items-center gap-2">
                      <span className="font-semibold">
                        Version {version.versionNo}
                      </span>
                      <TemplateVersionStatusBadge status={version.status} />
                      <TemplateValidationStatusBadge
                        status={version.validationStatus}
                      />
                      {template.currentPublishedVersionId ===
                        version.templateVersionId && <Badge>Hiện hành</Badge>}
                    </div>
                    <p className="mt-2 text-sm text-muted-foreground">
                      {version.changeNote || "Không có ghi chú thay đổi"}
                    </p>
                  </div>
                  <div className="text-sm text-muted-foreground sm:text-right">
                    <p>Tạo lúc {formatDateTime(version.createdDate)}</p>
                    {version.updatedDate && (
                      <p className="mt-1">
                        Cập nhật {formatDateTime(version.updatedDate)}
                      </p>
                    )}
                  </div>
                </CardContent>
              </Card>

              <Tabs
                value={activeTab}
                onValueChange={changeTab}
                className="gap-4"
              >
                <div className="overflow-x-auto pb-1">
                  <TabsList className="h-auto min-w-max">
                    <TabsTrigger value="overview">
                      <Info /> Tổng quan
                    </TabsTrigger>
                    <TabsTrigger value="terms">
                      <ListChecks /> Điều khoản
                    </TabsTrigger>
                    <TabsTrigger value="document">
                      <FileText /> Tài liệu DOCX
                    </TabsTrigger>
                    <TabsTrigger value="placeholders">
                      <Braces /> Placeholder
                    </TabsTrigger>
                    <TabsTrigger value="preview">
                      <Eye /> Preview & phát hành
                    </TabsTrigger>
                  </TabsList>
                </div>

                <TabsContent value="overview" className="space-y-4">
                  <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
                    <Card>
                      <CardHeader className="pb-2">
                        <CardTitle className="text-sm text-muted-foreground">
                          Điều khoản mềm
                        </CardTitle>
                      </CardHeader>
                      <CardContent className="text-2xl font-bold">
                        {version.terms.length}
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader className="pb-2">
                        <CardTitle className="text-sm text-muted-foreground">
                          Tài liệu nguồn
                        </CardTitle>
                      </CardHeader>
                      <CardContent className="font-semibold">
                        {hasDocument ? "Đã upload" : "Chưa có"}
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader className="pb-2">
                        <CardTitle className="text-sm text-muted-foreground">
                          Preview DOCX
                        </CardTitle>
                      </CardHeader>
                      <CardContent className="font-semibold">
                        {hasPreview ? "Đã tạo" : "Chưa có"}
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader className="pb-2">
                        <CardTitle className="text-sm text-muted-foreground">
                          PDF phát hành
                        </CardTitle>
                      </CardHeader>
                      <CardContent className="font-semibold">
                        {version.publishedPreviewPdfFileId
                          ? "Sẵn sàng"
                          : "Chưa có"}
                      </CardContent>
                    </Card>
                  </div>
                  <Alert>
                    <Info />
                    <AlertTitle>Quy trình đề xuất</AlertTitle>
                    <AlertDescription>
                      Hoàn thiện điều khoản → chèn placeholder vào DOCX → upload
                      và sửa lỗi validation → tạo preview → phát hành. Chỉ bản
                      nháp được chỉnh sửa.
                    </AlertDescription>
                  </Alert>
                </TabsContent>

                <TabsContent value="terms">
                  <ContractTemplateTermsEditor
                    key={`${version.templateVersionId}-${version.status}-${version.rowVersion}`}
                    version={version}
                    isBilingual={
                      template.languageMode === ContractLanguageMode.Bilingual
                    }
                    onRefresh={() => fetchWorkspace(false)}
                  />
                </TabsContent>

                <TabsContent value="document" className="space-y-4">
                  <Card>
                    <CardHeader>
                      <CardTitle className="flex items-center gap-2 text-lg">
                        <Upload className="size-5 text-primary" /> Tài liệu
                        nguồn DOCX
                      </CardTitle>
                      <p className="text-sm text-muted-foreground">
                        File tối đa 10 MB. Upload mới sẽ làm preview cũ mất hiệu
                        lực.
                      </p>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <div className="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-end">
                        <div className="space-y-2">
                          <Label htmlFor="template-docx">Chọn file DOCX</Label>
                          <input
                            ref={fileInputRef}
                            id="template-docx"
                            type="file"
                            accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                            disabled={!isDraft || isUploading}
                            onChange={(event) =>
                              selectDocument(event.target.files?.[0] ?? null)
                            }
                            className="block w-full rounded-md border bg-background px-3 py-2 text-sm file:mr-3 file:rounded-md file:border-0 file:bg-primary/10 file:px-3 file:py-1 file:text-primary disabled:cursor-not-allowed disabled:opacity-50"
                          />
                          {selectedFile && (
                            <p className="text-xs text-muted-foreground">
                              {selectedFile.name} ·{" "}
                              {(selectedFile.size / 1024 / 1024).toFixed(2)} MB
                            </p>
                          )}
                          <Button
                            onClick={uploadDocument}
                            disabled={!isDraft || !selectedFile || isUploading}
                          >
                            {isUploading ? (
                              <Loader2 className="size-4 animate-spin" />
                            ) : (
                              <Upload className="size-4" />
                            )}{" "}
                            Upload & kiểm tra
                          </Button>
                        </div>
                      </div>
                      {!isDraft && (
                        <p className="text-sm text-muted-foreground">
                          Phiên bản đã khóa; chỉ bản nháp mới được thay tài
                          liệu.
                        </p>
                      )}
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader>
                      <CardTitle className="text-lg">
                        Kết quả kiểm tra
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3">
                      <div className="flex flex-wrap items-center gap-2">
                        <TemplateValidationStatusBadge
                          status={version.validationStatus}
                        />
                        <span className="text-sm text-muted-foreground">
                          {hasDocument
                            ? `Document file #${version.documentFileId}`
                            : "Chưa upload tài liệu"}
                        </span>
                      </div>
                      {validationMessages.length > 0 && (
                        <Alert variant="destructive">
                          <XCircle />
                          <AlertTitle>DOCX chưa hợp lệ</AlertTitle>
                          <AlertDescription>
                            <ul className="list-disc space-y-1 pl-4">
                              {validationMessages.map((message) => (
                                <li key={message}>{message}</li>
                              ))}
                            </ul>
                          </AlertDescription>
                        </Alert>
                      )}
                      {isDocumentValid && (
                        <Alert className="border-emerald-200 dark:border-emerald-900">
                          <CheckCircle2 className="text-emerald-600" />
                          <AlertTitle>Tài liệu hợp lệ</AlertTitle>
                          <AlertDescription>
                            Có thể tạo preview từ tài liệu này.
                          </AlertDescription>
                        </Alert>
                      )}
                    </CardContent>
                  </Card>
                </TabsContent>

                <TabsContent value="placeholders">
                  <ContractTemplatePlaceholderCatalog />
                </TabsContent>

                <TabsContent
                  value="preview"
                  className="grid gap-4 lg:grid-cols-[1fr_360px]"
                >
                  <Card>
                    <CardHeader>
                      <CardTitle className="flex items-center gap-2 text-lg">
                        <Eye className="size-5 text-primary" /> Preview và bản
                        phát hành
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-5">
                      <div className="flex flex-wrap gap-2">
                        {isDraft && (
                          <Button
                            onClick={generatePreview}
                            disabled={!canGeneratePreview || isGenerating}
                          >
                            {isGenerating ? (
                              <Loader2 className="size-4 animate-spin" />
                            ) : (
                              <RefreshCw className="size-4" />
                            )}{" "}
                            Tạo preview
                          </Button>
                        )}
                        {hasPreview && (
                          <Button
                            variant="outline"
                            onClick={downloadPreview}
                            disabled={isDownloadingDocx}
                          >
                            {isDownloadingDocx ? (
                              <Loader2 className="size-4 animate-spin" />
                            ) : (
                              <Download className="size-4" />
                            )}{" "}
                            Tải preview DOCX
                          </Button>
                        )}
                        {version.publishedPreviewPdfFileId && (
                          <Button
                            variant="outline"
                            onClick={openPublishedPdf}
                            disabled={isOpeningPdf}
                          >
                            {isOpeningPdf ? (
                              <Loader2 className="size-4 animate-spin" />
                            ) : (
                              <Eye className="size-4" />
                            )}{" "}
                            Xem PDF phát hành
                          </Button>
                        )}
                      </div>
                      <dl className="grid gap-4 text-sm sm:grid-cols-2">
                        <div>
                          <dt className="text-muted-foreground">
                            Preview gần nhất
                          </dt>
                          <dd className="mt-1 font-medium">
                            {version.previewedAt
                              ? formatDateTime(version.previewedAt)
                              : "Chưa tạo"}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-muted-foreground">
                            Người tạo preview
                          </dt>
                          <dd className="mt-1 font-medium">
                            {version.previewedByEmployeeId
                              ? `Nhân viên #${version.previewedByEmployeeId}`
                              : "—"}
                          </dd>
                        </div>
                      </dl>
                      {isDraft && !canGeneratePreview && (
                        <Alert>
                          <Info />
                          <AlertTitle>Chưa thể tạo preview</AlertTitle>
                          <AlertDescription>
                            Cần upload DOCX và đạt validation hợp lệ trước.
                          </AlertDescription>
                        </Alert>
                      )}
                    </CardContent>
                  </Card>

                  <Card className="h-fit">
                    <CardHeader>
                      <CardTitle className="text-lg">Phát hành</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-5">
                      <ul className="space-y-2">
                        <RequirementRow met={hasTerms}>
                          Có ít nhất một điều khoản mềm
                        </RequirementRow>
                        <RequirementRow met={hasDocument}>
                          Đã upload DOCX
                        </RequirementRow>
                        <RequirementRow met={isDocumentValid}>
                          DOCX hợp lệ
                        </RequirementRow>
                        <RequirementRow met={hasPreview}>
                          Đã tạo preview hiện hành
                        </RequirementRow>
                      </ul>
                      {version.status === TemplateVersionStatus.Draft && (
                        <Button
                          className="w-full"
                          onClick={() => setIsPublishOpen(true)}
                          disabled={!canPublish}
                        >
                          <Send className="size-4" /> Phát hành version
                        </Button>
                      )}
                      {version.status === TemplateVersionStatus.Published && (
                        <Button
                          className="w-full"
                          variant="destructive"
                          onClick={() => setIsRetireOpen(true)}
                        >
                          <Archive className="size-4" /> Ngừng sử dụng
                        </Button>
                      )}
                      {version.status === TemplateVersionStatus.Retired && (
                        <div className="space-y-3">
                          <p className="text-sm text-muted-foreground">
                            Version này đã ngừng sử dụng, vẫn bất biến và chỉ
                            còn chế độ xem.
                          </p>
                          {canCreateDraftFromRetired && (
                            <Button asChild className="w-full" variant="outline">
                              <Link href={`/admin/contract-templates/${templateId}`}>
                                Quản lý và tạo bản nháp mới
                              </Link>
                            </Button>
                          )}
                        </div>
                      )}
                      <p className="text-xs text-muted-foreground">
                        Khi phát hành, version đang hiện hành trước đó của mẫu
                        sẽ tự chuyển sang ngừng sử dụng.
                      </p>
                    </CardContent>
                  </Card>
                </TabsContent>
              </Tabs>
            </>
          )}
        </div>
      </div>

      <ConfirmDialog
        isOpen={isPublishOpen}
        onClose={() => setIsPublishOpen(false)}
        onConfirm={publishVersion}
        title="Phát hành phiên bản mẫu?"
        description="Phiên bản này sẽ trở thành bản hiện hành. Bản đang hiện hành trước đó (nếu có) sẽ tự ngừng sử dụng."
        confirmText="Phát hành"
        isLoading={isPublishing}
      />
      <ConfirmDialog
        isOpen={isRetireOpen}
        onClose={() => setIsRetireOpen(false)}
        onConfirm={retireVersion}
        title="Ngừng sử dụng phiên bản?"
        description="Phiên bản sẽ không còn là bản hiện hành và không thể chỉnh sửa lại. Sau đó có thể sao chép bản Retired này thành một Draft mới nếu cần tiếp tục sử dụng mẫu."
        confirmText="Ngừng sử dụng"
        variant="destructive"
        isLoading={isRetiring}
      />
    </>
  );
}
