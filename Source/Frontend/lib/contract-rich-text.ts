export const CONTRACT_RICH_TEXT_PREFIX = "contract-rich-text:v4:";
export const CONTRACT_TABLE_TOTAL_BPS = 10_000;
export const CONTRACT_TABLE_MIN_COLUMN_BPS = 250;

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
      columnWidthsBps: number[];
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

export const isCanonicalColumnWidthsBps = (
  value: unknown,
  columnCount: number,
): value is number[] =>
  Array.isArray(value) &&
  value.length === columnCount &&
  value.every(
    (width) =>
      typeof width === "number" &&
      Number.isInteger(width) &&
      width >= CONTRACT_TABLE_MIN_COLUMN_BPS &&
      width <= CONTRACT_TABLE_TOTAL_BPS,
  ) &&
  value.reduce((sum, width) => sum + width, 0) === CONTRACT_TABLE_TOTAL_BPS;

export const normalizeColumnWidthsBps = (
  values: readonly number[],
  columnCount = values.length,
): number[] => {
  if (
    !Number.isInteger(columnCount) ||
    columnCount <= 0 ||
    columnCount * CONTRACT_TABLE_MIN_COLUMN_BPS > CONTRACT_TABLE_TOTAL_BPS
  ) {
    throw new Error("Số cột của bảng không hợp lệ.");
  }

  const weights = Array.from({ length: columnCount }, (_, index) => {
    const value = values[index];
    return typeof value === "number" && Number.isFinite(value) && value > 0
      ? value
      : 1;
  });
  const allocations = Array<number>(columnCount).fill(0);
  let available = CONTRACT_TABLE_TOTAL_BPS;
  let pending = Array.from({ length: columnCount }, (_, index) => index);

  while (pending.length > 0) {
    const weightTotal = pending.reduce(
      (sum, index) => sum + weights[index],
      0,
    );
    const belowMinimum = pending.filter(
      (index) =>
        (available * weights[index]) / weightTotal <
        CONTRACT_TABLE_MIN_COLUMN_BPS,
    );
    if (belowMinimum.length === 0) {
      pending.forEach((index) => {
        allocations[index] = (available * weights[index]) / weightTotal;
      });
      break;
    }

    belowMinimum.forEach((index) => {
      allocations[index] = CONTRACT_TABLE_MIN_COLUMN_BPS;
      available -= CONTRACT_TABLE_MIN_COLUMN_BPS;
    });
    const belowMinimumSet = new Set(belowMinimum);
    pending = pending.filter((index) => !belowMinimumSet.has(index));
  }

  const result = allocations.map(Math.floor);
  result[result.length - 1] +=
    CONTRACT_TABLE_TOTAL_BPS - result.reduce((sum, width) => sum + width, 0);
  return result;
};

export const columnWidthsBpsToPixels = (
  widthsBps: readonly number[],
  availableWidth: number,
) => {
  if (!isCanonicalColumnWidthsBps(widthsBps, widthsBps.length)) {
    throw new Error("Độ rộng cột canonical không hợp lệ.");
  }
  const safeWidth = Math.max(widthsBps.length, Math.floor(availableWidth));
  const result = widthsBps.map((width) =>
    Math.max(1, Math.floor((safeWidth * width) / CONTRACT_TABLE_TOTAL_BPS)),
  );
  result[result.length - 1] +=
    safeWidth - result.reduce((sum, width) => sum + width, 0);
  return result;
};

const getLogicalColumnCount = (rows: ContractRichTextRow[]) => {
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
        return null;
      }

      for (let column = cursor; column < cursor + colspan; column += 1) {
        occupied[column] = true;
        if (rowspan > 1) activeRowspans[column] = rowspan - 1;
      }
      cursor += colspan;
    }

    const width = occupied.lastIndexOf(true) + 1;
    if (width === 0 || (expectedWidth !== undefined && width !== expectedWidth)) {
      return null;
    }
    expectedWidth ??= width;
  }

  return activeRowspans.every((remaining) => remaining === 0)
    ? (expectedWidth ?? null)
    : null;
};

export const normalizeContractRichTextDocument = (
  value: unknown,
): ContractRichTextDocument | null => {
  if (!value || typeof value !== "object") return null;
  const blocks = (value as { blocks?: unknown }).blocks;
  if (!Array.isArray(blocks)) return null;

  const normalizedBlocks: ContractRichTextBlock[] = [];
  let invalid = false;
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
    const rows: ContractRichTextRow[] = block.rows
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
            const allowedCellKeys = new Set([
              "paragraphs",
              "colspan",
              "rowspan",
              "verticalAlign",
            ]);
            if (Object.keys(cell).some((key) => !allowedCellKeys.has(key))) {
              invalid = true;
            }
            if (
              !Array.isArray(cell.paragraphs) ||
              cell.paragraphs.length === 0 ||
              cell.paragraphs.length > 100
            ) {
              invalid = true;
            }
            const paragraphs = Array.isArray(cell.paragraphs)
              ? cell.paragraphs.slice(0, 100).map(normalizeParagraph)
              : [];
            const colspan = normalizeSpan(cell.colspan, 20);
            const rowspan = normalizeSpan(cell.rowspan, 50);
            const verticalAlign = normalizeVerticalAlignment(cell.verticalAlign);
            return {
              paragraphs,
              ...(colspan > 1 ? { colspan } : {}),
              ...(rowspan > 1 ? { rowspan } : {}),
              ...(verticalAlign ? { verticalAlign } : {}),
            };
          }),
        };
      });

    const columnCount = getLogicalColumnCount(rows);
    if (
      columnCount === null ||
      !isCanonicalColumnWidthsBps(block.columnWidthsBps, columnCount)
    ) {
      invalid = true;
      return;
    }

    if (rows.length > 0) {
      normalizedBlocks.push({
        type: "table",
        columnWidthsBps: [...block.columnWidthsBps],
        rows,
      });
    }
  });

  if (invalid) return null;
  return { blocks: normalizedBlocks };
};

const emptyDocument = (): ContractRichTextDocument => ({ blocks: [] });

export const parseContractRichText = (
  value?: string | null,
): ContractRichTextDocument => {
  const source = value ?? "";
  if (source.length === 0) {
    return emptyDocument();
  }
  if (!source.startsWith(CONTRACT_RICH_TEXT_PREFIX)) {
    throw new Error("Nội dung điều khoản không dùng định dạng rich text v4.");
  }

  try {
    const document = normalizeContractRichTextDocument(
      JSON.parse(source.slice(CONTRACT_RICH_TEXT_PREFIX.length)),
    );
    if (!document) throw new Error("Rich text v4 không hợp lệ.");
    return document;
  } catch (error) {
    if (error instanceof Error) throw error;
    throw new Error("Rich text v4 không hợp lệ.");
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
  const normalized = normalizeContractRichTextDocument(document);
  if (!normalized) throw new Error("Rich text v4 không hợp lệ.");
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

      const colgroup = `<colgroup>${block.columnWidthsBps
        .map((width) => `<col style="width:${width / 100}%">`)
        .join("")}</colgroup>`;
      return `<table style="table-layout:fixed;width:100%">${colgroup}<tbody>${block.rows
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
