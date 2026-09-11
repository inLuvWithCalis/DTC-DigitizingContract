import { Fragment } from "react";

import { cn } from "@/lib/utils";
import {
  parseContractRichText,
  type ContractRichTextAlignment,
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

const alignmentClass = (alignment?: ContractRichTextAlignment) =>
  alignment === "center"
    ? "text-center"
    : alignment === "right"
      ? "text-right"
      : "text-left";

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
          <p key={blockIndex} className={cn("min-h-[1lh] whitespace-pre-wrap", alignmentClass(block.alignment))}>
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
                        colSpan={cell.colspan}
                        rowSpan={cell.rowspan}
                        className="min-w-24 border border-border px-2 py-1.5 align-top"
                        style={{
                          verticalAlign: cell.verticalAlign ?? "top",
                          width: cell.colwidth?.length
                            ? `${cell.colwidth.reduce((sum, width) => sum + width, 0)}px`
                            : undefined,
                        }}
                      >
                        {cell.paragraphs.map((paragraph, paragraphIndex) => (
                          <p
                            key={paragraphIndex}
                            className={cn("min-h-[1lh] whitespace-pre-wrap", alignmentClass(paragraph.alignment))}
                          >
                            <RichRuns runs={paragraph.runs} />
                          </p>
                        ))}
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
