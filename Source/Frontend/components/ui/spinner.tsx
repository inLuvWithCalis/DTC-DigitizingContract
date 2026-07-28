import * as React from "react";
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
      <svg
        className={cn("size-8 animate-spin text-primary", iconClassName)}
        viewBox="22 22 44 44"
        role="status"
        aria-label="Loading"
      >
        <style>{`
          @keyframes spinner-dash {
            0% {
              stroke-dasharray: 1px, 200px;
              stroke-dashoffset: 0;
            }
            50% {
              stroke-dasharray: 100px, 200px;
              stroke-dashoffset: -15px;
            }
            100% {
              stroke-dasharray: 100px, 200px;
              stroke-dashoffset: -125px;
            }
          }
          .animate-spinner-dash {
            animation: spinner-dash 1.4s ease-in-out infinite;
          }
        `}</style>
        <circle
          className="opacity-20"
          cx="44"
          cy="44"
          r="20"
          fill="none"
          stroke="currentColor"
          strokeWidth="3.6"
        />
        <circle
          className="animate-spinner-dash"
          cx="44"
          cy="44"
          r="20"
          fill="none"
          stroke="currentColor"
          strokeWidth="3.6"
          strokeLinecap="round"
        />
      </svg>

      {text && (
        <p className="text-sm font-medium text-muted-foreground animate-pulse">
          {text}
        </p>
      )}
    </div>
  );
}

export { Spinner };
