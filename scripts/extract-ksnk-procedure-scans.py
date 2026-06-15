from __future__ import annotations

import argparse
import json
import os
import re
from pathlib import Path

import easyocr
import fitz


PROCEDURES = {
    "QT.KSNK.09": "PHẪU THUẬT",
    "QT.KSNK.12": "XỬ LÝ DỤNG CỤ Y TẾ.pdf",
    "QT.KSNK.16": "KHỬ KHUẨN MỨC ĐỘ CAO",
    "QT.KSNK.17": "TAY KHOAN NHA KHOA",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Render and OCR scanned KSNK procedure PDFs page by page."
    )
    parser.add_argument("--input-dir", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--image-dir", required=True, type=Path)
    parser.add_argument("--model-dir", required=True, type=Path)
    parser.add_argument("--code", choices=PROCEDURES)
    parser.add_argument("--scale", type=float, default=2.0)
    parser.add_argument("--canvas-size", type=int, default=896)
    parser.add_argument("--mag-ratio", type=float, default=1.0)
    parser.add_argument("--start-page", type=int, default=1)
    parser.add_argument("--end-page", type=int)
    return parser.parse_args()


def find_pdf(input_dir: Path, marker: str) -> Path:
    matches = [
        path
        for path in input_dir.glob("*.pdf")
        if marker.casefold() in path.name.casefold()
    ]
    if len(matches) != 1:
        raise RuntimeError(f"Expected one PDF containing {marker!r}, found {matches}")
    return matches[0]


def normalize_text(value: str) -> str:
    return re.sub(r"\s+", " ", value).strip()


def reading_order(result: tuple) -> tuple[float, float]:
    box = result[0]
    return min(point[1] for point in box), min(point[0] for point in box)


def ocr_page(reader: easyocr.Reader, image_path: Path, args: argparse.Namespace) -> list[dict]:
    results = reader.readtext(
        str(image_path),
        detail=1,
        paragraph=False,
        decoder="greedy",
        workers=0,
        batch_size=1,
        canvas_size=args.canvas_size,
        mag_ratio=args.mag_ratio,
    )
    lines = []
    for box, text, confidence in sorted(results, key=reading_order):
        normalized = normalize_text(text)
        if not normalized:
            continue
        lines.append(
            {
                "text": normalized,
                "confidence": round(float(confidence), 4),
                "box": [[round(float(x), 1), round(float(y), 1)] for x, y in box],
            }
        )
    return lines


def render_page(page: fitz.Page, output: Path, scale: float) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    pixmap = page.get_pixmap(matrix=fitz.Matrix(scale, scale), alpha=False)
    pixmap.save(output)


def write_summary(code: str, pdf: Path, pages: list[dict], output_dir: Path) -> None:
    slug = code.lower().replace(".", "-")
    payload = {
        "procedureCode": code,
        "sourcePdf": pdf.name,
        "sourceSizeBytes": pdf.stat().st_size,
        "pageCount": len(pages),
        "ocrEngine": "EasyOCR 1.7.2 vi+en",
        "pages": pages,
    }
    output_dir.mkdir(parents=True, exist_ok=True)
    (output_dir / f"{slug}-ocr.json").write_text(
        json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    markdown = [
        f"# {code} OCR extraction",
        "",
        f"- Source: `{pdf.name}`",
        f"- Pages: {len(pages)}",
        "- Engine: EasyOCR 1.7.2 (`vi`, `en`)",
        "- Status: machine extraction; low-confidence lines require visual review.",
        "",
    ]
    for page in pages:
        markdown.extend(
            [
                f"## Page {page['pageNumber']}",
                "",
                f"Average confidence: `{page['averageConfidence']:.4f}`",
                "",
                page["text"] or "_(No text detected)_",
                "",
            ]
        )
    (output_dir / f"{slug}-ocr.md").write_text("\n".join(markdown), encoding="utf-8")


def process(code: str, pdf: Path, args: argparse.Namespace, reader: easyocr.Reader) -> None:
    slug = code.lower().replace(".", "-")
    checkpoint_dir = args.image_dir / slug / "ocr-pages"
    checkpoint_dir.mkdir(parents=True, exist_ok=True)
    pages = []
    document = fitz.open(pdf)
    last_page = min(args.end_page or len(document), len(document))

    for page_number in range(1, len(document) + 1):
        checkpoint = checkpoint_dir / f"page-{page_number:03d}.json"
        image_path = args.image_dir / slug / f"page-{page_number:03d}.png"
        should_process = args.start_page <= page_number <= last_page
        if checkpoint.exists():
            page_data = json.loads(checkpoint.read_text(encoding="utf-8"))
        elif should_process:
            render_page(document[page_number - 1], image_path, args.scale)
            lines = ocr_page(reader, image_path, args)
            confidence = sum(line["confidence"] for line in lines) / max(len(lines), 1)
            page_data = {
                "pageNumber": page_number,
                "imageFile": image_path.name,
                "averageConfidence": round(confidence, 4),
                "text": "\n".join(line["text"] for line in lines),
                "lines": lines,
            }
            checkpoint.write_text(
                json.dumps(page_data, ensure_ascii=False, indent=2), encoding="utf-8"
            )
            print(
                f"{code} page {page_number}/{len(document)}: "
                f"{len(lines)} lines, confidence {confidence:.3f}",
                flush=True,
            )
        else:
            continue
        pages.append(page_data)

    if len(pages) == len(document):
        write_summary(code, pdf, sorted(pages, key=lambda item: item["pageNumber"]), args.output_dir)
        print(f"{code}: complete ({len(pages)} pages)", flush=True)
    else:
        print(f"{code}: checkpointed {len(pages)}/{len(document)} pages", flush=True)


def main() -> None:
    args = parse_args()
    selected = {args.code: PROCEDURES[args.code]} if args.code else PROCEDURES
    reader = easyocr.Reader(
        ["vi", "en"],
        gpu=False,
        model_storage_directory=str(args.model_dir),
        download_enabled=False,
        verbose=False,
    )
    for code, marker in selected.items():
        process(code, find_pdf(args.input_dir, marker), args, reader)


if __name__ == "__main__":
    os.environ.setdefault("PYTHONIOENCODING", "utf-8")
    main()
