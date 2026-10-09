# AI-first chatbot workflow

Updated 9 October 2026 to match the intended product: AI answers first; Krushi Officers handle cases that AI cannot reliably solve.

## Decision and routing

| Case | Farmer receives | Officer task |
|---|---|---|
| Routine farming question, including a simple clarifying response | Immediate AI answer, labeled AI answer | None |
| Clear photo with a supported assessment | Immediate AI answer | None |
| Unclear/non-crop image or uncertain disease diagnosis | Explanation that officer help is needed | Assigned automatically |
| Complex case requiring specialist investigation | Officer handoff status | Assigned automatically |
| Provider failure, malformed or truncated assessment | Honest handoff status | Assigned automatically |
| Escalated case and no active verified officer exists | Saved assignment-queue status | Admin intervention needed |

The provider returns structured answer, requiresOfficer, uncertain, complexity, reason and imageAssessment fields. The server validates these and forces escalation for uncertainty, complex cases and unclear image assessments even when requiresOfficer is false. It does not use fabricated confidence percentages. These model assessments are routing signals, not calibrated certainty or guaranteed diagnostic accuracy.

Officer selection: first honor an active farmer-officer assignment whose officer is active and email-verified; otherwise prefer a matching district, then the lowest count of open escalations, then ID for a stable tie-break. Selection is per query and does not create a permanent farmer assignment. Queue counts are a snapshot rather than a strict concurrent load-balancing guarantee.

AIAnswered (enum value 4 in the existing integer field) is distinct from officer Approved/Modified. Direct answers are visible immediately in chat/history and accept feedback; they do not appear in Officer Replies or the officer review queue. Escalated draft assessments stay private until reviewed. Officers see the reason for escalation.

## Verification

- Full isolated suite: 69 passed, 0 failed, 0 skipped (21 new AI-first cases plus the existing 48 tests).
- Tested routine text/voice/image publication, immediate API answer, no officer task, ratings on AI answers, markup escaping, uncertain/complex/provider-failure escalation, automatic district assignment, workload selection, disabled/unverified officers, and no-officer fallback.
- Structured provider parsing tests cover clear/unclear/non-crop image signals, contradictory flags, malformed JSON and truncated responses.
- Real Gemini + local MariaDB + Chrome checks on the supplied farmer account:
  - Query 70: basic compost question -> Answered / AIAnswered; answer immediately visible; no escalation or officer assigned.
  - Query 71: synthetic unclear image -> PendingApproval; officer 10 assigned automatically.
  - Query 72: conflicting crop diagnostic reports -> PendingApproval; officer 10 assigned automatically.
- Mobile chat check: no horizontal overflow or JavaScript errors.
- EF check: no pending model changes; no schema migration required.
- Screenshots and live response summaries: artifacts/ai-first (ignored local evidence).

Existing historical pending queries were not automatically reclassified or published; old drafts may not have the new structured triage information. Reviewed answers were preserved. Three labeled QA queries were added by this verification. The new behavior applies to subsequent text, photo and voice submissions.

## References

Gemini structured generation request follows the official REST reference: https://ai.google.dev/api/generate-content . Provider routing is verified locally with both controlled payloads and real requests; model quality still needs representative agricultural evaluation before launch.
