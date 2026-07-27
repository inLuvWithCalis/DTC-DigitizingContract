"use client";

import { useMemo, useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import {
  ArrowLeft,
  ArrowRight,
  Building2,
  CheckCircle2,
  FileSignature,
  FileText,
  Package,
  Save,
  ShieldCheck,
  Sparkles,
  WalletCards,
  Loader2,
  ChevronLeft,
  ChevronRight,
  LayoutTemplate,
} from "lucide-react";
import { toast } from "sonner";

// Các import UI component
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
import { DateRangeFilter } from "@/components/ui/custom/date-range-filter";
import { format } from "date-fns";
import { CreateContractTermsMock } from "@/components/contracts/create-contract-terms-mock";

import {
  ContractType,
  ContractLanguageMode,
  ContractItemType,
  CreateContractRequest,
  CreateContractItemRequest,
  contractApi,
  getContractTypeLabel,
  getContractLanguageModeLabel,
} from "@/services/contract-api";
import { customerApi, CustomerResponse } from "@/services/customers-api";
import { productApi } from "@/services/catalog/products-api";
import { serviceApi } from "@/services/catalog/services-api";
import {
  cloneMockTerms,
  MockContractTerm,
  mockContractTemplates,
} from "@/services/contract-templates-mock";

const steps = [
  { title: "Khách hàng & Mẫu", description: "Thiết lập cơ bản" },
  { title: "Sản phẩm", description: "Chọn phạm vi" },
  { title: "Điều khoản", description: "Soạn nội dung" },
  { title: "Xem trước", description: "Kiểm tra nháp" },
];

type CatalogItem = {
  id: string; // "p-{id}" or "s-{id}"
  originalId: number;
  itemName: string;
  itemType: ContractItemType;
  unitPrice: number;
  quantity: number;
  discountPercent: number;
  vatPercent: number;
};

const contractTypeOptions = [
  ContractType.SoftwareSupply,
  ContractType.SoftwareMaintenance,
  ContractType.SoftwareUpkeep,
];

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(amount);
};

export default function CreateContractPage() {
  const router = useRouter();

  // -- STATES --
  const [currentStep, setCurrentStep] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [customers, setCustomers] = useState<CustomerResponse[]>([]);
  const [isLoadingCustomers, setIsLoadingCustomers] = useState(true);

  const [catalogItems, setCatalogItems] = useState<CatalogItem[]>([]);
  const [isLoadingCatalog, setIsLoadingCatalog] = useState(true);

  // Fetch data
  useEffect(() => {
    const fetchData = async () => {
      try {
        const [customerRes, productRes, serviceRes] = await Promise.all([
          customerApi.getList({ page: 1, pageSize: 100 }),
          productApi.getList({ page: 1, pageSize: 100 }),
          serviceApi.getList({ page: 1, pageSize: 100 }),
        ]);

        setCustomers(customerRes.items);

        const mappedProducts: CatalogItem[] = productRes.items.map((p) => ({
          id: `p-${p.productId}`,
          originalId: p.productId,
          itemName: p.productName || "Sản phẩm không tên",
          itemType: ContractItemType.Product,
          unitPrice: p.productPrice || 0,
          quantity: 1,
          discountPercent: 0,
          vatPercent: 10,
        }));

        const mappedServices: CatalogItem[] = serviceRes.items.map((s) => ({
          id: `s-${s.serviceId}`,
          originalId: s.serviceId,
          itemName: s.serviceName || "Dịch vụ không tên",
          itemType: ContractItemType.Service,
          unitPrice: s.servicePrice || 0,
          quantity: 1,
          discountPercent: 0,
          vatPercent: 10,
        }));

        setCatalogItems([...mappedProducts, ...mappedServices]);
      } catch (err) {
        console.error("Failed to fetch data:", err);
      } finally {
        setIsLoadingCustomers(false);
        setIsLoadingCatalog(false);
      }
    };
    fetchData();
  }, []);

  // Step 1: Customer & Basic Info
  const [customerId, setCustomerId] = useState<string>("");
  const [contractType, setContractType] = useState<ContractType>(
    ContractType.SoftwareSupply,
  );
  const [templateVersionId, setTemplateVersionId] = useState<string>("");
  const [languageMode, setLanguageMode] = useState<ContractLanguageMode>(
    ContractLanguageMode.Vietnamese,
  );
  const [contractTitle, setContractTitle] = useState(
    "Triển khai hệ thống quản lý hợp đồng điện tử",
  );
  const [draftTerms, setDraftTerms] = useState<MockContractTerm[]>([]);

  // Step 2: Items, Filter & Pagination
  const [selectedItems, setSelectedItems] = useState<string[]>([]);
  const [itemFilter, setItemFilter] = useState<"all" | "product" | "service">(
    "all",
  );
  const [catalogPage, setCatalogPage] = useState(1);
  const catalogPageSize = 5;

  useEffect(() => {
    setCatalogPage(1);
  }, [itemFilter]);

  const filteredCatalog = useMemo(() => {
    if (itemFilter === "product")
      return catalogItems.filter(
        (i) => i.itemType === ContractItemType.Product,
      );
    if (itemFilter === "service")
      return catalogItems.filter(
        (i) => i.itemType === ContractItemType.Service,
      );
    return catalogItems;
  }, [catalogItems, itemFilter]);

  const catalogTotalPages = Math.ceil(filteredCatalog.length / catalogPageSize);

  const paginatedCatalog = useMemo(() => {
    const startIndex = (catalogPage - 1) * catalogPageSize;
    return filteredCatalog.slice(startIndex, startIndex + catalogPageSize);
  }, [filteredCatalog, catalogPage]);

  // Step 3: Terms & Dates
  const [effectiveDate, setEffectiveDate] = useState("2026-08-01");
  const [expiredDate, setExpiredDate] = useState("2027-08-01");
  // -- COMPUTED VALUES --
  const selectedCustomer = customers.find(
    (item) => item.customerId === Number(customerId),
  );
  const selectedTemplate = mockContractTemplates.find(
    (item) => item.versionId === Number(templateVersionId),
  );

  const selectedCatalogItems = catalogItems.filter((item) =>
    selectedItems.includes(item.id),
  );

  const totalValue = selectedCatalogItems.reduce(
    (sum, item) => sum + item.unitPrice * item.quantity,
    0,
  );

  const progressValue = ((currentStep + 1) / steps.length) * 100;

  const isStepCompleted = (stepIdx: number) => {
    if (stepIdx === 0)
      return !!customerId && !!templateVersionId && !!contractTitle.trim();
    if (stepIdx === 1) return selectedItems.length > 0;
    if (stepIdx === 2)
      return (
        !!effectiveDate &&
        !!expiredDate &&
        draftTerms.length > 0 &&
        draftTerms.every(
          (term) => term.termTitle.trim() && term.termContent.trim(),
        )
      );
    return false;
  };

  const canGoNext = useMemo(() => {
    if (currentStep === 3) return true;
    return isStepCompleted(currentStep);
  }, [
    currentStep,
    customerId,
    templateVersionId,
    contractTitle,
    selectedItems.length,
    effectiveDate,
    expiredDate,
    draftTerms,
  ]);

  const handleContractTypeChange = (value: string) => {
    const nextType = Number(value) as ContractType;
    setContractType(nextType);

    if (selectedTemplate?.contractType !== nextType) {
      setTemplateVersionId("");
      setDraftTerms([]);
    }
  };

  const handleTemplateSelect = (versionId: number) => {
    const template = mockContractTemplates.find(
      (item) => item.versionId === versionId,
    );
    if (!template) return;

    setTemplateVersionId(String(versionId));
    setDraftTerms(cloneMockTerms(template.terms));
  };

  const toggleCatalogItem = (id: string) => {
    setSelectedItems((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id],
    );
  };

  const updateQuantity = (id: string, quantity: number) => {
    setCatalogItems((prev) =>
      prev.map((item) =>
        item.id === id ? { ...item, quantity: Math.max(1, quantity) } : item,
      ),
    );
  };

  // -- SUBMIT ACTION --
  const handleSubmit = async () => {
    // 1. Validate Step 0: Khách hàng & Mẫu
    if (!customerId) {
      toast.error("Vui lòng chọn khách hàng / đối tác.");
      setCurrentStep(0);
      return;
    }
    if (!contractTitle.trim()) {
      toast.error("Vui lòng nhập tên hợp đồng.");
      setCurrentStep(0);
      return;
    }
    if (!templateVersionId) {
      toast.error("Vui lòng chọn template hợp đồng.");
      setCurrentStep(0);
      return;
    }

    // 2. Validate Step 1: Sản phẩm
    if (selectedItems.length === 0) {
      toast.error("Vui lòng chọn ít nhất 1 sản phẩm hoặc dịch vụ.");
      setCurrentStep(1);
      return;
    }

    // 3. Validate Step 2: Điều khoản
    if (!effectiveDate || !expiredDate) {
      toast.error("Vui lòng chọn thời hạn hiệu lực của hợp đồng.");
      setCurrentStep(2);
      return;
    }
    if (draftTerms.length === 0) {
      toast.error("Hợp đồng phải có ít nhất một điều khoản.");
      setCurrentStep(2);
      return;
    }
    if (
      draftTerms.some(
        (term) => !term.termTitle.trim() || !term.termContent.trim(),
      )
    ) {
      toast.error("Vui lòng nhập đầy đủ tiêu đề và nội dung điều khoản.");
      setCurrentStep(2);
      return;
    }

    setIsSubmitting(true);
    try {
      const itemsPayload: CreateContractItemRequest[] =
        selectedCatalogItems.map((item, index) => ({
          itemType: item.itemType,
          sourceProductId:
            item.itemType === ContractItemType.Product ? item.originalId : null,
          sourceServiceId:
            item.itemType === ContractItemType.Service ? item.originalId : null,
          itemName: item.itemName,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          discountPercent: item.discountPercent,
          vatPercent: item.vatPercent,
          displayOrder: index + 1,
        }));

      const payload: CreateContractRequest = {
        customerId: Number(customerId),
        contractType: contractType,
        templateVersionId: Number(templateVersionId), // Truyền chính xác Template ID đã chọn
        contractName: contractTitle.trim(),
        effectiveDate: effectiveDate
          ? new Date(effectiveDate).toISOString()
          : null,
        expireDate: expiredDate ? new Date(expiredDate).toISOString() : null,
        currencyCode: "VND",
        languageMode: languageMode,
        items: itemsPayload,
      };

      const response = await contractApi.create(payload);

      toast.success(`Tạo hợp đồng thành công! Mã HĐ: ${response.contractCode}`);
      router.push("/contracts");
    } catch (error: any) {
      const data = error?.response?.data;
      let errorMessage = "Đã xảy ra lỗi khi tạo hợp đồng.";

      if (data) {
        if (data.errors) {
          errorMessage = Object.values(data.errors).flat().join(", ");
        } else if (typeof data === "string") {
          errorMessage = data;
        } else {
          errorMessage = data.message || data.title || errorMessage;
        }
      }

      toast.error(errorMessage);
    } finally {
      setIsSubmitting(false);
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
                Tạo hợp đồng nháp
              </h1>
            </div>
            <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
              Luồng khởi tạo hợp đồng trực tiếp nối API.
            </p>
          </div>
        </div>

        <Card className="shadow-sm py-0">
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
                <button
                  type="button"
                  key={step.title}
                  onClick={() => setCurrentStep(index)}
                  className={`rounded-xl border p-3 cursor-pointer hover:border-primary/50 transition-colors text-left ${
                    index === currentStep
                      ? "border-primary bg-primary/5"
                      : isStepCompleted(index)
                        ? "border-emerald-200 bg-emerald-50/60 dark:border-emerald-900 dark:bg-emerald-950/20"
                        : "bg-muted/30"
                  }`}
                >
                  <div className="flex items-center gap-2">
                    <div
                      className={`flex size-7 items-center justify-center rounded-full text-xs font-semibold ${
                        isStepCompleted(index) || index === currentStep
                          ? "bg-primary text-primary-foreground"
                          : "bg-muted text-muted-foreground"
                      }`}
                    >
                      {isStepCompleted(index) ? (
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
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
          <Card className="shadow-sm">
            <CardHeader>
              <CardTitle>
                {currentStep === 0 && "Thiết lập cơ bản"}
                {currentStep === 1 && "Sản phẩm / dịch vụ trong hợp đồng"}
                {currentStep === 2 && "Điều khoản & thời hạn"}
                {currentStep === 3 && "Xem trước hợp đồng nháp"}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* STEP 1: CUSTOMER & TEMPLATE */}
              {currentStep === 0 && (
                <div className="grid gap-5 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label>
                      Khách hàng / đối tác{" "}
                      <span className="text-red-500">*</span>
                    </Label>
                    <Select value={customerId} onValueChange={setCustomerId}>
                      <SelectTrigger className="w-full">
                        <SelectValue
                          placeholder={
                            isLoadingCustomers
                              ? "Đang tải..."
                              : "Chọn khách hàng"
                          }
                        />
                      </SelectTrigger>
                      <SelectContent>
                        {customers.map((customer) => (
                          <SelectItem
                            key={customer.customerId}
                            value={String(customer.customerId)}
                          >
                            {customer.customerCompany ||
                              customer.customerFullName}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2">
                    <Label>Loại hợp đồng</Label>
                    <Select
                      value={String(contractType)}
                      onValueChange={handleContractTypeChange}
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {contractTypeOptions.map((type) => (
                          <SelectItem key={type} value={String(type)}>
                            {getContractTypeLabel(type)}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2 md:col-span-2">
                    <Label>
                      Tên hợp đồng <span className="text-red-500">*</span>
                    </Label>
                    <Input
                      value={contractTitle}
                      onChange={(event) => setContractTitle(event.target.value)}
                      placeholder="Nhập tên hợp đồng..."
                    />
                  </div>

                  <div className="space-y-3 md:col-span-2">
                    <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
                      <div>
                        <Label>
                          Template hợp đồng{" "}
                          <span className="text-red-500">*</span>
                        </Label>
                        <p className="mt-1 text-xs text-muted-foreground">
                          Chọn mẫu phù hợp để nạp sẵn bộ điều khoản.
                        </p>
                      </div>
                      <Badge variant="secondary">Dữ liệu mock</Badge>
                    </div>

                    <div className="grid gap-3 lg:grid-cols-3">
                      {mockContractTemplates.map((template) => {
                        const selected =
                          template.versionId === Number(templateVersionId);
                        const compatible =
                          template.contractType === contractType;

                        return (
                          <button
                            key={template.versionId}
                            type="button"
                            disabled={!compatible}
                            onClick={() =>
                              handleTemplateSelect(template.versionId)
                            }
                            className={`rounded-2xl border p-4 text-left transition-all ${
                              selected
                                ? "border-primary bg-primary/5 shadow-sm ring-1 ring-primary"
                                : compatible
                                  ? "bg-background hover:border-primary/50 hover:bg-accent/40"
                                  : "cursor-not-allowed bg-muted/30 opacity-50"
                            }`}
                          >
                            <div className="flex items-start justify-between gap-3">
                              <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
                                <LayoutTemplate className="size-5" />
                              </div>
                              {selected ? (
                                <Badge className="gap-1">
                                  <CheckCircle2 className="size-3.5" />
                                  Đã chọn
                                </Badge>
                              ) : (
                                <Badge variant="outline">
                                  v{template.version}
                                </Badge>
                              )}
                            </div>

                            <p className="mt-4 font-semibold leading-snug">
                              {template.name}
                            </p>
                            <p className="mt-2 line-clamp-2 text-xs leading-5 text-muted-foreground">
                              {template.description}
                            </p>

                            <div className="mt-4 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
                              <span>{template.terms.length} điều khoản</span>
                              <span>•</span>
                              <span>{template.templateCode}</span>
                            </div>
                            <p className="mt-2 text-xs text-muted-foreground">
                              {compatible
                                ? `Cập nhật ${template.updatedAt}`
                                : getContractTypeLabel(template.contractType)}
                            </p>
                          </button>
                        );
                      })}
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label>Ngôn ngữ hợp đồng</Label>
                    <Select
                      value={String(languageMode)}
                      onValueChange={(value) =>
                        setLanguageMode(Number(value) as ContractLanguageMode)
                      }
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem
                          value={String(ContractLanguageMode.Vietnamese)}
                        >
                          {getContractLanguageModeLabel(
                            ContractLanguageMode.Vietnamese,
                          )}
                        </SelectItem>
                        <SelectItem
                          value={String(ContractLanguageMode.Bilingual)}
                        >
                          {getContractLanguageModeLabel(
                            ContractLanguageMode.Bilingual,
                          )}
                        </SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              )}

              {/* STEP 2: ITEMS, FILTER, PAGINATION */}
              {currentStep === 1 && (
                <div className="space-y-4">
                  {/* Filter Tabs */}
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between border-b pb-2">
                    <div className="flex bg-muted p-1 rounded-lg">
                      <button
                        onClick={() => setItemFilter("all")}
                        className={`px-3 py-1.5 text-sm font-medium rounded-md transition-all ${
                          itemFilter === "all"
                            ? "bg-background shadow-sm text-foreground"
                            : "text-muted-foreground hover:text-foreground"
                        }`}
                      >
                        Tất cả
                      </button>
                      <button
                        onClick={() => setItemFilter("product")}
                        className={`px-3 py-1.5 text-sm font-medium rounded-md transition-all ${
                          itemFilter === "product"
                            ? "bg-background shadow-sm text-foreground"
                            : "text-muted-foreground hover:text-foreground"
                        }`}
                      >
                        Sản phẩm
                      </button>
                      <button
                        onClick={() => setItemFilter("service")}
                        className={`px-3 py-1.5 text-sm font-medium rounded-md transition-all ${
                          itemFilter === "service"
                            ? "bg-background shadow-sm text-foreground"
                            : "text-muted-foreground hover:text-foreground"
                        }`}
                      >
                        Dịch vụ
                      </button>
                    </div>
                    <div className="text-sm font-medium text-muted-foreground px-1">
                      Đã chọn:{" "}
                      <span className="text-primary font-bold">
                        {selectedItems.length}
                      </span>
                    </div>
                  </div>

                  {isLoadingCatalog && (
                    <div className="py-8 text-center text-sm text-muted-foreground flex flex-col items-center justify-center gap-2">
                      <Loader2 className="size-5 animate-spin text-primary" />
                      Đang tải danh sách...
                    </div>
                  )}

                  {!isLoadingCatalog && paginatedCatalog.length === 0 && (
                    <div className="py-8 text-center text-sm text-muted-foreground bg-muted/30 rounded-xl border border-dashed">
                      Không tìm thấy{" "}
                      {itemFilter === "product"
                        ? "sản phẩm"
                        : itemFilter === "service"
                          ? "dịch vụ"
                          : "sản phẩm/dịch vụ"}{" "}
                      nào.
                    </div>
                  )}

                  {/* Danh sách Paginated */}
                  <div className="space-y-3">
                    {paginatedCatalog.map((item) => {
                      const selected = selectedItems.includes(item.id);
                      return (
                        <div
                          key={item.id}
                          className={`w-full rounded-xl border p-4 text-left transition-colors ${
                            selected
                              ? "border-primary bg-primary/5"
                              : "bg-background hover:bg-accent"
                          }`}
                        >
                          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                            <button
                              type="button"
                              className="flex items-start gap-3 flex-1 cursor-pointer text-left"
                              onClick={() => toggleCatalogItem(item.id)}
                            >
                              <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
                                <Package className="size-5" />
                              </div>
                              <div>
                                <p className="font-semibold">{item.itemName}</p>
                                <p className="mt-1 text-sm text-muted-foreground">
                                  Phân loại:{" "}
                                  {item.itemType === ContractItemType.Product
                                    ? "Sản phẩm"
                                    : "Dịch vụ"}
                                </p>
                              </div>
                            </button>
                            <div className="flex items-center gap-3">
                              {selected && (
                                <div className="flex items-center gap-2 mr-2">
                                  <span className="text-xs font-medium text-muted-foreground">
                                    SL:
                                  </span>
                                  <Input
                                    type="number"
                                    min={1}
                                    maxLength={9}
                                    value={item.quantity}
                                    onChange={(e) =>
                                      updateQuantity(
                                        item.id,
                                        Number(e.target.value),
                                      )
                                    }
                                    className="w-32 h-8 text-center"
                                  />
                                </div>
                              )}
                              <span className="font-semibold text-primary">
                                {formatCurrency(item.unitPrice * item.quantity)}
                              </span>
                              <button
                                type="button"
                                className="cursor-pointer ml-1"
                                onClick={() => toggleCatalogItem(item.id)}
                              >
                                {selected ? (
                                  <Badge className="gap-1">
                                    <CheckCircle2 className="size-3.5" />
                                    Đã chọn
                                  </Badge>
                                ) : (
                                  <span className="h-6 px-3 border rounded-full text-xs font-medium flex items-center justify-center text-muted-foreground hover:bg-muted">
                                    Chọn
                                  </span>
                                )}
                              </button>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>

                  {/* Pagination Controls */}
                  {catalogTotalPages > 1 && (
                    <div className="flex items-center justify-between pt-4">
                      <p className="text-sm text-muted-foreground">
                        Trang {catalogPage} / {catalogTotalPages}
                      </p>
                      <div className="flex gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() =>
                            setCatalogPage((p) => Math.max(1, p - 1))
                          }
                          disabled={catalogPage === 1}
                        >
                          <ChevronLeft className="size-4 mr-1" /> Trước
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() =>
                            setCatalogPage((p) =>
                              Math.min(catalogTotalPages, p + 1),
                            )
                          }
                          disabled={catalogPage === catalogTotalPages}
                        >
                          Sau <ChevronRight className="size-4 ml-1" />
                        </Button>
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* STEP 3: TERMS & DATES */}
              {currentStep === 2 && (
                <div className="space-y-5">
                  <div className="rounded-2xl border bg-muted/20 p-4">
                    <div className="space-y-2">
                      <Label>Thời hạn hiệu lực</Label>
                      <DateRangeFilter
                        dateRange={{
                          from: effectiveDate
                            ? new Date(effectiveDate)
                            : undefined,
                          to: expiredDate ? new Date(expiredDate) : undefined,
                        }}
                        onChange={(range) => {
                          setEffectiveDate(
                            range.from
                              ? format(range.from, "yyyy-MM-dd")
                              : "",
                          );
                          setExpiredDate(
                            range.to ? format(range.to, "yyyy-MM-dd") : "",
                          );
                        }}
                      />
                      <p className="text-xs text-muted-foreground">
                        Thời hạn này được dùng cho bản hợp đồng nháp khi khởi
                        tạo.
                      </p>
                    </div>
                  </div>

                  <CreateContractTermsMock
                    terms={draftTerms}
                    templateName={selectedTemplate?.name}
                    onChange={setDraftTerms}
                  />
                </div>
              )}

              {/* STEP 4: PREVIEW */}
              {currentStep === 3 && (
                <div className="space-y-5">
                  <Alert>
                    <FileSignature className="size-4" />
                    <AlertTitle>Sẵn sàng khởi tạo</AlertTitle>
                    <AlertDescription>
                      Thông tin cơ bản được gửi tới backend để tạo hợp đồng.
                      Bộ điều khoản bên dưới đang là mock và chỉ dùng để xem
                      trước giao diện.
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
                          {selectedCustomer?.customerCompany || "Chưa chọn"}
                        </p>
                        <p className="text-sm text-muted-foreground">
                          {selectedCustomer?.customerFullName}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-muted-foreground">
                          Mẫu hợp đồng
                        </p>
                        <p className="font-semibold text-blue-600">
                          {selectedTemplate?.name || "Chưa chọn template"}
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
                        Sản phẩm / dịch vụ ({selectedCatalogItems.length})
                      </p>
                      <div className="space-y-2">
                        {selectedCatalogItems.map((item) => (
                          <div
                            key={item.id}
                            className="flex items-center justify-between rounded-lg bg-background p-3 text-sm"
                          >
                            <span>{item.itemName}</span>
                            <span className="font-medium">
                              {formatCurrency(item.unitPrice)}
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>

                    <Separator className="my-5" />

                    <div>
                      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
                        <p className="text-sm font-semibold">
                          Điều khoản hợp đồng ({draftTerms.length})
                        </p>
                        <Badge variant="secondary">Dữ liệu mock</Badge>
                      </div>
                      <div className="space-y-2">
                        {draftTerms.map((term) => (
                          <div
                            key={term.id}
                            className="rounded-xl bg-background p-3"
                          >
                            <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                              <div>
                                <p className="text-sm font-semibold">
                                  {term.termTitle}
                                </p>
                                <p className="mt-1 line-clamp-2 text-sm leading-6 text-muted-foreground">
                                  {term.termContent}
                                </p>
                              </div>
                              <Badge
                                variant="outline"
                                className={
                                  term.isNegotiable
                                    ? "shrink-0 text-emerald-700"
                                    : "shrink-0 text-amber-700"
                                }
                              >
                                {term.isNegotiable
                                  ? "Có thể đàm phán"
                                  : "Cố định"}
                              </Badge>
                            </div>
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
                  disabled={currentStep === 0 || isSubmitting}
                  onClick={() =>
                    setCurrentStep((step) => Math.max(step - 1, 0))
                  }
                >
                  <ArrowLeft className="size-4 mr-2" />
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
                    <ArrowRight className="size-4 ml-2" />
                  </Button>
                ) : (
                  <Button onClick={handleSubmit} disabled={isSubmitting}>
                    {isSubmitting ? (
                      <Loader2 className="size-4 mr-2 animate-spin" />
                    ) : (
                      <Save className="size-4 mr-2" />
                    )}
                    {isSubmitting ? "Đang lưu..." : "Lưu bản nháp"}
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
                      {selectedCustomer?.customerCompany ||
                        "Chưa chọn khách hàng"}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {selectedCustomer?.customerEmail || "Chọn khách ở bước 1"}
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-3">
                  <div className="flex size-9 items-center justify-center rounded-xl bg-primary/10 text-primary">
                    <LayoutTemplate className="size-4" />
                  </div>
                  <div>
                    <p className="text-sm font-medium line-clamp-1">
                      {selectedTemplate?.name || "Chưa chọn Template"}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {getContractTypeLabel(contractType)}
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-3">
                  <div className="flex size-9 items-center justify-center rounded-xl bg-primary/10 text-primary">
                    <ShieldCheck className="size-4" />
                  </div>
                  <div>
                    <p className="text-sm font-medium">
                      {draftTerms.length} điều khoản
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {
                        draftTerms.filter((term) => term.isNegotiable).length
                      }{" "}
                      điều khoản có thể đàm phán
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
          </div>
        </div>
      </div>
    </>
  );
}
