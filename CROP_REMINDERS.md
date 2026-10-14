# Crop calendar and reminder setup

Farmers add a crop, name its field and variety, and set their sowing and estimated harvest dates. Dates cover one crop/harvest cycle of up to two years. The calendar preview must be confirmed before its reminders become active. It suggests preparation, sowing confirmation, weekly inspections and harvest-readiness checks, not chemical or irrigation prescriptions. Every task can be completed, skipped, postponed until tomorrow or rescheduled. Farmers can add their own dated tasks. A confirmed actual harvest stops remaining reminders.

Editing crop dates preserves existing tasks and asks the farmer to review the proposed calendar. Confirming that calendar replaces pending suggested tasks, preserves custom tasks and completed/skipped history, and excludes dates already in the past. Website reminders are calculated from India dates on every visit, including overdue tasks; they do not depend on an in-memory timer.

Selected-crop AI chat uses farmer-owned details and the last three completed tasks. The user-visible question remains unchanged. Each saved query stores the context snapshot so an assigned officer can see relevant crop information. Conversations retain their selected crop; switching crops requires a new chat. Ordinary AI answers remain immediate, with expert escalation only through the existing uncertainty policy.

## Optional email delivery

Email is off by default for every crop. Enable it in that crop's Edit details page. Only active, verified farmer accounts with opted-in, growing crops and due pending tasks receive a daily summary. Turning the preference off stops later sends. A crop harvest also stops sends for that crop.

Runtime settings:

- `Reminders__Enabled=true`
- `Reminders__SchedulerToken`: random token of at least 32 characters, stored only in private Render settings.
- `Reminders__PublicUrl=https://digital-krushi-officer.onrender.com`
- `Reminders__DailyEmailLimit=100` (hard maximum 100 daily summaries, leaving capacity for account emails).
- GitHub repository Actions secret `DKO_REMINDER_TOKEN` must match the runtime token. No database credentials or Brevo key are placed in GitHub Actions.

The `Crop reminder delivery` workflow calls the authenticated POST endpoint `/internal/reminders/run` at approximately 09:07 and 10:07 India time. A persisted unique farmer/date claim prevents repeated scheduler requests from sending the same daily summary again. At most one provider attempt per farmer/day is made; timeout outcomes are not retried blindly. A failed or unfinished delivery remains visible in the crop calendar; use website tasks if delivery fails. Provider acceptance does not confirm inbox delivery.

The endpoint returns 404 when disabled and 401 for a wrong/missing token. It exposes only aggregate counts, and accepts no recipient or arbitrary message payload. Keep the token out of Git, frontend, URLs, screenshots and logs.

Free scheduled workflows can be delayed and public-repository schedules are disabled after 60 days without repository activity. Render Free can sleep, and mail-provider quotas still apply. Check the workflow's last successful run in GitHub Actions periodically. This free demo setup cannot guarantee exact-time notifications. [GitHub schedule documentation](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#schedule)

## Deployment

Back up `TblFarmerCrop`, `TblQuery` and migration history, apply `CropCalendarAndReminders` with the private migration account, then deploy the reviewed source. The runtime database account continues to have only CRUD access. `App_Data/cloud/reminders-secrets.json` holds the ignored local scheduler settings; do not publish it.

Validation covers crop and task ownership, date validation, plan history, task actions, harvest cancellation, crop AI context, scheduler authentication, daily delivery caps, HTML escaping, repeated calls and concurrent updates. No reminder emails are sent by automated tests.

## Verified deployment — 14 October 2026

- Live deployment: `0f2ea97`, Render deploy `dep-db7l2oqj9qps73crcvqg`, succeeded in 1m46s. The existing Free plan was retained.
- Backed up crop/query tables and migration history privately before applying the cloud migration. Existing crop records were preserved; email preference defaulted off.
- 139 automated tests passed. GitHub CI for the deployed commit passed tests, Linux Docker build and container startup.
- The owner approved the limited scheduler token for Render and this repository's Actions secrets. `DKO_REMINDER_TOKEN` was stored successfully. No database or email-provider credentials were sent to GitHub Actions.
- Live scheduler authentication verified: missing token returned 401; valid token returned 200 with sent=0, failed=0. No crop was opted in during verification, so no live reminder email was sent.
- GitHub manual workflow run succeeded: https://github.com/chirag-rana-22/DigitalKrushiOfficer/actions/runs/38546207684 . Daily scheduled runs are configured for approximately 09:07 and 10:07 India time; the future scheduled execution has not yet been observed.
- Farmer login, dashboard due-task summary, crop edit screen, calendar preview and selected-crop chat were checked live. A real AI test correctly identified the selected Bajra record, 25-acre area and 13 October sowing date without officer escalation (conversation 114).
- Farmer field details, calendar dates and email preferences were left for the owner/farmer to confirm. No actual harvest or agronomic task was marked complete by the deployment checks.
- Safe local screenshots: crop-calendar-live.png, crop-edit-live.png, crop-ai-context-live.png, github-reminder-secret.png and reminder-workflow-success.png under ignored artifacts/cloud-deployment.
