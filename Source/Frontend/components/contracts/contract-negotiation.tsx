"use client";

import { MessageSquareText } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

import { ContractDetailResponse } from "@/services/contract-api";

export function ContractNegotiation({
  contract,
}: {
  contract: ContractDetailResponse;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <CardTitle>Lịch sử đàm phán / comment</CardTitle>
        <Button variant="outline" size="sm">
          <MessageSquareText className="size-4 mr-2" />
          Thêm ghi chú đàm phán
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Placeholder cho API Comments sau này */}
        <div className="rounded-xl border border-dashed p-8 text-center">
          <MessageSquareText className="mx-auto mb-3 size-8 text-muted-foreground" />
          <p className="font-medium">Chưa có comment đàm phán</p>
          <p className="mt-1 text-sm text-muted-foreground">
            Sau này có thể gắn API revision/comment của hợp đồng tại đây.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
