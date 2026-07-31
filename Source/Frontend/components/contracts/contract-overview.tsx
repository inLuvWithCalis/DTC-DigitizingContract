"use client";

import { useState, useEffect } from "react";
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
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Progress } from "@/components/ui/progress";
import { Separator } from "@/components/ui/separator";
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

import {
  ContractDetailResponse,
  ContractStatus,
  ContractItemType,
  getContractTypeLabel,
} from "@/services/contract-api";
import { InfoCard } from "./contract-helpers";
import { formatDate } from "@/lib/format-date";

export function ContractOverview({
  contract,
  setContract,
}: {
  contract: ContractDetailResponse;
  setContract: React.Dispatch<
    React.SetStateAction<ContractDetailResponse | null>
  >;
}) {
  const isEditable =
    contract.status === ContractStatus.Draft ||
    contract.status === ContractStatus.Negotiating;

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
      discountPercent: 0,
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

  const handleItemChange = (itemId: number, field: string, value: any) => {
    setContract((prev) => {
      if (!prev || !prev.currentVersion) return prev;
      const updatedItems = prev.currentVersion.items.map((item) =>
        item.contractItemId === itemId ? { ...item, [field]: value } : item,
      );
      return {
        ...prev,
        currentVersion: { ...prev.currentVersion, items: updatedItems },
      };
    });
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

          <div className="grid gap-4 sm:grid-cols-2">
            <InfoCard
              icon={<Users className="size-4" />}
              label="Khách hàng"
              value={contract.customer?.customerFullName}
            />
            <InfoCard
              icon={<FileSignature className="size-4" />}
              label="Loại hợp đồng"
              value={getContractTypeLabel(contract.contractType)}
            />

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

            <InfoCard
              icon={<WalletCards className="size-4" />}
              label="Giá trị hợp đồng (Tạm tính)"
              value={
                <span className="text-primary">
                  {formatCurrency(contract.totalAmount)}
                </span>
              }
            />
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
                              {formatCurrency(item.unitPrice)}
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
              {paginatedItems.map((item) => (
                <div
                  key={item.contractItemId}
                  className="flex flex-col sm:flex-row gap-3 items-center justify-between p-3 border rounded-lg"
                >
                  <p className="font-medium flex-1">{item.itemName}</p>

                  {isEditable ? (
                    <div className="flex items-center gap-2">
                      <div className="w-32">
                        <span className="text-xs text-muted-foreground">
                          Số lượng
                        </span>
                        <Input
                          type="number"
                          min={1}
                          maxLength={9}
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
                      <Button
                        variant="ghost"
                        size="icon"
                        className="text-destructive hover:text-destructive hover:bg-destructive/10 mt-4 shrink-0"
                        onClick={() => handleRemoveItem(item.contractItemId)}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  ) : (
                    <div className="text-right">
                      <p className="text-sm text-muted-foreground">
                        SL: {item.quantity}
                      </p>
                      <p className="font-semibold text-primary">
                        {formatCurrency(item.unitPrice)}
                      </p>
                    </div>
                  )}
                </div>
              ))}

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
