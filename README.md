# Digital Krushi Officer

An AI-first agriculture assistant built with ASP.NET Core MVC. Farmers can ask questions, share crop and soil photos, manage their crops and reminders, check weather and mandi reports, and receive help from a Krushi Officer when a case needs specialist attention.

**Live website:** [digital-krushi-officer.onrender.com](https://digital-krushi-officer.onrender.com/)

[![.NET build and tests](https://github.com/chirag-rana-22/DigitalKrushiOfficer/actions/workflows/dotnet.yml/badge.svg)](https://github.com/chirag-rana-22/DigitalKrushiOfficer/actions/workflows/dotnet.yml)

> The live demo uses Render Free and may take longer to open after inactivity. External AI, email, weather and market services have their own availability and quotas. Feature and verification notes below were updated on 15 October 2026.

## How it works

1. A farmer registers, verifies their email and chooses a language.
2. The farmer asks Krushi AI a question, optionally selecting a crop or attaching photos.
3. Ordinary questions receive a direct AI answer. Missing details or unclear photos can prompt a clarification request.
4. Unresolved crop diagnosis and complex cases enter officer review. Available officers are selected using the farmer's preferred assignment, district and open workload; an administrator can handle unassigned review cases.
5. Farmers can continue conversations, read officer replies and rate the help they receive.

Uploading an image does not automatically assign the question to an officer. Technical AI failures are saved for retry instead of creating unnecessary officer tasks. AI assessments and photo observations can be uncertain; the application does not guarantee a diagnosis.

## Farmer features

| Area | Available functionality |
| --- | --- |
| Dashboard | Recent queries, query counts, growing crops, weather summary and due crop tasks. |
| AI chat | Conversation history, new conversations, follow-up questions, immediate question/pending display, and answers in the selected language. |
| Photo questions | Select multiple crop/soil photos, preview attachments, paste or drag images, and analyze them together. Up to 20 photos, 5 MB each and 25 MB combined. |
| Voice input | Browser-supported speech recognition with a transcript to review before sending. Availability depends on the browser and device. |
| Crop context | Start “Ask AI” from a saved crop. The conversation receives recorded crop/field details and recent completed tasks to reduce repeated questions. |
| My Crops | Choose from the expanded crop catalog or use Other for a private custom crop name; record area, field, variety, sowing and estimated harvest dates. Edit details and manage crop status. |
| Crop calendar | Preview and confirm suggested preparation, sowing, inspection and harvest-readiness tasks for the crop cycle. |
| Custom reminders | Add a title, due date and notes; complete, skip, postpone to tomorrow or reschedule pending tasks. |
| Today’s tasks | View due and overdue work across growing crops. |
| Harvest tracking | Record actual harvest and stop remaining reminders for that crop. Estimated harvest dates remain planning estimates. |
| Email reminders | Opt in separately for each crop to receive a daily summary of due tasks. Email is off by default. |
| Mandi prices | Ask by commodity, state and district/mandi. Replies use verified provider records with report date, market, source and units. See the current coverage notes below. |
| Weather | Search supported locations and view forecast information, temperature, humidity, rain probability and wind. |
| Disease library | Browse and search crop disease reference information and images. |
| Government schemes | Browse by category and read scheme details, eligibility and linked information. |
| Query history | Search previous questions, filter by status and browse paginated results. |
| Officer replies and feedback | Read expert replies and submit ratings or feedback. |
| Account | View/edit profile, choose language, change password and use email-based password recovery. |

Suggested calendar tasks are planning aids, not automatic irrigation, fertilizer or pesticide prescriptions. Farmers can adjust their dates and tasks to actual field conditions.

## Krushi Officer features

- Role-specific dashboard and assigned escalation list, with status filtering.
- Open an assigned case with relevant earlier conversation text, crop context and authorized photos.
- Review pending AI drafts; approve, edit and publish, or reject with a reason for revision.
- Reply directly and mark a case In Progress, Solved or Rejected, with a reply/reason required.
- Notify the farmer of an officer reply by email when the email provider is available.
- View and edit the officer profile and change password.

Officers verify their email and require administrator activation. Pending review is for cases that need expert help; ordinary AI answers do not wait for officer approval. Private diagnostic drafts remain separate from farmer-visible summaries.

## Administrator features

- Dashboard statistics and analytics for users, queries, escalations and feedback.
- Search farmers and activate/deactivate farmer and officer accounts.
- Assign or unassign a preferred officer for a farmer and inspect an officer's farmer assignments.
- Inspect queries, filter/search query records and manage the unassigned review queue.
- Assign review cases to officers.
- Add, edit and enable/disable crop catalog entries.
- Add, edit and enable/disable government schemes.
- Add, edit and enable/disable disease reference records, including reference image upload.
- Review farmer feedback and ratings.

## Languages

The website supports **English, Hindi, Gujarati and Marathi**. English is the default for new visitors and accounts. The language selector updates interface text and the account preference used for new AI answers and email templates.

Previously saved chat messages, custom crop/field names, custom reminders and officers' original replies keep their original text. Administrator-authored content without a catalog translation remains in its source language. This is a maintained four-language catalog, not automatic translation into every language.

## Mandi-price availability

The application tries the official daily **AGMARKNET dataset on data.gov.in** when a server-side key is configured. Valid daily records display minimum, modal and maximum wholesale prices in INR per quintal, along with the report date and location.

If the daily connection fails or has no usable report, the application can use the **public official AGMARKNET weekly report**. That report supplies a reported price rather than separate Min/Modal/Max fields. Older reports are explicitly labelled as older; numeric prices are not invented by the AI.

**Live verification on 15 October 2026:** the daily API worked in local verification, but Render logged a transport connection failure to data.gov.in. The live chatbot successfully returned the official weekly fallback for Onion / Surat APMC / Gujarat, dated 13 October 2026. Daily prices on Render are therefore not yet verified as working. Availability depends on provider connectivity and mandi submissions; today's prices are not guaranteed for every commodity or location.

## Reminders and notifications

Website reminders show due and overdue tasks whenever the farmer opens the relevant pages. Opt-in email summaries use an authenticated scheduler endpoint called by the repository's Crop reminder delivery workflow at approximately **09:07 and 10:07 India time**. A persisted farmer/date delivery claim prevents duplicate daily summaries from repeated calls.

Delivery depends on the farmer's opt-in, verified active account, growing crop, pending tasks, email-provider quota and scheduler availability. Free hosting/workflow delays mean exact delivery times are not guaranteed. Recording harvest stops later reminders for that crop. See [crop reminder setup](CROP_REMINDERS.md).

## Technology and hosting

| Component | Technology |
| --- | --- |
| Application | ASP.NET Core MVC, C#, .NET 10, Razor views |
| Interface | HTML, CSS, Bootstrap and JavaScript |
| Data access | Entity Framework Core 9 with Pomelo MySQL provider |
| Database | MariaDB for local XAMPP setup; MySQL supported for cloud deployment |
| AI | Google Gemini, configured server-side |
| Weather | OpenWeather |
| Market reports | data.gov.in / official AGMARKNET sources |
| Photo storage | Local private files or Supabase private Storage |
| Email | SMTP or Brevo HTTPS transactional email |
| Live deployment | Render Docker service, Aiven MySQL and Supabase Storage |
| Automation | GitHub Actions for tests, Docker startup checks and reminder delivery |

XAMPP's MySQL/MariaDB service supplies the local database. The .NET application runs through ASP.NET Core; Apache does not serve it.

## Local setup

### Prerequisites

- .NET 10 SDK.
- A compatible MySQL/MariaDB server; the local defaults target XAMPP MariaDB 10.4.32.
- Provider credentials for the integrations you want to enable.

### Run

```bash
git clone https://github.com/chirag-rana-22/DigitalKrushiOfficer.git
cd DigitalKrushiOfficer
dotnet restore "Digital Krushi Officer.sln"
```

Create an **ignored** `appsettings.Local.json` in the project root. Merge only the settings needed for your installation; a minimal example is:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=DigitalKrushiOfficer;User=YOUR_APP_USER;Password=YOUR_LOCAL_PASSWORD;CharSet=utf8mb4;"
  },
  "AISettings": {
    "GeminiApiKey": "YOUR_GEMINI_KEY",
    "OpenWeatherApiKey": "YOUR_OPENWEATHER_KEY"
  },
  "MarketPrices": {
    "OfficialSourceEnabled": true,
    "ApiKey": "YOUR_DATA_GOV_IN_KEY"
  }
}
```

Use a dedicated database account and configure email before testing registration or password recovery. Development startup applies existing migrations; do not create a second InitialCreate migration or import legacy cleanup SQL over an existing database. Review [local setup](SETUP.md) for bootstrap account instructions and change any seeded demo password before production.

Start the database service, then run:

```bash
dotnet run --launch-profile http
```

Open **[http://localhost:5175](http://localhost:5175)**. Environment variables override local configuration. For cloud deployment use private environment settings such as `MarketPrices__ApiKey`; never place real keys in this README, tracked configuration, frontend code or commits.

## Tests and deployment

```bash
dotnet test Tests/Dko.Tests.csproj --configuration Release
dotnet publish "Digital Krushi Officer.csproj" -c Release -o artifacts/publish
```

Automated tests use isolated test data and fake external services; they do not confirm live provider availability or inbox delivery. The .NET workflow also builds the Linux Docker image and checks container startup through `/health/live`. That endpoint reports application liveness, not database or provider readiness.

Deployment uses the included `Dockerfile` and `render.yaml`. Automatic deployment is disabled in the supplied Render configuration. Apply production migrations with a separate migration account, use a limited runtime database account and keep secrets in the host's private settings. Deployment guides contain dated checkpoints; use their latest checkpoints together with current provider dashboards.

## Security and privacy

- BCrypt password hashing, role-based authorization and email verification.
- Protected account recovery, request validation, antiforgery protection and advisory rate limiting.
- Farmer ownership checks and assigned-officer access checks for private queries and photos.
- Private photo storage with validation and normalization of uploaded images.
- Server-side provider credentials; private local settings, runtime data and generated artifacts excluded from Git.
- Optional database-persisted Data Protection keys with deployment-certificate encryption.
- AI service failures and missing provider data are reported without fabricating results.

## Project structure

```text
Controllers/          MVC actions and role-specific routes
Models/               Entities and view models
Views/                Razor pages and shared layouts
Services/             AI, market, weather, storage, email and crop workflows
Data/                 Database context, migrations and import resources
Resources/            Language catalogs
Tests/                Automated regression and integration tests
scripts/              Setup, migration and deployment helpers
wwwroot/              Styles, scripts and static assets
.github/workflows/    Build/test and reminder automation
```

## Documentation

- [Local setup](SETUP.md)
- [Deployment](DEPLOYMENT.md) and [free demo deployment runbook](FREE_DEPLOYMENT.md)
- [AI-first workflow](AI_FIRST_WORKFLOW.md)
- [Crop plans and reminders](CROP_REMINDERS.md)
- [Website languages](LOCALIZATION.md)
- [Market-price integration and verification history](MARKET_AND_CLARIFICATION.md)
- [Farmer/officer assignment](FARMER_OFFICER_ASSIGNMENT.md)
- [Chat workspace verification](CHAT_WORKSPACE_VERIFICATION.md)

## Project author

**Chirag Rana** — Sixth Semester .NET academic project.

Digital Krushi Officer is an independent agriculture assistance project. Scheme information links to external sources; it does not represent a government authority.
