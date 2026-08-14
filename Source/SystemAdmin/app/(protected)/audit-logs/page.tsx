"use client";

import { useCallback, useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, RefreshCw, Search, ShieldCheck } from "lucide-react";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Header } from "@/components/ui/custom/header";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { getApiErrorMessage } from "@/lib/api-error";
import {
  CENTRAL_SECURITY_AUDIT_ACTIONS,
  CENTRAL_SECURITY_AUDIT_RESULTS,
  centralSecurityAuditApi,
  type CentralSecurityAuditPagedResult,
} from "@/services/security-audits-api";

const PAGE_SIZE = 20;
const toUtc = (value: string) => value ? new Date(value).toISOString() : undefined;

export default function CentralSecurityAuditsPage() {
  const [data, setData] = useState<CentralSecurityAuditPagedResult>({
    items: [], totalCount: 0, page: 1, pageSize: PAGE_SIZE, totalPages: 0,
  });
  const [page, setPage] = useState(1);
  const [action, setAction] = useState("all");
  const [result, setResult] = useState("all");
  const [tenantCode, setTenantCode] = useState("");
  const [actorId, setActorId] = useState("");
  const [fromUtc, setFromUtc] = useState("");
  const [toUtcValue, setToUtcValue] = useState("");
  const [queryVersion, setQueryVersion] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadAudits = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    if (fromUtc && toUtcValue && new Date(fromUtc) > new Date(toUtcValue)) {
      setError("Thời điểm bắt đầu không được sau thời điểm kết thúc.");
      setIsLoading(false);
      return;
    }
    try {
      setData(await centralSecurityAuditApi.getList({
        page,
        pageSize: PAGE_SIZE,
        action: action === "all" ? undefined : action,
        result: result === "all" ? undefined : result,
        tenantCode: tenantCode.trim() || undefined,
        actorSystemAdminId: actorId ? Number(actorId) : undefined,
        fromUtc: toUtc(fromUtc),
        toUtc: toUtc(toUtcValue),
      }));
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, "Không thể tải nhật ký bảo mật trung tâm."));
    } finally {
      setIsLoading(false);
    }
  }, [action, actorId, fromUtc, page, queryVersion, result, tenantCode, toUtcValue]);

  useEffect(() => { void loadAudits(); }, [loadAudits]);

  const applyFilters = () => {
    setPage(1);
    setQueryVersion((value) => value + 1);
  };

  return (
    <>
      <Header title="Nhật ký bảo mật" />
      <div className="grow space-y-6 overflow-y-auto p-3 sm:p-6 lg:p-10">
        <div className="flex items-start gap-3">
          <span className="flex size-11 items-center justify-center rounded-xl bg-primary/10 text-primary"><ShieldCheck className="size-5" /></span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight">Nhật ký bảo mật trung tâm</h1>
            <p className="mt-1 text-sm text-muted-foreground">Theo dõi đăng nhập, từ chối truy cập, tạo tenant và thay đổi Manager.</p>
          </div>
        </div>

        <Card>
          <CardContent className="grid gap-4 pt-6 md:grid-cols-2 xl:grid-cols-3">
            <div className="space-y-2">
              <Label>Hành động</Label>
              <Select value={action} onValueChange={setAction}>
                <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                <SelectContent showSearch={false}>
                  <SelectItem value="all">Tất cả</SelectItem>
                  {CENTRAL_SECURITY_AUDIT_ACTIONS.map((item) => <SelectItem key={item} value={item}>{item}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Kết quả</Label>
              <Select value={result} onValueChange={setResult}>
                <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                <SelectContent showSearch={false}>
                  <SelectItem value="all">Tất cả</SelectItem>
                  {CENTRAL_SECURITY_AUDIT_RESULTS.map((item) => <SelectItem key={item} value={item}>{item}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="central-tenant">Tenant code</Label>
              <Input id="central-tenant" value={tenantCode} onChange={(event) => setTenantCode(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="central-actor">ID System Admin</Label>
              <Input id="central-actor" inputMode="numeric" value={actorId} onChange={(event) => setActorId(event.target.value.replace(/\D/g, ""))} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="central-from">Từ thời điểm</Label>
              <Input id="central-from" type="datetime-local" value={fromUtc} onChange={(event) => setFromUtc(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="central-to">Đến thời điểm</Label>
              <Input id="central-to" type="datetime-local" value={toUtcValue} onChange={(event) => setToUtcValue(event.target.value)} />
            </div>
            <div className="flex gap-2 md:col-span-2 xl:col-span-3 xl:justify-end">
              <Button onClick={applyFilters}><Search /> Áp dụng bộ lọc</Button>
              <Button variant="outline" onClick={() => void loadAudits()} disabled={isLoading}><RefreshCw className={isLoading ? "animate-spin" : undefined} /> Tải lại</Button>
            </div>
          </CardContent>
        </Card>

        {error && <Alert variant="destructive"><AlertDescription>{error}</AlertDescription></Alert>}

        <Card className="overflow-hidden">
          <Table>
            <TableHeader><TableRow><TableHead>Thời gian</TableHead><TableHead>Tenant</TableHead><TableHead>Hành động</TableHead><TableHead>Kết quả</TableHead><TableHead>System Admin</TableHead><TableHead>Đối tượng</TableHead><TableHead>Mã lỗi</TableHead><TableHead>Correlation ID</TableHead></TableRow></TableHeader>
            <TableBody>
              {!isLoading && data.items.length === 0 ? (
                <TableRow><TableCell colSpan={8} className="h-28 text-center text-muted-foreground">Không có dữ liệu phù hợp.</TableCell></TableRow>
              ) : data.items.map((item) => (
                <TableRow key={item.centralSecurityAuditId}>
                  <TableCell className="whitespace-nowrap">{new Date(item.occurredAt).toLocaleString("vi-VN")}</TableCell>
                  <TableCell>{item.tenantCode || (item.tenantId ? `#${item.tenantId}` : "—")}</TableCell>
                  <TableCell className="font-medium">{item.action}</TableCell>
                  <TableCell><Badge className={item.result === "Success" ? "bg-emerald-600 text-white" : item.result === "Denied" ? "bg-amber-600 text-white" : "bg-destructive text-destructive-foreground"}>{item.result}</Badge></TableCell>
                  <TableCell>{item.actorSystemAdminId ? `#${item.actorSystemAdminId}` : "—"}</TableCell>
                  <TableCell>{item.targetType || "—"}{item.targetId ? ` #${item.targetId}` : ""}</TableCell>
                  <TableCell>{item.failureCode || "—"}</TableCell>
                  <TableCell className="max-w-48 truncate font-mono text-xs" title={item.correlationId}>{item.correlationId}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <div className="flex items-center justify-between border-t px-4 py-3">
            <p className="text-sm text-muted-foreground">{data.totalCount} bản ghi · Trang {data.page}/{Math.max(data.totalPages, 1)}</p>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" disabled={isLoading || page <= 1} onClick={() => setPage((value) => value - 1)}><ChevronLeft /> Trước</Button>
              <Button variant="outline" size="sm" disabled={isLoading || page >= data.totalPages} onClick={() => setPage((value) => value + 1)}>Sau <ChevronRight /></Button>
            </div>
          </div>
        </Card>
      </div>
    </>
  );
}
