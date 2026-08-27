"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import {
  CalendarDays,
  FileText,
  KeyRound,
  Link2Off,
  Loader2,
  LockKeyhole,
  MessageSquareText,
  PackageOpen,
  Phone,
  RefreshCw,
  Send,
  ShieldCheck,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { PublicContractDiscussionModal } from "@/components/contracts/public-contract-comments";
import { ThemeToggle } from "@/components/theme-toggle";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
} from "@/components/ui/input-otp";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatCurrency } from "@/lib/format-currency";
import { formatDate } from "@/lib/format-date";
import {
  CreateCustomerNegotiationCommentRequest,
  CustomerPublicNegotiationCommentResponse,
  CustomerSharedContractResponse,
  CustomerAccessLinkAvailabilityState,
  publicContractApi,
} from "@/services/public-contract-api";

type AccessStep =
  | "checking"
  | "phone"
  | "otp"
  | "contract"
  | "unavailable"
  | "error";

const getStatus = (error: any) => error?.response?.status as number | undefined;

const getErrorMessage = (error: any, fallback: string) => {
  const data = error?.response?.data;
  return (
    data?.message || data?.title || (typeof data === "string" ? data : fallback)
  );
};

const PublicThemeToggle = () => (
  <div className="fixed right-4 top-4 z-50 sm:right-6 sm:top-6">
    <ThemeToggle />
  </div>
);

export default function PublicContractPage() {
  const params = useParams<{ tenantCode: string; linkToken: string }>();
  const tenantCode = params.tenantCode;
  const linkToken = params.linkToken;

  const [step, setStep] = useState<AccessStep>("checking");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [otp, setOtp] = useState("");
  const [publicChallengeId, setPublicChallengeId] = useState<string | null>(
    null,
  );
  const [contract, setContract] =
    useState<CustomerSharedContractResponse | null>(null);
  const [pageError, setPageError] = useState<string | null>(null);
  const [isRequestingOtp, setIsRequestingOtp] = useState(false);
  const [isVerifyingOtp, setIsVerifyingOtp] = useState(false);

  const resetToPhone = useCallback((message?: string) => {
    setContract(null);
    setPublicChallengeId(null);
    setOtp("");
    setPageError(null);
    setStep("phone");
    if (message) toast.error(message);
  }, []);

  const showLinkUnavailable = useCallback(
    (state: CustomerAccessLinkAvailabilityState) => {
      setContract(null);
      setPublicChallengeId(null);
      setOtp("");
      setPageError(
        state === "PendingActivation"
          ? "Hợp đồng chưa bắt đầu đàm phán. Vui lòng thử lại sau khi nhân viên kích hoạt link."
          : "Link này đã hết hạn, bị thu hồi hoặc được thay thế bởi một link mới. Vui lòng liên hệ nhân viên phụ trách để nhận link mới.",
      );
      setStep("unavailable");
    },
    [],
  );

  const loadSharedContract = useCallback(async () => {
    try {
      setStep("checking");
      setPageError(null);
      const result = await publicContractApi.getShared(tenantCode);
      setContract(result);
      setStep("contract");
      return true;
    } catch (error: any) {
      if (getStatus(error) === 401) {
        resetToPhone();
      } else {
        setPageError(
          getErrorMessage(error, "Không thể tải hợp đồng được chia sẻ."),
        );
        setStep("error");
      }
      return false;
    }
  }, [resetToPhone, tenantCode]);

  const initializeAccess = useCallback(async () => {
    setStep("checking");
    setPageError(null);

    try {
      const availability = await publicContractApi.getLinkAvailability(
        tenantCode,
        linkToken,
      );
      if (!availability.isAvailable) {
        showLinkUnavailable(availability.state);
        return;
      }

      await loadSharedContract();
    } catch (error: any) {
      setPageError(
        getErrorMessage(error, "Không thể kiểm tra link truy cập."),
      );
      setStep("error");
    }
  }, [linkToken, loadSharedContract, showLinkUnavailable, tenantCode]);

  useEffect(() => {
    void initializeAccess();
  }, [initializeAccess]);

  const sortedItems = useMemo(
    () =>
      [...(contract?.items ?? [])].sort(
        (a, b) => a.displayOrder - b.displayOrder,
      ),
    [contract?.items],
  );

  const sortedTerms = useMemo(
    () =>
      [...(contract?.terms ?? [])].sort(
        (a, b) => a.displayOrder - b.displayOrder,
      ),
    [contract?.terms],
  );

  const handleRequestOtp = async () => {
    const normalizedPhone = phoneNumber.trim();
    if (!normalizedPhone) {
      toast.error("Vui lòng nhập số điện thoại xác minh.");
      return;
    }

    try {
      setIsRequestingOtp(true);
      const availability = await publicContractApi.getLinkAvailability(
        tenantCode,
        linkToken,
      );
      if (!availability.isAvailable) {
        showLinkUnavailable(availability.state);
        return;
      }

      const result = await publicContractApi.requestOtp(tenantCode, linkToken, {
        phoneNumber: normalizedPhone,
      });
      setPublicChallengeId(result.publicChallengeId);
      setOtp("");
      setStep("otp");
      toast.info("Nếu thông tin hợp lệ, mã xác thực sẽ được gửi.");
    } catch (error: any) {
      toast.error(getErrorMessage(error, "Không thể yêu cầu mã xác thực."));
    } finally {
      setIsRequestingOtp(false);
    }
  };

  const handleVerifyOtp = async () => {
    if (!publicChallengeId) {
      resetToPhone(
        "Phiên yêu cầu OTP không còn hợp lệ. Vui lòng yêu cầu mã mới.",
      );
      return;
    }

    const normalizedOtp = otp.trim();
    if (normalizedOtp.length !== 6) {
      toast.error("Vui lòng nhập đủ 6 chữ số OTP.");
      return;
    }

    try {
      setIsVerifyingOtp(true);
      await publicContractApi.verifyOtp(tenantCode, linkToken, {
        publicChallengeId,
        otp: normalizedOtp,
      });
      setOtp("");
      setPublicChallengeId(null);
      toast.success("Xác thực thành công!");
      await initializeAccess();
    } catch (error: any) {
      resetToPhone(
        getErrorMessage(error, "Không thể xác thực mã. Hãy yêu cầu mã mới."),
      );
    } finally {
      setIsVerifyingOtp(false);
    }
  };

  const handleCreateComment = async (
    request: CreateCustomerNegotiationCommentRequest,
  ): Promise<CustomerPublicNegotiationCommentResponse> => {
    try {
      const created = await publicContractApi.createComment(
        tenantCode,
        request,
      );
      setContract((current) =>
        current
          ? { ...current, comments: [...current.comments, created] }
          : current,
      );
      return created;
    } catch (error: any) {
      if (getStatus(error) === 401) {
        resetToPhone("Phiên truy cập đã hết hạn. Vui lòng xác thực lại.");
      }
      throw error;
    }
  };

  if (step === "checking") {
    return (
      <>
        <PublicThemeToggle />
        <div className="flex min-h-screen items-center justify-center px-4">
          <div className="flex flex-col items-center gap-3 text-muted-foreground">
            <Loader2 className="size-8 animate-spin text-primary" />
            <p>Đang kiểm tra quyền truy cập...</p>
          </div>
        </div>
      </>
    );
  }

  if (step === "error") {
    return (
      <>
        <PublicThemeToggle />
        <div className="mx-auto flex min-h-screen max-w-lg items-center px-4">
          <Alert variant="destructive">
            <AlertTitle>Không thể mở hợp đồng</AlertTitle>
            <AlertDescription className="mt-2 space-y-3">
              <p>{pageError}</p>
              <Button variant="outline" size="sm" onClick={initializeAccess}>
                <RefreshCw /> Thử lại
              </Button>
            </AlertDescription>
          </Alert>
        </div>
      </>
    );
  }

  if (step === "unavailable") {
    return (
      <>
        <PublicThemeToggle />
        <div className="flex min-h-screen items-center justify-center px-4 py-10">
          <Card className="w-full max-w-md text-center shadow-lg">
            <CardHeader>
              <div className="mx-auto mb-2 flex size-12 items-center justify-center rounded-full bg-destructive/10 text-destructive">
                <Link2Off />
              </div>
              <CardTitle>Link không còn khả dụng</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <p className="text-sm text-muted-foreground">{pageError}</p>
              <Button variant="outline" onClick={initializeAccess}>
                <RefreshCw /> Kiểm tra lại
              </Button>
            </CardContent>
          </Card>
        </div>
      </>
    );
  }

  if (step === "phone" || step === "otp") {
    return (
      <div className="flex min-h-screen items-center justify-center px-4 py-10">
        <PublicThemeToggle />
        <Card className="w-full max-w-md shadow-lg">
          <CardHeader className="text-center">
            <div className="mx-auto mb-2 flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary">
              {step === "phone" ? <Phone /> : <KeyRound />}
            </div>
            <CardTitle>
              {step === "phone" ? "Xác minh khách hàng" : "Nhập mã OTP"}
            </CardTitle>
            <p className="text-sm text-muted-foreground">
              {step === "phone"
                ? "Nhập số điện thoại đã được chọn để truy cập hợp đồng."
                : "Nhập mã xác thực được gửi tới số điện thoại của bạn."}
            </p>
          </CardHeader>
          <CardContent className="space-y-4">
            {step === "phone" ? (
              <>
                <div className="space-y-2">
                  <Label htmlFor="customer-phone">Số điện thoại</Label>
                  <Input
                    id="customer-phone"
                    type="tel"
                    inputMode="tel"
                    autoComplete="tel"
                    placeholder="Nhập số điện thoại"
                    value={phoneNumber}
                    onChange={(event) => setPhoneNumber(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") void handleRequestOtp();
                    }}
                    disabled={isRequestingOtp}
                    autoFocus
                  />
                </div>
                <Button
                  className="w-full"
                  onClick={handleRequestOtp}
                  disabled={isRequestingOtp}
                >
                  {isRequestingOtp ? (
                    <Loader2 className="animate-spin" />
                  ) : (
                    <Send />
                  )}
                  Yêu cầu mã OTP
                </Button>
              </>
            ) : (
              <>
                <div className="space-y-2">
                  <InputOTP
                    id="customer-otp"
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    maxLength={6}
                    value={otp}
                    onChange={(value) => setOtp(value.replace(/\D/g, ""))}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") void handleVerifyOtp();
                    }}
                    disabled={isVerifyingOtp}
                    containerClassName="justify-center"
                    autoFocus
                  >
                    <InputOTPGroup>
                      {Array.from({ length: 6 }, (_, index) => (
                        <InputOTPSlot
                          key={index}
                          index={index}
                          className="size-12 text-lg font-semibold"
                        />
                      ))}
                    </InputOTPGroup>
                  </InputOTP>
                </div>
                <Button
                  className="w-full"
                  onClick={handleVerifyOtp}
                  disabled={isVerifyingOtp}
                >
                  {isVerifyingOtp ? (
                    <Loader2 className="animate-spin" />
                  ) : (
                    <ShieldCheck />
                  )}
                  Xác thực và xem hợp đồng
                </Button>
                <div className="flex items-center justify-between gap-3">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => resetToPhone()}
                    disabled={isVerifyingOtp}
                  >
                    Đổi số điện thoại
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleRequestOtp}
                    disabled={isRequestingOtp || isVerifyingOtp}
                  >
                    {isRequestingOtp && <Loader2 className="animate-spin" />}
                    Gửi lại mã
                  </Button>
                </div>
              </>
            )}
            <p className="flex items-start gap-2 text-xs text-muted-foreground">
              <LockKeyhole className="mt-0.5 size-3.5 shrink-0" />
              Mã OTP và thông tin truy cập không được lưu trên trình duyệt.
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!contract) return <PublicThemeToggle />;

  return (
    <div className="mx-auto w-full max-w-6xl space-y-6 px-4 py-6 sm:px-6 sm:py-10">
      <PublicThemeToggle />
      <Card className="rounded-2xl">
        <CardContent className="p-5 sm:p-8">
          <div className="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <Badge variant="outline">Hợp đồng được chia sẻ</Badge>
              <h1 className="mt-3 text-2xl font-bold sm:text-3xl">
                {contract.contractName}
              </h1>
              {contract.contractNameEn && (
                <p className="mt-1 text-muted-foreground italic">
                  {contract.contractNameEn}
                </p>
              )}
              {contract.contractCode && (
                <p className="mt-3 text-sm font-medium text-primary">
                  Mã hợp đồng: {contract.contractCode}
                </p>
              )}
            </div>
            <div className="rounded-xl bg-primary/5 px-4 py-3 text-right">
              <p className="text-xs text-muted-foreground">Tổng giá trị</p>
              <p className="mt-1 text-xl font-bold text-primary">
                {formatCurrency(contract.totalAmount, contract.currencyCode)}
              </p>
            </div>
          </div>
          <div className="mt-5 flex flex-wrap gap-x-6 gap-y-2 border-t pt-4 text-sm text-muted-foreground">
            <span className="flex items-center gap-2">
              <CalendarDays className="size-4" /> Hiệu lực:{" "}
              {formatDate(contract.effectiveDate)}
            </span>
            <span className="flex items-center gap-2">
              <CalendarDays className="size-4" /> Hết hạn:{" "}
              {formatDate(contract.expireDate)}
            </span>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <PackageOpen className="size-5 text-primary" />
            Sản phẩm và dịch vụ
          </CardTitle>
        </CardHeader>
        <CardContent>
          {sortedItems.length === 0 ? (
            <p className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">
              Hợp đồng chưa có sản phẩm hoặc dịch vụ.
            </p>
          ) : (
            <div className="overflow-x-auto rounded-lg border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nội dung</TableHead>
                    <TableHead className="text-right">Số lượng</TableHead>
                    <TableHead>Đơn vị</TableHead>
                    <TableHead className="text-right">Thành tiền</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {sortedItems.map((item, index) => (
                    <TableRow key={`${item.itemName}-${index}`}>
                      <TableCell>
                        <p className="font-medium">{item.itemName}</p>
                        {item.itemNameEn && (
                          <p className="text-xs text-muted-foreground italic">
                            {item.itemNameEn}
                          </p>
                        )}
                        {item.itemDescription && (
                          <p className="mt-1 text-xs text-muted-foreground">
                            {item.itemDescription}
                          </p>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        {item.quantity}
                      </TableCell>
                      <TableCell>{item.unitName || "—"}</TableCell>
                      <TableCell className="text-right font-medium">
                        {formatCurrency(item.lineTotal, contract.currencyCode)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardContent className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between sm:p-6">
          <div className="flex min-w-0 items-start gap-3">
            <div className="rounded-xl bg-primary/10 p-2.5 text-primary">
              <MessageSquareText className="size-5" />
            </div>
            <div className="min-w-0">
              <h2 className="font-semibold">Trao đổi chung</h2>
              <p className="mt-1 max-w-2xl text-sm leading-6 text-muted-foreground">
                Thảo luận các nội dung áp dụng cho toàn bộ hợp đồng, không gắn
                với một điều khoản cụ thể.
              </p>
            </div>
          </div>

          <PublicContractDiscussionModal
            termId={null}
            comments={contract.comments}
            canWrite
            onCreate={handleCreateComment}
            triggerClassName="mt-0 shrink-0"
          />
        </CardContent>
      </Card>

      <section className="space-y-4">
        <div>
          <h2 className="flex items-center gap-2 text-xl font-semibold">
            <FileText className="size-5 text-primary" /> Điều khoản hợp đồng
          </h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Chỉ các điều khoản được đánh dấu có thể thương lượng mới cho phép
            tạo trao đổi mới.
          </p>
        </div>

        {sortedTerms.length === 0 ? (
          <p className="rounded-xl border border-dashed bg-background p-8 text-center text-muted-foreground">
            Hợp đồng chưa có điều khoản.
          </p>
        ) : (
          sortedTerms.map((term) => (
            <Card key={term.termId}>
              <CardHeader>
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <CardTitle className="text-base">
                      {[term.termCode, term.termTitle]
                        .filter(Boolean)
                        .join(" · ")}
                    </CardTitle>
                    {term.termTitleEn && (
                      <p className="mt-1 text-sm text-muted-foreground italic">
                        {term.termTitleEn}
                      </p>
                    )}
                  </div>
                  <Badge variant={term.isNegotiable ? "default" : "secondary"}>
                    {term.isNegotiable
                      ? "Có thể thương lượng"
                      : "Điều khoản cố định"}
                  </Badge>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-3 text-sm leading-7">
                  <p className="whitespace-pre-wrap">
                    {term.termContent || "—"}
                  </p>
                  {term.termContentEn && (
                    <p className="whitespace-pre-wrap text-muted-foreground italic">
                      {term.termContentEn}
                    </p>
                  )}
                </div>
                <PublicContractDiscussionModal
                  termId={term.termId}
                  termCode={term.termCode}
                  termTitle={term.termTitle}
                  comments={contract.comments}
                  canWrite={term.isNegotiable}
                  onCreate={handleCreateComment}
                />
              </CardContent>
            </Card>
          ))
        )}
      </section>

      <Alert>
        <LockKeyhole className="size-4" />
        <AlertTitle>Phiên truy cập được bảo vệ</AlertTitle>
        <AlertDescription>
          Trang này chỉ hỗ trợ xem và trao đổi hợp đồng. Chức năng ký điện tử
          chưa được cung cấp trong luồng này.
        </AlertDescription>
      </Alert>
    </div>
  );
}
