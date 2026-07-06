# System-wide Remediation Plan

Status: Complete

## Goal
Fix confirmed UI, security, realtime, clinical export, chatbot, and Docker regressions without broad architectural churn.

## Phases
1. [Security, auth, realtime](phase-01-security-auth-realtime.md) - complete
2. [Sidebar and filters](phase-02-sidebar-and-filters.md) - complete
3. [Clinical signature and export](phase-03-clinical-signature-export.md) - complete
4. [Chatbot and Docker](phase-04-chatbot-docker.md) - complete
5. [Verification, docs, release](phase-05-verification-docs-release.md) - complete

## Key Decisions
- Replace raw browser user id with an expiring ASP.NET Data Protection token.
- Restrict SignalR user-group joins to validated tokens.
- Use a singleton in-process change bus for cross-circuit refresh; document single-web-instance scope.
- Keep the existing hospital logo asset and resolve hospital name from the root department.
- Export one selected-patient professional dossier with printable A4 HTML.
- Persist validated signature PNG metadata and bind it into the integrity hash.
- Use the injected rich chatbot context builder and expose Docker chatbot configuration clearly.

## Dependencies
- Existing SQL Server schema remains unchanged.
- Existing `/brand/logo-hos.jpg` remains the branding source.
- Existing Docker Compose workflow remains the release path.

## Definition Of Done
- Release build and all tests pass.
- Docker image rebuilt, service healthy, browser smoke passes.
- Adversarial code review has no unresolved high-severity finding.
- Docs updated, focused commit pushed to upstream branch.
