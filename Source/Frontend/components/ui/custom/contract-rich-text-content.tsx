import { Fragment } from "react";

import { cn } from "@/lib/utils";
import {
  parseContractRichText,
  type ContractRichTextRun,
} from "@/lib/contract-rich-text";

interface ContractRichTextContentProps {
  value?: string | null;
  emptyText?: string;
  className?: string;
}

const RichRuns = ({ runs }: { runs: ContractRichTextRun[] }) =>
  runs.map((run, runIndex) => {
    const content = run.text.split("\n").map((part, partIndex) => (
      <Fragment key={`${runIndex}-${partIndex}`}>
        {partIndex > 0 && <br />}
        {part}
      </Fragment>
    ));

    return (
      <span
        key={runIndex}
        className={cn(
          run.bold && "font-bold",
          run.italic && "italic",
          run.underline && "underline",
        )}
        style={run.fontSize ? { fontSize: `${run.fontSize}pt` } : undefined}
      >
        {content}
      </span>
    );
  });

export function ContractRichTextContent({
  value,
  emptyText = "Chưa có nội dung",
  className,
}: ContractRichTextContentProps) {
  const document = parseContractRichText(value);
  const hasContent = document.blocks.some(
    (block) =>
      block.type === "table" ||
      block.runs.some((run) => run.text.trim().length > 0),
  );

  if (!hasContent) {
    return <p className={className}>{emptyText}</p>;
  }

  return (
    <div className={cn("space-y-2 text-sm leading-6", className)}>
      {document.blocks.map((block, blockIndex) =>
        block.type === "paragraph" ? (
          <p key={blockIndex} className="min-h-[1lh] whitespace-pre-wrap">
            <RichRuns runs={block.runs} />
          </p>
        ) : (
          <div key={blockIndex} className="overflow-x-auto">
            <table className="w-full min-w-96 border-collapse text-left text-sm">
              <tbody>
                {block.rows.map((row, rowIndex) => (
                  <tr key={rowIndex}>
                    {row.cells.map((cell, cellIndex) => (
                      <td
                        key={cellIndex}
                        className="min-w-24 border border-border px-2 py-1.5 align-top"
                      >
                        <RichRuns runs={cell.runs} />
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ),
      )}
    </div>
  );
}
