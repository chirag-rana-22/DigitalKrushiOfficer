# Deployment handoff

For the current free Render + Aiven + Supabase + Brevo demo workflow, see [FREE_DEPLOYMENT.md](FREE_DEPLOYMENT.md). Cloud accounts and live validation are still required; configuration files alone do not constitute deployment.

This project has been hardened and tested locally. Do not interpret a successful build as certification that every future bug or deployment-specific issue has been eliminated.

## Required owner/deployment inputs

- Production domain, hosting target and support/operator identity.
- Fresh production database and backups; use a maintained MariaDB deployment, separate from the repaired development XAMPP instance.
- Rotate any Gemini/OpenWeather/SMTP credentials that were previously shared or committed. Moving values out of appsettings.json does not erase Git history or revoke provider credentials.
- A non-demo administrator password and a monitored administrator contact address.
- SMTP delivery verification to an address controlled by the operator. Automated tests never send live mail.
- Public privacy contact and retention/deletion schedule. The privacy page currently describes behavior and explicitly identifies these operator responsibilities.

## Production configuration

Use environment variables or the hosting provider's secret manager:

- `ConnectionStrings__DefaultConnection`
- `AISettings__GeminiApiKey`, `AISettings__GeminiModel`, `AISettings__OpenWeatherApiKey`
- `SmtpSettings__Host`, `SmtpSettings__Port`, `SmtpSettings__Username`, `SmtpSettings__Password`, `SmtpSettings__FromName`
- `AllowedHosts` set to the actual hostname(s), not `*`.
- `ASPNETCORE_ENVIRONMENT=Production`.

Production startup rejects database root/blank passwords, the default admin password, wildcard hosts and unapplied migrations. Review and apply migrations through a deployment account; the normal runtime account should only have the database operations it needs. Do not routinely enable `Database__ApplyMigrations` in production.

Terminate HTTPS correctly, configure trusted forwarded headers for the selected reverse proxy, and keep the database port private. Persist ASP.NET Data Protection keys and App_Data/uploads across restarts/releases. Back up both the database and private uploads, and test a restore on a separate instance. The current in-memory rate limits, session and weather cache target one application instance; scale-out needs shared rate limiting/cache and shared Data Protection keys.

## Release verification

1. Run the test suite and dependency audit.
2. Publish the csproj, not the solution, to artifacts/publish.
3. Confirm appsettings.Local.json, App_Data, backups and Tests are absent from the release package.
4. Restore a backup to staging; apply reviewed migrations.
5. Test registration/OTP/reset with a controlled mailbox, an actual representative crop photo, review/assignment, forecast failures, mobile layouts and password/session invalidation.
6. Confirm storage persistence, TLS, logs, monitoring, rollback and restore before announcing availability.

## Development database repair record — 8 October 2026

XAMPP's mysql.db and mysql.procs_priv permission tables were already corrupt. With explicit user approval, locked backup copies were made under App_Data/backups/mysql-permissions-20261008-142431. Online repair reported that mysql.db changed from three rows to zero. Those damaged permission entries were not recovered. Other local database accounts may need their database-specific grants restored; the project cannot infer those grants safely.

The server then crashed on GRANT/INSERT into the repaired table. Offline safe recovery did not resolve writes; rebuilding mysql.db using ALTER TABLE FORCE did. The normal GRANT command then succeeded. The app now uses dko_local_3fc414@localhost with SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX and REFERENCES limited to DigitalKrushiOfficer. Its random password is stored only in ignored local configuration. No application data table was removed by the permission-table repair.

A project database dump is saved privately at App_Data/backups/pre-hardening.sql. Treat that dump as sensitive. Original user edits to the legacy SQL file were retained.

## Product scope

This remains a single-organization advisory application. Billing/subscriptions, tenant isolation, commercial contracts and a complete translated UI are future product work, not implemented features. Advisory responses support Gujarati, Hindi and English; most interface labels are English. No unsupported usage/customer/satisfaction metrics remain on the landing page.
