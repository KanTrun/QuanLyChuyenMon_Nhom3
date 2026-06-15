# KSNK Procedure Scan Extraction

This folder stores machine OCR output for the four scanned source PDFs used by the internal medical procedure workflow. The PDFs have no text layer, so OCR is retained as auditable source evidence before any seed data is promoted from `OCR_PENDING` to publishable content.

## Extraction Command

```powershell
$env:PYTHONIOENCODING='utf-8'
python scripts\extract-ksnk-procedure-scans.py `
  --input-dir 'D:\Downloads\ipv' `
  --output-dir 'docs\procedure-source-extraction' `
  --image-dir 'D:\BenhVienQuanLy_Nhom3\.tools\procedure-ocr' `
  --model-dir 'D:\BenhVienQuanLy_Nhom3\.tools\easyocr' `
  --scale 2.0 `
  --canvas-size 896 `
  --mag-ratio 1.0
```

## Coverage

| Procedure | Source pages | Average confidence | Low-confidence pages requiring visual review |
|---|---:|---:|---|
| `QT.KSNK.09` | 52 | 0.8315 | 1, 36, 43 |
| `QT.KSNK.12` | 43 | 0.8255 | 19, 21, 27, 38, 41 |
| `QT.KSNK.16` | 22 | 0.8203 | 11, 14 |
| `QT.KSNK.17` | 12 | 0.8234 | 10 |

## Review Rules

- Treat the source PDF images as the source of truth.
- Treat OCR text as a working extraction, not final clinical content.
- Keep the publication gate active until low-confidence pages and all flowchart/table pages are visually checked against rendered page images.
- Use the JSON files for bounding boxes and confidence. Use the Markdown files for quick human review.
