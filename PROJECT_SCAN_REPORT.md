# Digital Krushi Officer — scan and run report

Date: 8 October 2026

## Run result

- Running locally at http://localhost:5175 using the `http` launch profile.
- ASP.NET Core MVC, target .NET 10; installed SDK 10.0.302.
- EF Core/Pomelo 9 connects to XAMPP MariaDB 10.4.32, database `DigitalKrushiOfficer`, port 3306.
- `dotnet build --no-restore`: succeeded, 0 errors, 4 warnings.
- All seven recorded migrations are applied. Startup applied no additional migrations.
- Startup reports pending model changes. The random BCrypt admin password hash in `Data/AppDbContext.cs:156` is one clear source of model instability; compare the complete snapshot before creating a correction migration.
- The application is served by .NET/Kestrel. XAMPP supplies the database; Apache is not serving this .NET application.

## Verified at runtime

- Home, login, registration, forgot-password and privacy pages: HTTP 200.
- Anonymous Admin, Farmer, Officer and Chat routes: HTTP 302 to login.
- The admin credentials documented in SETUP.md successfully opened the admin dashboard.
- Authenticated admin Farmers, Officers, Crops, Diseases, Schemes, Queries, Feedback, Analytics and AssignFarmerToOfficer pages: HTTP 200.
- Authenticated Scheme list: HTTP 200.
- The browser launch command was issued successfully. Browser automation failed because its sandbox helper could not initialize, so visual appearance and browser JavaScript behavior were not verified.

These are startup/page smoke checks, not a complete end-to-end certification. Farmer/Officer authenticated flows, record creation/edit/delete, SMTP delivery, live Gemini output, actual weather responses, uploads and mobile layouts still need functional testing. No emails were sent during this review. Admin login updates its LastLoginDate as normal. Existing application code and business records were not deliberately edited; generated build output and normal startup/login side effects occurred.

## What the application does

Farmers register and verify an email OTP, maintain location and crop profiles, add crops, ask text/image/voice questions, consult crop disease information, view schemes/weather, read officer replies and submit feedback. AI text requests call Gemini with matched crop/disease/scheme database records. The app classifies question complexity and creates officer escalations for complex text questions. Officers review AI responses and reply to assigned escalations. Admin manages users, crop/disease/scheme content, farmer-officer assignments, queries, feedback and aggregate analytics.

## Fix before public launch

| Priority | Finding and evidence | Required change |
|---|---|---|
| Critical | Registration only checks that Role is nonempty, then stores `model.Role` (`Controllers/AccountController.cs:33,55`). The view model has no role allowlist. A crafted registration can request Admin; after email verification, login trusts the stored role. This was identified statically, not exploited. | Enforce allowed public roles server-side. Keep Admin creation private. Require admin approval for officer access. |
| High | Forms contain antiforgery fields but controllers/global MVC configuration have no antiforgery validation. Login also succeeded without an antiforgery token. | Enforce global validation for unsafe methods and preserve token headers for AJAX requests. Add login/OTP rate limits and attempt caps. |
| High | AI output is rendered through `Html.Raw` and unsanitized `marked.parse` (`Views/Chat/Index.cshtml:161,270,500`; officer review views also use Html.Raw). | Encode stored text and sanitize rendered Markdown/HTML. Test malicious markup in a local fixture before releasing. |
| High | Officer review actions filter by response ID/status, not assigned officer (`Controllers/OfficerController.cs:165-237`). The separate escalation detail/reply actions correctly check officer ownership. | Enforce ownership consistently for listing, viewing, approval, rejection and modifications, or explicitly implement an authorized shared-review pool. |
| High | Complex text answers are returned immediately despite Pending approval (`Controllers/ChatController.cs:133`); chat history also exposes them. Image/voice paths bypass the text review policy. | Use one review policy for all input types. Hold actionable answers until approval, and keep query/response/escalation states consistent. |
| High | Image upload sends only a text prompt to Gemini (`Controllers/ChatController.cs:168`). `AnalyzeCropImageAsync` never reads the supplied image and returns the first matching disease (`Services/AIService.cs:265-276`). | Implement actual image input and image-quality checks. Label the existing behavior as an unverified reference lookup until this is implemented. |
| High | AI success always receives confidence 95; fallback receives 75 (`Services/AIService.cs:183,321`). Fallback includes unverified weather/pesticide advice. The system prompt claims official Gujarat Government affiliation. | Remove unsupported confidence/affiliation claims. Clearly identify unavailable AI responses; use reviewed sources and escalate uncertain advice. |
| High | Weather failures silently return fixed 32°C/mock conditions; rain probability is assigned 60 or 15 based on presence of a rain object (`Services/WeatherService.cs:49,63,120`). | Show unavailable/stale state, identify source/time, and obtain precipitation probability from an appropriate forecast field. Verify wind units against the provider and the km/h UI. |
| High | `appsettings.json` is Git-tracked and contains database/API/SMTP credential fields; local DB access uses root. The documented default admin password works. | Move real secrets to protected configuration, rotate any previously shared credentials, provision a limited database user, and replace demo admin credentials before deployment. |
| High | Build warns about vulnerable MailKit 4.9.0 and MimeKit 4.9.0. | Upgrade to compatible patched versions and rerun build and mail tests. Advisory links below. |
| Medium | Escalation chooses the first active Officer, ignoring the farmer-officer assignment table (`Controllers/ChatController.cs:105`). No-officer handling can leave a pending response without an actionable queue. | Route to the assigned officer, use an explicit fallback queue, and expose overdue/unassigned work to admin. |
| Medium | RejectResponse says AI will regenerate, but only sets NeedsRevision (`Controllers/OfficerController.cs:211-229`). Approval does not resolve the escalation state. | Implement revision/resubmission and a tested state transition flow. |
| Medium | OTPs use Random, are stored in a browser session, and there is no resend endpoint. Failed/expired verification can strand registered users. | Add cryptographically generated codes, expiry/attempt/cooldown controls, resend and recovery flows. Avoid account-existence leaks. |
| Medium | Model seeding hashes the admin password on every model creation; migrations contain admin hash updates. Startup always runs migrations. | Make seed metadata deterministic; move initial admin provisioning out of model seeding. Use reviewed deployment migrations with backup/restore procedures. |
| Medium | Uploads are publicly stored; crop image validation only checks extension and admin disease uploads lack type checks. | Validate actual file contents, size and image dimensions; decide which uploads require authenticated access. |
| Medium | Two nullable warnings: AssignFarmerToOfficer.cshtml:77 and OfficerReplies.cshtml:9. Privacy page remains a template. | Handle missing records safely and write an accurate privacy/data-use page before launch. |

## Suggested implementation order

1. Security: registration roles, officer authorization, antiforgery, safe output rendering, OTP limits, secrets and dependencies.
2. Functional correctness: real image analysis, honest AI/weather failure handling, assignment-based routing and one consistent review workflow.
3. Product experience: Gujarati/Hindi/English UI, mobile testing, clear loading/error/retry states, searchable history and pagination, notification preferences and accessible forms.
4. Operational readiness: staging instance, HTTPS/domain configuration, persistent upload storage, health checks, structured error monitoring, database backups with a tested restore, deployment instructions and automated checks for the important user journeys.
5. Commercial features: organization accounts with isolated data, branding and officer management, usage/cost limits, subscription entitlements, support tooling, audit history and customer onboarding. Start with one target buyer type and validate the existing farmer/officer workflow before expanding features.

## Checks required for a saleable release

- Farmer registration, OTP verification/resend/expiry and password reset.
- Role and cross-account access tests (including forged Admin registration and another officer's review ID).
- Farmer crop lifecycle, image uploads, text/voice flows and feedback validation.
- Assignment, escalation, approval, rejection, revision and notification transitions.
- Missing API key, provider timeout, quota errors, no-officer and empty-database states.
- Mobile browser behavior, offline/slow-network recovery and Gujarati/Hindi text rendering.
- Production deployment and backup restore on a separate database.

## Re-run locally

Keep XAMPP MySQL running. From the project directory:

```powershell
dotnet run --launch-profile http
```

Then open http://localhost:5175. The current development server was left running for inspection. Do not start a second instance on the same port.

## Advisory sources checked

- MailKit: https://github.com/advisories/GHSA-9j88-vvj5-vhgr (lists 4.16.0 as the patched version for this advisory).
- MimeKit: https://github.com/advisories/GHSA-g7hc-96xr-gvvx.

This report contains proposed changes; it does not claim those changes have been implemented.
