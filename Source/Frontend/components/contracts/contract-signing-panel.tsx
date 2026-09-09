"use client";

import { ChangeEvent, useCallback, useEffect, useState } from "react";
import {
  CheckCircle2,
  Download,
  Eye,
  FileCheck2,
  FileClock,
  FileUp,
  Loader2,
  RefreshCw,
  ShieldCheck,
} from "lucide-react";

import { downloadBlob } from "@/components/contract-templates/contract-template-utils";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { toast } from "@/components/ui/sonner";
import {
  getApiErrorMessage,
  getBlobApiErrorMessage,
  isStaleRowVersion,
} from "@/lib/api-error";
import { formatDateTime } from "@/lib/format-date-time";
import {
  ContractDetailResponse,
  ContractStatus,
} from "@/services/contract-api";
import {
  contractSigningApi,
  ContractSignedEvidenceResponse,
  ContractSigningDetailResponse,
  SignedEvidenceStatus,
} from "@/services/contract-signing-api";

const MAX_FILE_SIZE = 20 * 1024 * 1024;
const ACCEPTED_EXTENSIONS = ["pdf", "jpg", "jpeg", "png"];

const getExtension = (fileName: string) =>
  fileName.split(".").pop()?.toLowerCase() ?? "";

const formatFileSize = (size: number) => {
  if (size < 1024 * 1024) return `${Math.ceil(size / 1024)} KB`;
  return `${(size / (1024 * 1024)).toFixed(1)} MB`;
};

interface ContractSigningPanelProps {
  contract: ContractDetailResponse;
  canManageSigning: boolean;
  onContractRefetch: () => void | Promise<void>;
}

export function ContractSigningPanel({
  contract,
  canManageSigning,
  onContractRefetch,
}: ContractSigningPanelProps) {
  const [detail, setDetail] = useState<ContractSigningDetailResponse | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [supersedeReason, setSupersedeReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [downloadingFileId, setDownloadingFileId] = useState<number | null>(
    null,
  );

  const loadDetail = useCallback(async () => {
    try {
      setIsLoading(true);
      setLoadError(null);
      const response = await contractSigningApi.get(contract.contractId);
      setDetail(response);
    } catch (error) {
      setLoadError(
        getApiErrorMessage(error, "Không thể tải hồ sơ ký hợp đồng."),
      );
    } finally {
      setIsLoading(false);
    }
  }, [contract.contractId]);

  useEffect(() => {
    void loadDetail();
  }, [loadDetail, contract.status]);

  const isInitialUpload =
    detail?.contractStatus === ContractStatus.PendingSignature &&
    !detail.activeEvidence;
  const isSupersede =
    detail?.contractStatus === ContractStatus.Signed && !!detail.activeEvidence;
  const canSubmit = canManageSigning && (isInitialUpload || isSupersede);

  const selectFile = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null;
    event.target.value = "";
    if (!file) return;
    if (!ACCEPTED_EXTENSIONS.includes(getExtension(file.name))) {
      toast.error("Chỉ chấp nhận PDF, JPG, JPEG hoặc PNG.");
      return;
    }
    if (file.size <= 0 || file.size > MAX_FILE_SIZE) {
      toast.error("File phải có nội dung và không vượt quá 20 MB.");
      return;
    }
    setSelectedFile(file);
  };

  const submit = async () => {
    if (!detail || !selectedFile || !canSubmit) return;
    if (isSupersede && !supersedeReason.trim()) {
      toast.error("Vui lòng nhập lý do thay bản scan.");
      return;
    }

    const filePayload = {
      file: selectedFile,
      currentVersionId: detail.versionId,
      contractRowVersion: detail.contractRowVersion,
      versionRowVersion: detail.versionRowVersion,
    };

    try {
      setIsSubmitting(true);
      if (isSupersede && detail.activeEvidence) {
        await contractSigningApi.supersede(
          contract.contractId,
          detail.activeEvidence.signedEvidenceId,
          {
            ...filePayload,
            evidenceRowVersion: detail.activeEvidence.rowVersion,
            reason: supersedeReason.trim(),
          },
        );
        toast.success("Đã thay bản scan và giữ lại bản cũ trong lịch sử.");
      } else {
        await contractSigningApi.upload(contract.contractId, filePayload);
        toast.success("Đã lưu bản scan. Hợp đồng đã chuyển sang Đã ký.");
      }
      setSelectedFile(null);
      setSupersedeReason("");
      await Promise.all([loadDetail(), Promise.resolve(onContractRefetch())]);
    } catch (error) {
      if (isStaleRowVersion(error)) {
        await Promise.all([loadDetail(), Promise.resolve(onContractRefetch())]);
        toast.error(
          "Dữ liệu đã thay đổi. Trang đã tải lại phiên bản mới nhất.",
        );
      } else {
        toast.error(
          getApiErrorMessage(error, "Không thể lưu bản scan hợp đồng ký."),
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const download = async (
    fileId: number,
    fileName: string,
    previewPdf: boolean,
  ) => {
    const previewWindow = previewPdf
      ? window.open("about:blank", "_blank")
      : null;
    try {
      setDownloadingFileId(fileId);
      const blob = await contractSigningApi.downloadFile(fileId);
      if (previewPdf && previewWindow) {
        const url = URL.createObjectURL(
          blob.type === "application/pdf"
            ? blob
            : new Blob([blob], { type: "application/pdf" }),
        );
        previewWindow.opener = null;
        previewWindow.location.replace(url);
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      } else {
        previewWindow?.close();
        downloadBlob(blob, fileName);
      }
    } catch (error) {
      previewWindow?.close();
      toast.error(
        await getBlobApiErrorMessage(error, "Không thể tải tệp hợp đồng."),
      );
    } finally {
      setDownloadingFileId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="flex min-h-48 items-center justify-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="size-5 animate-spin text-primary" />
        Đang tải hồ sơ ký...
      </div>
    );
  }

  if (loadError || !detail) {
    return (
      <Alert variant="destructive">
        <AlertTitle>Không thể tải hồ sơ ký</AlertTitle>
        <AlertDescription className="mt-2 space-y-3">
          <p>{loadError}</p>
          <Button variant="outline" onClick={() => void loadDetail()}>
            <RefreshCw className="size-4" /> Thử lại
          </Button>
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-6">
      <Alert className="border-blue-500/30 bg-blue-500/5">
        <ShieldCheck className="size-4 text-blue-600" />
        <AlertTitle>Ký giấy và lưu bằng chứng</AlertTitle>
        <AlertDescription>
          Tải một file scan đã có đủ chữ ký hai bên. Hệ thống không thực hiện ký
          điện tử hoặc ký bằng OTP trong luồng này.
        </AlertDescription>
      </Alert>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileCheck2 className="size-5 text-primary" />
              Artifact Version {detail.versionNo} đã duyệt
            </CardTitle>
          </CardHeader>
          <CardContent>
            {detail.approvedArtifacts.length === 0 ? (
              <Alert variant="destructive">
                <AlertTitle>Chưa có artifact đã duyệt hợp lệ</AlertTitle>
                <AlertDescription>
                  Không thể tải bản scan cho đến khi DOCX/PDF bất biến của
                  version đã được Manager duyệt đầy đủ.
                </AlertDescription>
              </Alert>
            ) : (
              <div className="grid gap-3 sm:grid-cols-2">
                {detail.approvedArtifacts.map((artifact) => {
                  const isPdf = artifact.fileType.toLowerCase() === "pdf";
                  return (
                    <Button
                      key={artifact.fileId}
                      variant="outline"
                      className="h-auto justify-start p-4"
                      disabled={downloadingFileId === artifact.fileId}
                      onClick={() =>
                        void download(artifact.fileId, artifact.fileName, isPdf)
                      }
                    >
                      {downloadingFileId === artifact.fileId ? (
                        <Loader2 className="size-5 animate-spin" />
                      ) : isPdf ? (
                        <Eye className="size-5" />
                      ) : (
                        <Download className="size-5" />
                      )}
                      <span className="min-w-0 text-left">
                        <span className="block truncate font-medium">
                          {artifact.fileName}
                        </span>
                        <span className="block text-xs text-muted-foreground">
                          {formatFileSize(artifact.fileSize)}
                        </span>
                      </span>
                    </Button>
                  );
                })}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <CheckCircle2 className="size-5 text-emerald-600" />
              Bản scan đang hiệu lực
            </CardTitle>
          </CardHeader>
          <CardContent>
            {detail.activeEvidence ? (
              <EvidenceCard
                evidence={detail.activeEvidence}
                downloading={downloadingFileId === detail.activeEvidence.fileId}
                onDownload={download}
              />
            ) : (
              <p className="rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
                Chưa có bản scan hợp đồng đã ký.
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      {canSubmit && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileUp className="size-5 text-primary" />
              {isSupersede
                ? "Thay bản scan đang hiệu lực"
                : "Tải bản scan đã ký"}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            {isSupersede && (
              <div className="space-y-2">
                <Label htmlFor="signed-evidence-reason">
                  Lý do thay bản scan{" "}
                  <span className="text-destructive">*</span>
                </Label>
                <Textarea
                  id="signed-evidence-reason"
                  value={supersedeReason}
                  maxLength={1000}
                  disabled={isSubmitting}
                  placeholder="Ví dụ: bản trước bị thiếu trang có chữ ký..."
                  onChange={(event) => setSupersedeReason(event.target.value)}
                />
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="signed-evidence-file">
                File scan <span className="text-destructive">*</span>
              </Label>
              <Input
                id="signed-evidence-file"
                type="file"
                accept=".pdf,.jpg,.jpeg,.png"
                disabled={isSubmitting}
                onChange={selectFile}
              />
              <p className="text-xs text-muted-foreground">
                PDF, JPG, JPEG hoặc PNG · Tối đa 20 MB
              </p>
              {selectedFile && (
                <p className="text-sm font-medium">
                  {selectedFile.name} · {formatFileSize(selectedFile.size)}
                </p>
              )}
            </div>

            <Button
              disabled={isSubmitting || !selectedFile}
              onClick={() => void submit()}
            >
              {isSubmitting ? (
                <Loader2 className="size-4 animate-spin" />
              ) : (
                <FileUp className="size-4" />
              )}
              {isSupersede ? "Lưu bản thay thế" : "Lưu và đánh dấu Đã ký"}
            </Button>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileClock className="size-5 text-primary" />
            Lịch sử bằng chứng ký
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {detail.evidenceHistory.length === 0 ? (
            <p className="py-6 text-center text-sm text-muted-foreground">
              Chưa có lịch sử bản scan.
            </p>
          ) : (
            detail.evidenceHistory.map((evidence) => (
              <EvidenceCard
                key={evidence.signedEvidenceId}
                evidence={evidence}
                downloading={downloadingFileId === evidence.fileId}
                onDownload={download}
              />
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function EvidenceCard({
  evidence,
  downloading,
  onDownload,
}: {
  evidence: ContractSignedEvidenceResponse;
  downloading: boolean;
  onDownload: (
    fileId: number,
    fileName: string,
    previewPdf: boolean,
  ) => Promise<void>;
}) {
  const isActive = evidence.status === SignedEvidenceStatus.Active;
  const isPdf = evidence.fileType.toLowerCase() === "pdf";
  return (
    <div className="rounded-xl border p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <p className="truncate font-semibold">{evidence.fileName}</p>
            <Badge variant={isActive ? "default" : "secondary"}>
              {isActive ? "Đang hiệu lực" : "Đã thay thế"}
            </Badge>
            <Badge variant="outline">Version {evidence.versionNo}</Badge>
          </div>
          <p className="mt-1 text-xs text-muted-foreground">
            {formatFileSize(evidence.fileSize)} · Tải bởi{" "}
            {evidence.uploadedByEmployeeName ||
              `Nhân viên #${evidence.uploadedByEmployeeId}`}{" "}
            lúc {formatDateTime(evidence.uploadedAt)}
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          disabled={downloading}
          onClick={() =>
            void onDownload(evidence.fileId, evidence.fileName, isPdf)
          }
        >
          {downloading ? (
            <Loader2 className="size-4 animate-spin" />
          ) : isPdf ? (
            <Eye className="size-4" />
          ) : (
            <Download className="size-4" />
          )}
          {isPdf ? "Xem" : "Tải"}
        </Button>
      </div>
      {evidence.supersedeReason && (
        <p className="mt-3 rounded-lg border border-amber-500/20 bg-amber-500/5 p-3 text-sm">
          <span className="font-medium">Lý do thay: </span>
          {evidence.supersedeReason}
        </p>
      )}
    </div>
  );
}
