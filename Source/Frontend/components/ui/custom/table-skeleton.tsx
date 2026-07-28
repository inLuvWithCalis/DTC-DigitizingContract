import { Skeleton } from "@/components/ui/skeleton";
import {
  TableRow,
  TableCell,
  TableHeader,
  TableHead,
} from "@/components/ui/table";

interface TableSkeletonProps {
  columnCount: number;
  rowCount?: number;
}

export function TableHeaderSkeleton({ columnCount }: { columnCount: number }) {
  return (
    <TableHeader className="bg-secondary/50">
      <TableRow className="hover:bg-transparent border-b-border">
        {Array.from({ length: columnCount }).map((_, colIndex) => (
          <TableHead
            key={colIndex}
            className={`h-12 py-2 ${
              colIndex === columnCount - 1 ? "w-24 text-right pr-4" : ""
            }`}
          >
            <Skeleton
              className={`h-5 rounded-md ${
                colIndex === 0
                  ? "w-5"
                  : colIndex === 1
                    ? "w-24"
                    : colIndex === columnCount - 1
                      ? "w-16 ml-auto"
                      : "w-20"
              }`}
            />
          </TableHead>
        ))}
      </TableRow>
    </TableHeader>
  );
}

export function TableRowSkeleton({
  columnCount,
  rowCount = 5,
}: TableSkeletonProps) {
  return (
    <>
      {Array.from({ length: rowCount }).map((_, rowIndex) => (
        <TableRow
          key={rowIndex}
          className="hover:bg-transparent border-b-border"
        >
          {Array.from({ length: columnCount }).map((_, colIndex) => (
            <TableCell
              key={colIndex}
              className={`py-3.5 ${
                colIndex === columnCount - 1 ? "w-24 text-right pr-4" : ""
              }`}
            >
              <Skeleton
                className={`rounded-md ${
                  colIndex === 0
                    ? "w-5 h-5"
                    : colIndex === 1
                      ? "w-28 h-10"
                      : colIndex === columnCount - 1
                        ? "w-32 h-8 ml-auto"
                        : "w-4/5 h-5"
                }`}
              />
            </TableCell>
          ))}
        </TableRow>
      ))}
    </>
  );
}

export function MobileCardSkeleton({ count = 4 }: { count?: number }) {
  return (
    <div className="flex flex-col gap-3">
      {Array.from({ length: count }).map((_, index) => (
        <div
          key={index}
          className="rounded-xl border border-border bg-card p-4 shadow-sm space-y-3"
        >
          <div className="flex items-center justify-between">
            <Skeleton className="h-5 w-1/3" />
            <Skeleton className="h-5 w-16 rounded-full" />
          </div>
          <Skeleton className="h-4 w-2/3" />
          <div className="pt-2 border-t border-border flex justify-between items-center">
            <Skeleton className="h-4 w-1/4" />
            <Skeleton className="h-4 w-1/5" />
          </div>
        </div>
      ))}
    </div>
  );
}

interface TableToolbarSkeletonProps {
  hasFilter?: boolean;
  hasSearch?: boolean;
}

export function TableToolbarSkeleton({
  hasFilter = true,
  hasSearch = true,
}: TableToolbarSkeletonProps) {
  if (!hasFilter && !hasSearch) return null;

  return (
    <div className="flex flex-col gap-3 w-full mb-4 md:flex-row md:items-center md:justify-between md:gap-4 animate-in fade-in duration-200">
      {hasFilter ? (
        <div className="flex flex-wrap items-center gap-2 md:gap-3">
          <Skeleton className="h-4 w-12 hidden md:block" />
          <Skeleton className="h-9 w-32 rounded-lg" />
          <Skeleton className="h-9 w-40 rounded-lg" />
        </div>
      ) : (
        <div />
      )}

      {hasSearch && <Skeleton className="h-9 w-full md:w-96 rounded-lg" />}
    </div>
  );
}

export function PageHeaderSkeleton() {
  return (
    <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 animate-in fade-in duration-200">
      <div className="space-y-2">
        <Skeleton className="h-7 w-48 rounded-md" />
        <Skeleton className="h-4 w-80 rounded-md" />
      </div>
      <Skeleton className="h-10 w-28 rounded-lg" />
    </div>
  );
}

export function TablePaginationSkeleton() {
  return (
    <div className="flex flex-col gap-3 py-4 mt-auto border-t border-transparent sm:flex-row sm:items-center sm:justify-between animate-in fade-in duration-200">
      <Skeleton className="h-5 w-44 rounded-md" />
      <div className="flex items-center justify-between gap-3 sm:gap-6">
        <div className="flex items-center gap-2">
          <Skeleton className="h-4 w-12 hidden sm:inline-block" />
          <Skeleton className="h-8 w-[75px] rounded-lg" />
        </div>
        <div className="flex items-center gap-1.5 sm:gap-2">
          <Skeleton className="h-8 w-16 rounded-md" />
          <Skeleton className="h-5 w-14 rounded-md" />
          <Skeleton className="h-8 w-16 rounded-md" />
        </div>
      </div>
    </div>
  );
}
