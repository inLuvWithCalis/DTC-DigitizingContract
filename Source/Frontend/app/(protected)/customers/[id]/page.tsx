"use client";

import { useState, useEffect, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import { Header } from "@/components/ui/custom/header";
import {
  ArrowLeft,
  Phone,
  Mail,
  Users,
  MessageSquare,
  Plus,
  Pencil,
  Save,
  Loader2,
  Clock,
  UserSquare2,
  Building2,
  MapPin,
  Globe,
  CalendarIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch"; // <-- Import Switch
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import { format } from "date-fns";

import {
  customerApi,
  CustomerResponse,
  UpdateCustomerRequest,
} from "@/services/customers-api";
import {
  customerInteractionApi,
  CustomerInteractionResponse,
  CustomerInteractionType,
  getCustomerInteractionTypeLabel,
} from "@/services/customer-interactions-api";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import { cn } from "@/lib/utils";

function InteractionFormModal({
  isOpen,
  onClose,
  onSuccess,
  customerId,
  item,
}: {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  customerId: number;
  item?: CustomerInteractionResponse | null;
}) {
  const isEditMode = !!item;
  const [isSaving, setIsSaving] = useState(false);
  const [interactionType, setInteractionType] = useState<string>(
    CustomerInteractionType.Call,
  );
  const [interactionSubject, setInteractionSubject] = useState("");
  const [content, setContent] = useState("");
  const [nextFollowUpDate, setNextFollowUpDate] = useState("");

  useEffect(() => {
    if (isOpen) {
      if (item) {
        setInteractionType(
          item.interactionType || CustomerInteractionType.Call,
        );
        setInteractionSubject(item.interactionSubject || "");
        setContent(item.content || "");
        setNextFollowUpDate(
          item.nextFollowUpDate ? item.nextFollowUpDate.substring(0, 16) : "",
        );
      } else {
        setInteractionType(CustomerInteractionType.Call);
        setInteractionSubject("");
        setContent("");
        setNextFollowUpDate("");
      }
    }
  }, [isOpen, item]);

  const handleSubmit = async () => {
    if (!interactionSubject.trim()) {
      toast.error("Vui lòng nhập chủ đề tương tác");
      return;
    }

    setIsSaving(true);
    try {
      const payload = {
        interactionType,
        interactionSubject: interactionSubject.trim(),
        content: content.trim() || null,
        nextFollowUpDate: nextFollowUpDate || null,
      };

      if (isEditMode && item) {
        await customerInteractionApi.update(
          customerId,
          item.interactionId,
          payload,
        );
        toast.success("Cập nhật tương tác thành công");
      } else {
        await customerInteractionApi.create(customerId, payload);
        toast.success("Đã ghi nhận tương tác mới");
      }
      onSuccess();
      onClose();
    } catch (error) {
      toast.error("Có lỗi xảy ra khi lưu tương tác");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditMode ? "Chỉnh sửa tương tác" : "Thêm lịch sử tương tác"}
          </DialogTitle>
        </DialogHeader>
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label>
              Hình thức liên hệ <span className="text-destructive">*</span>
            </Label>
            <Select value={interactionType} onValueChange={setInteractionType}>
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.values(CustomerInteractionType).map((type) => (
                  <SelectItem key={type} value={type}>
                    {getCustomerInteractionTypeLabel(type)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="grid gap-2">
            <Label>
              Chủ đề <span className="text-destructive">*</span>
            </Label>
            <Input
              placeholder="VD: Báo giá phần mềm..."
              value={interactionSubject}
              onChange={(e) => setInteractionSubject(e.target.value)}
            />
          </div>
          <div className="grid gap-2">
            <Label>Nội dung trao đổi</Label>
            <Textarea
              rows={4}
              placeholder="Ghi chú chi tiết cuộc trao đổi..."
              value={content}
              onChange={(e) => setContent(e.target.value)}
            />
          </div>
          <div className="grid gap-2">
            <Label>Lịch hẹn tiếp theo</Label>
            <Popover>
              <PopoverTrigger asChild>
                <Button
                  variant={"outline"}
                  className={cn(
                    "w-full justify-start text-left font-normal",
                    !nextFollowUpDate && "text-muted-foreground",
                  )}
                >
                  <CalendarIcon className="mr-2 h-4 w-4" />
                  {nextFollowUpDate ? (
                    format(new Date(nextFollowUpDate), "dd/MM/yyyy")
                  ) : (
                    <span>Chọn ngày...</span>
                  )}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <Calendar
                  mode="single"
                  selected={
                    nextFollowUpDate ? new Date(nextFollowUpDate) : undefined
                  }
                  onSelect={(date) => {
                    if (date) {
                      setNextFollowUpDate(format(date, "yyyy-MM-dd"));
                    } else {
                      setNextFollowUpDate("");
                    }
                  }}
                  initialFocus
                />
              </PopoverContent>
            </Popover>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isSaving}>
            Hủy
          </Button>
          <Button onClick={handleSubmit} disabled={isSaving}>
            {isSaving && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
            {isEditMode ? "Cập nhật" : "Lưu tương tác"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export default function CustomerDetailPage() {
  const params = useParams();
  const router = useRouter();
  const customerId = Number(params.id);

  const [isLoadingCustomer, setIsLoadingCustomer] = useState(true);
  const [isSavingCustomer, setIsSavingCustomer] = useState(false);
  const [isEditingProfile, setIsEditingProfile] = useState(false);
  const [isLoadingInteractions, setIsLoadingInteractions] = useState(true);

  const [customer, setCustomer] = useState<CustomerResponse | null>(null);
  const [interactions, setInteractions] = useState<
    CustomerInteractionResponse[]
  >([]);

  const [isInteractionModalOpen, setIsInteractionModalOpen] = useState(false);
  const [editingInteraction, setEditingInteraction] =
    useState<CustomerInteractionResponse | null>(null);

  const [isTogglingStatus, setIsTogglingStatus] = useState(false);

  const [formData, setFormData] = useState<CustomerResponse>({
    customerId: 0,
    customerCode: "",
    customerFullName: "",
    customerCompany: "",
    customerEmail: "",
    customerMobile: "",
    customerPhone: "",
    customerTaxCode: "",
    customerAddress: "",
    customerCity: "",
    customerCountry: "",
    customerWebsite: "",
    customerNotes: "",
    status: 1,
    dateCreated: "",
    dateModified: "",
    totalContracts: 0,
  });

  const fetchCustomer = useCallback(async () => {
    setIsLoadingCustomer(true);
    try {
      const res = await customerApi.getById(customerId);
      setCustomer(res);
      setFormData({
        customerId: res.customerId,
        customerCode: res.customerCode,
        customerFullName: res.customerFullName,
        customerCompany: res.customerCompany,
        customerEmail: res.customerEmail,
        customerMobile: res.customerMobile,
        customerPhone: res.customerPhone,
        customerTaxCode: res.customerTaxCode,
        customerAddress: res.customerAddress,
        customerCity: res.customerCity,
        customerCountry: res.customerCountry,
        customerWebsite: res.customerWebsite,
        customerNotes: res.customerNotes,
        status: res.status,
        dateCreated: res.dateCreated,
        dateModified: res.dateModified,
        totalContracts: res.totalContracts,
      });
    } catch (error) {
      toast.error("Không tìm thấy dữ liệu khách hàng");
      router.push("/customers");
    } finally {
      setIsLoadingCustomer(false);
    }
  }, [customerId, router]);

  const fetchInteractions = useCallback(async () => {
    setIsLoadingInteractions(true);
    try {
      const res = await customerInteractionApi.getByCustomer(customerId);
      setInteractions(res);
    } catch (error) {
      toast.error("Không thể tải lịch sử tương tác");
    } finally {
      setIsLoadingInteractions(false);
    }
  }, [customerId]);

  const handleToggleStatus = async (checked: boolean) => {
    const newStatus = checked ? 1 : 0;
    setIsTogglingStatus(true);
    try {
      await customerApi.setStatus(customerId, newStatus);
      setFormData((prev) => ({ ...prev, status: newStatus }));
      setCustomer((prev) => (prev ? { ...prev, status: newStatus } : null));

      toast.success(
        newStatus === 1
          ? "Đã mở trạng thái hoạt động cho khách hàng"
          : "Đã tạm khóa khách hàng này",
      );
    } catch (error) {
      toast.error("Không thể cập nhật trạng thái khách hàng");
    } finally {
      setIsTogglingStatus(false);
    }
  };

  useEffect(() => {
    if (customerId) {
      fetchCustomer();
      fetchInteractions();
    }
  }, [customerId, fetchCustomer, fetchInteractions]);

  const handleSaveCustomer = async () => {
    if (!formData.customerFullName?.trim()) {
      toast.error("Vui lòng nhập tên khách hàng");
      return;
    }
    setIsSavingCustomer(true);
    try {
      await customerApi.update(customerId, {
        ...formData,
        customerFullName: formData.customerFullName?.trim(),
      } as UpdateCustomerRequest);

      toast.success("Đã cập nhật thông tin khách hàng");
      setIsEditingProfile(false);
      fetchCustomer();
    } catch (error) {
      toast.error("Không thể cập nhật thông tin");
    } finally {
      setIsSavingCustomer(false);
    }
  };

  const getInteractionIcon = (type: string) => {
    switch (type) {
      case "Call":
        return <Phone className="w-4 h-4 text-blue-500" />;
      case "Email":
        return <Mail className="w-4 h-4 text-emerald-500" />;
      case "Meeting":
        return <Users className="w-4 h-4 text-amber-500" />;
      case "Zalo":
        return <MessageSquare className="w-4 h-4 text-indigo-500" />;
      default:
        return <Clock className="w-4 h-4 text-muted-foreground" />;
    }
  };

  if (isLoadingCustomer) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-2 text-primary">
          <Loader2 className="w-8 h-8 animate-spin" />
          <p className="text-sm font-medium">Đang tải dữ liệu...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-screen overflow-hidden bg-background">
      <Header />
      <div className="grow overflow-y-auto p-4 lg:p-10 space-y-6 w-full mx-auto">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <Button
              variant="ghost"
              onClick={() => router.back()}
              className="pl-0 hover:bg-transparent hover:text-primary w-fit"
            >
              <ArrowLeft className="w-4 h-4 mr-2" /> Quay lại
            </Button>
            <div className="hidden md:flex items-center gap-3 border-l border-border pl-4">
              <h1 className="text-2xl font-bold tracking-tight text-foreground">
                {customer?.customerFullName}
              </h1>
              {customer?.customerCode && (
                <Badge className="bg-primary/10 text-primary hover:bg-primary/15">
                  {customer.customerCode}
                </Badge>
              )}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-1 space-y-6">
            <Card className="border-border shadow-sm bg-card gap-0 pb-0">
              <CardHeader className="flex flex-col gap-3 border-b border-border/50 py-4 sm:flex-row sm:items-center sm:justify-between">
                <CardTitle className="text-base font-semibold flex items-center gap-2 text-foreground">
                  <UserSquare2 className="w-5 h-5 text-primary" />
                  Hồ sơ khách hàng
                </CardTitle>
                <div className="flex items-center gap-2">
                  <Label className="text-xs font-semibold cursor-pointer">
                    Hoạt động
                  </Label>
                  <Switch
                    checked={formData.status === 1}
                    disabled={isTogglingStatus}
                    onCheckedChange={handleToggleStatus}
                  />
                </div>
              </CardHeader>
              <CardContent className="p-5 space-y-4">
                <div className="grid gap-2">
                  <Label className="text-xs font-semibold uppercase text-muted-foreground">
                    Tên KH / Đại diện *
                  </Label>
                  <Input
                    readOnly={!isEditingProfile}
                    className={`h-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                    value={formData.customerFullName ?? ""}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        customerFullName: e.target.value,
                      })
                    }
                  />
                </div>

                <div className="grid gap-2">
                  <Label className="text-xs font-semibold uppercase text-muted-foreground">
                    Tên công ty / Tổ chức
                  </Label>
                  <div className="relative">
                    <Building2 className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                      readOnly={!isEditingProfile}
                      className={`h-9 pl-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                      value={formData.customerCompany ?? ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          customerCompany: e.target.value,
                        })
                      }
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div className="grid gap-2">
                    <Label className="text-xs font-semibold uppercase text-muted-foreground">
                      Mã KH
                    </Label>
                    <Input
                      readOnly={!isEditingProfile}
                      className={`h-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                      value={formData.customerCode ?? ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          customerCode: e.target.value,
                        })
                      }
                    />
                  </div>
                  <div className="grid gap-2">
                    <Label className="text-xs font-semibold uppercase text-muted-foreground">
                      Mã số thuế
                    </Label>
                    <Input
                      readOnly={!isEditingProfile}
                      className={`h-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                      value={formData.customerTaxCode ?? ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          customerTaxCode: e.target.value,
                        })
                      }
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div className="grid gap-2">
                    <Label className="text-xs font-semibold uppercase text-muted-foreground">
                      Số di động
                    </Label>
                    <div className="relative">
                      <Phone className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                      <Input
                        readOnly={!isEditingProfile}
                        className={`h-9 pl-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                        value={formData.customerMobile ?? ""}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            customerMobile: e.target.value,
                          })
                        }
                      />
                    </div>
                  </div>
                  <div className="grid gap-2">
                    <Label className="text-xs font-semibold uppercase text-muted-foreground">
                      SĐT cố định
                    </Label>
                    <Input
                      readOnly={!isEditingProfile}
                      className={`h-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                      value={formData.customerPhone ?? ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          customerPhone: e.target.value,
                        })
                      }
                    />
                  </div>
                </div>

                <div className="grid gap-2">
                  <Label className="text-xs font-semibold uppercase text-muted-foreground">
                    Email
                  </Label>
                  <div className="relative">
                    <Mail className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                      readOnly={!isEditingProfile}
                      className={`h-9 pl-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                      value={formData.customerEmail ?? ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          customerEmail: e.target.value,
                        })
                      }
                    />
                  </div>
                </div>

                <div className="grid gap-2">
                  <Label className="text-xs font-semibold uppercase text-muted-foreground">
                    Địa chỉ
                  </Label>
                  <div className="relative">
                    <MapPin className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                      readOnly={!isEditingProfile}
                      className={`h-9 pl-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                      value={formData.customerAddress ?? ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          customerAddress: e.target.value,
                        })
                      }
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div className="grid gap-2">
                    <Label className="text-xs font-semibold uppercase text-muted-foreground">
                      Tỉnh/TP
                    </Label>
                    <Input
                      readOnly={!isEditingProfile}
                      className={`h-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                      value={formData.customerCity ?? ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          customerCity: e.target.value,
                        })
                      }
                    />
                  </div>
                  <div className="grid gap-2">
                    <Label className="text-xs font-semibold uppercase text-muted-foreground">
                      Quốc gia
                    </Label>
                    <Input
                      readOnly={!isEditingProfile}
                      className={`h-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                      value={formData.customerCountry ?? ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          customerCountry: e.target.value,
                        })
                      }
                    />
                  </div>
                </div>

                <div className="grid gap-2">
                  <Label className="text-xs font-semibold uppercase text-muted-foreground">
                    Website
                  </Label>
                  <div className="relative">
                    <Globe className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                      readOnly={!isEditingProfile}
                      className={`h-9 pl-9 ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                      value={formData.customerWebsite ?? ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          customerWebsite: e.target.value,
                        })
                      }
                    />
                  </div>
                </div>

                <div className="grid gap-2">
                  <Label className="text-xs font-semibold uppercase text-muted-foreground">
                    Ghi chú thêm
                  </Label>
                  <Textarea
                    readOnly={!isEditingProfile}
                    rows={3}
                    className={`resize-none ${!isEditingProfile ? "bg-muted/40 border-transparent shadow-none cursor-default font-medium text-foreground focus-visible:ring-0" : ""}`}
                    value={formData.customerNotes ?? ""}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        customerNotes: e.target.value,
                      })
                    }
                  />
                </div>

                {isEditingProfile ? (
                  <div className="flex items-center gap-2 pt-2">
                    <Button
                      variant="outline"
                      className="w-1/3"
                      onClick={() => {
                        if (customer) {
                          setFormData({
                            customerId: customer.customerId,
                            customerCode: customer.customerCode,
                            customerFullName: customer.customerFullName,
                            customerCompany: customer.customerCompany,
                            customerEmail: customer.customerEmail,
                            customerMobile: customer.customerMobile,
                            customerPhone: customer.customerPhone,
                            customerTaxCode: customer.customerTaxCode,
                            customerAddress: customer.customerAddress,
                            customerCity: customer.customerCity,
                            customerCountry: customer.customerCountry,
                            customerWebsite: customer.customerWebsite,
                            customerNotes: customer.customerNotes,
                            status: customer.status,
                            dateCreated: customer.dateCreated,
                            dateModified: customer.dateModified,
                            totalContracts: customer.totalContracts,
                          });
                        }
                        setIsEditingProfile(false);
                      }}
                      disabled={isSavingCustomer}
                    >
                      Hủy
                    </Button>
                    <Button
                      className="w-2/3"
                      onClick={handleSaveCustomer}
                      disabled={isSavingCustomer}
                    >
                      {isSavingCustomer ? (
                        <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      ) : (
                        <Save className="w-4 h-4 mr-2" />
                      )}
                      Lưu thay đổi
                    </Button>
                  </div>
                ) : (
                  <Button
                    variant="outline"
                    className="w-full border-primary/20 text-primary hover:bg-primary/5 mt-2"
                    onClick={() => setIsEditingProfile(true)}
                  >
                    <Pencil className="w-4 h-4 mr-2" /> Chỉnh sửa hồ sơ
                  </Button>
                )}
              </CardContent>
            </Card>
          </div>

          <div className="lg:col-span-2 space-y-6">
            <Card className="border-border shadow-sm bg-card flex flex-col gap-0 min-h-[600px] pb-0">
              <CardHeader className="top-0 z-10 flex shrink-0 flex-col gap-3 rounded-t-xl border-b border-border/50 bg-card py-4 sm:flex-row sm:items-center sm:justify-between">
                <CardTitle className="text-base font-semibold flex items-center gap-2 text-foreground">
                  <MessageSquare className="w-5 h-5 text-primary" />
                  Nhật ký trao đổi & Tương tác
                </CardTitle>
                <Button
                  size="sm"
                  variant="outline"
                  className="h-8 border-primary/30 text-primary hover:bg-primary/10"
                  onClick={() => {
                    setEditingInteraction(null);
                    setIsInteractionModalOpen(true);
                  }}
                >
                  <Plus className="w-4 h-4 mr-1" /> Thêm tương tác
                </Button>
              </CardHeader>

              <CardContent className="p-6 overflow-y-auto">
                {isLoadingInteractions ? (
                  <div className="flex justify-center py-10">
                    <Loader2 className="w-6 h-6 animate-spin text-primary" />
                  </div>
                ) : interactions.length === 0 ? (
                  <div className="text-center py-16 text-muted-foreground flex flex-col items-center">
                    <div className="w-16 h-16 bg-muted rounded-full flex items-center justify-center mb-4">
                      <MessageSquare className="w-8 h-8 text-muted-foreground/50" />
                    </div>
                    <p className="font-medium text-foreground/80">
                      Chưa có lịch sử chăm sóc
                    </p>
                    <p className="text-sm mt-1">
                      Bấm "Thêm tương tác" để ghi nhận cuộc gọi/meeting đầu
                      tiên.
                    </p>
                  </div>
                ) : (
                  <div className="relative ml-3 space-y-8 pb-4">
                    {interactions.map((interaction, index) => (
                      <div
                        key={interaction.interactionId}
                        className="relative pl-8"
                      >
                        {index !== interactions.length - 1 && (
                          <div className="absolute left-[-2px] top-7 bottom-[-32px] w-[2px] bg-border z-0" />
                        )}

                        <div className="absolute -left-[15px] top-0 w-7 h-7 rounded-full bg-background border-2 border-border flex items-center justify-center shadow-sm z-10">
                          {getInteractionIcon(interaction.interactionType)}
                        </div>

                        <div
                          className={`rounded-xl p-4 transition-all group ${
                            index === 0
                              ? "bg-primary/5 border-0 shadow-sm"
                              : "bg-muted/30 border border-border hover:border-primary/40 hover:shadow-sm"
                          }`}
                        >
                          <div className="flex items-start justify-between gap-4 mb-2">
                            <div>
                              <h4 className="font-semibold text-foreground text-base">
                                {interaction.interactionSubject ||
                                  "Không có chủ đề"}
                              </h4>
                              <div className="flex items-center flex-wrap gap-2 text-sm text-muted-foreground mt-1">
                                <span className="font-medium text-foreground/80">
                                  {interaction.employeeName || "Hệ thống"}
                                </span>
                                <span>•</span>
                                <span>
                                  {format(
                                    new Date(interaction.interactionDate),
                                    "dd/MM/yyyy HH:mm",
                                  )}
                                </span>
                                <span>•</span>
                                <Badge
                                  variant="secondary"
                                  className="bg-background border-border text-foreground/80 font-normal py-0"
                                >
                                  {getCustomerInteractionTypeLabel(
                                    interaction.interactionType,
                                  )}
                                </Badge>
                              </div>
                            </div>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-8 w-8 text-muted-foreground hover:text-primary opacity-0 group-hover:opacity-100 transition-opacity"
                              onClick={() => {
                                setEditingInteraction(interaction);
                                setIsInteractionModalOpen(true);
                              }}
                            >
                              <Pencil className="w-4 h-4" />
                            </Button>
                          </div>

                          {interaction.content && (
                            <div className="text-sm text-foreground/90 whitespace-pre-wrap mt-3 bg-card p-3 rounded-md border border-border/50 leading-relaxed">
                              {interaction.content}
                            </div>
                          )}

                          {interaction.nextFollowUpDate && (
                            <div className="flex items-center gap-2 mt-4 text-xs font-medium text-amber-600 dark:text-amber-500 bg-amber-500/10 border border-amber-500/20 w-fit px-2.5 py-1.5 rounded-md">
                              <CalendarIcon className="w-3.5 h-3.5 text-amber-500" />
                              Lịch hẹn tiếp theo:{" "}
                              {format(
                                new Date(interaction.nextFollowUpDate),
                                "dd/MM/yyyy - HH:mm",
                              )}
                            </div>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      </div>

      <InteractionFormModal
        isOpen={isInteractionModalOpen}
        onClose={() => setIsInteractionModalOpen(false)}
        onSuccess={fetchInteractions}
        customerId={customerId}
        item={editingInteraction}
      />
    </div>
  );
}
