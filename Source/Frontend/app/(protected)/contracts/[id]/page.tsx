"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft,
  Clock,
  DatabaseZap,
  FileSignature,
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
  ContractStatus,
  UpdateContractDraftRequest,
} from "@/services/contract-api";
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

export default function ContractDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();

  const [contract, setContract] = useState<ContractDetailResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isUpdating, setIsUpdating] = useState(false);
  const [isStartingNegotiation, setIsStartingNegotiation] = useState(false);
  const [isSubmittingApproval, setIsSubmittingApproval] = useState(false);

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

    for (const item of contract.currentVersion.items) {
      if (!item.quantity || item.quantity <= 0) {
        toast.error(
          "Vui lòng nhập số lượng hợp lệ cho tất cả sản phẩm/dịch vụ.",
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
          discountPercent: item.discountPercent,
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
      toast.success("Cập nhật bản nháp thành công!");
    } catch (err: any) {
      console.error("Lỗi cập nhật bản nháp:", err);
      toast.error("Cập nhật bản nháp thất bại. Vui lòng thử lại.");
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
            {(contract.status === ContractStatus.Draft ||
              contract.status === ContractStatus.Negotiating) && (
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
            label="Giá trị"
            value={
              <span className="text-primary">
                {formatCurrency(contract.totalAmount)}
              </span>
            }
          />
          <InfoCard
            icon={<Clock className="size-4" />}
            label="Người phụ trách"
            value={contract.responsibleEmployee?.employeeFullName}
          />
        </div>

        <Tabs defaultValue="overview" className="space-y-4">
          <TabsList className="flex h-auto w-full flex-wrap justify-start">
            <TabsTrigger value="overview">Tổng quan</TabsTrigger>
            <TabsTrigger value="terms">Điều khoản</TabsTrigger>
            <TabsTrigger value="negotiation">Đàm phán</TabsTrigger>
            <TabsTrigger value="signature">Ký điện tử</TabsTrigger>
            <TabsTrigger value="documents">Chứng từ</TabsTrigger>
            <TabsTrigger value="closing">Đóng hợp đồng</TabsTrigger>
          </TabsList>

          <TabsContent value="overview">
            <ContractOverview contract={contract} setContract={setContract} />
          </TabsContent>

          <TabsContent value="terms">
            <ContractTerms contract={contract} setContract={setContract} />
          </TabsContent>

          <TabsContent value="negotiation">
            <ContractNegotiation contract={contract} />
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
      </div>
    </>
  );
}
