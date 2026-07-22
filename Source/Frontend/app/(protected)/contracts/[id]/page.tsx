"use client";

import { useMemo } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft,
  CalendarDays,
  CheckCircle2,
  Clock,
  Copy,
  DatabaseZap,
  FileCheck2,
  FileSignature,
  FileText,
  Link2,
  MessageSquareText,
  Phone,
  ReceiptText,
  ShieldCheck,
  Truck,
  Users,
  WalletCards,
} from "lucide-react";
import {
  ContractAttachmentItem,
  ContractAttachments,
} from "@/components/contracts/contract-attachments";
import { Header } from "@/components/ui/custom/header";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { formatCurrency } from "@/lib/format-currency";
import {
  CONTRACT_STATUS_LABELS,
  CONTRACT_TYPE_LABELS,
  ContractMock,
  ContractStatus,
  mockContracts,
} from "@/services/contracts-mock";

const statusClasses: Record<ContractStatus, string> = {
  Draft:
    "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-500/10 dark:text-amber-400 dark:border-amber-500/20",
  Negotiating:
    "bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-500/10 dark:text-blue-400 dark:border-blue-500/20",
  Approved:
    "bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-500/10 dark:text-indigo-400 dark:border-indigo-500/20",
  Signed:
    "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-500/10 dark:text-emerald-400 dark:border-emerald-500/20",
  Closing:
    "bg-orange-50 text-orange-700 border-orange-200 dark:bg-orange-500/10 dark:text-orange-400 dark:border-orange-500/20",
  Closed:
    "bg-slate-50 text-slate-700 border-slate-200 dark:bg-slate-500/10 dark:text-slate-400 dark:border-slate-500/20",
};

function ContractStatusBadge({ status }: { status: ContractStatus }) {
  return (
    <Badge variant="outline" className={statusClasses[status]}>
      {CONTRACT_STATUS_LABELS[status]}
    </Badge>
  );
}

function formatShortDate(value: string) {
  return new Date(value).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

function InfoCard({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border bg-muted/30 p-4">
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        {icon}
        {label}
      </div>
      <div className="mt-2 font-semibold text-foreground">{value}</div>
    </div>
  );
}

function ContractOverview({ contract }: { contract: ContractMock }) {
  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
      <Card>
        <CardHeader>
          <CardTitle>Tổng quan hợp đồng</CardTitle>
        </CardHeader>
        <CardContent className="space-y-5">
          <p className="leading-7 text-muted-foreground">{contract.summary}</p>

          <div className="grid gap-4 sm:grid-cols-2">
            <InfoCard
              icon={<Users className="size-4" />}
              label="Khách hàng"
              value={
                <div>
                  <p>{contract.customerName}</p>
                  <p className="text-sm font-normal text-muted-foreground">
                    {contract.customerCompany}
                  </p>
                </div>
              }
            />
            <InfoCard
              icon={<FileSignature className="size-4" />}
              label="Loại hợp đồng"
              value={CONTRACT_TYPE_LABELS[contract.type]}
            />
            <InfoCard
              icon={<CalendarDays className="size-4" />}
              label="Hiệu lực"
              value={`${formatShortDate(contract.effectiveDate)} - ${formatShortDate(
                contract.expiredDate,
              )}`}
            />
            <InfoCard
              icon={<WalletCards className="size-4" />}
              label="Giá trị hợp đồng"
              value={<span className="text-primary">{formatCurrency(contract.value)}</span>}
            />
          </div>
        </CardContent>
      </Card>

      <div className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Thanh toán</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="mb-2 flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Tiến độ thanh toán</span>
              <span className="font-semibold">{contract.paymentProgress}%</span>
            </div>
            <Progress value={contract.paymentProgress} className="h-2" />
            <p className="mt-3 text-sm text-muted-foreground">
              Điều kiện đóng hợp đồng yêu cầu khách thanh toán đủ 100%.
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Bản cứng</CardTitle>
          </CardHeader>
          <CardContent className="flex items-center gap-3">
            <div className="flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <Truck className="size-5" />
            </div>
            <div>
              <p className="font-semibold">{contract.hardCopyStatus}</p>
              <p className="text-sm text-muted-foreground">
                Theo dõi gửi/nhận bản cứng với khách hàng.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function ContractTerms() {
  const hardTerms = [
    "Không chuyển giao, sao chép hoặc triển khai phần mềm sang server khác khi chưa có chấp thuận.",
    "Thông tin sản phẩm, phạm vi triển khai và chi phí theo phụ lục/báo giá đã thống nhất.",
    "Bảo hành/bảo trì miễn phí 12 tháng kể từ ngày nghiệm thu.",
  ];
  const softTerms = [
    "Thời gian hỗ trợ kỹ thuật ngoài giờ.",
    "Mốc nghiệm thu từng giai đoạn.",
    "Điều kiện thanh toán theo từng đợt.",
  ];

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ShieldCheck className="size-5 text-primary" />
            Điều khoản cứng
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {hardTerms.map((term) => (
            <div key={term} className="rounded-xl border bg-muted/30 p-4">
              <p className="text-sm leading-6">{term}</p>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MessageSquareText className="size-5 text-primary" />
            Điều khoản có thể đàm phán
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {softTerms.map((term) => (
            <div key={term} className="rounded-xl border bg-muted/30 p-4">
              <p className="text-sm leading-6">{term}</p>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}

function ContractNegotiation({ contract }: { contract: ContractMock }) {
  return (
    <Card>
      <CardHeader className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <CardTitle>Lịch sử đàm phán / comment</CardTitle>
        <Button variant="outline" size="sm">
          <MessageSquareText className="size-4" />
          Thêm ghi chú mock
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {contract.comments.length === 0 ? (
          <div className="rounded-xl border border-dashed p-8 text-center">
            <MessageSquareText className="mx-auto mb-3 size-8 text-muted-foreground" />
            <p className="font-medium">Chưa có comment đàm phán</p>
            <p className="mt-1 text-sm text-muted-foreground">
              Sau này có thể gắn API revision/comment của hợp đồng tại đây.
            </p>
          </div>
        ) : (
          contract.comments.map((comment) => (
            <div key={comment.id} className="rounded-xl border p-4">
              <div className="mb-2 flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="font-semibold">{comment.author}</p>
                  <p className="text-xs text-muted-foreground">
                    {comment.role}
                  </p>
                </div>
                <span className="text-xs text-muted-foreground">
                  {new Date(comment.createdAt).toLocaleString("vi-VN")}
                </span>
              </div>
              <p className="text-sm leading-6 text-muted-foreground">
                {comment.content}
              </p>
            </div>
          ))
        )}
      </CardContent>
    </Card>
  );
}

function ContractSignature({ contract }: { contract: ContractMock }) {
  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Phone className="size-5 text-primary" />
            Ký điện tử bằng OTP
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <Alert>
            <ShieldCheck className="size-4" />
            <AlertTitle>Mock UI chờ API ký số</AlertTitle>
            <AlertDescription>
              Luồng thật cần API gửi OTP và xác nhận chữ ký cho khách hàng và
              đại diện nhà cung cấp.
            </AlertDescription>
          </Alert>

          <div className="grid gap-3">
            <div className="rounded-xl border p-4">
              <p className="font-medium">Khách hàng</p>
              <p className="mt-1 text-sm text-muted-foreground">
                OTP gửi tới số điện thoại đại diện bên mua.
              </p>
            </div>
            <div className="rounded-xl border p-4">
              <p className="font-medium">Nhà cung cấp</p>
              <p className="mt-1 text-sm text-muted-foreground">
                OTP gửi tới số điện thoại đại diện pháp luật nội bộ.
              </p>
            </div>
          </div>

          <Button disabled className="w-full">
            Gửi OTP ký hợp đồng
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Link khách hàng xem hợp đồng</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="rounded-xl border bg-muted/30 p-4">
            <p className="break-all font-mono text-sm text-muted-foreground">
              {contract.publicLink}
            </p>
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              onClick={() => navigator.clipboard?.writeText(contract.publicLink)}
            >
              <Copy className="size-4" />
              Copy link
            </Button>
            <Button disabled>
              <Link2 className="size-4" />
              Gửi cho khách
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function ContractDocuments({ contract }: { contract: ContractMock }) {
  const documentTypeMap: Record<
    string,
    { value: ContractAttachmentItem["documentType"]; label: string }
  > = {
    Acceptance: { value: 1, label: "Biên bản nghiệm thu" },
    Handover: { value: 2, label: "Biên bản bàn giao" },
    Liquidation: { value: 3, label: "Biên bản thanh lý" },
    Invoice: { value: 4, label: "Hóa đơn VAT" },
    Guarantee: { value: 5, label: "Bảo lãnh ngân hàng" },
    "Signed Contract": { value: 6, label: "Bản scan đã ký" },
  };

  const attachments: ContractAttachmentItem[] = contract.documents.map(
    (document) => {
      const documentType = documentTypeMap[document.type] || {
        value: 99,
        label: "Tài liệu khác",
      };

      return {
        id: document.id,
        name: document.name,
        documentType: documentType.value,
        documentTypeName: documentType.label,
        uploadedAt: document.uploadedAt,
        uploadedBy: document.owner,
      };
    },
  );

  return (
    <ContractAttachments
      contractId={contract.id}
      initialAttachments={attachments}
      mockMode
    />
  );
}

function ContractClosing({ contract }: { contract: ContractMock }) {
  const checks = [
    {
      label: "Hai bên đã ký điện tử",
      completed: ["Signed", "Closing", "Closed"].includes(contract.status),
      icon: <FileSignature className="size-4" />,
    },
    {
      label: "Đã gửi/nhận bản cứng",
      completed: contract.hardCopyStatus === "Đã nhận",
      icon: <Truck className="size-4" />,
    },
    {
      label: "Đã upload biên bản nghiệm thu/bàn giao",
      completed: contract.documents.some(
        (document) =>
          ["Acceptance", "Handover"].includes(document.type) &&
          document.status === "Completed",
      ),
      icon: <FileCheck2 className="size-4" />,
    },
    {
      label: "Đã upload hóa đơn/chứng từ kế toán",
      completed: contract.documents.some(
        (document) =>
          ["Invoice", "Guarantee"].includes(document.type) &&
          document.status === "Completed",
      ),
      icon: <ReceiptText className="size-4" />,
    },
    {
      label: "Khách hàng đã thanh toán 100%",
      completed: contract.paymentProgress === 100,
      icon: <WalletCards className="size-4" />,
    },
  ];
  const completedCount = checks.filter((check) => check.completed).length;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Checklist đóng hợp đồng</CardTitle>
      </CardHeader>
      <CardContent className="space-y-5">
        <div>
          <div className="mb-2 flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Hoàn tất</span>
            <span className="font-semibold">
              {completedCount}/{checks.length}
            </span>
          </div>
          <Progress value={(completedCount / checks.length) * 100} />
        </div>

        <div className="space-y-3">
          {checks.map((check) => (
            <div
              key={check.label}
              className="flex items-center gap-3 rounded-xl border p-4"
            >
              <div
                className={`flex size-9 items-center justify-center rounded-full ${
                  check.completed
                    ? "bg-emerald-500/10 text-emerald-600"
                    : "bg-muted text-muted-foreground"
                }`}
              >
                {check.completed ? (
                  <CheckCircle2 className="size-4" />
                ) : (
                  check.icon
                )}
              </div>
              <p className="text-sm font-medium">{check.label}</p>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

export default function ContractDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const contract = useMemo(
    () =>
      mockContracts.find(
        (item) => item.id === Number(params.id),
      ) || null,
    [params.id],
  );

  if (!contract) {
    return (
      <>
        <Header />
        <div className="grow overflow-y-auto p-2 lg:p-10">
          <Alert variant="destructive">
            <AlertTitle>Không tìm thấy hợp đồng</AlertTitle>
            <AlertDescription>
              Hợp đồng mock này không tồn tại.{" "}
              <Link href="/contracts" className="underline">
                Quay lại danh sách
              </Link>
              .
            </AlertDescription>
          </Alert>
        </div>
      </>
    );
  }

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
              <ArrowLeft className="size-4" />
              Quay lại danh sách
            </Button>
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-2xl font-bold tracking-tight text-foreground">
                {contract.contractNo}
              </h1>
              <ContractStatusBadge status={contract.status} />
            </div>
            <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
              {contract.title}
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <Button variant="outline">
              <DatabaseZap className="size-4" />
              Tạo từ báo giá
            </Button>
            <Button>
              <FileSignature className="size-4" />
              Gửi ký OTP
            </Button>
          </div>
        </div>

        <div className="grid gap-4 md:grid-cols-4">
          <InfoCard
            icon={<Users className="size-4" />}
            label="Khách hàng"
            value={contract.customerCompany}
          />
          <InfoCard
            icon={<FileText className="size-4" />}
            label="Báo giá liên quan"
            value={contract.quotationNo || "Chưa liên kết"}
          />
          <InfoCard
            icon={<WalletCards className="size-4" />}
            label="Giá trị"
            value={<span className="text-primary">{formatCurrency(contract.value)}</span>}
          />
          <InfoCard
            icon={<Clock className="size-4" />}
            label="Người phụ trách"
            value={contract.ownerName}
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
            <ContractOverview contract={contract} />
          </TabsContent>

          <TabsContent value="terms">
            <ContractTerms />
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

        <Separator />
        <p className="text-center text-xs text-muted-foreground">
          Đây là mock UI để chờ backend Contract API. Các nút thao tác chính
          hiện chỉ mô phỏng luồng nghiệp vụ.
        </p>
      </div>
    </>
  );
}
