import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import {
  getTemplateValidationStatusLabel,
  getTemplateVersionStatusLabel,
  TemplateValidationStatus,
  TemplateVersionStatus,
} from "@/services/contract-template-api";

const VERSION_STYLES: Record<TemplateVersionStatus, string> = {
  [TemplateVersionStatus.Draft]:
    "border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-300",
  [TemplateVersionStatus.Published]:
    "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-300",
  [TemplateVersionStatus.Retired]:
    "border-slate-200 bg-slate-50 text-slate-600 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300",
};

const VALIDATION_STYLES: Record<TemplateValidationStatus, string> = {
  [TemplateValidationStatus.NotValidated]:
    "border-slate-200 bg-slate-50 text-slate-600 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300",
  [TemplateValidationStatus.Valid]:
    "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-300",
  [TemplateValidationStatus.Invalid]:
    "border-red-200 bg-red-50 text-red-700 dark:border-red-900 dark:bg-red-950/40 dark:text-red-300",
};

export function TemplateVersionStatusBadge({
  status,
  className,
}: {
  status: TemplateVersionStatus;
  className?: string;
}) {
  return (
    <Badge variant="outline" className={cn(VERSION_STYLES[status], className)}>
      {getTemplateVersionStatusLabel(status)}
    </Badge>
  );
}

export function TemplateValidationStatusBadge({
  status,
  className,
}: {
  status: TemplateValidationStatus;
  className?: string;
}) {
  return (
    <Badge
      variant="outline"
      className={cn(VALIDATION_STYLES[status], className)}
    >
      {getTemplateValidationStatusLabel(status)}
    </Badge>
  );
}
