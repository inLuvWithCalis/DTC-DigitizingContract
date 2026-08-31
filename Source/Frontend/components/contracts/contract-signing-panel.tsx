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
import { DateFilter } from "@/components/ui/custom/date-filter";
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

type SigningForm = {
  providerSignerName: string;
  providerSignerTitle: string;
  providerSigningDate: string;
  customerSignerName: string;
  customerSignerTitle: string;
  customerSigningDate: string;
  reason: string;
};

const EMPTY_FORM: SigningForm = {
  providerSignerName: "",
  providerSignerTitle: "",
  providerSigningDate: "",
  customerSignerName: "",
  customerSignerTitle: "",
  customerSigningDate: "",
  reason: "",
};

const getExtension = (fileName: string) =>
  fileName.split(".").pop()?.toLowerCase() ?? "";

const formatFileSize = (size: number) => {
  if (size < 1024 * 1024) return `${Math.ceil(size / 1024)} KB`;
  return `${(size / (1024 * 1024)).toFixed(1)} MB`;
};

const dateInputValue = (value: string) => value.slice(0, 10);

const parseDateInputValue = (value: string) => {
  if (!value) return undefined;

  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) return undefined;

  return new Date(year, month - 1, day);
};

const toDateInputValue = (value: Date | undefined) => {
  if (!value) return "";

  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};

interface ContractSigningPanelProps {
  contract: ContractDetailResponse;
  canManage: boolean;
  onContractRefetch: () => void | Promise<void>;
}

export function ContractSigningPanel({
  contract,
  canManage,
  onContractRefetch,
}: ContractSigningPanelProps) {
  const [detail, setDetail] = useState<ContractSigningDetailResponse | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [form, setForm] = useState<SigningForm>(EMPTY_FORM);
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
      if (response.activeEvidence) {
        const active = response.activeEvidence;
        setForm((current) => ({
          ...current,
          providerSignerName: active.providerSignerName,
          providerSignerTitle: active.providerSignerTitle,
          providerSigningDate: dateInputValue(active.providerSigningDate),
          customerSignerName: active.customerSignerName,
          customerSignerTitle: active.customerSignerTitle,
          customerSigningDate: dateInputValue(active.customerSigningDate),
        }));
      }
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
  const canSubmit = canManage && (isInitialUpload || isSupersede);

  const updateForm = (field: keyof SigningForm, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
  };

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
    const requiredValues = [
      form.providerSignerName,
      form.providerSignerTitle,
      form.providerSigningDate,
      form.customerSignerName,
      form.customerSignerTitle,
      form.customerSigningDate,
    ];
    if (requiredValues.some((value) => !value.trim())) {
      toast.error("Vui lòng nhập đủ thông tin người ký của hai bên.");
      return;
    }
    if (isSupersede && !form.reason.trim()) {
      toast.error("Vui lòng nhập lý do thay bản scan.");
      return;
    }

    const payload = {
      file: selectedFile,
      currentVersionId: detail.versionId,
      contractRowVersion: detail.contractRowVersion,
      versionRowVersion: detail.versionRowVersion,
      providerSignerName: form.providerSignerName.trim(),
      providerSignerTitle: form.providerSignerTitle.trim(),
      providerSigningDate: form.providerSigningDate,
      customerSignerName: form.customerSignerName.trim(),
      customerSignerTitle: form.customerSignerTitle.trim(),
      customerSigningDate: form.customerSigningDate,
    };

    try {
      setIsSubmitting(true);
      if (isSupersede && detail.activeEvidence) {
        await contractSigningApi.supersede(
          contract.contractId,
          detail.activeEvidence.signedEvidenceId,
          {
            ...payload,
            evidenceRowVersion: detail.activeEvidence.rowVersion,
            reason: form.reason.trim(),
          },
        );
        toast.success("Đã thay bản scan và giữ lại bản cũ trong lịch sử.");
      } else {
        await contractSigningApi.upload(contract.contractId, payload);
        toast.success("Đã lưu bản scan. Hợp đồng đã chuyển sang Đã ký.");
      }
      setSelectedFile(null);
      setForm((current) => ({ ...current, reason: "" }));
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
            <div className="grid gap-4 lg:grid-cols-2">
              <SignerFields
                prefix="provider"
                title="Nhà cung cấp"
                name={form.providerSignerName}
                signerTitle={form.providerSignerTitle}
                date={form.providerSigningDate}
                disabled={isSubmitting}
                onChange={updateForm}
              />
              <SignerFields
                prefix="customer"
                title="Khách hàng"
                name={form.customerSignerName}
                signerTitle={form.customerSignerTitle}
                date={form.customerSigningDate}
                disabled={isSubmitting}
                onChange={updateForm}
              />
            </div>

            {isSupersede && (
              <div className="space-y-2">
                <Label htmlFor="signed-evidence-reason">
                  Lý do thay bản scan{" "}
                  <span className="text-destructive">*</span>
                </Label>
                <Textarea
                  id="signed-evidence-reason"
                  value={form.reason}
                  maxLength={1000}
                  disabled={isSubmitting}
                  placeholder="Ví dụ: bản trước bị thiếu trang có chữ ký..."
                  onChange={(event) => updateForm("reason", event.target.value)}
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

function SignerFields({
  prefix,
  title,
  name,
  signerTitle,
  date,
  disabled,
  onChange,
}: {
  prefix: "provider" | "customer";
  title: string;
  name: string;
  signerTitle: string;
  date: string;
  disabled: boolean;
  onChange: (field: keyof SigningForm, value: string) => void;
}) {
  const nameField = `${prefix}SignerName` as keyof SigningForm;
  const titleField = `${prefix}SignerTitle` as keyof SigningForm;
  const dateField = `${prefix}SigningDate` as keyof SigningForm;
  return (
    <div className="space-y-4 rounded-xl border p-4">
      <h3 className="font-semibold">Người ký phía {title}</h3>
      <div className="space-y-2">
        <Label htmlFor={`${prefix}-signer-name`}>
          Họ tên <span className="text-red-500">*</span>
        </Label>
        <Input
          id={`${prefix}-signer-name`}
          value={name}
          maxLength={200}
          disabled={disabled}
          onChange={(event) => onChange(nameField, event.target.value)}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor={`${prefix}-signer-title`}>
          Chức danh <span className="text-red-500">*</span>
        </Label>
        <Input
          id={`${prefix}-signer-title`}
          value={signerTitle}
          maxLength={200}
          disabled={disabled}
          onChange={(event) => onChange(titleField, event.target.value)}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor={`${prefix}-signing-date`}>
          Ngày ký <span className="text-red-500">*</span>
        </Label>
        <DateFilter
          id={`${prefix}-signing-date`}
          date={parseDateInputValue(date)}
          placeholder="Chọn ngày ký"
          className="flex-1"
          disabled={disabled}
          onChange={(value) => onChange(dateField, toDateInputValue(value))}
        />
      </div>
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
      <div className="mt-4 grid gap-3 text-sm md:grid-cols-2">
        <div className="rounded-lg bg-muted/50 p-3">
          <p className="font-medium">Nhà cung cấp</p>
          <p>{evidence.providerSignerName}</p>
          <p className="text-muted-foreground">
            {evidence.providerSignerTitle} ·{" "}
            {new Date(evidence.providerSigningDate).toLocaleDateString("vi-VN")}
          </p>
        </div>
        <div className="rounded-lg bg-muted/50 p-3">
          <p className="font-medium">Khách hàng</p>
          <p>{evidence.customerSignerName}</p>
          <p className="text-muted-foreground">
            {evidence.customerSignerTitle} ·{" "}
            {new Date(evidence.customerSigningDate).toLocaleDateString("vi-VN")}
          </p>
        </div>
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
