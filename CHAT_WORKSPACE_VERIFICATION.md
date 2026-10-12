# Chat workspace verification — 10 October 2026

Implemented account-owned conversations using ParentQueryId, new chat, recent history and title search. Each chat loads its latest 50 turns with pagination for older messages. The latest six published turns are supplied as AI context; answers are capped at 3,000 characters each in the provider context. Prior photos are not resent automatically; upload them again when a fresh visual comparison is needed.

The composer immediately displays the user's question and a pending reply without navigating/reloading. Text grows with input, Enter sends and Shift+Enter adds a line. Photos support multiple selection, additive selections, individual removal, clipboard paste and drag/drop. Failed requests retain the draft and selected photos. Replies are shown when the full provider response is available (not token streaming).

Limits: 20 JPEG/PNG/WebP photos per message, 5 MB each, 25 MB total. More photos can be sent in a subsequent message. Images are decoded, normalized to JPEG and resized to a maximum long edge of 1,600 pixels. All photos reach the AI provider and remain accessible through protected media endpoints to authorized users. Officer review and farmer history show all attachments. Existing single-photo records remain compatible.

AI-first routing is unchanged: ordinary questions are answered directly; ambiguous/complex/unavailable AI cases are automatically escalated.

## Verification

- 73 automated tests passed, including same-chat context, new-chat isolation, cross-account access rejection, history search/pagination, six-turn context bound, three-photo upload, normalized dimensions, private media access, attachment count limits, and provider payload containing multiple photos plus user/model history.
- Live Gemini/browser checks: cotton context remembered across two follow-ups; two unclear photos uploaded together and automatically escalated. The routine text answers were published directly.
- Browser checks: no document navigation on send, immediate pending user bubble, photo selection/removal, paste/drop handlers, failure retaining text/photos, history switching, mobile drawer, desktop and 390px mobile layout. No JavaScript errors observed. Voice transcription still depends on browser microphone support; real microphone capture was not tested in this pass.
- Local migration 20261010150710_ConversationPhotos applied.
- Screenshots and live QA evidence: artifacts/chat-workspace (ignored).

## Authorized cleanup

Removed all 77 pre-existing queries, 76 AI responses, 13 escalations and 8 feedback records; removed 20 referenced uploaded media files. A local private recovery SQL backup remains at App_Data/backups/queries-before-clear-20261010.sql (ignored).

After browser tests, removed only QA query IDs 78–81 and their two uploaded photos. Verified query/AI-response/escalation/feedback counts are all zero. User accounts, crop catalog and government schemes were preserved.

The application is available locally at http://localhost:5175/Chat/Index?newChat=true. No deployment or GitHub push was performed in this change.
