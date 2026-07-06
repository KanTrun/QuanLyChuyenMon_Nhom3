# Phase 03 - Clinical Signature And Export

## Overview
Priority: Critical
Status: Complete

Make clinical signing visible, durable, and professionally printable.

## Related Files
- `Application/Signature/SignatureService.cs`
- `Services/Admin/ClinicalExportService.cs`
- `Components/Pages/Admin/LamSangPage.razor`
- `Components/Pages/Clinical/ClinicalPage.razor`

## Implementation
1. Capture PNG data URL from the signature pad, validate size/type, persist metadata.
2. Bind metadata into the signature integrity hash while preserving legacy verification.
3. Remove direct signed/revoked choices from ordinary clinical status selectors.
4. Export selected-patient dossier with logo, root hospital name, professional A4 margins, ordered sections, signature evidence, and revoked state.
5. Escape all user-controlled export values.

## Success Criteria
- Dark mode signature pad remains visible.
- Drawn signature appears in exported signed dossier.
- Signed/revoked states can only be reached through signature workflow.
- Export is printable as a professional A4 hospital document.
