"use client";

import { Copy, Link2, Phone, ShieldCheck } from "lucide-react";
import { toast } from "sonner";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

import { ContractDetailResponse } from "@/services/contract-api";

export function ContractSignature({
  contract,
}: {
  contract: ContractDetailResponse;
}) {
  const publicLink = `https://yourdomain.com/public/contracts/${contract.contractCode}`;

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
              <p className="font-medium">
                Khách hàng: {contract.customer?.customerFullName}
              </p>
              <p className="mt-1 text-sm text-muted-foreground">
                OTP sẽ gửi tới:{" "}
                {contract.customer?.customerMobile || "Chưa có SĐT"}
              </p>
            </div>
            <div className="rounded-xl border p-4">
              <p className="font-medium">
                Nhà cung cấp: {contract.responsibleEmployee?.employeeFullName}
              </p>
              <p className="mt-1 text-sm text-muted-foreground">
                OTP sẽ gửi tới:{" "}
                {contract.responsibleEmployee?.employeeMobile || "Chưa có SĐT"}
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
              {publicLink}
            </p>
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              onClick={() => {
                navigator.clipboard?.writeText(publicLink);
                toast.success("Đã copy link!");
              }}
            >
              <Copy className="size-4 mr-2" />
              Copy link
            </Button>
            <Button disabled>
              <Link2 className="size-4 mr-2" />
              Gửi cho khách
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
