# Free demo deployment: Digital Krushi Officer

Prepared 11 October 2026. This is a deployment runbook, not confirmation of a live deployment.
Local MariaDB, SMTP and disk uploads remain the defaults. Cloud integrations are opt-in.

## Current state

Local verification completed: 117 automated tests passed (fake external providers), Release publish succeeded, private deployment secrets were excluded from Git/publish, and the updated app returned HTTP 200 for `/health/live` and `/Account/Login`. A fresh local SQL backup was saved before applying the additive ProtectionKeys migration. No existing application records were deleted. The full test suite includes cloud photo authorization and failed-upload cleanup regressions. At initial preparation no cloud account or public website had been created; see the Aiven checkpoint below for subsequent progress. Docker is not installed locally; a Linux image build/startup check was added to GitHub CI but has not yet run there.

- Dockerfile targets .NET 10, runs as a non-root user, listens on port 8080.
- render.yaml explicitly selects the free plan and disables automatic deployment.
- MySQL/MariaDB version is configurable; MariaDB-only conditional column SQL was replaced with metadata-based DDL.
- Supabase private storage supports new uploads, authenticated downloads, and failed-batch cleanup.
- Existing query ownership/officer access checks execute before fetching cloud photos.
- Disease reference photos have a public application route backed by the private bucket.
- Brevo HTTPS transactional email can replace SMTP on hosts that block SMTP.
- Data Protection keys can persist in the application database, encrypted with a separate deployment certificate.
- Media export compares bytes after upload, skips identical objects, and refuses differing existing objects.
- Cloud accounts, live provider credentials, Linux container execution, MySQL cloud restore and public end-to-end tests are still required.

## 1. Owner accounts (no payment method)

1. Create a free account at https://console.aiven.io/ and verify your email. Create a MySQL **Free** service, not a paid trial. Record the actual server version, host, port and database. Download its CA certificate to `App_Data/cloud/aiven-ca.pem`.
2. Create a free Supabase account at https://supabase.com/dashboard. Create a Free project and a **private** Storage bucket called `dko-media`. Keep public access disabled. Use its project URL and server-only legacy `service_role` JWT key (this adapter sends Bearer authentication). Never use this key in browser JavaScript or a public policy.
3. Create a Brevo Free account at https://app.brevo.com/. Complete its account/sender verification and enable transactional email. Create an API key, not an SMTP password. A free-address sender may be rewritten by Brevo; verify delivery to your own mailbox before enabling public registration. Do not buy a domain for this demo unless you later choose to.
4. Create a Render account at https://dashboard.render.com/ and connect only the intended GitHub repository. Do not deploy before steps 2–5 below are complete. Free service eligibility may require provider-specific account checks; stop if only a paid option is offered.

Account login, CAPTCHA and email codes are owner steps. No passwords or API keys need to be pasted into chat.

## 2. Prepare private settings

Run once in PowerShell 7 from the project folder:

```powershell
./scripts/Prepare-CloudSecrets.ps1
```

This creates `App_Data/cloud/deployment-secrets.json`, ignored by Git, with a generated certificate and random password. Keep an encrypted/offline backup of this file. Do not regenerate it on each deployment: old authentication keys depend on it. Plan certificate rotation before its five-year expiry.

Fill the blank values there from the provider dashboards. The file uses Render-style environment names with double underscores.

Connection string example (replace placeholders privately):

```text
Server=HOST;Port=PORT;Database=DigitalKrushiOfficer;User=APP_USER;Password=PASSWORD;SslMode=VerifyFull;SslCa=App_Data/cloud/aiven-ca.pem;MaximumPoolSize=10;
```

Use the local CA path for migration commands. In Render upload the CA as a secret file named `aiven-ca.pem` and use `SslCa=/etc/secrets/aiven-ca.pem` instead. Use the actual MySQL server version; do not leave the local MariaDB version selected.

Use a migration account for schema changes and a dedicated runtime account for application tables. Runtime needs SELECT/INSERT/UPDATE/DELETE, including ProtectionKeys. Keep `Database__ApplyMigrations=false` on the website.

## 3. Back up and copy the database

1. Stop application writes during the final copy. Keep the original XAMPP database intact.
2. Take a fresh SQL dump of **DigitalKrushiOfficer only**, including table definitions, data and `__EFMigrationsHistory`. Keep it under ignored `App_Data/backups`; also back up `App_Data/uploads` and disease/legacy media. Never export/import the XAMPP `mysql` system database, permission tables or root users.
3. Restore into a new empty cloud database with a TLS-verifying MySQL client. MariaDB dumps need compatibility review (collations, comments, SQL modes, definers). Do not paste the dump into a public website. Preserve table-name casing, keys, indexes, IDs and migration history. Do not run initial EF seeding first and then blindly import duplicate rows.
4. Check source/target row counts, foreign keys, password hashes and representative farmer/officer conversations. Resolve import errors before proceeding. A cloud endpoint is required to verify this step; it has not yet run.
5. Build Release, then apply only pending migrations to the restored cloud database:

```powershell
dotnet build "Digital Krushi Officer.csproj" -c Release
./scripts/Invoke-CloudCommand.ps1 -Action MigrateDatabase
```

The command uses the private cloud connection and exits without starting the website. It restores process environment variables afterward. Review migration SQL first with `dotnet ef migrations script --configuration Release` and save output privately if it includes seeded account data.

6. Replace the demo administrator password through the application before export, or securely update the restored staging account. Production refuses the demo password. Do not publish an admin password in Git, scripts or this guide.

## 4. Copy existing photos

From the source computer, while local configuration still points to XAMPP:

```powershell
./scripts/Invoke-CloudCommand.ps1 -Action CopyMedia
```

This reads the local DB and local files, overrides only Storage settings, and copies referenced media to `queries/` and `diseases/` inside the private bucket. No local row or file is deleted. Missing/oversized source files stop the preflight. Interrupted runs can be rerun: identical existing objects are verified and skipped. Conflicting objects are not overwritten. Source photos must be at most 5 MB each; review larger legacy files before export.

Keep DB and photo exports at the same write-free checkpoint. New local queries after the copy will not automatically appear in cloud.

## 5. Deploy the website

1. Review and push the release source to the intended GitHub repository; never add App_Data, appsettings.Local.json or credentials. Docker build context excludes local secrets, tests, SQL dumps and runtime media.
2. Render: New → Blueprint → select repository → review `render.yaml`. Confirm **Free**. Alternatively create a Docker Web Service manually with the same environment values.
3. Enter private configuration fields. Set `AllowedHosts` to the exact Render hostname. Add Gemini, weather and market API keys as environment secrets. No billing upgrade is necessary for this demo.
4. Set the verified ingress proxy network CIDR in `Hosting__TrustedProxyNetworks__0` (additional networks use __1, __2). Confirm the actual ingress topology during setup; do not use Render outbound IPs, `0.0.0.0/0`, or an arbitrary guessed range. The app trusts only configured forwarding proxies. Check HTTPS redirects and client IP rate limiting before opening registration.
5. Set both Data Protection certificate values from the generated private file and `DataProtection__PersistToDatabase=true`. Set Storage=Supabase and Email=Brevo. Production guards refuse missing cloud persistence/email/TLS settings.
6. Set the cloud connection's CA path to `/etc/secrets/aiven-ca.pem`. Keep the original local connection unchanged on your computer.
7. Deploy the Docker image. `/health/live` checks only the running process; it does not certify DB, storage, email or AI readiness.

## 6. Verify before sharing

- Login/logout and correct role access; old sessions still work after restart with database-persisted encrypted keys.
- Registration and password-reset codes reach the owner's mailbox; wrong/expired codes stay rejected.
- Short text, Gujarati/Hindi follow-ups, live price-source failures, clear/unclear multi-photo queries and appropriate officer escalation.
- Images remain available after redeploy; another farmer/unassigned officer cannot fetch private photos.
- Officer reply/reject and farmer history display correctly.
- XAMPP and the local app can be stopped while the cloud site continues operating.
- Compare the restored DB and uploaded files; retain source backup until a separate restore is verified.

## Free-tier constraints (verified against official documentation)

- Render: sleeps after 15 idle minutes, ephemeral local disk, SMTP ports blocked, no free persistent disk. Without a payment method, excess included usage suspends services/builds instead of billing. https://render.com/docs/free
- Aiven Free MySQL: 1 GB storage, no card required, no time-limited trial; inactive services may be powered off. https://aiven.io/docs/products/mysql/concepts/mysql-free-tier
- Supabase Free: 1 GB object storage, inactive projects may pause. https://supabase.com/pricing
- Brevo Free: 300 sends/day; sender verification and account approval apply. https://help.brevo.com/hc/en-us/articles/208589409-About-Brevo-s-pricing-plans
- Brevo free-address sender rewriting: https://help.brevo.com/hc/en-us/articles/14925263522578-Comply-with-Gmail-Yahoo-and-Microsoft-s-requirements-for-email-senders
- Gemini project quotas still apply after website deployment. Hosting changes do not fix a 429 or create unlimited AI access. https://ai.google.dev/gemini-api/docs/rate-limits

This setup is a limited demo, not an always-on commercial service guarantee. Live cloud validation remains pending until owner accounts and credentials are available.

## Aiven checkpoint — 11 October 2026

- Existing service mysql-39bdd3b5 was changed from trial-funded Developer to Free-1-1gb; dashboard confirmed Running in India (blr). No paid upgrade or payment method was added.
- MySQL 8.4.11 connected using hostname/certificate-verified TLS.
- Fresh source backup: App_Data/backups/aiven-source-20261011-165737.sql.
- Created a new DigitalKrushiOfficer cloud database. Restored all 18 tables and verified all 101 source rows with content hashes. Windows lowercase table names were converted to the exact EF model names for Linux/MySQL compatibility. Verification report: App_Data/cloud/migration-verification.json (private).
- With explicit owner approval, created a dedicated SSL-required runtime user with SELECT/INSERT/UPDATE/DELETE on DigitalKrushiOfficer only, host %. Restrict network access further once hosting egress addresses are available. Credentials are in ignored App_Data/cloud settings.
- Kept the migration/admin connection separately in Migration__ConnectionString; it must never become the Render runtime connection.
- Actual .NET cloud smoke test started on localhost:5176 with the restricted runtime user and database-persisted encrypted Data Protection keys. Account/Login returned 200 with an antiforgery token. This added a legitimate ProtectionKeys record after the initial exact-copy verification. No farmer queries were added by this smoke test.
- Cloud EF migration check reports no pending migrations. Local appsettings.Local.json still points to XAMPP.
- Supabase account login, photo transfer, Brevo setup, Render deployment, production admin password change and full public end-to-end verification remain pending. Subsequent local changes are not automatically synchronized to this cloud copy.

## Supabase checkpoint — 12 October 2026

- Project xqnjtgffyqnchbrhwiun is healthy in Mumbai. Created private dko-media bucket: 5 MiB per file, JPEG/PNG/WebP only, no public object policies.
- With owner approval, saved the existing server credential only in ignored App_Data/cloud/deployment-secrets.json. Dashboard retrieval of the new secret key did not succeed; the existing legacy service_role key works. Migrate to a modern secret key before the announced end-of-2026 legacy deprecation; do not publish either key.
- Copied and byte-verified both query photos referenced by the existing Aiven snapshot, using source files on this computer. Local XAMPP was not reachable during this run, so this verifies the cloud snapshot, not subsequent local database changes. Source rows and photos were not modified or removed.
- Verified both objects reject unauthenticated public downloads. Fixed Supabase HTTP 400 / NoSuchKey handling without hiding other provider failures. Release regression suite: 120 passed, zero failures.
- Revoked PUBLIC/anon/authenticated execution of the dashboard-created rls_auto_enable event-trigger function. Automatic RLS remains enabled; security advisor now reports no findings.
- Render currently shows its sign-in page. Render deployment, Brevo sender/account verification, admin password replacement, and public end-to-end tests remain pending. The website is not live yet.

## Render onboarding checkpoint — 12 October 2026

- Owner signed in to Rana's workspace. Render accepted the public source URL https://github.com/chirag-rana-22/DigitalKrushiOfficer and exposed Docker and $0/month compute options.
- Git provider connection subsequently appeared for chiragrana6109, whose listed repositories do not include this project. Do not substitute an unrelated repository. The public repository URL option is available.
- The configuration form reset when the Git connection completed; no service was submitted or deployed and no runtime secrets were sent to Render. Re-enter Docker, Free, Singapore, /health/live and manual deploy choices when prerequisites are complete.
- Brevo login page is open for the owner. Email credentials/sender verification, cloud administrator password replacement, trusted proxy configuration, and review/push of the uncommitted deployment code remain required before public startup.

## Brevo checkpoint — 12 October 2026

- Owner saved the normal Brevo API key in ignored deployment-secrets.json. Read-only account and sender API requests returned HTTP 200; transactional relay is enabled and the expected sender is active. Sender email has been configured privately.
- With explicit owner approval, sent exactly one setup-test email to the owner's verified address. Brevo accepted it (HTTP 201), then reported delivered at 15:45:26 IST. This confirms provider delivery, not inbox placement or an application registration/reset flow.
- Message ID and delivery evidence are kept under ignored App_Data/cloud/brevo-test-*.json. No credentials were printed or committed.
- Render has not been deployed. Source review/push, cloud administrator password replacement, verified proxy setup and public application end-to-end tests remain outstanding.

## Database restart checkpoint — 12 October 2026

- Cloud connection failed because the Aiven service was Powered off; its hostname did not resolve while another provider hostname did.
- Powered on the existing Free-1-1gb service without upgrading. Aiven confirmed power-on and reported Rebuilding; recovery was not yet complete at the last check.
- Stopped the failed loopback-only staging app. The ignored start-private-staging.ps1 script can start it again after database recovery, using cloud runtime credentials and Testing mode on 127.0.0.1:5176 only. Never use this Testing configuration for public Render deployment.
- Release publish and git diff --check succeeded. No Render service has been deployed. Resume by verifying Aiven Running/TLS connectivity, then let the owner replace the demo admin password through the private staging UI.
