---
name: markitdown
description: "Convert files to Markdown using Microsoft's markitdown tool. Supports PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), HTML, images, audio, CSV, JSON, XML, ZIP, and EPUB. Use this skill whenever the user wants to: extract text/content from a document, convert a file to markdown, read/parse a non-text file for analysis, or mentions 'markitdown'. Also trigger when the user says things like '转换为markdown', '转成md', '文件转markdown', '提取文件内容', '读取这个文档', '解析这个文件', or wants to analyze/summarize the contents of a supported file type. Even if the user just says 'help me read this PDF' or 'what's in this Excel file', this skill applies."
---

# MarkItDown — File to Markdown Converter

Convert virtually any document into clean Markdown text using Microsoft's `markitdown` CLI tool. This is especially useful for feeding document content into LLM analysis, summarization, or further processing.

## Supported Formats

| Format | Extensions |
|--------|-----------|
| PDF | `.pdf` |
| Word | `.docx` |
| Excel | `.xlsx`, `.xls` |
| PowerPoint | `.pptx` |
| HTML | `.html`, `.htm` |
| Images | `.jpg`, `.png`, `.gif`, `.bmp`, `.tiff` |
| Audio | `.mp3`, `.wav` |
| CSV/TSV | `.csv`, `.tsv` |
| JSON | `.json` |
| XML | `.xml` |
| Archive | `.zip` |
| eBook | `.epub` |

## How to Use

### Basic Conversion (output to stdout)

```bash
markitdown <file_path>
```

The converted Markdown content prints to stdout. Read it directly or capture it for further processing.

### Save to File

```bash
markitdown <file_path> -o output.md
```

### Workflow

When the user provides a file to convert or analyze:

1. **Identify the file path** — get the absolute path from the user's message or context
2. **Run markitdown** — execute the CLI command via Bash tool
3. **Process the output** — depending on what the user wants:
   - If they just want the content: display it directly
   - If they want it saved: use `-o` to write to a file
   - If they want analysis/summary: capture the output and work with it
   - If the output is very large: save to a file first, then read relevant sections

### Example Commands

```bash
# Convert a PDF and display content
markitdown "D:/documents/report.pdf"

# Convert Excel to markdown file
markitdown "D:/data/sales.xlsx" -o "D:/data/sales.md"

# Convert a Word document
markitdown "D:/docs/proposal.docx"

# Convert PowerPoint slides
markitdown "D:/presentations/deck.pptx" -o "D:/presentations/deck.md"
```

### Tips

- For large files, save output to a file first with `-o`, then read specific sections as needed — this avoids flooding the terminal
- Image conversion relies on OCR/description capabilities and may produce limited results depending on image content
- Audio conversion requires speech recognition dependencies (installed with `markitdown[all]`)
- File paths with spaces should be quoted
- The tool automatically detects file type from the extension

### Error Handling

If markitdown fails:
- Check the file path exists and is accessible
- Verify the file format is supported
- For corrupted files, the tool may produce partial output or an error message
- Some formats (audio, images) require optional dependencies that were installed with `[all]`
