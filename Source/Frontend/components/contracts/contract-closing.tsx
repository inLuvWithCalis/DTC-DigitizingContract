"use client";

import {
  CheckCircle2,
  FileCheck2,
  FileSignature,
  ReceiptText,
  Truck,
  WalletCards,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";

import {
  ContractDetailResponse,
  ContractStatus,
} from "@/services/contract-api";

export function ContractClosing({
  contract,
}: {
  contract: ContractDetailResponse;
}) {
  // Logic check dựa vào Enum trạng thái thực tế
  const isSigned = [ContractStatus.Signed, ContractStatus.Completed].includes(
    contract.status,
  );

  const checks = [
    {
      label: "Hai bên đã ký điện tử",
      completed: isSigned,
      icon: <FileSignature className="size-4" />,
    },
    {
      label: "Đã gửi/nhận bản cứng",
      completed: false,
      icon: <Truck className="size-4" />,
    },
    {
      label: "Đã upload biên bản nghiệm thu/bàn giao",
      completed: false,
      icon: <FileCheck2 className="size-4" />,
    },
    {
      label: "Đã upload hóa đơn/chứng từ kế toán",
      completed: false,
      icon: <ReceiptText className="size-4" />,
    },
    {
      label: "Khách hàng đã thanh toán 100%",
      completed: false,
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
