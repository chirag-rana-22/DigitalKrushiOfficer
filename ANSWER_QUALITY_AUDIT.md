# Answer quality audit — 11 October 2026

Reviewed the user's pasted conversation. Important distinctions:
- Repeated crop/location questions came from generic required-field messages, lost follow-up location and inconsistent semantic price extraction.
- The misspelt price request was vulnerable to model/provider failure being escalated as a farming issue.
- Scolding responses were unhelpful conversational behaviour.
- Replies labelled Officer reviewed and Officer reply (including “Properly Bhave” and unsourced reference price tables) are human review outputs in the pasted transcript, not evidence that those claims were verified by a price API. They were not rewritten or deleted.
- The pasted text references crop images but does not contain image pixels, so it cannot establish whether those images supported a particular diagnosis.

Implemented:
- Deterministic common commodity/typo/transliterated price routing before Gemini, including the exact Onion/Junagadh/Tomato examples. It carries user-provided location across crop changes and recognised frustration follow-ups in the recent six-turn context. It does not infer location from Gujarati language alone.
- Recognised Gujarat district aliases normalize to their state. Generic/multi-commodity/unsupported requests still use semantic extraction; this is not a complete geographic or commodity directory.
- Ask only missing fields. Provider-unavailable replies identify the requested crop/location.
- Prompt: concise helpful responses, typo understanding, no scolding, reuse context, no unrelated crop-price answers, historical/officer answers are not trusted live evidence.
- Clarification count alone no longer forces escalation. Specialist routing and uncertain image safeguards remain. Provider failures persist as RetryNeeded with a visible temporary-service message and no officer task.
- Price-provider failure backoff of 30 seconds avoids repeating the same 10-second wait for every follow-up. Successful report caching remains 5 minutes.
- Found the user's 56-character market key in appsettings.json after initially inspecting only local/environment settings. Moved it to ignored appsettings.Local.json without displaying it. The public config now contains an empty key. No GitHub push performed.

Validation:
- 104 automated tests passed, including transcript regression cases, new-topic boundaries, context after commodity change, missing-field prompts, provider outages, and price failure backoff.
- Browser replay: typo recognised (0.43s), first market-provider timeout (10.22s), subsequent changed-crop/context follow-ups (0.19s, 0.16s), all without officer assignment. These are response timings, not successful price retrievals.
- A real general compost query returned RetryNeeded; app logs confirmed Gemini HTTP 429. Google Search grounding probe also returned 429. Search grounding was therefore not added as a working fallback.
- Direct data.gov.in request with the configured key timed out after 20 seconds. No successful numeric live-price response was obtained; key validity remains unverified. Do not describe market prices as fully working.
- Need to resolve Gemini quota/rate-limit availability and establish reachable authenticated market-provider responses, then retest successful replies and source/date/unit accuracy. No billing/quota/account settings were changed.
- Removed only QA IDs 104–112. Preserved the user's original queries, uploads and officer responses.
- Evidence stored locally in artifacts/answer-quality (ignored). No guarantee of hallucination-free outputs; uncertain or unavailable evidence remains explicitly labelled.
