export const CONTRACT_RICH_TEXT_PREFIX = "contract-rich-text:v2:";
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
}

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
            const paragraphs = Array.isArray(cell.paragraphs)
              ? cell.paragraphs.slice(0, 100).map(normalizeParagraph)
              : [normalizeParagraph({ runs: cell.runs })];
            return { paragraphs };
          }),
        };
      })
      .filter((row) => row.cells.length > 0);

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

export const parseContractRichText = (
  value?: string | null,
): ContractRichTextDocument => {
  const source = value ?? "";
  const prefix = source.startsWith(CONTRACT_RICH_TEXT_PREFIX)
    ? CONTRACT_RICH_TEXT_PREFIX
    : source.startsWith(CONTRACT_RICH_TEXT_LEGACY_PREFIX)
      ? CONTRACT_RICH_TEXT_LEGACY_PREFIX
      : null;
  if (!prefix) {
    return plainTextDocument(source);
  }

  try {
    return (
      normalizeContractRichTextDocument(
        JSON.parse(source.slice(prefix.length)),
      ) ?? plainTextDocument("")
    );
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

      return `<table><tbody>${block.rows
        .map(
          (row) =>
            `<tr>${row.cells
              .map(
                (cell) => `<td>${cell.paragraphs
                  .map((paragraph) => {
                    const style = paragraph.alignment
                      ? ` style="text-align:${paragraph.alignment}"`
                      : "";
                    return `<p${style}>${runsToHtml(paragraph.runs) || "<br>"}</p>`;
                  })
                  .join("")}</td>`,
              )
              .join("")}</tr>`,
        )
        .join("")}</tbody></table><p><br></p>`;
    })
    .join("");
