"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  ArrowLeft,
  ArrowRight,
  Building2,
  CheckCircle2,
  FileSignature,
  FileText,
  Package,
  Plus,
  Save,
  ShieldCheck,
  Sparkles,
  Users,
  WalletCards,
} from "lucide-react";
import { toast } from "sonner";
import { Header } from "@/components/ui/custom/header";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Progress } from "@/components/ui/progress";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { Textarea } from "@/components/ui/textarea";
import { formatCurrency } from "@/lib/format-currency";
import { CONTRACT_TYPE_LABELS, ContractType } from "@/services/contracts-mock";

const steps = [
  { title: "Khách hàng", description: "Chọn bên mua" },
  { title: "Sản phẩm", description: "Chọn phạm vi" },
  { title: "Điều khoản", description: "Soạn nội dung" },
  { title: "Xem trước", description: "Kiểm tra nháp" },
];

const customers = [
  {
    id: "1",
    name: "Nguyễn Văn A",
    company: "Công ty TNHH ABC",
    email: "contact@abc.vn",
  },
  {
    id: "2",
    name: "Phạm Văn D",
    company: "MNO Company",
    email: "admin@mno.vn",
  },
  {
    id: "3",
    name: "Hoàng Văn E",
    company: "PQR Company",
    email: "business@pqr.vn",
  },
];

const catalogItems = [
  {
    id: "software-platform",
    name: "Nền tảng quản lý hợp đồng điện tử",
    type: "Sản phẩm",
    price: 350000000,
  },
  {
    id: "otp-signing",
    name: "Module ký điện tử OTP",
    type: "Dịch vụ",
    price: 65000000,
  },
  {
    id: "implementation",
    name: "Dịch vụ triển khai & đào tạo",
    type: "Dịch vụ",
    price: 35000000,
  },
  {
    id: "maintenance",
    name: "Gói bảo trì phần mềm 12 tháng",
    type: "Dịch vụ",
    price: 85000000,
  },
];

const contractTypeOptions: ContractType[] = [
  "Software",
  "Maintenance",
  "Appendix",
  "Liquidation",
];

export default function CreateContractPage() {
  const router = useRouter();
  const [currentStep, setCurrentStep] = useState(0);
  const [customerId, setCustomerId] = useState("");
  const [contractType, setContractType] = useState<ContractType>("Software");
  const [contractTitle, setContractTitle] = useState(
    "Triển khai hệ thống quản lý hợp đồng điện tử",
  );
  const [quotationNo, setQuotationNo] = useState("BG-2026-025");
  const [selectedItems, setSelectedItems] = useState<string[]>([
    "software-platform",
    "implementation",
  ]);
  const [effectiveDate, setEffectiveDate] = useState("2026-08-01");
  const [expiredDate, setExpiredDate] = useState("2027-08-01");
  const [paymentTerms, setPaymentTerms] = useState(
    "Thanh toán 40% sau khi ký hợp đồng, 60% sau nghiệm thu.",
  );
  const [softTerms, setSoftTerms] = useState(
    "Hai bên thống nhất lịch nghiệm thu theo từng giai đoạn triển khai. Các yêu cầu hỗ trợ ngoài phạm vi sẽ được ghi nhận bằng phụ lục.",
  );

  const selectedCustomer = customers.find((item) => item.id === customerId);
  const selectedCatalogItems = catalogItems.filter((item) =>
    selectedItems.includes(item.id),
  );
  const totalValue = selectedCatalogItems.reduce(
    (sum, item) => sum + item.price,
    0,
  );
  const progressValue = ((currentStep + 1) / steps.length) * 100;

  const canGoNext = useMemo(() => {
    if (currentStep === 0) return !!customerId && !!contractTitle.trim();
    if (currentStep === 1) return selectedItems.length > 0;
    if (currentStep === 2)
      return !!effectiveDate && !!expiredDate && !!paymentTerms.trim();
    return true;
  }, [
    currentStep,
    customerId,
    contractTitle,
    selectedItems.length,
    effectiveDate,
    expiredDate,
    paymentTerms,
  ]);

  const toggleCatalogItem = (id: string) => {
    setSelectedItems((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id],
    );
  };

  const handleCreateMock = () => {
    toast.success(
      "Đã tạo hợp đồng nháp mock. Chờ backend POST API để lưu thật.",
    );
    router.push("/contracts");
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
              <ArrowLeft className="size-4" />
              Quay lại danh sách
            </Button>
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-2xl font-bold tracking-tight text-foreground">
                Tạo hợp đồng nháp
              </h1>
              <Badge variant="secondary" className="gap-1.5">
                <Sparkles className="size-3.5" />
                Mock UI
              </Badge>
            </div>
            <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
              Wizard mô phỏng luồng tạo hợp đồng từ khách hàng, sản phẩm/dịch vụ
              và điều khoản. Khi có POST API, phần submit sẽ được nối vào
              backend.
            </p>
          </div>
        </div>

        <Card className="shadow-sm">
          <CardContent className="p-4 md:p-6">
            <div className="mb-4 flex items-center justify-between gap-4">
              <div>
                <p className="text-sm font-semibold">
                  Bước {currentStep + 1}/{steps.length}:{" "}
                  {steps[currentStep].title}
                </p>
                <p className="text-xs text-muted-foreground">
                  {steps[currentStep].description}
                </p>
              </div>
              <span className="text-sm font-medium text-primary">
                {Math.round(progressValue)}%
              </span>
            </div>
            <Progress value={progressValue} className="h-2" />

            <div className="mt-5 grid gap-3 md:grid-cols-4">
              {steps.map((step, index) => (
                <div
                  key={step.title}
                  className={`rounded-xl border p-3 ${
                    index === currentStep
                      ? "border-primary bg-primary/5"
                      : index < currentStep
                        ? "border-emerald-200 bg-emerald-50/60 dark:border-emerald-900 dark:bg-emerald-950/20"
                        : "bg-muted/30"
                  }`}
                >
                  <div className="flex items-center gap-2">
                    <div
                      className={`flex size-7 items-center justify-center rounded-full text-xs font-semibold ${
                        index <= currentStep
                          ? "bg-primary text-primary-foreground"
                          : "bg-muted text-muted-foreground"
                      }`}
                    >
                      {index < currentStep ? (
                        <CheckCircle2 className="size-4" />
                      ) : (
                        index + 1
                      )}
                    </div>
                    <div>
                      <p className="text-sm font-medium">{step.title}</p>
                      <p className="text-xs text-muted-foreground">
                        {step.description}
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
          <Card className="shadow-sm">
            <CardHeader>
              <CardTitle>
                {currentStep === 0 && "Thông tin khách hàng"}
                {currentStep === 1 && "Sản phẩm / dịch vụ trong hợp đồng"}
                {currentStep === 2 && "Điều khoản & thời hạn"}
                {currentStep === 3 && "Xem trước hợp đồng nháp"}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              {currentStep === 0 && (
                <div className="grid gap-5 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label>Khách hàng / đối tác</Label>
                    <Select value={customerId} onValueChange={setCustomerId}>
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder="Chọn khách hàng" />
                      </SelectTrigger>
                      <SelectContent>
                        {customers.map((customer) => (
                          <SelectItem key={customer.id} value={customer.id}>
                            {customer.company} · {customer.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2">
                    <Label>Loại hợp đồng</Label>
                    <Select
                      value={contractType}
                      onValueChange={(value) =>
                        setContractType(value as ContractType)
                      }
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {contractTypeOptions.map((type) => (
                          <SelectItem key={type} value={type}>
                            {CONTRACT_TYPE_LABELS[type]}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2 md:col-span-2">
                    <Label>Tên hợp đồng</Label>
                    <Input
                      value={contractTitle}
                      onChange={(event) => setContractTitle(event.target.value)}
                      placeholder="Nhập tên hợp đồng..."
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>Báo giá liên quan</Label>
                    <Input
                      value={quotationNo}
                      onChange={(event) => setQuotationNo(event.target.value)}
                      placeholder="Ví dụ: BG-2026-025"
                    />
                  </div>
                </div>
              )}

              {currentStep === 1 && (
                <div className="space-y-3">
                  {catalogItems.map((item) => {
                    const selected = selectedItems.includes(item.id);
                    return (
                      <button
                        key={item.id}
                        type="button"
                        onClick={() => toggleCatalogItem(item.id)}
                        className={`w-full rounded-xl border p-4 text-left transition-colors hover:bg-accent ${
                          selected
                            ? "border-primary bg-primary/5"
                            : "bg-background"
                        }`}
                      >
                        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                          <div className="flex items-start gap-3">
                            <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
                              <Package className="size-5" />
                            </div>
                            <div>
                              <p className="font-semibold">{item.name}</p>
                              <p className="mt-1 text-sm text-muted-foreground">
                                {item.type}
                              </p>
                            </div>
                          </div>
                          <div className="flex items-center gap-3">
                            <span className="font-semibold text-primary">
                              {formatCurrency(item.price)}
                            </span>
                            {selected && (
                              <Badge className="gap-1">
                                <CheckCircle2 className="size-3.5" />
                                Đã chọn
                              </Badge>
                            )}
                          </div>
                        </div>
                      </button>
                    );
                  })}
                </div>
              )}

              {currentStep === 2 && (
                <div className="space-y-5">
                  <div className="grid gap-5 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label>Ngày hiệu lực</Label>
                      <Input
                        type="date"
                        value={effectiveDate}
                        onChange={(event) =>
                          setEffectiveDate(event.target.value)
                        }
                      />
                    </div>
                    <div className="space-y-2">
                      <Label>Ngày hết hạn / kết thúc bảo hành</Label>
                      <Input
                        type="date"
                        value={expiredDate}
                        onChange={(event) => setExpiredDate(event.target.value)}
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label>Điều khoản thanh toán</Label>
                    <Textarea
                      value={paymentTerms}
                      onChange={(event) => setPaymentTerms(event.target.value)}
                      className="min-h-24"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>Điều khoản mềm / ghi chú đàm phán</Label>
                    <Textarea
                      value={softTerms}
                      onChange={(event) => setSoftTerms(event.target.value)}
                      className="min-h-28"
                    />
                  </div>
                </div>
              )}

              {currentStep === 3 && (
                <div className="space-y-5">
                  <Alert>
                    <FileSignature className="size-4" />
                    <AlertTitle>
                      Hợp đồng sẽ được tạo ở trạng thái Bản nháp
                    </AlertTitle>
                    <AlertDescription>
                      Đây là bản xem trước mock. Dữ liệu chưa được lưu xuống
                      backend cho tới khi có POST API hợp đồng.
                    </AlertDescription>
                  </Alert>

                  <div className="rounded-2xl border bg-muted/30 p-5">
                    <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                      <div>
                        <p className="text-sm text-muted-foreground">
                          Hợp đồng nháp
                        </p>
                        <h2 className="mt-1 text-xl font-bold">
                          {contractTitle || "Chưa nhập tên hợp đồng"}
                        </h2>
                      </div>
                      <Badge variant="outline">Draft</Badge>
                    </div>

                    <div className="grid gap-4 md:grid-cols-2">
                      <div>
                        <p className="text-xs text-muted-foreground">
                          Khách hàng
                        </p>
                        <p className="font-semibold">
                          {selectedCustomer?.company || "Chưa chọn"}
                        </p>
                        <p className="text-sm text-muted-foreground">
                          {selectedCustomer?.name}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-muted-foreground">
                          Loại hợp đồng
                        </p>
                        <p className="font-semibold">
                          {CONTRACT_TYPE_LABELS[contractType]}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-muted-foreground">
                          Hiệu lực
                        </p>
                        <p className="font-semibold">
                          {effectiveDate} - {expiredDate}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-muted-foreground">
                          Tổng giá trị dự kiến
                        </p>
                        <p className="font-semibold text-primary">
                          {formatCurrency(totalValue)}
                        </p>
                      </div>
                    </div>

                    <Separator className="my-5" />

                    <div>
                      <p className="mb-2 text-sm font-semibold">
                        Sản phẩm / dịch vụ
                      </p>
                      <div className="space-y-2">
                        {selectedCatalogItems.map((item) => (
                          <div
                            key={item.id}
                            className="flex items-center justify-between rounded-lg bg-background p-3 text-sm"
                          >
                            <span>{item.name}</span>
                            <span className="font-medium">
                              {formatCurrency(item.price)}
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              )}

              <Separator />

              <div className="flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
                <Button
                  variant="outline"
                  disabled={currentStep === 0}
                  onClick={() =>
                    setCurrentStep((step) => Math.max(step - 1, 0))
                  }
                >
                  <ArrowLeft className="size-4" />
                  Quay lại
                </Button>

                {currentStep < steps.length - 1 ? (
                  <Button
                    disabled={!canGoNext}
                    onClick={() =>
                      setCurrentStep((step) =>
                        Math.min(step + 1, steps.length - 1),
                      )
                    }
                  >
                    Tiếp tục
                    <ArrowRight className="size-4" />
                  </Button>
                ) : (
                  <Button onClick={handleCreateMock}>
                    <Save className="size-4" />
                    Tạo hợp đồng nháp mock
                  </Button>
                )}
              </div>
            </CardContent>
          </Card>

          <div className="space-y-6">
            <Card className="shadow-sm">
              <CardHeader>
                <CardTitle className="text-base">Tóm tắt nhanh</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-start gap-3">
                  <div className="flex size-9 items-center justify-center rounded-xl bg-primary/10 text-primary">
                    <Building2 className="size-4" />
                  </div>
                  <div>
                    <p className="text-sm font-medium">
                      {selectedCustomer?.company || "Chưa chọn khách hàng"}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {selectedCustomer?.email || "Chọn khách ở bước 1"}
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-3">
                  <div className="flex size-9 items-center justify-center rounded-xl bg-primary/10 text-primary">
                    <FileText className="size-4" />
                  </div>
                  <div>
                    <p className="text-sm font-medium">
                      {CONTRACT_TYPE_LABELS[contractType]}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {quotationNo || "Chưa liên kết báo giá"}
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-3">
                  <div className="flex size-9 items-center justify-center rounded-xl bg-primary/10 text-primary">
                    <WalletCards className="size-4" />
                  </div>
                  <div>
                    <p className="text-sm font-medium">
                      {formatCurrency(totalValue)}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {selectedItems.length} sản phẩm/dịch vụ đã chọn
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card className="border-dashed shadow-sm">
              <CardContent className="p-6">
                <div className="mb-3 flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
                  <ShieldCheck className="size-5" />
                </div>
                <h3 className="font-semibold">Gắn API sau</h3>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">
                  Khi backend có endpoint tạo hợp đồng, phần dữ liệu trên form
                  này có thể map thành payload cho `POST /api/contracts`.
                </p>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </>
  );
}
