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
  Search,
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
import { Switch } from "@/components/ui/switch";
import { DateRangeFilter } from "@/components/ui/custom/date-range-filter";
import { DecimalInput } from "@/components/ui/custom/decimal-input";
import { IntegerInput } from "@/components/ui/custom/integer-input";
import { format } from "date-fns";
import { CreateContractTermsMock } from "@/components/contracts/create-contract-terms-mock";

import {
  ContractType,
  ContractLanguageMode,
  ContractItemType,
  ContractItemDiscountMode,
  CreateContractRequest,
  CreateContractItemRequest,
  EligibleParentContractResponse,
  contractApi,
  getContractTypeLabel,
  getContractLanguageModeLabel,
} from "@/services/contract-api";
import {
  calculateContractItemAmounts,
  calculateContractTotals,
} from "@/lib/contract-finance";
import { customerApi, CustomerResponse } from "@/services/customers-api";
import { employeeApi, EmployeeResponse } from "@/services/employees-api";
import { productApi } from "@/services/catalog/products-api";
import { serviceApi } from "@/services/catalog/services-api";
import {
  cloneMockTerms,
  MockContractTerm,
  mockContractTemplates,
} from "@/services/contract-templates-mock";
import { useAuthStore } from "@/hooks/use-auth-store";

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
  itemNameEn: string;
  itemType: ContractItemType;
  unitPrice: number;
  quantity: number;
  discountMode: ContractItemDiscountMode;
  discountPercent: number;
  fixedDiscountAmount: number;
  isTaxable: boolean;
  vatPercent: number;
};

const contractTypeOptions = [
  ContractType.SoftwareSupply,
  ContractType.SoftwareMaintenance,
  ContractType.SoftwareUpkeep,
];

const formatCurrency = (amount: number, currencyCode: string) => {
  const currency = currencyCode.trim().toUpperCase() || "VND";
  return new Intl.NumberFormat(currency === "VND" ? "vi-VN" : "en-US", {
    style: "currency",
    currency,
    maximumFractionDigits: currency === "VND" ? 0 : 2,
  }).format(amount);
};

export default function CreateContractPage() {
  const router = useRouter();
  const user = useAuthStore((state) => state.user);

  // -- STATES --
  const [currentStep, setCurrentStep] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [customers, setCustomers] = useState<CustomerResponse[]>([]);
  const [isLoadingCustomers, setIsLoadingCustomers] = useState(true);

  const [employees, setEmployees] = useState<EmployeeResponse[]>([]);
  const [isLoadingEmployees, setIsLoadingEmployees] = useState(true);

  const [catalogItems, setCatalogItems] = useState<CatalogItem[]>([]);
  const [isLoadingCatalog, setIsLoadingCatalog] = useState(true);

  // Fetch data
  useEffect(() => {
    const fetchData = async () => {
      try {
        const [customerRes, productRes, serviceRes, employeeRes] =
          await Promise.all([
            customerApi.getList({ page: 1, pageSize: 100 }),
            productApi.getList({ page: 1, pageSize: 100 }),
            serviceApi.getList({ page: 1, pageSize: 100 }),
            employeeApi.getList({ page: 1, pageSize: 100 }),
          ]);

        setCustomers(customerRes.items);
        setEmployees(employeeRes.items || []);

        const mappedProducts: CatalogItem[] = productRes.items.map((p) => ({
          id: `p-${p.productId}`,
          originalId: p.productId,
          itemName: p.productName || "Sản phẩm không tên",
          itemNameEn: "",
          itemType: ContractItemType.Product,
          unitPrice: p.productPrice || 0,
          quantity: 1,
          discountMode: ContractItemDiscountMode.None,
          discountPercent: 0,
          fixedDiscountAmount: 0,
          isTaxable: true,
          vatPercent: 10,
        }));

        const mappedServices: CatalogItem[] = serviceRes.items.map((s) => ({
          id: `s-${s.serviceId}`,
          originalId: s.serviceId,
          itemName: s.serviceName || "Dịch vụ không tên",
          itemNameEn: "",
          itemType: ContractItemType.Service,
          unitPrice: s.servicePrice || 0,
          quantity: 1,
          discountMode: ContractItemDiscountMode.None,
          discountPercent: 0,
          fixedDiscountAmount: 0,
          isTaxable: true,
          vatPercent: 10,
        }));

        setCatalogItems([...mappedProducts, ...mappedServices]);
      } catch (err) {
        console.error("Failed to fetch data:", err);
      } finally {
        setIsLoadingCustomers(false);
        setIsLoadingEmployees(false);
        setIsLoadingCatalog(false);
      }
    };
    fetchData();
  }, []);

  // Step 1: Customer & Basic Info
  const [customerId, setCustomerId] = useState<string>("");
  const [responsibleEmployeeId, setResponsibleEmployeeId] =
    useState<string>("");

  const handleAssignToMe = () => {
    if (!user?.employeeId) return;

    setEmployees((currentEmployees) => {
      const currentUserExists = currentEmployees.some(
        (employee) => employee.employeeId === user.employeeId,
      );

      if (currentUserExists) return currentEmployees;

      return [
        {
          employeeId: user.employeeId,
          employeeCode: user.employeeCode,
          employeeAccount: user.employeeAccount,
          employeeFullName: user.employeeFullName,
          employeeMobile: user.employeeMobile,
          employeeEmail: user.employeeEmail,
          departmentId: user.departmentId,
          status: user.status,
        },
        ...currentEmployees,
      ];
    });
    setResponsibleEmployeeId(String(user.employeeId));
  };
  const [contractType, setContractType] = useState<ContractType>(
    ContractType.SoftwareSupply,
  );
  const [parentContractId, setParentContractId] = useState<string>("");
  const [eligibleParents, setEligibleParents] = useState<
    EligibleParentContractResponse[]
  >([]);
  const [isLoadingEligibleParents, setIsLoadingEligibleParents] =
    useState<boolean>(false);
  const [templateVersionId, setTemplateVersionId] = useState<string>("");
  const [templateSearch, setTemplateSearch] = useState("");
  const [templatePage, setTemplatePage] = useState(1);
  const templatePageSize = 3;
  const [languageMode, setLanguageMode] = useState<ContractLanguageMode>(
    ContractLanguageMode.Vietnamese,
  );
  const [currencyCode, setCurrencyCode] = useState("VND");
  const [contractTitle, setContractTitle] = useState(
    "Triển khai hệ thống quản lý hợp đồng điện tử",
  );
  const [contractTitleEn, setContractTitleEn] = useState("");
  const [draftTerms, setDraftTerms] = useState<MockContractTerm[]>([]);

  const filteredTemplates = useMemo(() => {
    const normalizedSearch = templateSearch.trim().toLocaleLowerCase("vi");

    if (!normalizedSearch) return mockContractTemplates;

    return mockContractTemplates.filter((template) =>
      [
        template.name,
        template.templateCode,
        template.description,
        template.version,
        getContractTypeLabel(template.contractType),
      ].some((value) =>
        value.toLocaleLowerCase("vi").includes(normalizedSearch),
      ),
    );
  }, [templateSearch]);

  const templateTotalPages = Math.max(
    1,
    Math.ceil(filteredTemplates.length / templatePageSize),
  );

  const paginatedTemplates = useMemo(() => {
    const startIndex = (templatePage - 1) * templatePageSize;
    return filteredTemplates.slice(startIndex, startIndex + templatePageSize);
  }, [filteredTemplates, templatePage]);

  // Fetch eligible parent contracts when customer or contractType changes
  useEffect(() => {
    if (
      (contractType === ContractType.SoftwareMaintenance ||
        contractType === ContractType.SoftwareUpkeep) &&
      customerId
    ) {
      const fetchEligibleParents = async () => {
        setIsLoadingEligibleParents(true);
        try {
          const res = await contractApi.getEligibleParents({
            customerId: Number(customerId),
            targetContractType: contractType,
            page: 1,
            pageSize: 100,
          });
          setEligibleParents(res.items || []);
        } catch (error) {
          console.error("Lỗi khi tải danh sách hợp đồng nguồn:", error);
          setEligibleParents([]);
        } finally {
          setIsLoadingEligibleParents(false);
        }
      };
      fetchEligibleParents();
    } else {
      setEligibleParents([]);
      setParentContractId("");
    }
  }, [customerId, contractType]);

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
  const selectedEmployee = employees.find(
    (item) => item.employeeId === Number(responsibleEmployeeId),
  );
  const selectedParentContract = eligibleParents.find(
    (item) => item.contractId === Number(parentContractId),
  );
  const selectedTemplate = mockContractTemplates.find(
    (item) => item.versionId === Number(templateVersionId),
  );

  const selectedCatalogItems = catalogItems.filter((item) =>
    selectedItems.includes(item.id),
  );

  const financialTotals = useMemo(
    () => calculateContractTotals(selectedCatalogItems, currencyCode),
    [selectedCatalogItems, currencyCode],
  );
  const totalValue = financialTotals.totalPayment;

  const progressValue = ((currentStep + 1) / steps.length) * 100;

  const isStepCompleted = (stepIdx: number) => {
    if (stepIdx === 0) {
      const isParentRequired =
        contractType === ContractType.SoftwareMaintenance ||
        contractType === ContractType.SoftwareUpkeep;
      return (
        !!customerId &&
        !!responsibleEmployeeId &&
        !!templateVersionId &&
        !!contractTitle.trim() &&
        (languageMode !== ContractLanguageMode.Bilingual ||
          !!contractTitleEn.trim()) &&
        (!isParentRequired || !!parentContractId)
      );
    }
    if (stepIdx === 1)
      return (
        selectedItems.length > 0 &&
        (languageMode !== ContractLanguageMode.Bilingual ||
          selectedCatalogItems.every((item) => item.itemNameEn.trim()))
      );
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
    responsibleEmployeeId,
    contractType,
    parentContractId,
    templateVersionId,
    contractTitle,
    contractTitleEn,
    languageMode,
    selectedItems.length,
    selectedCatalogItems,
    effectiveDate,
    expiredDate,
    draftTerms,
  ]);

  const handleContractTypeChange = (value: string) => {
    const nextType = Number(value) as ContractType;
    setContractType(nextType);
    setParentContractId("");
    setTemplatePage(1);

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

  const updateCatalogItem = (id: string, patch: Partial<CatalogItem>) => {
    setCatalogItems((prev) =>
      prev.map((item) => (item.id === id ? { ...item, ...patch } : item)),
    );
  };

  const updateQuantity = (id: string, quantity: number) => {
    updateCatalogItem(id, { quantity: Math.max(1, Math.trunc(quantity || 1)) });
  };

  const updateDiscountMode = (
    id: string,
    discountMode: ContractItemDiscountMode,
  ) => {
    updateCatalogItem(id, {
      discountMode,
      discountPercent: 0,
      fixedDiscountAmount: 0,
    });
  };

  // -- SUBMIT ACTION --
  const handleSubmit = async () => {
    // 1. Validate Step 0: Khách hàng & Mẫu
    if (!customerId) {
      toast.error("Vui lòng chọn khách hàng / đối tác.");
      setCurrentStep(0);
      return;
    }
    if (!responsibleEmployeeId) {
      toast.error("Vui lòng chọn nhân viên phụ trách.");
      setCurrentStep(0);
      return;
    }
    if (
      (contractType === ContractType.SoftwareMaintenance ||
        contractType === ContractType.SoftwareUpkeep) &&
      !parentContractId
    ) {
      toast.error(
        "Vui lòng chọn hợp đồng nguồn (hợp đồng cung cấp phần mềm gốc).",
      );
      setCurrentStep(0);
      return;
    }
    if (!contractTitle.trim()) {
      toast.error("Vui lòng nhập tên hợp đồng.");
      setCurrentStep(0);
      return;
    }
    if (
      languageMode === ContractLanguageMode.Bilingual &&
      !contractTitleEn.trim()
    ) {
      toast.error("Hợp đồng song ngữ phải có tên hợp đồng tiếng Anh.");
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
    const missingEnglishItem = selectedCatalogItems.find(
      (item) =>
        languageMode === ContractLanguageMode.Bilingual &&
        !item.itemNameEn.trim(),
    );
    if (missingEnglishItem) {
      toast.error(
        `Vui lòng nhập tên tiếng Anh cho “${missingEnglishItem.itemName}”.`,
      );
      setCurrentStep(1);
      return;
    }
    const invalidFinanceItem = selectedCatalogItems.find((item) => {
      const subtotal = item.quantity * item.unitPrice;
      return (
        item.quantity <= 0 ||
        item.unitPrice < 0 ||
        item.discountPercent < 0 ||
        item.discountPercent > 100 ||
        item.fixedDiscountAmount < 0 ||
        item.fixedDiscountAmount > subtotal ||
        item.vatPercent < 0 ||
        item.vatPercent > 100
      );
    });
    if (invalidFinanceItem) {
      toast.error(
        `Thông tin giá, giảm giá hoặc VAT của "${invalidFinanceItem.itemName}" chưa hợp lệ.`,
      );
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
          itemNameEn:
            languageMode === ContractLanguageMode.Bilingual
              ? item.itemNameEn.trim()
              : null,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          discountMode: item.discountMode,
          discountPercent: item.discountPercent,
          fixedDiscountAmount: item.fixedDiscountAmount,
          isTaxable: item.isTaxable,
          vatPercent: item.vatPercent,
          displayOrder: index + 1,
        }));

      const payload: CreateContractRequest = {
        customerId: Number(customerId),
        responsibleEmployeeId: Number(responsibleEmployeeId),
        contractType: contractType,
        templateVersionId: Number(templateVersionId),
        parentContractId:
          contractType !== ContractType.SoftwareSupply && parentContractId
            ? Number(parentContractId)
            : null,
        contractName: contractTitle.trim(),
        contractNameEn:
          languageMode === ContractLanguageMode.Bilingual
            ? contractTitleEn.trim()
            : null,
        effectiveDate: effectiveDate
          ? new Date(effectiveDate).toISOString()
          : null,
        expireDate: expiredDate ? new Date(expiredDate).toISOString() : null,
        currencyCode,
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
                    <Select
                      value={customerId}
                      onValueChange={(val) => {
                        setCustomerId(val);
                        setParentContractId("");
                      }}
                    >
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
                    <div className="flex items-center gap-2">
                      <Label>
                        Nhân viên phụ trách{" "}
                        <span className="text-red-500">*</span>
                      </Label>
                      <button
                        type="button"
                        onClick={handleAssignToMe}
                        disabled={isLoadingEmployees || !user?.employeeId}
                        className="text-xs font-medium text-primary underline-offset-4 hover:underline disabled:pointer-events-none disabled:opacity-50"
                      >
                        Gán cho tôi
                      </button>
                    </div>
                    <Select
                      value={responsibleEmployeeId}
                      onValueChange={setResponsibleEmployeeId}
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue
                          placeholder={
                            isLoadingEmployees
                              ? "Đang tải nhân viên..."
                              : "Chọn nhân viên phụ trách"
                          }
                        />
                      </SelectTrigger>
                      <SelectContent>
                        {employees.map((emp) => (
                          <SelectItem
                            key={emp.employeeId}
                            value={String(emp.employeeId)}
                          >
                            {emp.employeeCode} - {emp.employeeFullName}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2">
                    <Label>
                      Loại hợp đồng <span className="text-red-500">*</span>
                    </Label>
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

                  {/* SELECT HỢP ĐỒNG NGUỒN (CHỈ DÀNH CHO BẢO TRÌ HOẶC DUY TRÌ) */}
                  {contractType !== ContractType.SoftwareSupply && (
                    <div className="space-y-2">
                      <Label>
                        Hợp đồng nguồn (HĐ gốc){" "}
                        <span className="text-red-500">*</span>
                      </Label>
                      <Select
                        value={parentContractId}
                        onValueChange={setParentContractId}
                        disabled={!customerId || isLoadingEligibleParents}
                      >
                        <SelectTrigger className="w-full">
                          <SelectValue
                            placeholder={
                              !customerId
                                ? "Vui lòng chọn khách hàng trước"
                                : isLoadingEligibleParents
                                  ? "Đang tải hợp đồng nguồn..."
                                  : eligibleParents.length === 0
                                    ? "Không tìm thấy HĐ gốc phù hợp"
                                    : "Chọn hợp đồng gốc..."
                            }
                          />
                        </SelectTrigger>
                        <SelectContent>
                          {eligibleParents.map((parent) => (
                            <SelectItem
                              key={parent.contractId}
                              value={String(parent.contractId)}
                            >
                              {parent.contractCode
                                ? `${parent.contractCode} - ${parent.contractName}`
                                : parent.contractName}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  )}

                  {contractType !== ContractType.SoftwareSupply &&
                    customerId &&
                    !isLoadingEligibleParents &&
                    eligibleParents.length === 0 && (
                      <div className="md:col-span-2">
                        <Alert variant="destructive">
                          <AlertTitle>
                            Không có hợp đồng nguồn phù hợp
                          </AlertTitle>
                          <AlertDescription>
                            Khách hàng đã chọn hiện chưa có hợp đồng cung cấp
                            phần mềm gốc nào ở trạng thái đủ điều kiện để tạo
                            hợp đồng bảo trì hoặc duy trì.
                          </AlertDescription>
                        </Alert>
                      </div>
                    )}

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

                  {languageMode === ContractLanguageMode.Bilingual && (
                    <div className="space-y-2 md:col-span-2">
                      <Label>
                        Tên hợp đồng tiếng Anh{" "}
                        <span className="text-red-500">*</span>
                      </Label>
                      <Input
                        value={contractTitleEn}
                        onChange={(event) =>
                          setContractTitleEn(event.target.value)
                        }
                        placeholder="Enter the contract name..."
                      />
                      <p className="text-xs text-muted-foreground">
                        Backend yêu cầu trường này khi chọn hợp đồng song ngữ.
                      </p>
                    </div>
                  )}

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
                    </div>

                    <div className="relative">
                      <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        value={templateSearch}
                        onChange={(event) => {
                          setTemplateSearch(event.target.value);
                          setTemplatePage(1);
                        }}
                        className="pl-9"
                        placeholder="Tìm theo tên, mã hoặc mô tả template..."
                        aria-label="Tìm kiếm template hợp đồng"
                      />
                    </div>

                    <div className="grid gap-3 lg:grid-cols-3">
                      {paginatedTemplates.map((template) => {
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

                    {filteredTemplates.length === 0 ? (
                      <div className="rounded-2xl border border-dashed px-4 py-8 text-center">
                        <LayoutTemplate className="mx-auto size-8 text-muted-foreground/60" />
                        <p className="mt-3 text-sm font-medium">
                          Không tìm thấy template phù hợp
                        </p>
                        <p className="mt-1 text-xs text-muted-foreground">
                          Thử tìm bằng tên, mã template hoặc loại hợp đồng khác.
                        </p>
                      </div>
                    ) : (
                      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                        <p className="text-xs text-muted-foreground">
                          Hiển thị {(templatePage - 1) * templatePageSize + 1}–
                          {Math.min(
                            templatePage * templatePageSize,
                            filteredTemplates.length,
                          )}{" "}
                          trong {filteredTemplates.length} template
                        </p>
                        <div className="flex items-center gap-2">
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={templatePage === 1}
                            onClick={() =>
                              setTemplatePage((page) => Math.max(1, page - 1))
                            }
                          >
                            <ChevronLeft className="size-4" />
                            Trước
                          </Button>
                          <span className="min-w-20 text-center text-xs font-medium">
                            Trang {templatePage}/{templateTotalPages}
                          </span>
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={templatePage === templateTotalPages}
                            onClick={() =>
                              setTemplatePage((page) =>
                                Math.min(templateTotalPages, page + 1),
                              )
                            }
                          >
                            Sau
                            <ChevronRight className="size-4" />
                          </Button>
                        </div>
                      </div>
                    )}
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
                  <div className="space-y-2">
                    <Label>Đồng tiền thanh toán</Label>
                    <Select
                      value={currencyCode}
                      onValueChange={setCurrencyCode}
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="VND">VND — Việt Nam đồng</SelectItem>
                        <SelectItem value="USD">USD — Đô la Mỹ</SelectItem>
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
                      const amounts = calculateContractItemAmounts(
                        item,
                        currencyCode,
                      );
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
                                  <IntegerInput
                                    min={1}
                                    value={item.quantity}
                                    onValueChange={(value) =>
                                      updateQuantity(item.id, value)
                                    }
                                    className="h-8 w-24 bg-white text-center"
                                  />
                                </div>
                              )}
                              <span className="font-semibold text-primary">
                                {formatCurrency(
                                  amounts.lineTotal,
                                  currencyCode,
                                )}
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

                          {selected && (
                            <div className="mt-4 grid gap-3 border-t pt-4 sm:grid-cols-2 xl:grid-cols-5">
                              {languageMode ===
                                ContractLanguageMode.Bilingual && (
                                <div className="space-y-1.5 sm:col-span-2 xl:col-span-5">
                                  <Label className="text-xs text-muted-foreground">
                                    Tên sản phẩm / dịch vụ tiếng Anh
                                  </Label>
                                  <Input
                                    value={item.itemNameEn}
                                    onChange={(event) =>
                                      updateCatalogItem(item.id, {
                                        itemNameEn: event.target.value,
                                      })
                                    }
                                    className="bg-white"
                                    placeholder="Enter the English item name..."
                                  />
                                </div>
                              )}
                              <div className="space-y-1.5">
                                <Label className="text-xs text-muted-foreground">
                                  Đơn giá
                                </Label>
                                <DecimalInput
                                  min={0}
                                  value={item.unitPrice}
                                  className="bg-white"
                                  onValueChange={(value) =>
                                    updateCatalogItem(item.id, {
                                      unitPrice: value,
                                    })
                                  }
                                />
                              </div>

                              <div className="space-y-1.5">
                                <Label className="text-xs text-muted-foreground">
                                  Loại giảm giá
                                </Label>
                                <Select
                                  value={String(item.discountMode)}
                                  onValueChange={(value) =>
                                    updateDiscountMode(
                                      item.id,
                                      Number(value) as ContractItemDiscountMode,
                                    )
                                  }
                                >
                                  <SelectTrigger className="w-full bg-white">
                                    <SelectValue />
                                  </SelectTrigger>
                                  <SelectContent>
                                    <SelectItem
                                      value={String(
                                        ContractItemDiscountMode.None,
                                      )}
                                    >
                                      Không giảm
                                    </SelectItem>
                                    <SelectItem
                                      value={String(
                                        ContractItemDiscountMode.Percentage,
                                      )}
                                    >
                                      Theo phần trăm
                                    </SelectItem>
                                    <SelectItem
                                      value={String(
                                        ContractItemDiscountMode.FixedAmount,
                                      )}
                                    >
                                      Số tiền cố định
                                    </SelectItem>
                                  </SelectContent>
                                </Select>
                              </div>

                              <div className="space-y-1.5">
                                <Label className="text-xs text-muted-foreground">
                                  {item.discountMode ===
                                  ContractItemDiscountMode.FixedAmount
                                    ? "Tiền giảm"
                                    : "Giảm giá (%)"}
                                </Label>
                                <DecimalInput
                                  min={0}
                                  max={
                                    item.discountMode ===
                                    ContractItemDiscountMode.Percentage
                                      ? 100
                                      : amounts.lineSubtotal
                                  }
                                  disabled={
                                    item.discountMode ===
                                    ContractItemDiscountMode.None
                                  }
                                  value={
                                    item.discountMode ===
                                    ContractItemDiscountMode.FixedAmount
                                      ? item.fixedDiscountAmount
                                      : item.discountPercent
                                  }
                                  className="bg-white"
                                  onValueChange={(value) => {
                                    updateCatalogItem(
                                      item.id,
                                      item.discountMode ===
                                        ContractItemDiscountMode.FixedAmount
                                        ? { fixedDiscountAmount: value }
                                        : {
                                            discountPercent: Math.min(
                                              100,
                                              value,
                                            ),
                                          },
                                    );
                                  }}
                                />
                              </div>

                              <div className="space-y-1.5">
                                <Label className="text-xs text-muted-foreground">
                                  Thuế
                                </Label>
                                <div className="flex h-9 items-center justify-between rounded-md border bg-white px-3">
                                  <span className="text-sm">
                                    {item.isTaxable
                                      ? "Chịu thuế"
                                      : "Không chịu thuế"}
                                  </span>
                                  <Switch
                                    checked={item.isTaxable}
                                    onCheckedChange={(checked) =>
                                      updateCatalogItem(item.id, {
                                        isTaxable: checked,
                                        vatPercent: checked
                                          ? item.vatPercent || 10
                                          : 0,
                                      })
                                    }
                                  />
                                </div>
                              </div>

                              <div className="space-y-1.5">
                                <Label className="text-xs text-muted-foreground">
                                  VAT (%)
                                </Label>
                                <DecimalInput
                                  min={0}
                                  max={100}
                                  disabled={!item.isTaxable}
                                  value={item.vatPercent}
                                  className="bg-white"
                                  onValueChange={(value) =>
                                    updateCatalogItem(item.id, {
                                      vatPercent: value,
                                    })
                                  }
                                />
                              </div>

                              <div className="grid gap-2 rounded-lg bg-muted/40 p-3 text-xs sm:col-span-2 sm:grid-cols-4 xl:col-span-5">
                                <div>
                                  <p className="text-muted-foreground">
                                    Tạm tính
                                  </p>
                                  <p className="mt-1 font-semibold">
                                    {formatCurrency(
                                      amounts.lineSubtotal,
                                      currencyCode,
                                    )}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-muted-foreground">
                                    Giảm giá
                                  </p>
                                  <p className="mt-1 font-semibold text-rose-600">
                                    -
                                    {formatCurrency(
                                      amounts.discountAmount,
                                      currencyCode,
                                    )}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-muted-foreground">VAT</p>
                                  <p className="mt-1 font-semibold">
                                    {formatCurrency(
                                      amounts.vatAmount,
                                      currencyCode,
                                    )}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-muted-foreground">
                                    Thành tiền
                                  </p>
                                  <p className="mt-1 font-semibold text-primary">
                                    {formatCurrency(
                                      amounts.lineTotal,
                                      currencyCode,
                                    )}
                                  </p>
                                </div>
                              </div>
                            </div>
                          )}
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
                            range.from ? format(range.from, "yyyy-MM-dd") : "",
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
                      Thông tin cơ bản được gửi tới backend để tạo hợp đồng. Bộ
                      điều khoản bên dưới đang là mock và chỉ dùng để xem trước
                      giao diện.
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
                        {languageMode === ContractLanguageMode.Bilingual && (
                          <p className="mt-1 text-sm text-muted-foreground">
                            {contractTitleEn || "Chưa nhập tên tiếng Anh"}
                          </p>
                        )}
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
                          Nhân viên phụ trách
                        </p>
                        <p className="font-semibold">
                          {selectedEmployee?.employeeFullName || "Chưa chọn"}
                        </p>
                        {selectedEmployee?.employeeCode && (
                          <p className="text-sm text-muted-foreground">
                            Mã NV: {selectedEmployee.employeeCode}
                          </p>
                        )}
                      </div>
                      {selectedParentContract && (
                        <div>
                          <p className="text-xs text-muted-foreground">
                            Hợp đồng nguồn
                          </p>
                          <p className="font-semibold text-amber-600">
                            {selectedParentContract.contractCode ||
                              selectedParentContract.contractName}
                          </p>
                        </div>
                      )}
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
                          {formatCurrency(totalValue, currencyCode)}
                        </p>
                      </div>
                    </div>

                    <Separator className="my-5" />

                    <div>
                      <p className="mb-2 text-sm font-semibold">
                        Sản phẩm / dịch vụ ({selectedCatalogItems.length})
                      </p>
                      <div className="space-y-2">
                        {selectedCatalogItems.map((item) => {
                          const amounts = calculateContractItemAmounts(
                            item,
                            currencyCode,
                          );
                          return (
                            <div
                              key={item.id}
                              className="flex flex-col gap-2 rounded-lg bg-background p-3 text-sm sm:flex-row sm:items-center sm:justify-between"
                            >
                              <div>
                                <p>{item.itemName}</p>
                                {languageMode ===
                                  ContractLanguageMode.Bilingual && (
                                  <p className="text-xs text-muted-foreground">
                                    {item.itemNameEn || "Chưa có tên tiếng Anh"}
                                  </p>
                                )}
                              </div>
                              <div className="text-left sm:text-right">
                                <p className="font-semibold text-primary">
                                  {formatCurrency(
                                    amounts.lineTotal,
                                    currencyCode,
                                  )}
                                </p>
                                <p className="text-xs text-muted-foreground">
                                  Tạm tính{" "}
                                  {formatCurrency(
                                    amounts.lineSubtotal,
                                    currencyCode,
                                  )}
                                  {amounts.discountAmount > 0 &&
                                    ` · Giảm ${formatCurrency(amounts.discountAmount, currencyCode)}`}
                                  {amounts.vatAmount > 0 &&
                                    ` · VAT ${formatCurrency(amounts.vatAmount, currencyCode)}`}
                                </p>
                              </div>
                            </div>
                          );
                        })}
                      </div>

                      <div className="mt-4 space-y-2 rounded-xl border bg-background p-4 text-sm">
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground">
                            Tạm tính
                          </span>
                          <span>
                            {formatCurrency(
                              financialTotals.subtotal,
                              currencyCode,
                            )}
                          </span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground">
                            Tổng giảm giá
                          </span>
                          <span className="text-rose-600">
                            -
                            {formatCurrency(
                              financialTotals.totalDiscount,
                              currencyCode,
                            )}
                          </span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground">
                            Tổng VAT
                          </span>
                          <span>
                            {formatCurrency(
                              financialTotals.totalVat,
                              currencyCode,
                            )}
                          </span>
                        </div>
                        <Separator />
                        <div className="flex items-center justify-between font-semibold">
                          <span>Tổng thanh toán</span>
                          <span className="text-primary">
                            {formatCurrency(
                              financialTotals.totalPayment,
                              currencyCode,
                            )}
                          </span>
                        </div>
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
                      {draftTerms.filter((term) => term.isNegotiable).length}{" "}
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
                      {formatCurrency(totalValue, currencyCode)}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {selectedItems.length} sản phẩm/dịch vụ đã chọn
                    </p>
                  </div>
                </div>

                <div className="space-y-2 rounded-xl bg-muted/30 p-3 text-xs">
                  <div className="flex justify-between gap-3">
                    <span className="text-muted-foreground">Tạm tính</span>
                    <span>
                      {formatCurrency(financialTotals.subtotal, currencyCode)}
                    </span>
                  </div>
                  <div className="flex justify-between gap-3">
                    <span className="text-muted-foreground">Giảm giá</span>
                    <span className="text-rose-600">
                      -
                      {formatCurrency(
                        financialTotals.totalDiscount,
                        currencyCode,
                      )}
                    </span>
                  </div>
                  <div className="flex justify-between gap-3">
                    <span className="text-muted-foreground">VAT</span>
                    <span>
                      {formatCurrency(financialTotals.totalVat, currencyCode)}
                    </span>
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
