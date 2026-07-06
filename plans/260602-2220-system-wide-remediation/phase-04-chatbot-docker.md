# Phase 04 - Chatbot And Docker

## Overview
Priority: High
Status: Complete

Restore rich grounding, long-answer behavior, manual scroll control, and reproducible Docker configuration.

## Related Files
- `Components/Chatbot/ChatbotPanel.razor`
- `Services/Chatbot/GeminiChatbotClient.cs`
- `Services/Chatbot/AnthropicChatbotClient.cs`
- `Models/Chatbot/ChatbotOptions.cs`
- `docker-compose.yml`
- `.env.example`
- `src/telemedicine-landing-page/telemedicine-landing-page.csproj`

## Implementation
1. Resolve typed clients with injected `IChatbotContextBuilder`.
2. Respect manual upward scroll before auto-scroll and show new-content chip during streamed growth.
3. Raise default bounded token limit and surface Gemini `MAX_TOKENS` or safety stop notices.
4. Map Docker `CHATBOT_BASE_URL` and `CHATBOT_MAX_TOKENS`; add `UserSecretsId`.
5. Document Compose host variables and provider endpoint requirements.

## Success Criteria
- Live client request includes rich system context.
- User can scroll upward while response continues streaming.
- Truncated Gemini response explains that continuation is needed.
- Docker users can enable chatbot through `.env` without editing container files.
