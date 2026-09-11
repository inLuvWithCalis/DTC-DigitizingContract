export const CONTRACT_RICH_TEXT_PREFIX = "contract-rich-text:v3:";
export const CONTRACT_RICH_TEXT_V2_PREFIX = "contract-rich-text:v2:";
export const CONTRACT_RICH_TEXT_LEGACY_PREFIX = "contract-rich-text:v1:";

export const CONTRACT_RICH_TEXT_FONT_SIZES = [10, 12, 14, 18, 24, 32] as const;

export interface ContractRichTextRun {
  text: string;
  bold?: boolean;
  italic?: boolean;
  underline?: boolean;
  fontSize?: number;
}

export type ContractRichTextAlignment = "left" | "center" | "right";

export interface ContractRichTextParagraph {
  runs: ContractRichTextRun[];
  alignment?: ContractRichTextAlignment;
}

export interface ContractRichTextCell {
  paragraphs: ContractRichTextParagraph[];
  colspan?: number;
  rowspan?: number;
  colwidth?: number[];
  verticalAlign?: ContractRichTextVerticalAlignment;
}

export type ContractRichTextVerticalAlignment = "top" | "center" | "bottom";

export interface ContractRichTextRow {
  cells: ContractRichTextCell[];
}

export type ContractRichTextBlock =
  | {
      type: "paragraph";
      runs: ContractRichTextRun[];
      alignment?: ContractRichTextAlignment;
    }
  | {
      type: "table";
      rows: ContractRichTextRow[];
    };

export interface ContractRichTextDocument {
  blocks: ContractRichTextBlock[];
}

const normalizeFontSize = (value: unknown) => {
  if (typeof value !== "number" || !Number.isFinite(value)) return undefined;
  return Math.min(36, Math.max(8, Math.round(value)));
};

const normalizeRuns = (value: unknown): ContractRichTextRun[] => {
  if (!Array.isArray(value)) return [];

  const result: ContractRichTextRun[] = [];
  value.slice(0, 2_000).forEach((candidate) => {
    if (!candidate || typeof candidate !== "object") return;
    const record = candidate as Record<string, unknown>;
    if (typeof record.text !== "string" || record.text.length === 0) return;

    const run: ContractRichTextRun = { text: record.text };
    if (record.bold === true) run.bold = true;
    if (record.italic === true) run.italic = true;
    if (record.underline === true) run.underline = true;
    const fontSize = normalizeFontSize(record.fontSize);
    if (fontSize) run.fontSize = fontSize;

    const previous = result.at(-1);
    if (
      previous &&
      previous.bold === run.bold &&
      previous.italic === run.italic &&
      previous.underline === run.underline &&
      previous.fontSize === run.fontSize
    ) {
      previous.text += run.text;
    } else {
      result.push(run);
    }
  });

  return result;
};

const normalizeAlignment = (
  value: unknown,
): ContractRichTextAlignment | undefined =>
  value === "center" || value === "right" ? value : undefined;

const normalizeParagraph = (value: unknown): ContractRichTextParagraph => {
  const record =
    value && typeof value === "object"
      ? (value as Record<string, unknown>)
      : {};
  const alignment = normalizeAlignment(record.alignment);
  return {
    runs: normalizeRuns(record.runs),
    ...(alignment ? { alignment } : {}),
  };
};

const normalizeSpan = (value: unknown, maximum: number) =>
  typeof value === "number" &&
  Number.isInteger(value) &&
  value >= 1 &&
  value <= maximum
    ? value
    : 1;

const normalizeVerticalAlignment = (
  value: unknown,
): ContractRichTextVerticalAlignment | undefined =>
  value === "top" || value === "center" || value === "bottom"
    ? value
    : undefined;

const normalizeColumnWidths = (value: unknown, colspan: number) => {
  if (!Array.isArray(value) || value.length !== colspan) return undefined;
  const widths = value.map((width) =>
    typeof width === "number" && Number.isFinite(width)
      ? Math.round(width)
      : Number.NaN,
  );
  return widths.every((width) => width >= 25 && width <= 2_000)
    ? widths
    : undefined;
};

const hasValidLogicalGrid = (rows: ContractRichTextRow[]) => {
  const activeRowspans = Array<number>(20).fill(0);
  let expectedWidth: number | undefined;

  for (const row of rows) {
    const occupied = activeRowspans.map((remaining) => remaining > 0);
    activeRowspans.forEach((remaining, index) => {
      if (remaining > 0) activeRowspans[index] = remaining - 1;
    });

    let cursor = 0;
    for (const cell of row.cells) {
      while (cursor < 20 && occupied[cursor]) cursor += 1;
      const colspan = cell.colspan ?? 1;
      const rowspan = cell.rowspan ?? 1;
      if (
        cursor + colspan > 20 ||
        occupied.slice(cursor, cursor + colspan).some(Boolean)
      ) {
        return false;
      }

      for (let column = cursor; column < cursor + colspan; column += 1) {
        occupied[column] = true;
        if (rowspan > 1) activeRowspans[column] = rowspan - 1;
      }
      cursor += colspan;
    }

    const width = occupied.lastIndexOf(true) + 1;
    if (width === 0 || (expectedWidth !== undefined && width !== expectedWidth)) {
      return false;
    }
    expectedWidth ??= width;
  }

  return activeRowspans.every((remaining) => remaining === 0);
};

export const normalizeContractRichTextDocument = (
  value: unknown,
): ContractRichTextDocument | null => {
  if (!value || typeof value !== "object") return null;
  const blocks = (value as { blocks?: unknown }).blocks;
  if (!Array.isArray(blocks)) return null;

  const normalizedBlocks: ContractRichTextBlock[] = [];
  blocks.slice(0, 500).forEach((candidate) => {
    if (!candidate || typeof candidate !== "object") return;
    const block = candidate as Record<string, unknown>;

    if (block.type === "paragraph") {
      const alignment = normalizeAlignment(block.alignment);
      normalizedBlocks.push({
        type: "paragraph",
        runs: normalizeRuns(block.runs),
        ...(alignment ? { alignment } : {}),
      });
      return;
    }

    if (block.type !== "table" || !Array.isArray(block.rows)) return;
    let rows: ContractRichTextRow[] = block.rows
      .slice(0, 50)
      .map((rowCandidate) => {
        const cells =
          rowCandidate &&
          typeof rowCandidate === "object" &&
          Array.isArray((rowCandidate as { cells?: unknown }).cells)
            ? (rowCandidate as { cells: unknown[] }).cells
            : [];
        return {
          cells: cells.slice(0, 20).map((cellCandidate) => {
            const cell =
              cellCandidate && typeof cellCandidate === "object"
                ? (cellCandidate as Record<string, unknown>)
                : {};
            const paragraphs = Array.isArray(cell.paragraphs)
              ? cell.paragraphs.slice(0, 100).map(normalizeParagraph)
              : [normalizeParagraph({ runs: cell.runs })];
            const colspan = normalizeSpan(cell.colspan, 20);
            const rowspan = normalizeSpan(cell.rowspan, 50);
            const colwidth = normalizeColumnWidths(cell.colwidth, colspan);
            const verticalAlign = normalizeVerticalAlignment(cell.verticalAlign);
            return {
              paragraphs,
              ...(colspan > 1 ? { colspan } : {}),
              ...(rowspan > 1 ? { rowspan } : {}),
              ...(colwidth ? { colwidth } : {}),
              ...(verticalAlign ? { verticalAlign } : {}),
            };
          }),
        };
      });

    if (!hasValidLogicalGrid(rows)) {
      rows = rows.map((row) => ({
        cells: row.cells.map((cell) => ({ paragraphs: cell.paragraphs })),
      }));
    }

    if (rows.length > 0) normalizedBlocks.push({ type: "table", rows });
  });

  return { blocks: normalizedBlocks };
};

const plainTextDocument = (value: string): ContractRichTextDocument => ({
  blocks: value.split(/\r?\n/).map((line) => ({
    type: "paragraph" as const,
    runs: line ? [{ text: line }] : [],
  })),
});

const withoutV3TableLayout = (
  document: ContractRichTextDocument,
): ContractRichTextDocument => ({
  blocks: document.blocks.map((block) =>
    block.type === "paragraph"
      ? block
      : {
          type: "table",
          rows: block.rows.map((row) => ({
            cells: row.cells.map((cell) => ({
              paragraphs: cell.paragraphs,
            })),
          })),
        },
  ),
});

export const parseContractRichText = (
  value?: string | null,
): ContractRichTextDocument => {
  const source = value ?? "";
  const prefix = source.startsWith(CONTRACT_RICH_TEXT_PREFIX)
    ? CONTRACT_RICH_TEXT_PREFIX
    : source.startsWith(CONTRACT_RICH_TEXT_V2_PREFIX)
      ? CONTRACT_RICH_TEXT_V2_PREFIX
    : source.startsWith(CONTRACT_RICH_TEXT_LEGACY_PREFIX)
      ? CONTRACT_RICH_TEXT_LEGACY_PREFIX
      : null;
  if (!prefix) {
    return plainTextDocument(source);
  }

  try {
    const document =
      normalizeContractRichTextDocument(
        JSON.parse(source.slice(prefix.length)),
      ) ?? plainTextDocument("");
    return prefix === CONTRACT_RICH_TEXT_PREFIX
      ? document
      : withoutV3TableLayout(document);
  } catch {
    return plainTextDocument("");
  }
};

const hasMeaningfulContent = (document: ContractRichTextDocument) =>
  document.blocks.some(
    (block) =>
      block.type === "table" ||
      block.runs.some((run) => run.text.trim().length > 0),
  );

export const serializeContractRichText = (
  document: ContractRichTextDocument,
) => {
  const normalized = normalizeContractRichTextDocument(document) ?? {
    blocks: [],
  };
  if (!hasMeaningfulContent(normalized)) return "";
  return `${CONTRACT_RICH_TEXT_PREFIX}${JSON.stringify(normalized)}`;
};

const escapeHtml = (value: string) =>
  value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");

const runsToHtml = (runs: ContractRichTextRun[]) =>
  runs
    .map((run) => {
      const content = escapeHtml(run.text).replaceAll("\n", "<br>");
      const style = run.fontSize ? ` style="font-size:${run.fontSize}pt"` : "";
      const formatted = `<span${style}>${content}</span>`;
      const bold = run.bold ? `<strong>${formatted}</strong>` : formatted;
      const italic = run.italic ? `<em>${bold}</em>` : bold;
      return run.underline ? `<u>${italic}</u>` : italic;
    })
    .join("");

export const contractRichTextToEditorHtml = (value?: string | null) =>
  parseContractRichText(value).blocks
    .map((block) => {
      if (block.type === "paragraph") {
        const style = block.alignment
          ? ` style="text-align:${block.alignment}"`
          : "";
        return `<p${style}>${runsToHtml(block.runs) || "<br>"}</p>`;
      }

      return `<table style="table-layout:fixed;width:100%"><tbody>${block.rows
        .map(
          (row) =>
            `<tr>${row.cells
              .map((cell) => {
                const colspan = cell.colspan && cell.colspan > 1
                  ? ` colspan="${cell.colspan}"`
                  : "";
                const rowspan = cell.rowspan && cell.rowspan > 1
                  ? ` rowspan="${cell.rowspan}"`
                  : "";
                const cellStyles = [
                  cell.verticalAlign
                    ? `vertical-align:${cell.verticalAlign}`
                    : "",
                  cell.colwidth?.length
                    ? `width:${cell.colwidth.reduce((sum, width) => sum + width, 0)}px`
                    : "",
                ].filter(Boolean);
                const style = cellStyles.length > 0
                  ? ` style="${cellStyles.join(";")}"`
                  : "";
                return `<td${colspan}${rowspan}${style}>${cell.paragraphs
                  .map((paragraph) => {
                    const paragraphStyle = paragraph.alignment
                      ? ` style="text-align:${paragraph.alignment}"`
                      : "";
                    return `<p${paragraphStyle}>${runsToHtml(paragraph.runs) || "<br>"}</p>`;
                  })
                  .join("")}</td>`;
              })
              .join("")}</tr>`,
        )
        .join("")}</tbody></table><p><br></p>`;
    })
    .join("");
