# Farmer functionality test report

Test date: 9 October 2026 (Asia/Calcutta). Target: http://localhost:5175.

## Result

- Supplied farmer account logged in successfully; dashboard, forms and saved data were exercised in Chrome against the local MariaDB application.
- 26 page checks passed: 13 routes at desktop 1440px and mobile 390px. No page JavaScript errors or horizontal overflow were detected, including populated query history/chat. Mobile menu opened successfully.
- Full isolated integration suite: **48 passed, 0 failed, 0 skipped**. Twelve new farmer regression cases were added in Tests/FarmerFunctionalTests.cs.
- Browser action checks: **42 final checks passed** (latest result per named check).

## Feature-by-feature evidence

| Feature | What was checked | Result / test environment |
|---|---|---|
| Login and logout | Real farmer login, dashboard redirect, logout blocks subsequent access, re-login after fixes | Passed on supplied account |
| Dashboard | Four counters, quick-action destinations, recent queries, weather, crop empty state; counts changed from 0 queries to 3 and crops returned to 0 | Passed on supplied account |
| Profile | View, district/village dropdown loading, change language, save/reload and restore original profile; visible invalid-area error | Passed on supplied account; field persistence also tested in isolated DB |
| My Crops | Open modal, add 0.1-acre crop, verify Growing, mark Harvested, verify persisted status, delete only the test crop | Passed on supplied account |
| Crop validation/security | Malformed area/date, negative/excessive area, invalid status and another farmer's update/delete attempts | Live invalid-input checks and isolated ownership tests passed |
| Text advisory | Submit labeled question through UI, save to real DB, find it in history; empty text blocked; simulated server outage preserves text for retry | Passed on supplied account |
| Photo advisory | Upload synthetic 64x64 PNG through UI, save image query, read protected media; reject text file and >5 MB upload | Passed; synthetic image tests transport, not crop diagnosis |
| Photo privacy | Owner can read photo; anonymous access denied; role/ownership tests | Live and isolated checks passed |
| Voice | Simulated browser recognition fills transcript; real UploadVoice endpoint saves it; empty/invalid transcript tests | Transcript path passed; actual microphone/service recognition not verified |
| Query history | Search actual text question; status filter; all three QA questions visible; 52-record pagination and cross-farmer isolation | Live filters and isolated pagination/security passed |
| Officer replies | Empty state on real account; pending draft privacy; approve/edit/reject workflow and reviewed answer visibility | Isolated workflow tests passed; real account has no assigned officer yet |
| Feedback | Pending question and out-of-range ratings rejected live; answered-query rating saves/updates as one record and rejects other farmers | Live negative checks and isolated successful save/update passed |
| Disease library | Real catalog, detail modal open/close, empty search; crop and symptom search, inactive entries excluded | Live and isolated checks passed |
| Government schemes | Real empty state; category filtering, category-null rendering, active detail page, expired detail blocked | Isolated data tests passed; real DB has no currently available scheme |
| Weather | Real district forecast, Ahmedabad search, unknown district shows unavailable state; provider units/failure behavior | Live and isolated checks passed |
| Change password | Real wrong-current-password rejection; successful change and old-session invalidation in isolated tests | Passed; supplied account password was not changed |
| Authorization/CSRF | Farmer blocked from admin/officer routes; missing antiforgery token rejected | Live and isolated checks passed |
| Privacy and mobile navigation | Privacy page, sidebar opening, page layouts and screenshots | Passed |

## Bugs found and fixed during this test

1. Crop submission ignored model-binding errors, allowing malformed dates/areas. It now checks ModelState and caps land area at 1,000,000 acres. Invalid cases do not create records.
2. Officer Replies omitted officer-approved AI answers unless a separate OfficerReply field existed. It now includes approved/modified responses and displays the reviewed answer and approval date, while pending drafts remain private.
3. A scheme with an optional null category caused a dictionary lookup crash. The category icon now handles null safely.
4. Farmer profile field errors were not reliably visible in the summary. The summary now displays all validation errors.

## Data and limitations

- One temporary crop was created, harvested and removed. Original profile values were restored. Last-login/update timestamps can change as part of these tests.
- Three clearly labeled local test questions remain: [QA 2026-10-09 TEXT], [QA 2026-10-09 IMAGE] (query 67), and [QA 2026-10-09 VOICE] (query 68). Dashboard shows 3 total / 3 pending / 0 crops / 0 feedback. These are test records, not agricultural instructions.
- This account has no active officer assignment. Questions correctly remain Unassigned. Assign a verified active officer in Admin before testing a real staff response; this run did not fabricate an officer answer on the real account.
- No available scheme exists in the real database at test time. No schemes were invented or added to the user's database.
- Real microphone capture, speech-service accuracy, live OTP/SMTP delivery and expert validation of crop advice remain manual/staging checks. Password change and successful reviewed-answer feedback were tested with isolated users, not by changing this account's password or existing ratings.
- Some first browser harness attempts failed because of a missing optional video encoder, Unicode output, reload timing or a selector/mock mismatch. These harness issues were corrected; screenshots were used instead of video. Raw actions.json retains the initial speech-stub failure followed by its passing retry.
- Test scripts/session files and screenshots are under ignored local directories. No password was written into test source or the report. The temporary browser session file was removed after verification.
- Fixes and the new regression tests are local changes from this testing request; they have not been committed/pushed by this run.

## Reproduce / inspect

Run `dotnet test Tests/Dko.Tests.csproj -c Release` for the full isolated suite. Browser screenshots and raw JSON evidence are in artifacts/farmer-qa. Open artifacts/farmer-qa/report.html for the screenshot gallery and final action results.

## Subsequent catalog update
After the test run above, the requested catalog import refreshed five scheme entries and official links; the eight source crops were verified with a duplicate-safe import. See Data/Imports/README.md and artifacts/catalog-checks/results.json for the later state.


## AI-first workflow correction
The blanket-review behavior tested earlier was superseded at the owner's request. Routine AI answers now publish immediately; only uncertain/complex/failure cases route automatically to officers. See AI_FIRST_WORKFLOW.md for the current behavior and the passing 69-test suite.

