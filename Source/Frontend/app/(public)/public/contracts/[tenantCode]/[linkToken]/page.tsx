"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import {
  CalendarDays,
  FileText,
  KeyRound,
  Loader2,
  LockKeyhole,
  MessageSquareText,
  PackageOpen,
  Phone,
  RefreshCw,
  Send,
  ShieldCheck,
} from "lucide-react";
import { toast } from "sonner";

import { PublicContractComments } from "@/components/contracts/public-contract-comments";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
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
  publicContractApi,
} from "@/services/public-contract-api";

type AccessStep = "checking" | "phone" | "otp" | "contract" | "error";

const getStatus = (error: any) => error?.response?.status as number | undefined;

const getErrorMessage = (error: any, fallback: string) => {
  const data = error?.response?.data;
  return data?.message || data?.title || (typeof data === "string" ? data : fallback);
};

export default function PublicContractPage() {
  const params = useParams<{ tenantCode: string; linkToken: string }>();
  const tenantCode = params.tenantCode;
  const linkToken = params.linkToken;

  const [step, setStep] = useState<AccessStep>("checking");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [otp, setOtp] = useState("");
  const [publicChallengeId, setPublicChallengeId] = useState<string | null>(null);
  const [contract, setContract] = useState<CustomerSharedContractResponse | null>(null);
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
        setPageError(getErrorMessage(error, "Không thể tải hợp đồng được chia sẻ."));
        setStep("error");
      }
      return false;
    }
  }, [resetToPhone, tenantCode]);

  useEffect(() => {
    void loadSharedContract();
  }, [loadSharedContract]);

  const sortedItems = useMemo(
    () => [...(contract?.items ?? [])].sort((a, b) => a.displayOrder - b.displayOrder),
    [contract?.items],
  );

  const sortedTerms = useMemo(
    () => [...(contract?.terms ?? [])].sort((a, b) => a.displayOrder - b.displayOrder),
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
      const result = await publicContractApi.requestOtp(tenantCode, linkToken, {
        phoneNumber: normalizedPhone,
      });
      setPublicChallengeId(result.publicChallengeId);
      setOtp("");
      setStep("otp");
      toast.success("Nếu thông tin hợp lệ, mã xác thực sẽ được gửi.");
    } catch (error: any) {
      toast.error(getErrorMessage(error, "Không thể yêu cầu mã xác thực."));
    } finally {
      setIsRequestingOtp(false);
    }
  };

  const handleVerifyOtp = async () => {
    if (!publicChallengeId) {
      resetToPhone("Phiên yêu cầu OTP không còn hợp lệ. Vui lòng yêu cầu mã mới.");
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
      await loadSharedContract();
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
      const created = await publicContractApi.createComment(tenantCode, request);
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
      <div className="flex min-h-screen items-center justify-center px-4">
        <div className="flex flex-col items-center gap-3 text-muted-foreground">
          <Loader2 className="size-8 animate-spin text-primary" />
          <p>Đang kiểm tra quyền truy cập...</p>
        </div>
      </div>
    );
  }

  if (step === "error") {
    return (
      <div className="mx-auto flex min-h-screen max-w-lg items-center px-4">
        <Alert variant="destructive">
          <AlertTitle>Không thể mở hợp đồng</AlertTitle>
          <AlertDescription className="mt-2 space-y-3">
            <p>{pageError}</p>
            <Button variant="outline" size="sm" onClick={loadSharedContract}>
              <RefreshCw /> Thử lại
            </Button>
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  if (step === "phone" || step === "otp") {
    return (
      <div className="flex min-h-screen items-center justify-center px-4 py-10">
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
                  <Label htmlFor="customer-otp">Mã OTP</Label>
                  <Input
                    id="customer-otp"
                    type="text"
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    maxLength={6}
                    placeholder="Nhập mã xác thực"
                    value={otp}
                    onChange={(event) =>
                      setOtp(event.target.value.replace(/\D/g, ""))
                    }
                    onKeyDown={(event) => {
                      if (event.key === "Enter") void handleVerifyOtp();
                    }}
                    disabled={isVerifyingOtp}
                    autoFocus
                  />
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

  if (!contract) return null;

  return (
    <div className="mx-auto w-full max-w-6xl space-y-6 px-4 py-6 sm:px-6 sm:py-10">
      <header className="rounded-2xl border bg-background p-5 shadow-sm sm:p-8">
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
            <CalendarDays className="size-4" /> Hiệu lực: {formatDate(contract.effectiveDate)}
          </span>
          <span className="flex items-center gap-2">
            <CalendarDays className="size-4" /> Hết hạn: {formatDate(contract.expireDate)}
          </span>
        </div>
      </header>

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
                      <TableCell className="text-right">{item.quantity}</TableCell>
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
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MessageSquareText className="size-5 text-primary" />
            Trao đổi chung
          </CardTitle>
        </CardHeader>
        <CardContent>
          <PublicContractComments
            termId={null}
            comments={contract.comments}
            canCreateRoot
            onCreate={handleCreateComment}
          />
        </CardContent>
      </Card>

      <section className="space-y-4">
        <div>
          <h2 className="flex items-center gap-2 text-xl font-semibold">
            <FileText className="size-5 text-primary" /> Điều khoản hợp đồng
          </h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Chỉ các điều khoản được đánh dấu có thể thương lượng mới cho phép tạo trao đổi mới.
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
                      {[term.termCode, term.termTitle].filter(Boolean).join(" · ")}
                    </CardTitle>
                    {term.termTitleEn && (
                      <p className="mt-1 text-sm text-muted-foreground italic">
                        {term.termTitleEn}
                      </p>
                    )}
                  </div>
                  <Badge variant={term.isNegotiable ? "default" : "secondary"}>
                    {term.isNegotiable ? "Có thể thương lượng" : "Điều khoản cố định"}
                  </Badge>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-3 text-sm leading-7">
                  <p className="whitespace-pre-wrap">{term.termContent || "—"}</p>
                  {term.termContentEn && (
                    <p className="whitespace-pre-wrap text-muted-foreground italic">
                      {term.termContentEn}
                    </p>
                  )}
                </div>
                <PublicContractComments
                  termId={term.termId}
                  comments={contract.comments}
                  canCreateRoot={term.isNegotiable}
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
          Trang này chỉ hỗ trợ xem và trao đổi hợp đồng. Chức năng ký điện tử chưa được cung cấp trong luồng này.
        </AlertDescription>
      </Alert>
    </div>
  );
}
