---
date: 2026-06-02T14:21:00+07:00
topic: chatbot-project-grounding
type: technical-journal
---

# Chatbot Project Grounding

## Context

Ground chatbot in QLCM Pro workflows while keeping external AI use limited to sanitized software-operation guidance.

## What Happened

- Free-tier Gemini API key cannot be auto-acquired by the app. Account owner must create it in [Google AI Studio](https://aistudio.google.com/api-keys), accept provider terms, restrict it to Gemini API, then inject through environment variables or user-secrets. Repository must not register accounts, collect keys, or persist secrets.
- Official [Gemini API reference](https://ai.google.dev/api) requires `x-goog-api-key` header auth. [Gemini unpaid-service terms](https://ai.google.dev/gemini-api/terms#unpaid-services) allow submitted content and generated responses to improve products, may involve human review, prohibit sensitive/confidential/personal data, and prohibit clinical-practice or medical-advice use.
- Added curated QLCM grounding catalog with accent-insensitive topic retrieval for live and demo replies.
- Added context builder: mandatory core rules, relevant catalog topics, permission-filtered routes, aggregate-only operational counts. No patient records, visit data, notification content, or audit payloads.
- Added local privacy guard before transport and before saving AI prompt customization. Likely patient identifiers and medical-advice prompts stay local and receive refusal.
- Moved Gemini key from URL query to `x-goog-api-key` header.
- Scoped preferences and chatbot clients per Blazor circuit. Added provider/model/base URL allowlist validation and provider-compatible model resolution.

## Reflection

Grounding is a constrained operations assistant, not a clinical assistant. User customization may supplement voice and deployment guidance but cannot replace mandatory safety rules. Header auth reduces accidental key exposure in URLs but does not remove secret-rotation duties.

## Decisions

- Keep API key provisioning manual and owner-controlled.
- Keep free-tier chatbot input sanitized and non-identifying.
- Keep grounding mandatory, permission-aware, and aggregate-only.
- Fail closed for unsupported provider, model, or non-official HTTPS base URL.

## Verification

`dotnet test .\telemedicine-landing-page.sln -c Release --no-restore`: passed `175/175`; failed `0`; skipped `0`.

## Next

- Rotate any API key exposed locally during development, then re-inject a restricted key through secret storage.
- Re-check [Gemini models](https://ai.google.dev/gemini-api/docs/models) and [deprecations](https://ai.google.dev/gemini-api/docs/deprecations) before production. Local default is `gemini-2.5-flash`; stable model lifecycle may change.

## Unresolved Questions

- Which restricted production key owner and rotation cadence will be used?
- Which stable Gemini model will be approved after the pre-production catalog/deprecation review?
