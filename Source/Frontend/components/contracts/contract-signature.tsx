"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Check,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Copy,
  History,
  KeyRound,
  Link2,
  Loader2,
  Phone,
  RefreshCw,
  ShieldAlert,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
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
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { formatDateTime } from "@/lib/format-date-time";
import {
  contractApi,
  ContractCustomerAccessLinkResponse,
  ContractCustomerVerificationPhoneResponse,
  ContractCustomerVerificationPhoneSource,
  ContractDetailResponse,
  ContractStatus,
} from "@/services/contract-api";

type LinkAction = "replace" | "revoke";
type KnownCustomerAccessLink = Pick<
  ContractCustomerAccessLinkResponse,
  "linkId" | "state" | "expiresAt"
>;

const PHONE_HISTORY_PAGE_SIZE = 5;

const PHONE_SOURCE_LABELS: Record<
  ContractCustomerVerificationPhoneSource,
  string
> = {
  CustomerMobile: "Di động của khách hàng",
  CustomerPhone: "Điện thoại của khách hàng",
  Manual: "Nhập thủ công",
};

const LINK_STATE_LABELS: Record<string, { label: string; className: string }> =
  {
    Active: {
      label: "Đang hoạt động",
      className:
        "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-400",
    },
    PendingActivation: {
      label: "Chờ bắt đầu đàm phán",
      className:
        "border-yellow-200 bg-yellow-50 text-yellow-700 dark:border-yellow-500/20 dark:bg-yellow-500/10 dark:text-yellow-400",
    },
    Expired: {
      label: "Đã hết hạn",
      className:
        "border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-500/20 dark:bg-rose-500/10 dark:text-rose-400",
    },
    Hidden: {
      label: "Đã ẩn",
      className:
        "border-slate-200 bg-slate-50 text-slate-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-400",
    },
  };

const getErrorMessage = (error: any, fallback: string) =>
  error?.response?.data?.message ||
  error?.response?.data?.error ||
  error?.message ||
  fallback;

const withFrontendOrigin = (
  response: ContractCustomerAccessLinkResponse,
): ContractCustomerAccessLinkResponse => {
  if (typeof window === "undefined" || !response.publicUrl) return response;

  try {
    const generatedUrl = new URL(response.publicUrl);
    return {
      ...response,
      publicUrl: `${window.location.origin}${generatedUrl.pathname}${generatedUrl.search}${generatedUrl.hash}`,
    };
  } catch {
    return response;
  }
};

export function ContractSignature({
  contract,
  onContractRefetch,
  knownLink,
  hasUnsavedChanges,
  canManage,
  onCustomerAccessLinkChange,
}: {
  contract: ContractDetailResponse;
  onContractRefetch: () => Promise<void>;
  knownLink: KnownCustomerAccessLink | null;
  hasUnsavedChanges: boolean;
  canManage: boolean;
  onCustomerAccessLinkChange: (link: KnownCustomerAccessLink | null) => void;
}) {
  const [phones, setPhones] = useState<
    ContractCustomerVerificationPhoneResponse[]
  >([]);
  const [phoneSource, setPhoneSource] =
    useState<ContractCustomerVerificationPhoneSource>("CustomerMobile");
  const [manualPhoneNumber, setManualPhoneNumber] = useState("");
  const [phoneReason, setPhoneReason] = useState("");
  const [isLoadingPhones, setIsLoadingPhones] = useState(true);
  const [isSavingPhone, setIsSavingPhone] = useState(false);
  const [phoneLoadError, setPhoneLoadError] = useState<string | null>(null);
  const [phoneHistoryPage, setPhoneHistoryPage] = useState(1);

  const [activeLink, setActiveLink] =
    useState<ContractCustomerAccessLinkResponse | null>(null);
  const [oneTimePublicUrl, setOneTimePublicUrl] = useState<string | null>(null);
  const [isCopied, setIsCopied] = useState(false);
  const copyTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const [isCreatingLink, setIsCreatingLink] = useState(false);
  const [isCreateLinkConfirmOpen, setIsCreateLinkConfirmOpen] = useState(false);
  const [linkAction, setLinkAction] = useState<LinkAction | null>(null);
  const [linkActionReason, setLinkActionReason] = useState("");
  const [isSubmittingLinkAction, setIsSubmittingLinkAction] = useState(false);

  useEffect(() => {
    return () => {
      if (copyTimeoutRef.current) {
        clearTimeout(copyTimeoutRef.current);
      }
    };
  }, []);

  const currentPhone = useMemo(
    () => phones.find((phone) => phone.isCurrent) ?? null,
    [phones],
  );
  const phoneHistoryTotalPages = Math.ceil(
    phones.length / PHONE_HISTORY_PAGE_SIZE,
  );
  const paginatedPhones = phones.slice(
    (phoneHistoryPage - 1) * PHONE_HISTORY_PAGE_SIZE,
    phoneHistoryPage * PHONE_HISTORY_PAGE_SIZE,
  );

  const canCreateLink =
    canManage &&
    Boolean(currentPhone) &&
    !hasUnsavedChanges &&
    (contract.status === ContractStatus.Draft ||
      contract.status === ContractStatus.Negotiating);
  const linkStateBadge =
    LINK_STATE_LABELS[activeLink?.state ?? "Hidden"] ??
    LINK_STATE_LABELS.Hidden;

  useEffect(() => {
    const shouldClearOneTimeUrl =
      !knownLink || activeLink?.linkId !== knownLink.linkId;
    setActiveLink((current) => {
      if (!knownLink) return null;
      if (current?.linkId === knownLink.linkId) return current;

      return { ...knownLink, publicUrl: "" };
    });
    if (shouldClearOneTimeUrl) setOneTimePublicUrl(null);
  }, [
    activeLink?.linkId,
    knownLink?.expiresAt,
    knownLink?.linkId,
    knownLink?.state,
  ]);

  const loadPhones = useCallback(async () => {
    try {
      setIsLoadingPhones(true);
      setPhoneLoadError(null);
      const result = await contractApi.getCustomerVerificationPhones(
        contract.contractId,
      );
      setPhones(result);

      const selected = result.find((phone) => phone.isCurrent);
      if (selected) {
        setPhoneSource(selected.phoneSource);
      }
    } catch (error: any) {
      setPhoneLoadError(
        getErrorMessage(error, "Không thể tải lịch sử số xác minh."),
      );
    } finally {
      setIsLoadingPhones(false);
    }
  }, [contract.contractId]);

  useEffect(() => {
    void loadPhones();
  }, [loadPhones]);

  useEffect(() => {
    if (phoneHistoryTotalPages > 0) {
      setPhoneHistoryPage((currentPage) =>
        Math.min(Math.max(currentPage, 1), phoneHistoryTotalPages),
      );
    }
  }, [phoneHistoryTotalPages]);

  const refetchAfterMutation = async () => {
    try {
      await onContractRefetch();
    } catch {
      toast.warning(
        "Thao tác đã hoàn tất nhưng chưa thể tải rowVersion mới. Hãy tải lại trang trước khi thao tác tiếp.",
      );
    }
  };

  const handleUpdatePhone = async () => {
    if (!canManage) return;
    if (hasUnsavedChanges) {
      toast.error("Vui lòng lưu thay đổi hợp đồng trước khi quản lý truy cập.");
      return;
    }

    const reason = phoneReason.trim();
    const manualPhone = manualPhoneNumber.trim();

    if (!reason) {
      toast.error("Vui lòng nhập lý do chọn hoặc đổi số xác minh.");
      return;
    }

    if (phoneSource === "Manual" && !manualPhone) {
      toast.error("Vui lòng nhập số điện thoại xác minh.");
      return;
    }

    try {
      setIsSavingPhone(true);
      const updatedPhone = await contractApi.updateCustomerVerificationPhone(
        contract.contractId,
        {
          phoneSource,
          manualPhoneNumber: phoneSource === "Manual" ? manualPhone : null,
          reason,
          rowVersion: contract.rowVersion,
        },
      );

      setPhoneReason("");
      setManualPhoneNumber("");
      if (
        currentPhone &&
        updatedPhone.verificationPhoneId !== currentPhone.verificationPhoneId
      ) {
        setActiveLink(null);
        setOneTimePublicUrl(null);
        onCustomerAccessLinkChange(null);
      }
      setPhoneHistoryPage(1);
      await Promise.all([loadPhones(), refetchAfterMutation()]);
      toast.success("Đã cập nhật số điện thoại xác minh.");
    } catch (error: any) {
      toast.error(
        getErrorMessage(error, "Không thể cập nhật số điện thoại xác minh."),
      );
    } finally {
      setIsSavingPhone(false);
    }
  };

  const handleCreateLink = async () => {
    if (!canManage) return;
    if (hasUnsavedChanges) {
      toast.error("Vui lòng lưu thay đổi hợp đồng trước khi tạo link.");
      return;
    }

    if (!currentPhone) {
      toast.error("Hãy chọn số điện thoại xác minh trước khi tạo link.");
      return;
    }

    try {
      setIsCreatingLink(true);
      const result = await contractApi.createCustomerAccessLink(
        contract.contractId,
        { rowVersion: contract.rowVersion },
      );
      const publicLink = withFrontendOrigin(result);
      setActiveLink(publicLink);
      setOneTimePublicUrl(publicLink.publicUrl);
      onCustomerAccessLinkChange(publicLink);
      await refetchAfterMutation();
      setIsCreateLinkConfirmOpen(false);
      toast.success("Đã tạo link truy cập khách hàng.");
    } catch (error: any) {
      toast.error(getErrorMessage(error, "Không thể tạo link truy cập."));
    } finally {
      setIsCreatingLink(false);
    }
  };

  const handleCopyLink = async () => {
    if (!oneTimePublicUrl) return;

    try {
      await navigator.clipboard.writeText(oneTimePublicUrl);
      toast.success("Đã sao chép link truy cập.");
      setIsCopied(true);
      if (copyTimeoutRef.current) {
        clearTimeout(copyTimeoutRef.current);
      }
      copyTimeoutRef.current = setTimeout(() => {
        setIsCopied(false);
      }, 5000);
    } catch {
      toast.error(
        "Không thể sao chép tự động. Vui lòng sao chép link thủ công.",
      );
    }
  };

  const handleLinkAction = async () => {
    if (!canManage) return;
    if (!activeLink || !linkAction) return;
    if (hasUnsavedChanges) {
      toast.error("Vui lòng lưu thay đổi hợp đồng trước khi quản lý link.");
      return;
    }

    const reason = linkActionReason.trim();
    if (!reason) {
      toast.error(
        linkAction === "replace"
          ? "Vui lòng nhập lý do thay link."
          : "Vui lòng nhập lý do thu hồi link.",
      );
      return;
    }

    try {
      setIsSubmittingLinkAction(true);

      if (linkAction === "replace") {
        const result = await contractApi.replaceCustomerAccessLink(
          contract.contractId,
          activeLink.linkId,
          { rowVersion: contract.rowVersion, reason },
        );
        const publicLink = withFrontendOrigin(result);
        setActiveLink(publicLink);
        setOneTimePublicUrl(publicLink.publicUrl);
        onCustomerAccessLinkChange(publicLink);
        toast.success("Đã thay link truy cập. Link cũ không còn hiệu lực.");
      } else {
        await contractApi.revokeCustomerAccessLink(
          contract.contractId,
          activeLink.linkId,
          { rowVersion: contract.rowVersion, reason },
        );
        setActiveLink(null);
        setOneTimePublicUrl(null);
        onCustomerAccessLinkChange(null);
        toast.success("Đã thu hồi link truy cập khách hàng.");
      }

      setLinkAction(null);
      setLinkActionReason("");
      await refetchAfterMutation();
    } catch (error: any) {
      toast.error(
        getErrorMessage(
          error,
          linkAction === "replace"
            ? "Không thể thay link truy cập."
            : "Không thể thu hồi link truy cập.",
        ),
      );
    } finally {
      setIsSubmittingLinkAction(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="flex items-center gap-2 text-xl font-semibold">
          <KeyRound className="size-5 text-primary" />
          Truy cập khách hàng
        </h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Thiết lập số nhận OTP và quản lý link để khách hàng xem, đàm phán hợp
          đồng.
        </p>
      </div>

      <Alert>
        <ShieldAlert className="size-4" />
        <AlertTitle>Link có thể mất hiệu lực</AlertTitle>
        <AlertDescription>
          Đổi số xác minh, tạo vòng đàm phán mới hoặc hủy hợp đồng sẽ làm link
          hiện tại, OTP đang chờ và phiên truy cập của khách hàng mất hiệu lực.
        </AlertDescription>
      </Alert>

      {hasUnsavedChanges && (
        <Alert className="border-amber-300 bg-amber-50/60 dark:bg-amber-950/20">
          <ShieldAlert className="size-4 text-amber-700" />
          <AlertTitle>Hợp đồng có thay đổi chưa được lưu</AlertTitle>
          <AlertDescription>
            Hãy lưu nội dung hợp đồng trước khi chọn số, tạo, thay hoặc thu hồi
            link khách hàng.
          </AlertDescription>
        </Alert>
      )}

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(22rem,0.9fr)]">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Phone className="size-5 text-primary" />
              Số điện thoại xác minh
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            {currentPhone ? (
              <div className="flex flex-col gap-3 rounded-xl border bg-muted/30 p-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <div className="flex items-center gap-2">
                    <p className="font-mono text-lg font-semibold">
                      {currentPhone.maskedPhoneNumber}
                    </p>
                    <Badge>
                      <CheckCircle2 /> Đang sử dụng
                    </Badge>
                  </div>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {PHONE_SOURCE_LABELS[currentPhone.phoneSource]} · Chọn lúc{" "}
                    {formatDateTime(currentPhone.createdDate)}
                  </p>
                </div>
              </div>
            ) : (
              <Alert>
                <AlertTitle>Chưa chọn số xác minh</AlertTitle>
                <AlertDescription>
                  Chọn một nguồn số bên dưới trước khi tạo link cho khách hàng.
                </AlertDescription>
              </Alert>
            )}

            <div
              className={`grid gap-4 ${phoneSource === "Manual" ? "sm:grid-cols-2" : "sm:grid-cols-1"}`}
            >
              <div className="space-y-2">
                <Label htmlFor="verification-phone-source">Nguồn số</Label>
                <Select
                  value={phoneSource}
                  onValueChange={(value) =>
                    setPhoneSource(
                      value as ContractCustomerVerificationPhoneSource,
                    )
                  }
                  disabled={
                    !canManage ||
                    isSavingPhone ||
                    hasUnsavedChanges ||
                    contract.status === ContractStatus.Cancelled
                  }
                >
                  <SelectTrigger
                    id="verification-phone-source"
                    className="w-full"
                  >
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent showSearch={false}>
                    <SelectItem value="CustomerMobile">
                      Di động của khách hàng
                    </SelectItem>
                    <SelectItem value="CustomerPhone">
                      Điện thoại của khách hàng
                    </SelectItem>
                    <SelectItem value="Manual">Nhập thủ công</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {phoneSource === "Manual" && (
                <div className="space-y-2">
                  <Label htmlFor="manual-verification-phone">
                    Số điện thoại <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="manual-verification-phone"
                    type="tel"
                    autoComplete="tel"
                    placeholder="Ví dụ: 0901234567"
                    maxLength={11}
                    value={manualPhoneNumber}
                    onChange={(event) =>
                      setManualPhoneNumber(event.target.value)
                    }
                    disabled={
                      !canManage ||
                      isSavingPhone ||
                      hasUnsavedChanges ||
                      contract.status === ContractStatus.Cancelled
                    }
                  />
                </div>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="verification-phone-reason">
                Lý do chọn/đổi số <span className="text-destructive">*</span>
              </Label>
              <Textarea
                id="verification-phone-reason"
                placeholder="Nhập lý do để lưu vào lịch sử thay đổi..."
                value={phoneReason}
                onChange={(event) => setPhoneReason(event.target.value)}
                maxLength={1000}
                disabled={
                  !canManage ||
                  isSavingPhone ||
                  hasUnsavedChanges ||
                  contract.status === ContractStatus.Cancelled
                }
              />
            </div>

            <Button
              onClick={handleUpdatePhone}
              disabled={
                !canManage ||
                isSavingPhone ||
                hasUnsavedChanges ||
                contract.status === ContractStatus.Cancelled
              }
            >
              {isSavingPhone ? <Loader2 className="animate-spin" /> : <Phone />}
              {currentPhone ? "Cập nhật số xác minh" : "Chọn số xác minh"}
            </Button>

            <div className="border-t pt-5">
              <div className="mb-3 flex items-center justify-between gap-3">
                <h3 className="flex items-center gap-2 font-medium">
                  <History className="size-4 text-muted-foreground" />
                  Lịch sử số xác minh
                </h3>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => void loadPhones()}
                  disabled={isLoadingPhones}
                >
                  <RefreshCw
                    className={isLoadingPhones ? "animate-spin" : undefined}
                  />
                  Tải lại
                </Button>
              </div>

              {isLoadingPhones ? (
                <div className="flex items-center justify-center gap-2 py-8 text-sm text-muted-foreground">
                  <Loader2 className="size-4 animate-spin" /> Đang tải lịch
                  sử...
                </div>
              ) : phoneLoadError ? (
                <Alert variant="destructive">
                  <AlertDescription>{phoneLoadError}</AlertDescription>
                </Alert>
              ) : phones.length === 0 ? (
                <p className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">
                  Chưa có lịch sử số xác minh.
                </p>
              ) : (
                <div className="space-y-2">
                  {paginatedPhones.map((phone) => (
                    <div
                      key={phone.verificationPhoneId}
                      className="flex flex-col gap-2 rounded-lg border px-3 py-3 sm:flex-row sm:items-center sm:justify-between"
                    >
                      <div>
                        <p className="font-mono font-medium">
                          {phone.maskedPhoneNumber}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {PHONE_SOURCE_LABELS[phone.phoneSource]}
                        </p>
                      </div>
                      <div className="flex items-center gap-2 sm:flex-col sm:items-end">
                        {phone.isCurrent && <Badge>Đang dùng</Badge>}
                        <span className="text-xs text-muted-foreground">
                          {formatDateTime(phone.createdDate)}
                        </span>
                      </div>
                    </div>
                  ))}

                  {phoneHistoryTotalPages > 1 && (
                    <div className="flex items-center justify-between gap-3 border-t pt-3">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() =>
                          setPhoneHistoryPage((page) => Math.max(1, page - 1))
                        }
                        disabled={phoneHistoryPage === 1}
                      >
                        <ChevronLeft className="size-4" />
                        Trước
                      </Button>
                      <span className="text-xs font-medium text-muted-foreground">
                        Trang {phoneHistoryPage} / {phoneHistoryTotalPages}
                      </span>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() =>
                          setPhoneHistoryPage((page) =>
                            Math.min(phoneHistoryTotalPages, page + 1),
                          )
                        }
                        disabled={phoneHistoryPage === phoneHistoryTotalPages}
                      >
                        Sau
                        <ChevronRight className="size-4" />
                      </Button>
                    </div>
                  )}
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        <Card className="h-fit">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Link2 className="size-5 text-primary" />
              Link truy cập
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {oneTimePublicUrl ? (
              <Alert className="border-primary/40 bg-primary/5">
                <Link2 className="size-4" />
                <AlertTitle className="flex items-center justify-between gap-2">
                  Sao chép URL ngay
                  <Badge variant="outline" className={linkStateBadge.className}>
                    {linkStateBadge.label}
                  </Badge>
                </AlertTitle>
                <AlertDescription className="space-y-3">
                  <p className="break-all rounded-md border bg-background p-3 font-mono text-xs text-foreground w-full">
                    {oneTimePublicUrl}
                  </p>
                  {activeLink && (
                    <p className="text-sm text-muted-foreground">
                      Hết hạn: {formatDateTime(activeLink.expiresAt)}
                    </p>
                  )}
                  <div className="flex flex-wrap gap-2">
                    <Button
                      size="sm"
                      onClick={handleCopyLink}
                      className={
                        isCopied
                          ? "bg-emerald-600 text-white hover:bg-emerald-700"
                          : undefined
                      }
                    >
                      {isCopied ? (
                        <>
                          <Check className="size-4" /> Đã sao chép
                        </>
                      ) : (
                        <>
                          <Copy className="size-4" /> Sao chép link
                        </>
                      )}
                    </Button>
                    {activeLink && (
                      <>
                        <Button
                          variant="outline"
                          onClick={() => setLinkAction("replace")}
                          disabled={!canManage || hasUnsavedChanges}
                        >
                          <RefreshCw /> Thay link mới
                        </Button>
                        <Button
                          variant="destructive"
                          onClick={() => setLinkAction("revoke")}
                          disabled={!canManage || hasUnsavedChanges}
                        >
                          <Trash2 /> Thu hồi
                        </Button>
                      </>
                    )}
                    {/* <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => setOneTimePublicUrl(null)}
                    >
                      <X /> Đã lưu, ẩn URL
                    </Button> */}
                  </div>
                  {!activeLink && (
                    <div className="rounded-xl border border-dashed p-5 text-center">
                      <Link2 className="mx-auto size-8 text-muted-foreground" />
                      <p className="mt-3 font-medium">
                        Chưa tạo link trong phiên này
                      </p>
                      <p className="mt-1 text-sm text-muted-foreground">
                        Link chỉ tạo được khi hợp đồng ở trạng thái Nháp hoặc
                        Đang đàm phán và đã có số xác minh.
                      </p>
                    </div>
                  )}
                </AlertDescription>
              </Alert>
            ) : (
              activeLink && (
                <div className="space-y-4 rounded-xl border bg-muted/30 p-4">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <p className="mt-1 text-sm text-muted-foreground">
                        Hết hạn: {formatDateTime(activeLink.expiresAt)}
                      </p>
                    </div>
                    <Badge
                      variant="outline"
                      className={linkStateBadge.className}
                    >
                      {linkStateBadge.label}
                    </Badge>
                  </div>
                  {!oneTimePublicUrl && (
                    <p className="text-xs text-muted-foreground">
                      URL đã được ẩn và không thể lấy lại. Hãy thay link nếu bạn
                      chưa lưu URL.
                    </p>
                  )}
                  <div className="flex flex-wrap gap-2">
                    <Button
                      variant="outline"
                      onClick={() => setLinkAction("replace")}
                      disabled={!canManage || hasUnsavedChanges}
                    >
                      <RefreshCw /> Thay link
                    </Button>
                    <Button
                      variant="destructive"
                      onClick={() => setLinkAction("revoke")}
                      disabled={!canManage || hasUnsavedChanges}
                    >
                      <Trash2 /> Thu hồi
                    </Button>
                  </div>
                </div>
              )
            )}

            {!activeLink && (
              <Button
                className="w-full"
                onClick={() => setIsCreateLinkConfirmOpen(true)}
                disabled={!canCreateLink || isCreatingLink}
              >
                {isCreatingLink ? (
                  <Loader2 className="animate-spin" />
                ) : (
                  <Link2 />
                )}
                Tạo link truy cập
              </Button>
            )}
            {!canCreateLink && (
              <p className="text-xs text-muted-foreground italic">
                Không thể tạo link. Đảm bảo rằng đã có số điện thoại được xác
                minh, hợp đồng ở trạng thái "Nháp" hoăc "Đang đàm phán", và
                không có thay đổi chưa được lưu!
              </p>
            )}

            {contract.status === ContractStatus.Draft && (
              <p className="text-sm text-muted-foreground font-semibold underline">
                Ở trạng thái Nháp, link sẽ không hoạt động cho tới khi bắt đầu
                đàm phán.
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      <ConfirmDialog
        isOpen={isCreateLinkConfirmOpen}
        onClose={() => setIsCreateLinkConfirmOpen(false)}
        onConfirm={handleCreateLink}
        title="Tạo link truy cập khách hàng?"
        description={
          contract.status === ContractStatus.Draft
            ? "Link sẽ chờ kích hoạt cho đến khi bắt đầu đàm phán. URL chỉ hiển thị một lần sau khi tạo, hãy sao chép và lưu lại."
            : "URL chỉ hiển thị một lần sau khi tạo. Version hợp đồng hiện tại sẽ chuyển sang chỉ xem; bạn cần tạo vòng đàm phán mới nếu muốn tiếp tục chỉnh sửa."
        }
        icon={<Link2 className="size-5 text-primary" />}
        confirmText="Tạo link"
        cancelText="Hủy"
        isLoading={isCreatingLink}
      />

      <Dialog
        open={linkAction !== null}
        onOpenChange={(open) => {
          if (!open && !isSubmittingLinkAction) {
            setLinkAction(null);
            setLinkActionReason("");
          }
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {linkAction === "replace"
                ? "Thay link truy cập"
                : "Thu hồi link truy cập"}
            </DialogTitle>
            <DialogDescription>
              {linkAction === "replace"
                ? "Link cũ, OTP đang chờ và phiên khách hàng sẽ mất hiệu lực ngay. URL mới chỉ hiển thị một lần."
                : "Link, OTP đang chờ và toàn bộ phiên khách hàng sẽ mất hiệu lực ngay sau khi thu hồi."}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-2">
            <Label htmlFor="customer-link-action-reason">
              Lý do <span className="text-destructive">*</span>
            </Label>
            <Textarea
              id="customer-link-action-reason"
              placeholder={
                linkAction === "replace"
                  ? "Nhập lý do thay link..."
                  : "Nhập lý do thu hồi link..."
              }
              value={linkActionReason}
              onChange={(event) => setLinkActionReason(event.target.value)}
              maxLength={1000}
              disabled={isSubmittingLinkAction}
              autoFocus
            />
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setLinkAction(null);
                setLinkActionReason("");
              }}
              disabled={isSubmittingLinkAction}
            >
              Hủy
            </Button>
            <Button
              variant={linkAction === "revoke" ? "destructive" : "default"}
              onClick={handleLinkAction}
              disabled={
                !canManage || isSubmittingLinkAction || hasUnsavedChanges
              }
            >
              {isSubmittingLinkAction ? (
                <Loader2 className="animate-spin" />
              ) : linkAction === "replace" ? (
                <RefreshCw />
              ) : (
                <Trash2 />
              )}
              {linkAction === "replace" ? "Thay link" : "Thu hồi link"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
