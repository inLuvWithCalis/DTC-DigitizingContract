"use client";

import { useState } from "react";
import {
  CheckCircle2,
  FileLock2,
  GitBranch,
  Loader2,
  MessageSquareText,
} from "lucide-react";
import { toast } from "sonner";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Textarea } from "@/components/ui/textarea";
import {
  contractApi,
  ContractDetailResponse,
  ContractStatus,
} from "@/services/contract-api";

export function ContractNegotiation({
  contract,
  setContract,
}: {
  contract: ContractDetailResponse;
  setContract: React.Dispatch<
    React.SetStateAction<ContractDetailResponse | null>
  >;
}) {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [changeNote, setChangeNote] = useState("");
  const [isCreatingRound, setIsCreatingRound] = useState(false);

  const currentVersion = contract.currentVersion;
  const canCreateRound =
    contract.status === ContractStatus.Negotiating &&
    !currentVersion.isLocked;

  const handleCreateRound = async () => {
    const normalizedChangeNote = changeNote.trim();
    if (!normalizedChangeNote) {
      toast.error("Vui lòng nhập lý do tạo vòng đàm phán mới.");
      return;
    }

    setIsCreatingRound(true);
    try {
      const result = await contractApi.createNegotiationRound(
        contract.contractId,
        {
          currentVersionId: currentVersion.versionId,
          rowVersion: contract.rowVersion,
          currentVersionRowVersion: currentVersion.rowVersion,
          changeNote: normalizedChangeNote,
        },
      );

      const updatedContract = await contractApi.getDetail(
        contract.contractId,
      );
      setContract(updatedContract);
      setChangeNote("");
      setIsDialogOpen(false);
      toast.success(
        `Đã khóa phiên bản ${result.sourceVersion.versionNo} và tạo phiên bản ${result.currentVersion.versionNo}.`,
      );
    } catch (error: any) {
      console.error("Lỗi tạo vòng đàm phán:", error);
      const data = error?.response?.data;
      const message = data?.errors
        ? Object.values(data.errors).flat().join("; ")
        : data?.message ||
          data?.title ||
          "Không thể tạo vòng đàm phán mới. Vui lòng thử lại.";
      toast.error(message);
    } finally {
      setIsCreatingRound(false);
    }
  };

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
      <Card>
        <CardHeader className="flex flex-col gap-3 space-y-0 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <GitBranch className="size-5 text-primary" />
              Vòng đàm phán
            </CardTitle>
            <p className="mt-1 text-sm text-muted-foreground">
              Mỗi vòng mới sẽ lưu snapshot của phiên bản trước khi chỉnh sửa.
            </p>
          </div>
          {canCreateRound && (
            <Button
              className="w-full sm:w-auto"
              onClick={() => setIsDialogOpen(true)}
            >
              <GitBranch className="mr-2 size-4" />
              Tạo vòng mới
            </Button>
          )}
        </CardHeader>
        <CardContent className="space-y-5">
          {contract.status === ContractStatus.Draft && (
            <Alert>
              <MessageSquareText className="size-4" />
              <AlertTitle>Hợp đồng chưa bước vào đàm phán</AlertTitle>
              <AlertDescription>
                Hãy lưu hoàn chỉnh bản nháp rồi chọn “Bắt đầu đàm phán”.
              </AlertDescription>
            </Alert>
          )}

          {contract.status === ContractStatus.Negotiating && (
            <Alert>
              <GitBranch className="size-4" />
              <AlertTitle>Quy trình tạo revision</AlertTitle>
              <AlertDescription>
                Trước một lượt điều chỉnh mới, hãy tạo vòng đàm phán. Backend
                sẽ khóa phiên bản hiện tại, tạo bản sao mới rồi bạn mới sửa và
                lưu nội dung trên phiên bản đó.
              </AlertDescription>
            </Alert>
          )}

          <div className="rounded-xl border bg-muted/20 p-4 sm:p-5">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <p className="text-sm text-muted-foreground">
                  Phiên bản hiện hành
                </p>
                <p className="mt-1 text-2xl font-bold">
                  Version {currentVersion.versionNo}
                </p>
                {currentVersion.sourceVersionId && (
                  <p className="mt-1 text-sm text-muted-foreground">
                    Phát sinh từ version ID {currentVersion.sourceVersionId}
                  </p>
                )}
              </div>
              <Badge
                variant={currentVersion.isLocked ? "secondary" : "outline"}
                className="w-fit gap-1.5"
              >
                {currentVersion.isLocked ? (
                  <FileLock2 className="size-3.5" />
                ) : (
                  <CheckCircle2 className="size-3.5" />
                )}
                {currentVersion.isLocked ? "Đã khóa" : "Đang chỉnh sửa"}
              </Badge>
            </div>

            {currentVersion.changeNote && (
              <>
                <Separator className="my-4" />
                <div>
                  <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    Lý do tạo version
                  </p>
                  <p className="mt-2 text-sm leading-6">
                    {currentVersion.changeNote}
                  </p>
                </div>
              </>
            )}
          </div>

          <div className="rounded-xl border border-dashed p-6 text-center">
            <MessageSquareText className="mx-auto mb-3 size-8 text-muted-foreground" />
            <p className="font-medium">Chưa có API lịch sử/comment</p>
            <p className="mt-1 text-sm text-muted-foreground">
              Hiện tại hệ thống mới hỗ trợ snapshot và tạo version mới. Danh
              sách toàn bộ version sẽ nối khi backend có API lịch sử.
            </p>
          </div>
        </CardContent>
      </Card>

      <Card className="h-fit">
        <CardHeader>
          <CardTitle className="text-base">Luồng thao tác</CardTitle>
        </CardHeader>
        <CardContent>
          <ol className="space-y-4 text-sm">
            {[
              "Nhập lý do và tạo vòng mới.",
              "Backend snapshot và khóa version hiện tại.",
              "Hệ thống tải version mới chưa khóa.",
              "Sửa tổng quan hoặc điều khoản rồi lưu thay đổi.",
              "Gửi duyệt khi hai bên đã chốt nội dung.",
            ].map((step, index) => (
              <li key={step} className="flex gap-3">
                <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                  {index + 1}
                </span>
                <span className="pt-0.5 text-muted-foreground">{step}</span>
              </li>
            ))}
          </ol>
        </CardContent>
      </Card>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Tạo vòng đàm phán mới</DialogTitle>
            <DialogDescription>
              Version {currentVersion.versionNo} sẽ được khóa và snapshot.
              Hệ thống sau đó tạo version {currentVersion.versionNo + 1} để
              bạn tiếp tục chỉnh sửa.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-2 py-2">
            <Label htmlFor="negotiation-change-note">
              Lý do thay đổi <span className="text-destructive">*</span>
            </Label>
            <Textarea
              id="negotiation-change-note"
              value={changeNote}
              onChange={(event) => setChangeNote(event.target.value)}
              maxLength={2000}
              rows={5}
              placeholder="Ví dụ: Điều chỉnh phạm vi và điều khoản thanh toán theo phản hồi khách hàng..."
              disabled={isCreatingRound}
            />
            <p className="text-right text-xs text-muted-foreground">
              {changeNote.length}/2000
            </p>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsDialogOpen(false)}
              disabled={isCreatingRound}
            >
              Hủy
            </Button>
            <Button
              onClick={handleCreateRound}
              disabled={isCreatingRound || !changeNote.trim()}
            >
              {isCreatingRound ? (
                <Loader2 className="mr-2 size-4 animate-spin" />
              ) : (
                <GitBranch className="mr-2 size-4" />
              )}
              Tạo version {currentVersion.versionNo + 1}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
