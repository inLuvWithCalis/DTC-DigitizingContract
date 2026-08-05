"use client";

import { useState, useEffect, type Dispatch, type SetStateAction } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft,
  Clock,
  FileText,
  Loader2,
  MessageSquareText,
  Save,
  Send,
  Users,
  WalletCards,
} from "lucide-react";

import { Header } from "@/components/ui/custom/header";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { formatCurrency } from "@/lib/format-currency";

import {
  contractApi,
  ContractDetailResponse,
  ContractItemDiscountMode,
  ContractLanguageMode,
  ContractStatus,
  UpdateContractDraftRequest,
} from "@/services/contract-api";
import { roundContractMoney } from "@/lib/contract-finance";
import { toast } from "sonner";

import {
  ContractStatusBadge,
  InfoCard,
} from "@/components/contracts/contract-helpers";
import { ContractOverview } from "@/components/contracts/contract-overview";
import { ContractTerms } from "@/components/contracts/contract-terms";
import { ContractNegotiation } from "@/components/contracts/contract-negotiation";
import { ContractSignature } from "@/components/contracts/contract-signature";
import { ContractDocuments } from "@/components/contracts/contract-attachments";
import { ContractClosing } from "@/components/contracts/contract-closing";
import { TransferResponsibilityModal } from "@/components/contracts/transfer-responsibility-modal";

const CONTRACT_TABS = [
  "overview",
  "terms",
  "negotiation",
  "signature",
  "documents",
  "closing",
] as const;

type ContractTab = (typeof CONTRACT_TABS)[number];

const isContractTab = (value: string): value is ContractTab =>
  CONTRACT_TABS.some((tab) => tab === value);

export default function ContractDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();

  const [contract, setContract] = useState<ContractDetailResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isUpdating, setIsUpdating] = useState(false);
  const [isStartingNegotiation, setIsStartingNegotiation] = useState(false);
  const [isSubmittingApproval, setIsSubmittingApproval] = useState(false);
  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<ContractTab>("overview");
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);

  useEffect(() => {
    const syncTabFromHash = () => {
      const tabFromHash = window.location.hash.slice(1);

      if (isContractTab(tabFromHash)) {
        setActiveTab(tabFromHash);
      }
    };

    syncTabFromHash();
    window.addEventListener("hashchange", syncTabFromHash);

    return () => window.removeEventListener("hashchange", syncTabFromHash);
  }, []);

  const handleTabChange = (value: string) => {
    if (!isContractTab(value)) return;

    setActiveTab(value);
    window.history.replaceState(window.history.state, "", `#${value}`);
  };

  const handleContractChange: Dispatch<
    SetStateAction<ContractDetailResponse | null>
  > = (value) => {
    setContract(value);
    setHasUnsavedChanges(true);
  };

  useEffect(() => {
    if (!hasUnsavedChanges) return;

    const handleBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [hasUnsavedChanges]);

  // Gọi API lấy dữ liệu hợp đồng
  useEffect(() => {
    if (!params.id) return;

    const fetchContractDetail = async () => {
      try {
        setIsLoading(true);
        // Do API trả về theo cấu trúc { data: { ... } }, interceptor thường tự unwrap
        // Nếu không, bạn lấy res.data hoặc res.data.data tùy theo config của bạn
        const res: any = await contractApi.getDetail(Number(params.id));

        // Gán dữ liệu (Phụ thuộc vào cấu trúc BaseResponse của bạn)
        const contractData = res.data ? res.data : res;
        setContract(contractData);
        setHasUnsavedChanges(false);
      } catch (err: any) {
        console.error("Lỗi lấy chi tiết hợp đồng:", err);
        setError("Không thể lấy dữ liệu hợp đồng. Vui lòng thử lại sau.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchContractDetail();
  }, [params.id]);

  if (isLoading) {
    return (
      <>
        <Header />
        <div className="flex h-[50vh] items-center justify-center">
          <div className="flex flex-col items-center gap-2 text-muted-foreground">
            <Loader2 className="size-8 animate-spin text-primary" />
            <p>Đang tải chi tiết hợp đồng...</p>
          </div>
        </div>
      </>
    );
  }

  if (error || !contract) {
    return (
      <>
        <Header />
        <div className="grow overflow-y-auto p-2 lg:p-10">
          <Alert variant="destructive">
            <AlertTitle>Không tìm thấy hợp đồng</AlertTitle>
            <AlertDescription>
              {error || "Hợp đồng này không tồn tại hoặc đã bị xóa."}{" "}
              <Link href="/contracts" className="underline font-semibold">
                Quay lại danh sách
              </Link>
            </AlertDescription>
          </Alert>
        </div>
      </>
    );
  }

  const handleUpdateDraft = async () => {
    if (!contract || !contract.currentVersion) return;

    if (!contract.contractName.trim()) {
      toast.error("Tên hợp đồng không được để trống.");
      return;
    }

    if (contract.currentVersion.items.length === 0) {
      toast.error("Hợp đồng phải có ít nhất một sản phẩm hoặc dịch vụ.");
      return;
    }

    if (contract.currentVersion.terms.length === 0) {
      toast.error("Hợp đồng phải có ít nhất một điều khoản.");
      return;
    }

    if (
      contract.effectiveDate &&
      contract.expireDate &&
      new Date(contract.expireDate) < new Date(contract.effectiveDate)
    ) {
      toast.error("Ngày hết hạn không được trước ngày hiệu lực.");
      return;
    }

    if (
      contract.languageMode === ContractLanguageMode.Bilingual &&
      !contract.contractNameEn?.trim()
    ) {
      toast.error("Hợp đồng song ngữ phải có tên hợp đồng tiếng Anh.");
      return;
    }

    for (const item of contract.currentVersion.items) {
      if (
        contract.languageMode === ContractLanguageMode.Bilingual &&
        !item.itemNameEn?.trim()
      ) {
        toast.error(`Vui lòng nhập tên tiếng Anh cho “${item.itemName}”.`);
        return;
      }
      if (!Number.isFinite(item.quantity) || item.quantity <= 0) {
        toast.error(
          "Vui lòng nhập số lượng hợp lệ cho tất cả sản phẩm/dịch vụ.",
        );
        return;
      }

      if (!Number.isFinite(item.unitPrice) || item.unitPrice < 0) {
        toast.error(`Đơn giá của “${item.itemName}” không hợp lệ.`);
        return;
      }

      if (
        item.discountMode === ContractItemDiscountMode.Percentage &&
        (item.discountPercent < 0 || item.discountPercent > 100)
      ) {
        toast.error(`Chiết khấu của “${item.itemName}” phải từ 0% đến 100%.`);
        return;
      }

      const lineSubtotal = roundContractMoney(
        item.quantity * item.unitPrice,
        contract.currencyCode,
      );
      if (
        item.discountMode === ContractItemDiscountMode.FixedAmount &&
        (item.fixedDiscountAmount < 0 ||
          item.fixedDiscountAmount > lineSubtotal)
      ) {
        toast.error(
          `Số tiền giảm của “${item.itemName}” không được vượt quá tạm tính.`,
        );
        return;
      }

      if (item.isTaxable && (item.vatPercent < 0 || item.vatPercent > 100)) {
        toast.error(`Thuế VAT của “${item.itemName}” phải từ 0% đến 100%.`);
        return;
      }
    }

    for (const term of contract.currentVersion.terms) {
      if (!term.termTitle.trim()) {
        toast.error("Tiêu đề điều khoản không được để trống.");
        return;
      }

      if (
        contract.languageMode === ContractLanguageMode.Bilingual &&
        (!term.termTitleEn?.trim() ||
          (!!term.termContent?.trim() && !term.termContentEn?.trim()))
      ) {
        toast.error(
          `Điều khoản “${term.termCode}” chưa đủ nội dung tiếng Anh.`,
        );
        return;
      }
    }

    setIsUpdating(true);
    try {
      // Map payload chuẩn hóa theo UpdateContractDraftRequest
      const payload: UpdateContractDraftRequest = {
        rowVersion: contract.rowVersion,
        currentVersionId: contract.currentVersion.versionId,
        currentVersionRowVersion: contract.currentVersion.rowVersion,
        customerId: contract.customer.customerId,
        contractName: contract.contractName,
        contractNameEn: contract.contractNameEn,
        effectiveDate: contract.effectiveDate,
        expireDate: contract.expireDate,
        currencyCode: contract.currencyCode,

        // Map Items (Bao gồm Id và rowVersion)
        items: contract.currentVersion.items.map((item) => ({
          contractItemId: item.contractItemId < 0 ? null : item.contractItemId,
          rowVersion: item.contractItemId < 0 ? null : item.rowVersion,
          itemType: item.itemType,
          sourceProductId: item.sourceProductId,
          sourceServiceId: item.sourceServiceId,
          itemCode: item.itemCode,
          itemName: item.itemName,
          itemNameEn: item.itemNameEn,
          itemDescription: item.itemDescription,
          itemDescriptionEn: item.itemDescriptionEn,
          unitName: item.unitName,
          unitNameEn: item.unitNameEn,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          discountMode: item.discountMode,
          discountPercent: item.discountPercent,
          fixedDiscountAmount: item.fixedDiscountAmount,
          isTaxable: item.isTaxable,
          vatPercent: item.vatPercent,
          displayOrder: item.displayOrder,
        })),

        // Map Terms (Bao gồm Id và rowVersion)
        terms: contract.currentVersion.terms.map((term) => ({
          termId: term.termId < 0 ? null : term.termId,
          rowVersion: term.termId < 0 ? null : term.rowVersion,
          termCode: term.termCode,
          termTitle: term.termTitle,
          termTitleEn: term.termTitleEn,
          termContent: term.termContent,
          termContentEn: term.termContentEn,
          isNegotiable: term.isNegotiable,
          displayOrder: term.displayOrder,
        })),
      };

      const res: any = await contractApi.updateDraft(
        contract.contractId,
        payload,
      );
      const updatedData = res.data ? res.data : res;

      setContract(updatedData); // Cập nhật lại UI với data mới
      setHasUnsavedChanges(false);
      toast.success("Cập nhật bản nháp thành công!");
    } catch (err: any) {
      console.error("Lỗi cập nhật bản nháp:", err);
      const data = err?.response?.data;
      const message =
        data?.message ||
        data?.title ||
        (typeof data === "string" ? data : null) ||
        "Cập nhật bản nháp thất bại. Vui lòng thử lại.";
      toast.error(message);
    } finally {
      setIsUpdating(false);
    }
  };

  const handleStartNegotiation = async () => {
    if (!contract) return;

    setIsStartingNegotiation(true);
    try {
      const res: any = await contractApi.startNegotiation(contract.contractId, {
        rowVersion: contract.rowVersion,
      });
      const updatedData = res.data ? res.data : res;
      setContract(updatedData);
      handleTabChange("negotiation");
      toast.success("Hợp đồng đã chuyển sang trạng thái Đàm phán!");
    } catch (err: any) {
      console.error("Lỗi bắt đầu đàm phán:", err);
      const errorData = err?.response?.data;
      const message = errorData?.errors
        ? Object.values(errorData.errors).flat().join("; ")
        : errorData?.title || "Không thể bắt đầu đàm phán. Vui lòng thử lại.";
      toast.error(message);
    } finally {
      setIsStartingNegotiation(false);
    }
  };

  const handleSubmitApproval = async () => {
    if (!contract || !contract.currentVersion) return;

    setIsSubmittingApproval(true);
    try {
      const payload = {
        rowVersion: contract.rowVersion,
        currentVersionId: contract.currentVersion.versionId,
        currentVersionRowVersion: contract.currentVersion.rowVersion,
        workflowId: null,
      };
      await contractApi.submitApproval(contract.contractId, payload);

      const detailRes: ContractDetailResponse = await contractApi.getDetail(
        contract.contractId,
      );
      setContract(detailRes);

      toast.success("Hợp đồng đã được gửi duyệt!");
    } catch (err: any) {
      console.error("Lỗi gửi duyệt hợp đồng:", err);
      const errorData = err?.response?.data;
      const message = errorData?.errors
        ? Object.values(errorData.errors).flat().join("; ")
        : errorData?.title || "Không thể gửi duyệt. Vui lòng thử lại.";
      toast.error(message);
    } finally {
      setIsSubmittingApproval(false);
    }
  };

  const canUpdateDraft =
    (contract.status === ContractStatus.Draft ||
      (contract.status === ContractStatus.Negotiating &&
        contract.currentVersion.sourceVersionId != null)) &&
    !contract.currentVersion.isLocked;

  return (
    <>
      <Header />

      <div className="grow overflow-y-auto p-2 lg:p-10 space-y-6">
        <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
          <div>
            <Button
              variant="ghost"
              className="-ml-3 mb-2 text-muted-foreground"
              onClick={() => router.push("/contracts")}
            >
              <ArrowLeft className="size-4 mr-2" />
              Quay lại danh sách
            </Button>
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-2xl font-bold tracking-tight text-foreground">
                {contract.contractCode}
              </h1>
              <ContractStatusBadge status={contract.status} />
            </div>
            <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
              {contract.contractName}
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            {canUpdateDraft && (
              <Button
                variant="outline"
                onClick={handleUpdateDraft}
                disabled={isUpdating}
                className="bg-blue-50 text-blue-700 hover:bg-blue-100 hover:text-blue-800 border-blue-200"
              >
                {isUpdating ? (
                  <Loader2 className="size-4 mr-2 animate-spin" />
                ) : (
                  <Save className="size-4 mr-2" />
                )}
                Lưu thay đổi
              </Button>
            )}
            {contract.status === ContractStatus.Draft && (
              <Button
                onClick={handleStartNegotiation}
                disabled={isStartingNegotiation}
                className="bg-emerald-600 hover:bg-emerald-700 text-white"
              >
                {isStartingNegotiation ? (
                  <Loader2 className="size-4 mr-2 animate-spin" />
                ) : (
                  <MessageSquareText className="size-4 mr-2" />
                )}
                Bắt đầu đàm phán
              </Button>
            )}
            {contract.status === ContractStatus.Negotiating && (
              <Button
                onClick={handleSubmitApproval}
                disabled={isSubmittingApproval}
                className="bg-amber-600 hover:bg-amber-700 text-white"
              >
                {isSubmittingApproval ? (
                  <Loader2 className="size-4 mr-2 animate-spin" />
                ) : (
                  <Send className="size-4 mr-2" />
                )}
                Gửi duyệt
              </Button>
            )}
            {/* <Button variant="outline">
              <DatabaseZap className="size-4 mr-2" />
              Tạo từ báo giá
            </Button>
            <Button>
              <FileSignature className="size-4 mr-2" />
              Gửi ký OTP
            </Button> */}
          </div>
        </div>

        <div className="grid gap-4 md:grid-cols-4">
          <InfoCard
            icon={<Users className="size-4" />}
            label="Khách hàng"
            value={
              contract.customer?.customerCompany ||
              contract.customer?.customerFullName
            }
          />
          <InfoCard
            icon={<FileText className="size-4" />}
            label="Báo giá liên quan"
            value={"Chưa liên kết"}
          />
          <InfoCard
            icon={<WalletCards className="size-4" />}
            label="Tổng thanh toán"
            value={
              <span className="text-primary">
                {formatCurrency(
                  contract.totalPayment ?? contract.totalAmount,
                  contract.currencyCode,
                )}
              </span>
            }
          />
          <div className="rounded-xl border bg-muted/30 p-4 flex flex-col justify-between">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Clock className="size-4" />
                Người phụ trách
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 px-2 text-xs text-primary hover:bg-primary/10"
                onClick={() => setIsTransferModalOpen(true)}
              >
                Chuyển giao
              </Button>
            </div>
            <div className="mt-2 font-semibold text-foreground">
              {contract.responsibleEmployee?.employeeFullName || "Chưa gán"}
            </div>
          </div>
        </div>

        <Tabs
          value={activeTab}
          onValueChange={handleTabChange}
          className="space-y-4"
        >
          <TabsList className="flex h-auto w-full flex-wrap justify-start">
            <TabsTrigger value="overview">Tổng quan</TabsTrigger>
            <TabsTrigger value="terms">Điều khoản</TabsTrigger>
            <TabsTrigger value="negotiation">Đàm phán</TabsTrigger>
            <TabsTrigger value="signature">Ký điện tử</TabsTrigger>
            <TabsTrigger value="documents">Chứng từ</TabsTrigger>
            <TabsTrigger value="closing">Đóng hợp đồng</TabsTrigger>
          </TabsList>

          <TabsContent value="overview">
            <ContractOverview
              contract={contract}
              setContract={handleContractChange}
              onOpenTransferModal={() => setIsTransferModalOpen(true)}
            />
          </TabsContent>

          <TabsContent value="terms">
            <ContractTerms
              contract={contract}
              setContract={setContract}
              onDraftChange={() => setHasUnsavedChanges(true)}
            />
          </TabsContent>

          <TabsContent value="negotiation">
            <ContractNegotiation
              contract={contract}
              setContract={setContract}
            />
          </TabsContent>

          <TabsContent value="signature">
            <ContractSignature contract={contract} />
          </TabsContent>

          <TabsContent value="documents">
            <ContractDocuments contract={contract} />
          </TabsContent>

          <TabsContent value="closing">
            <ContractClosing contract={contract} />
          </TabsContent>
        </Tabs>

        <TransferResponsibilityModal
          isOpen={isTransferModalOpen}
          onClose={() => setIsTransferModalOpen(false)}
          contractId={contract.contractId}
          rowVersion={contract.rowVersion}
          currentEmployeeId={contract.responsibleEmployee?.employeeId}
          currentEmployeeName={contract.responsibleEmployee?.employeeFullName}
          onSuccess={(updated) => setContract(updated)}
        />

        {hasUnsavedChanges && canUpdateDraft && (
          <Alert
            className="fixed bottom-6 right-6 z-50 w-[calc(100%-3rem)] max-w-md animate-in fade-in-0 slide-in-from-bottom-8 bg-background shadow-lg duration-300"
            aria-live="polite"
          >
            <AlertTitle>Hợp đồng có thay đổi chưa được lưu</AlertTitle>
            <AlertDescription className="mt-2 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <span>Bạn có muốn lưu các thay đổi vừa thực hiện không?</span>
              <Button
                onClick={handleUpdateDraft}
                disabled={isUpdating}
                size="sm"
                className="shrink-0"
              >
                {isUpdating ? (
                  <Loader2 className="size-4 mr-2 animate-spin" />
                ) : (
                  <Save className="size-4 mr-2" />
                )}
                Lưu thay đổi
              </Button>
            </AlertDescription>
          </Alert>
        )}
      </div>
    </>
  );
}
