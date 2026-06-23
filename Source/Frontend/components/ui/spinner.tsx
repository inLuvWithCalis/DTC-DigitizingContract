import * as React from "react";
import { Loader2Icon } from "lucide-react";
import { cn } from "@/lib/utils";

interface SpinnerProps extends React.ComponentProps<"div"> {
  text?: string;
  iconClassName?: string;
}

function Spinner({ className, text, iconClassName, ...props }: SpinnerProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-4",
        className,
      )}
      {...props}
    >
      <Loader2Icon
        role="status"
        aria-label="Loading"
        className={cn("size-8 animate-spin text-primary", iconClassName)}
      />

      {text && (
        <p className="text-sm font-medium text-muted-foreground animate-pulse">
          {text}
        </p>
      )}
    </div>
  );
}

export { Spinner };
