import { Skeleton } from "@/components/ui/skeleton";
import { TableRow, TableCell } from "@/components/ui/table";

interface TableSkeletonProps {
  columnCount: number;
  rowCount?: number;
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
            <TableCell key={colIndex} className="py-3.5">
              <Skeleton
                className={`h-5 rounded-md ${
                  colIndex === 0
                    ? "w-5"
                    : colIndex === 1
                      ? "w-28"
                      : colIndex === columnCount - 1
                        ? "w-20 ml-auto"
                        : "w-4/5"
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
