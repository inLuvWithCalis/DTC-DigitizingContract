"use client";

import { Badge } from "@/components/ui/badge";
import {
  ContractStatus,
  getContractStatusLabel,
  statusClasses,
} from "@/services/contract-api";

export function ContractStatusBadge({ status }: { status: ContractStatus }) {
  return (
    <Badge variant="outline" className={statusClasses[status] || "bg-gray-50"}>
      {getContractStatusLabel(status)}
    </Badge>
  );
}

export function InfoCard({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border bg-muted/30 p-4">
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        {icon}
        {label}
      </div>
      <div className="mt-2 font-semibold text-foreground">{value}</div>
    </div>
  );
}
