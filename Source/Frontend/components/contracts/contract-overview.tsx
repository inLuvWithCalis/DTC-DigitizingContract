"use client";

import { useState, useEffect, useMemo } from "react";
import {
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  FileSignature,
  Loader2,
  Package,
  Plus,
  Search,
  Trash2,
  Truck,
  Users,
  WalletCards,
} from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Progress } from "@/components/ui/progress";
import { Separator } from "@/components/ui/separator";
import { Switch } from "@/components/ui/switch";
import { formatCurrency } from "@/lib/format-currency";
import { DateRangeFilter } from "@/components/ui/custom/date-range-filter";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { productApi } from "@/services/catalog/products-api";
import { serviceApi } from "@/services/catalog/services-api";

import { customerApi, CustomerResponse } from "@/services/customers-api";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Clock } from "lucide-react";

import {
  ContractDetailResponse,
  ContractLanguageMode,
  ContractStatus,
  ContractItemType,
  ContractItemDiscountMode,
  getContractTypeLabel,
} from "@/services/contract-api";
import { InfoCard } from "./contract-helpers";
import { formatDate } from "@/lib/format-date";
import {
  calculateContractItemAmounts,
  calculateContractTotals,
} from "@/lib/contract-finance";

export function ContractOverview({
  contract,
  setContract,
  onOpenTransferModal,
}: {
  contract: ContractDetailResponse;
  setContract: React.Dispatch<
    React.SetStateAction<ContractDetailResponse | null>
  >;
  onOpenTransferModal?: () => void;
}) {
  const isEditable =
    (contract.status === ContractStatus.Draft ||
      (contract.status === ContractStatus.Negotiating &&
        contract.currentVersion.sourceVersionId != null)) &&
    !contract.currentVersion.isLocked;

  const draftFinancialTotals = useMemo(
    () =>
      calculateContractTotals(
        contract.currentVersion?.items || [],
        contract.currencyCode,
      ),
    [contract.currentVersion?.items, contract.currencyCode],
  );

  const persistedFinancialTotals = {
    subtotal:
      contract.subtotal ?? contract.currentVersion?.subtotal ?? 0,
    totalDiscount:
      contract.totalDiscount ?? contract.currentVersion?.totalDiscount ?? 0,
    totalVat:
      contract.totalVat ?? contract.currentVersion?.totalVat ?? 0,
    totalPayment:
      contract.totalPayment ??
      contract.currentVersion?.totalPayment ??
      contract.totalAmount ??
      0,
  };

  const hasUnsavedFinancialChanges =
    isEditable &&
    (Math.abs(
      draftFinancialTotals.subtotal - persistedFinancialTotals.subtotal,
    ) > 0.009 ||
      Math.abs(
        draftFinancialTotals.totalDiscount -
          persistedFinancialTotals.totalDiscount,
      ) > 0.009 ||
      Math.abs(
        draftFinancialTotals.totalVat - persistedFinancialTotals.totalVat,
      ) > 0.009 ||
      Math.abs(
        draftFinancialTotals.totalPayment -
          persistedFinancialTotals.totalPayment,
      ) > 0.009);

  const financialTotals = isEditable
    ? draftFinancialTotals
    : persistedFinancialTotals;

  const [customers, setCustomers] = useState<CustomerResponse[]>([]);

  useEffect(() => {
    if (isEditable) {
      customerApi
        .getList({ page: 1, pageSize: 100 })
        .then((res) => setCustomers(res.items || []))
        .catch((err) => console.error(err));
    }
  }, [isEditable]);

  const [catalogItems, setCatalogItems] = useState<any[]>([]);
  const [isCatalogOpen, setIsCatalogOpen] = useState(false);
  const [isLoadingCatalog, setIsLoadingCatalog] = useState(false);

  const [catalogSearchQuery, setCatalogSearchQuery] = useState("");
  const [debouncedCatalogQuery, setDebouncedCatalogQuery] = useState("");

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedCatalogQuery(catalogSearchQuery);
    }, 500);
    return () => clearTimeout(handler);
  }, [catalogSearchQuery]);

  const [itemsSearchQuery, setItemsSearchQuery] = useState("");
  const [itemsCurrentPage, setItemsCurrentPage] = useState(1);
  const itemsPerPage = 5;

  const filteredItems = (contract.currentVersion?.items || []).filter((item) =>
    item.itemName.toLowerCase().includes(itemsSearchQuery.toLowerCase()),
  );

  const itemsTotalPages = Math.ceil(filteredItems.length / itemsPerPage);

  useEffect(() => {
    if (itemsCurrentPage > itemsTotalPages && itemsTotalPages > 0) {
      setItemsCurrentPage(itemsTotalPages);
    }
  }, [itemsTotalPages, itemsCurrentPage]);

  const paginatedItems = filteredItems.slice(
    (itemsCurrentPage - 1) * itemsPerPage,
    itemsCurrentPage * itemsPerPage,
  );

  useEffect(() => {
    if (isCatalogOpen) {
      const fetchCatalog = async () => {
        setIsLoadingCatalog(true);
        try {
          const [productRes, serviceRes] = await Promise.all([
            productApi.getList({
              page: 1,
              pageSize: 100,
              keyword: debouncedCatalogQuery,
            }),
            serviceApi.getList({
              page: 1,
              pageSize: 100,
              keyword: debouncedCatalogQuery,
            }),
          ]);

          const mappedProducts = productRes.items.map((p) => ({
            id: `p-${p.productId}`,
            originalId: p.productId,
            itemName: p.productName || "Sản phẩm không tên",
            itemType: ContractItemType.Product,
            unitPrice: p.productPrice || 0,
          }));

          const mappedServices = serviceRes.items.map((s) => ({
            id: `s-${s.serviceId}`,
            originalId: s.serviceId,
            itemName: s.serviceName || "Dịch vụ không tên",
            itemType: ContractItemType.Service,
            unitPrice: s.servicePrice || 0,
          }));

          setCatalogItems([...mappedProducts, ...mappedServices]);
        } catch (error) {
          console.error(error);
        } finally {
          setIsLoadingCatalog(false);
        }
      };
      fetchCatalog();
    }
  }, [isCatalogOpen, debouncedCatalogQuery]);

  const handleAddCatalogItem = (catalogItem: any) => {
    if (!contract || !contract.currentVersion) return;

    const exists = contract.currentVersion.items.some(
      (i) =>
        (catalogItem.itemType === ContractItemType.Product &&
          i.sourceProductId === catalogItem.originalId) ||
        (catalogItem.itemType === ContractItemType.Service &&
          i.sourceServiceId === catalogItem.originalId),
    );

    if (exists) {
      toast.info("Sản phẩm/dịch vụ này đã có trong hợp đồng.");
      return;
    }

    const newItem: any = {
      contractItemId: -(Math.floor(Math.random() * 100000) + 1),
      rowVersion: "",
      itemType: catalogItem.itemType,
      sourceProductId:
        catalogItem.itemType === ContractItemType.Product
          ? catalogItem.originalId
          : null,
      sourceServiceId:
        catalogItem.itemType === ContractItemType.Service
          ? catalogItem.originalId
          : null,
      itemCode: null,
      itemName: catalogItem.itemName,
      itemNameEn: null,
      itemDescription: null,
      itemDescriptionEn: null,
      unitName: null,
      unitNameEn: null,
      quantity: 1,
      unitPrice: catalogItem.unitPrice,
      discountMode: ContractItemDiscountMode.None,
      discountPercent: 0,
      fixedDiscountAmount: 0,
      isTaxable: true,
      vatPercent: 10,
      displayOrder: contract.currentVersion.items.length + 1,
    };

    setContract((prev) => {
      if (!prev || !prev.currentVersion) return prev;
      return {
        ...prev,
        currentVersion: {
          ...prev.currentVersion,
          items: [...prev.currentVersion.items, newItem],
        },
      };
    });

    toast.success(`Đã thêm ${catalogItem.itemName}`);
  };

  const toLocalISOString = (date?: Date | null) => {
    if (!date) return null;
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString();
  };

  const handleRootChange = (
    field: keyof ContractDetailResponse,
    value: any,
  ) => {
    setContract((prev) => (prev ? { ...prev, [field]: value } : prev));
  };

  const handleItemPatch = (
    itemId: number,
    patch: Record<string, unknown>,
  ) => {
    setContract((prev) => {
      if (!prev || !prev.currentVersion) return prev;
      const updatedItems = prev.currentVersion.items.map((item) =>
        item.contractItemId === itemId ? { ...item, ...patch } : item,
      );
      return {
        ...prev,
        currentVersion: { ...prev.currentVersion, items: updatedItems },
      };
    });
  };

  const handleItemChange = (itemId: number, field: string, value: unknown) => {
    handleItemPatch(itemId, { [field]: value });
  };

  const handlePercentageFocus = (
    event: React.FocusEvent<HTMLInputElement>,
  ) => {
    if (Number(event.currentTarget.value) === 0) {
      event.currentTarget.select();
    }
  };

  const handlePercentageBlur = (
    event: React.FocusEvent<HTMLInputElement>,
    itemId: number,
    field: "discountPercent" | "vatPercent",
  ) => {
    const parsedValue = Number(event.currentTarget.value);
    const normalizedValue = Number.isFinite(parsedValue)
      ? Math.min(100, Math.max(0, parsedValue))
      : 0;

    event.currentTarget.value = String(normalizedValue);
    handleItemChange(itemId, field, normalizedValue);
  };

  const handleRemoveItem = (contractItemId: number) => {
    setContract((prev) => {
      if (!prev || !prev.currentVersion) return prev;
      const updatedItems = prev.currentVersion.items.filter(
        (item) => item.contractItemId !== contractItemId,
      );
      return {
        ...prev,
        currentVersion: { ...prev.currentVersion, items: updatedItems },
      };
    });
  };

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
      <Card>
        <CardHeader>
          <CardTitle>Tổng quan hợp đồng</CardTitle>
        </CardHeader>
        <CardContent className="space-y-5">
          {/* SỬA TÊN HỢP ĐỒNG */}
          <div>
            <p className="text-sm font-semibold mb-1 text-muted-foreground">
              Tên hợp đồng
            </p>
            {isEditable ? (
              <Input
                value={contract.contractName}
                onChange={(e) =>
                  handleRootChange("contractName", e.target.value)
                }
                className="font-semibold text-lg"
              />
            ) : (
              <p className="text-lg font-semibold">{contract.contractName}</p>
            )}
          </div>

          {contract.languageMode === ContractLanguageMode.Bilingual && (
            <div>
              <p className="mb-1 text-sm font-semibold text-muted-foreground">
                Tên hợp đồng tiếng Anh
              </p>
              {isEditable ? (
                <Input
                  value={contract.contractNameEn || ""}
                  onChange={(e) =>
                    handleRootChange("contractNameEn", e.target.value)
                  }
                  placeholder="Enter the contract name..."
                />
              ) : (
                <p className="font-medium">
                  {contract.contractNameEn || "Chưa cập nhật"}
                </p>
              )}
            </div>
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            {isEditable ? (
              <div className="rounded-xl border bg-muted/30 p-4">
                <div className="flex items-center gap-2 text-sm text-muted-foreground mb-2">
                  <Users className="size-4" /> Khách hàng
                </div>
                <Select
                  value={String(contract.customer?.customerId || "")}
                  onValueChange={(val) => {
                    const found = customers.find(
                      (c) => c.customerId === Number(val),
                    );
                    if (found) {
                      setContract((prev) =>
                        prev
                          ? {
                              ...prev,
                              customer: {
                                ...prev.customer,
                                customerId: found.customerId,
                                customerCode: found.customerCode,
                                customerFullName: found.customerFullName,
                                customerCompany: found.customerCompany,
                              },
                            }
                          : prev,
                      );
                    }
                  }}
                >
                  <SelectTrigger className="w-full h-9 bg-background">
                    <SelectValue placeholder="Chọn khách hàng" />
                  </SelectTrigger>
                  <SelectContent>
                    {customers.map((c) => (
                      <SelectItem
                        key={c.customerId}
                        value={String(c.customerId)}
                      >
                        {c.customerCompany || c.customerFullName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            ) : (
              <InfoCard
                icon={<Users className="size-4" />}
                label="Khách hàng"
                value={
                  contract.customer?.customerCompany ||
                  contract.customer?.customerFullName
                }
              />
            )}

            <div className="rounded-xl border bg-muted/30 p-4 flex flex-col justify-between">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Clock className="size-4" /> Người phụ trách
                </div>
                {onOpenTransferModal && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-6 px-2 text-xs text-primary hover:bg-primary/10"
                    onClick={onOpenTransferModal}
                  >
                    Chuyển giao
                  </Button>
                )}
              </div>
              <div className="mt-2 font-semibold text-foreground">
                {contract.responsibleEmployee?.employeeFullName || "Chưa gán"}
              </div>
            </div>

            <InfoCard
              icon={<FileSignature className="size-4" />}
              label="Loại hợp đồng"
              value={getContractTypeLabel(contract.contractType)}
            />

            {/* SỬA NGÀY THÁNG */}

            {/* SỬA NGÀY THÁNG */}
            <div className="rounded-xl border bg-muted/30 p-4">
              <div className="flex items-center gap-2 text-sm text-muted-foreground mb-2">
                <CalendarDays className="size-4" /> Hiệu lực
              </div>
              {isEditable ? (
                <DateRangeFilter
                  dateRange={{
                    from: contract.effectiveDate
                      ? new Date(contract.effectiveDate)
                      : undefined,
                    to: contract.expireDate
                      ? new Date(contract.expireDate)
                      : undefined,
                  }}
                  onChange={(range) => {
                    handleRootChange(
                      "effectiveDate",
                      toLocalISOString(range.from),
                    );
                    handleRootChange("expireDate", toLocalISOString(range.to));
                  }}
                />
              ) : (
                <div className="font-semibold text-foreground">
                  {`${formatDate(contract.effectiveDate)} - ${formatDate(contract.expireDate)}`}
                </div>
              )}
            </div>
          </div>

          <Separator />

          {/* SỬA SẢN PHẨM / DỊCH VỤ (ITEMS) */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <h4 className="font-semibold">Sản phẩm / Dịch vụ</h4>
              {isEditable && (
                <Dialog open={isCatalogOpen} onOpenChange={setIsCatalogOpen}>
                  <DialogTrigger asChild>
                    <Button variant="outline" size="sm">
                      <Plus className="size-4 mr-2" />
                      Thêm SP/DV
                    </Button>
                  </DialogTrigger>
                  <DialogContent className="max-w-2xl max-h-[80vh] flex flex-col">
                    <DialogHeader>
                      <DialogTitle>Thêm Sản phẩm / Dịch vụ</DialogTitle>
                    </DialogHeader>

                    <div className="relative mt-2">
                      <Input
                        type="text"
                        placeholder="Tìm theo mã hoặc tên sản phẩm/dịch vụ..."
                        className="bg-background w-full"
                        value={catalogSearchQuery}
                        onChange={(e) => setCatalogSearchQuery(e.target.value)}
                      />
                    </div>

                    <div className="flex-1 overflow-y-auto pr-2 space-y-2 mt-4">
                      {isLoadingCatalog ? (
                        <div className="py-8 text-center text-sm text-muted-foreground flex flex-col items-center justify-center gap-2">
                          <Loader2 className="size-5 animate-spin text-primary" />
                          Đang tải danh sách...
                        </div>
                      ) : (
                        catalogItems.map((item) => (
                          <button
                            type="button"
                            key={item.id}
                            className="flex items-center justify-between p-3 border rounded-lg hover:bg-muted/50 cursor-pointer w-full text-left"
                            onClick={() => handleAddCatalogItem(item)}
                          >
                            <div className="flex items-center gap-3">
                              <div className="flex size-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                                <Package className="size-5" />
                              </div>
                              <div>
                                <p className="font-medium">{item.itemName}</p>
                                <p className="text-xs text-muted-foreground">
                                  {item.itemType === ContractItemType.Product
                                    ? "Sản phẩm"
                                    : "Dịch vụ"}
                                </p>
                              </div>
                            </div>
                            <span className="font-semibold text-primary">
                              {formatCurrency(
                                item.unitPrice,
                                contract.currencyCode,
                              )}
                            </span>
                          </button>
                        ))
                      )}
                    </div>
                  </DialogContent>
                </Dialog>
              )}
            </div>

            {/* Thanh tìm kiếm Items */}
            {(contract.currentVersion?.items?.length || 0) > 0 && (
              <div className="relative mb-3">
                <Input
                  type="text"
                  placeholder="Tìm kiếm sản phẩm / dịch vụ..."
                  className="bg-background w-full"
                  value={itemsSearchQuery}
                  onChange={(e) => {
                    setItemsSearchQuery(e.target.value);
                    setItemsCurrentPage(1);
                  }}
                />
              </div>
            )}

            <div className="space-y-3">
              {paginatedItems.map((item) => {
                const amounts = isEditable
                  ? calculateContractItemAmounts(item, contract.currencyCode)
                  : {
                      lineSubtotal: item.lineSubtotal,
                      discountAmount: item.discountAmount,
                      vatAmount: item.vatAmount,
                      lineTotal: item.lineTotal,
                    };

                return (
                  <div
                    key={item.contractItemId}
                    className="overflow-hidden rounded-xl border bg-background"
                  >
                    <div className="flex items-start justify-between gap-3 border-b bg-muted/30 p-4">
                      <div className="min-w-0">
                        <p className="font-semibold">{item.itemName}</p>
                        <p className="mt-1 text-xs text-muted-foreground">
                          {item.itemCode ||
                            (item.itemType === ContractItemType.Product
                              ? "Sản phẩm"
                              : "Dịch vụ")}
                        </p>
                      </div>
                      {isEditable && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="shrink-0 text-destructive hover:bg-destructive/10 hover:text-destructive"
                          onClick={() => handleRemoveItem(item.contractItemId)}
                        >
                          <Trash2 className="size-4" />
                          <span className="sr-only">Xóa sản phẩm</span>
                        </Button>
                      )}
                    </div>

                    {isEditable ? (
                      <div className="grid gap-3 p-4 sm:grid-cols-2 xl:grid-cols-5">
                        {contract.languageMode ===
                          ContractLanguageMode.Bilingual && (
                          <div className="space-y-1.5 sm:col-span-2 xl:col-span-5">
                            <span className="text-xs font-medium text-muted-foreground">
                              Tên sản phẩm / dịch vụ tiếng Anh
                            </span>
                            <Input
                              value={item.itemNameEn || ""}
                              onChange={(e) =>
                                handleItemChange(
                                  item.contractItemId,
                                  "itemNameEn",
                                  e.target.value,
                                )
                              }
                              placeholder="Enter the English item name..."
                            />
                          </div>
                        )}
                        <div className="space-y-1.5">
                          <span className="text-xs font-medium text-muted-foreground">
                            Số lượng
                          </span>
                          <Input
                            type="number"
                            min={0.001}
                            step={0.001}
                            value={item.quantity}
                            onChange={(e) =>
                              handleItemChange(
                                item.contractItemId,
                                "quantity",
                                Number(e.target.value),
                              )
                            }
                          />
                        </div>
                        <div className="space-y-1.5">
                          <span className="text-xs font-medium text-muted-foreground">
                            Đơn giá
                          </span>
                          <Input
                            type="number"
                            min={0}
                            step={1000}
                            value={item.unitPrice}
                            onChange={(e) =>
                              handleItemChange(
                                item.contractItemId,
                                "unitPrice",
                                Number(e.target.value),
                              )
                            }
                          />
                        </div>
                        <div className="space-y-1.5">
                          <span className="text-xs font-medium text-muted-foreground">
                            Chiết khấu
                          </span>
                          <Select
                            value={String(item.discountMode)}
                            onValueChange={(value) =>
                              handleItemPatch(item.contractItemId, {
                                discountMode: Number(
                                  value,
                                ) as ContractItemDiscountMode,
                                discountPercent: 0,
                                fixedDiscountAmount: 0,
                              })
                            }
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem
                                value={String(ContractItemDiscountMode.None)}
                              >
                                Không chiết khấu
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

                        {item.discountMode ===
                          ContractItemDiscountMode.Percentage && (
                          <div className="space-y-1.5">
                            <span className="text-xs font-medium text-muted-foreground">
                              Mức giảm (%)
                            </span>
                            <Input
                              type="number"
                              min={0}
                              max={100}
                              step={0.01}
                              value={item.discountPercent}
                              onFocus={handlePercentageFocus}
                              onChange={(e) =>
                                handleItemChange(
                                  item.contractItemId,
                                  "discountPercent",
                                  Number(e.target.value),
                                )
                              }
                              onBlur={(e) =>
                                handlePercentageBlur(
                                  e,
                                  item.contractItemId,
                                  "discountPercent",
                                )
                              }
                            />
                          </div>
                        )}

                        {item.discountMode ===
                          ContractItemDiscountMode.FixedAmount && (
                          <div className="space-y-1.5">
                            <span className="text-xs font-medium text-muted-foreground">
                              Số tiền giảm
                            </span>
                            <Input
                              type="number"
                              min={0}
                              step={1000}
                              value={item.fixedDiscountAmount}
                              onChange={(e) =>
                                handleItemChange(
                                  item.contractItemId,
                                  "fixedDiscountAmount",
                                  Number(e.target.value),
                                )
                              }
                            />
                          </div>
                        )}

                        <div className="space-y-1.5">
                          <span className="text-xs font-medium text-muted-foreground">
                            Thuế VAT
                          </span>
                          <div className="flex h-9 items-center justify-between rounded-md border px-3">
                            <span className="text-sm">
                              {item.isTaxable ? "Có thuế" : "Không thuế"}
                            </span>
                            <Switch
                              checked={item.isTaxable}
                              onCheckedChange={(checked) =>
                                handleItemPatch(item.contractItemId, {
                                  isTaxable: checked,
                                  vatPercent: checked
                                    ? item.vatPercent || 10
                                    : 0,
                                })
                              }
                            />
                          </div>
                        </div>

                        {item.isTaxable && (
                          <div className="space-y-1.5">
                            <span className="text-xs font-medium text-muted-foreground">
                              Thuế suất (%)
                            </span>
                            <Input
                              type="number"
                              min={0}
                              max={100}
                              step={0.01}
                              value={item.vatPercent}
                              onFocus={handlePercentageFocus}
                              onChange={(e) =>
                                handleItemChange(
                                  item.contractItemId,
                                  "vatPercent",
                                  Number(e.target.value),
                                )
                              }
                              onBlur={(e) =>
                                handlePercentageBlur(
                                  e,
                                  item.contractItemId,
                                  "vatPercent",
                                )
                              }
                            />
                          </div>
                        )}
                      </div>
                    ) : (
                      <div className="grid gap-3 p-4 text-sm sm:grid-cols-2 xl:grid-cols-4">
                        <div>
                          <p className="text-muted-foreground">Số lượng</p>
                          <p className="mt-1 font-medium">{item.quantity}</p>
                        </div>
                        <div>
                          <p className="text-muted-foreground">Đơn giá</p>
                          <p className="mt-1 font-medium">
                            {formatCurrency(
                              item.unitPrice,
                              contract.currencyCode,
                            )}
                          </p>
                        </div>
                        <div>
                          <p className="text-muted-foreground">Chiết khấu</p>
                          <p className="mt-1 font-medium">
                            {item.discountMode ===
                            ContractItemDiscountMode.Percentage
                              ? `${item.discountPercent}%`
                              : item.discountMode ===
                                  ContractItemDiscountMode.FixedAmount
                                ? formatCurrency(
                                    item.fixedDiscountAmount,
                                    contract.currencyCode,
                                  )
                                : "Không"}
                          </p>
                        </div>
                        <div>
                          <p className="text-muted-foreground">VAT</p>
                          <p className="mt-1 font-medium">
                            {item.isTaxable ? `${item.vatPercent}%` : "Không thuế"}
                          </p>
                        </div>
                        {contract.languageMode ===
                          ContractLanguageMode.Bilingual && (
                          <div className="sm:col-span-2 xl:col-span-4">
                            <p className="text-muted-foreground">
                              Tên tiếng Anh
                            </p>
                            <p className="mt-1 font-medium">
                              {item.itemNameEn || "Chưa cập nhật"}
                            </p>
                          </div>
                        )}
                      </div>
                    )}

                    <div className="grid gap-3 border-t bg-muted/20 px-4 py-3 text-sm sm:grid-cols-2 xl:grid-cols-4">
                      <div className="flex justify-between gap-3 xl:block">
                        <span className="text-muted-foreground">Tạm tính</span>
                        <p className="font-medium">
                          {formatCurrency(
                            amounts.lineSubtotal,
                            contract.currencyCode,
                          )}
                        </p>
                      </div>
                      <div className="flex justify-between gap-3 xl:block">
                        <span className="text-muted-foreground">Chiết khấu</span>
                        <p className="font-medium text-emerald-600">
                          -{formatCurrency(
                            amounts.discountAmount,
                            contract.currencyCode,
                          )}
                        </p>
                      </div>
                      <div className="flex justify-between gap-3 xl:block">
                        <span className="text-muted-foreground">VAT</span>
                        <p className="font-medium">
                          {formatCurrency(
                            amounts.vatAmount,
                            contract.currencyCode,
                          )}
                        </p>
                      </div>
                      <div className="flex justify-between gap-3 xl:block">
                        <span className="text-muted-foreground">Thành tiền</span>
                        <p className="font-semibold text-primary">
                          {formatCurrency(
                            amounts.lineTotal,
                            contract.currencyCode,
                          )}
                        </p>
                      </div>
                    </div>
                  </div>
                );
              })}

              {itemsTotalPages > 1 && (
                <div className="flex items-center justify-center gap-2 mt-4 pt-4 border-t">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() =>
                      setItemsCurrentPage((p) => Math.max(1, p - 1))
                    }
                    disabled={itemsCurrentPage === 1}
                  >
                    <ChevronLeft className="size-4 mr-1" /> Trước
                  </Button>
                  <span className="text-sm font-medium">
                    Trang {itemsCurrentPage} / {itemsTotalPages}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() =>
                      setItemsCurrentPage((p) =>
                        Math.min(itemsTotalPages, p + 1),
                      )
                    }
                    disabled={itemsCurrentPage === itemsTotalPages}
                  >
                    Sau <ChevronRight className="size-4 ml-1" />
                  </Button>
                </div>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="space-y-6">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between gap-3 space-y-0">
            <CardTitle className="text-base">Giá trị hợp đồng</CardTitle>
            <Badge variant={hasUnsavedFinancialChanges ? "secondary" : "outline"}>
              {hasUnsavedFinancialChanges
                ? "Dự kiến · chưa lưu"
                : `Backend · Version ${contract.currentVersion.versionNo}`}
            </Badge>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="rounded-lg bg-muted/50 px-3 py-2 text-xs leading-5 text-muted-foreground">
              {hasUnsavedFinancialChanges
                ? `Đang hiển thị số liệu dự kiến theo các thay đổi trên màn hình. Tổng đã lưu: ${formatCurrency(
                    persistedFinancialTotals.totalPayment,
                    contract.currencyCode,
                  )}.`
                : "Số liệu đã được backend tính và lưu cho phiên bản hợp đồng hiện hành."}
            </p>
            {isEditable && (
              <div className="space-y-1.5 pb-2">
                <span className="text-xs font-medium text-muted-foreground">
                  Đồng tiền
                </span>
                <Select
                  value={contract.currencyCode}
                  onValueChange={(value) =>
                    handleRootChange("currencyCode", value)
                  }
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
            )}
            <div className="flex justify-between gap-3 text-sm">
              <span className="text-muted-foreground">Tạm tính</span>
              <span>
                {formatCurrency(
                  financialTotals.subtotal,
                  contract.currencyCode,
                )}
              </span>
            </div>
            <div className="flex justify-between gap-3 text-sm">
              <span className="text-muted-foreground">Chiết khấu</span>
              <span className="text-emerald-600">
                -{formatCurrency(
                  financialTotals.totalDiscount,
                  contract.currencyCode,
                )}
              </span>
            </div>
            <div className="flex justify-between gap-3 text-sm">
              <span className="text-muted-foreground">VAT</span>
              <span>
                {formatCurrency(
                  financialTotals.totalVat,
                  contract.currencyCode,
                )}
              </span>
            </div>
            <Separator />
            <div className="flex justify-between gap-3">
              <span className="font-medium">Tổng thanh toán</span>
              <span className="text-lg font-bold text-primary">
                {formatCurrency(
                  financialTotals.totalPayment,
                  contract.currencyCode,
                )}
              </span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Thanh toán</CardTitle>
          </CardHeader>
          <CardContent>
            {/* TODO: Tích hợp API tracking luồng tiền sau */}
            <div className="mb-2 flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Tiến độ thanh toán</span>
              <span className="font-semibold">0%</span>
            </div>
            <Progress value={0} className="h-2" />
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
              <p className="font-semibold">Chưa nhận</p>
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
