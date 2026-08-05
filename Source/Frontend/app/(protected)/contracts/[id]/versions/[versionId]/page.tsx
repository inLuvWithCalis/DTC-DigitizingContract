"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import {
  ArrowLeft,
  CheckCircle2,
  Clock,
  FileLock2,
  FileText,
  Loader2,
  Users,
  WalletCards,
} from "lucide-react";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Header } from "@/components/ui/custom/header";
import {
  ContractStatusBadge,
  InfoCard,
} from "@/components/contracts/contract-helpers";
import { ContractVersionSnapshotTabs } from "@/components/contracts/contract-version-snapshot-tabs";
import { formatCurrency } from "@/lib/format-currency";
import { formatDate } from "@/lib/format-date";
import {
  contractApi,
  ContractDetailResponse,
  ContractVersionDetailResponse,
} from "@/services/contract-api";

const getApiErrorMessage = (error: any) => {
  const data = error?.response?.data;
  return (
    data?.message ||
    data?.title ||
    (typeof data === "string" ? data : null) ||
    "Không thể tải chi tiết snapshot. Vui lòng thử lại."
  );
};

export default function ContractVersionDetailPage() {
  const params = useParams<{ id: string; versionId: string }>();
  const contractId = Number(params.id);
  const versionId = Number(params.versionId);

  const [contract, setContract] = useState<ContractDetailResponse | null>(null);
  const [version, setVersion] = useState<ContractVersionDetailResponse | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!Number.isInteger(contractId) || !Number.isInteger(versionId)) {
      setError("Đường dẫn hợp đồng hoặc version không hợp lệ.");
      setIsLoading(false);
      return;
    }

    let isCancelled = false;
    const loadPage = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const [contractDetail, versionDetail] = await Promise.all([
          contractApi.getDetail(contractId),
          contractApi.getVersionDetail(contractId, versionId),
        ]);
        if (!isCancelled) {
          setContract(contractDetail);
          setVersion(versionDetail);
        }
      } catch (loadError: any) {
        console.error("Lỗi tải trang snapshot:", loadError);
        if (!isCancelled) {
          setError(getApiErrorMessage(loadError));
        }
      } finally {
        if (!isCancelled) setIsLoading(false);
      }
    };

    void loadPage();
    return () => {
      isCancelled = true;
    };
  }, [contractId, versionId]);

  if (isLoading) {
    return (
      <>
        <Header />
        <div className="flex h-[50vh] items-center justify-center">
          <Loader2 className="mr-2 size-7 animate-spin text-primary" />
          <span className="text-muted-foreground">
            Đang tải chi tiết snapshot...
          </span>
        </div>
      </>
    );
  }

  if (error || !contract || !version) {
    return (
      <>
        <Header />
        <div className="grow overflow-y-auto p-3 sm:p-6 lg:p-10">
          <Alert variant="destructive">
            <AlertTitle>Không thể mở snapshot</AlertTitle>
            <AlertDescription className="space-y-3">
              <p>{error || "Không tìm thấy dữ liệu version."}</p>
              <Button variant="outline" size="sm" asChild>
                <Link href={`/contracts/${params.id}`}>Quay lại hợp đồng</Link>
              </Button>
            </AlertDescription>
          </Alert>
        </div>
      </>
    );
  }

  return (
    <>
      <Header />
      <div className="grow space-y-6 overflow-y-auto p-3 sm:p-6 lg:p-10">
        <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
          <div>
            <Button
              variant="ghost"
              className="-ml-3 mb-2 text-muted-foreground"
              asChild
            >
              <Link href={`/contracts/${contract.contractId}`}>
                <ArrowLeft className="mr-2 size-4" />
                Quay lại hợp đồng
              </Link>
            </Button>
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-2xl font-bold tracking-tight">
                {contract.contractCode}
              </h1>
              <ContractStatusBadge status={contract.status} />
            </div>
            <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
              {contract.contractName}
            </p>
          </div>
        </div>

        <div className="grid gap-4 md:grid-cols-4">
          <InfoCard
            icon={<Users className="size-4" />}
            label="Khách hàng"
            value={
              contract.customer?.customerCompany ||
              contract.customer?.customerFullName ||
              "Chưa cập nhật"
            }
          />
          <InfoCard
            icon={<FileText className="size-4" />}
            label="Báo giá liên quan"
            value="Chưa liên kết"
          />
          <InfoCard
            icon={<WalletCards className="size-4" />}
            label={`Tổng thanh toán · Version ${version.versionNo}`}
            value={
              <span className="text-primary">
                {formatCurrency(version.totalPayment, version.currencyCode)}
              </span>
            }
          />
          <InfoCard
            icon={<Clock className="size-4" />}
            label="Người phụ trách"
            value={contract.responsibleEmployee?.employeeFullName || "Chưa gán"}
          />
        </div>

        <Card>
          <CardHeader className="flex flex-col gap-3 space-y-0 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Snapshot lịch sử
              </p>
              <CardTitle className="mt-1">
                Chi tiết Version {version.versionNo}
              </CardTitle>
              <p className="mt-2 text-sm text-muted-foreground">
                Tạo ngày {formatDate(version.createdDate)} ·{" "}
                {version.changeNote || "Khởi tạo hợp đồng."}
              </p>
            </div>
            <Badge
              variant={version.isLocked ? "secondary" : "outline"}
              className="w-fit gap-1.5"
            >
              {version.isLocked ? (
                <FileLock2 className="size-3.5" />
              ) : (
                <CheckCircle2 className="size-3.5" />
              )}
              {version.isLocked ? "Snapshot đã khóa" : "Đang chỉnh sửa"}
            </Badge>
          </CardHeader>
          <CardContent>
            <ContractVersionSnapshotTabs
              contractId={contract.contractId}
              version={version}
            />
          </CardContent>
        </Card>
      </div>
    </>
  );
}
