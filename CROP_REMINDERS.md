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
