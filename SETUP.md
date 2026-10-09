# Digital Krushi Officer — local setup

The application is ASP.NET Core MVC on .NET 10, with EF Core/Pomelo 9 and MariaDB (the MySQL service bundled with XAMPP). Apache does not serve the .NET app.

## Run this installation

1. Start XAMPP MySQL.
2. Open a terminal in this project directory.
3. Run `dotnet run --launch-profile http`.
4. Open http://localhost:5175.

The private `appsettings.Local.json` file contains this installation's database and provider credentials. It is ignored by Git and excluded from publishing. Keep it private. Environment variables override that file.

## Fresh checkout

Install the .NET 10 SDK and a compatible MariaDB server. Copy the safe settings sections from appsettings.json into an ignored appsettings.Local.json and fill in local credentials. Use a dedicated database account. Run `dotnet restore`, then `dotnet run --launch-profile http`. Development startup applies the existing migrations; do not generate another InitialCreate migration.

The historical migration seeds a development admin (`admin@digitalkrushi.com` / `Admin@123`). Change this password at Account → Change password before any production launch. Production startup rejects the demo password.

Do not import the legacy AutoCleanup SQL file over an existing database. Back up and test restores first.

## Roles and advisory workflow

- Farmers can register and verify an email code.
- Officers must verify email and then be activated by an administrator.
- Admin → Farmer assignments optionally sets a preferred officer for a farmer.
- Normal text, clear-photo and voice-transcript questions receive an immediate AI answer. These use AIAnswered status and do not create an officer task.
- Uncertain diagnosis, unclear images, complex cases and AI failures are escalated automatically. Prefer an existing active verified officer assignment; otherwise choose a district match, then lowest open workload and officer ID. Only when no active verified officer is available does the query remain Unassigned for admin intervention.
- The assigned officer can approve, mark for revision, edit and publish, or reply directly. Marking for revision does not pretend that AI automatically regenerates an answer.
- Query history supports text search, status filters and 50-record pages.

## External services

- Gemini receives text and, for photo questions, normalized JPEG image bytes. No fixed confidence percentage is displayed for new answers.
- OpenWeather returns the next district forecast interval; rain probability comes from `pop`, wind is converted from m/s to km/h. Data is cached for 15 minutes.
- SMTP delivers account verification/password reset messages. Code validity is 10 minutes, with five attempts and a one-minute resend cooldown.
- Missing/failed providers are reported honestly. No fabricated disease or weather response is substituted.
- Voice input uses browser speech recognition when available. Users review the transcript before sending. Raw audio is not retained.

## Tests and release output

```powershell
dotnet test Tests/Dko.Tests.csproj
dotnet publish "Digital Krushi Officer.csproj" -c Release -o artifacts/publish
dotnet list "Digital Krushi Officer.csproj" package --vulnerable --include-transitive
```

Tests use an isolated SQLite database and fake mail/AI/weather providers, without sending emails or modifying the user's database. The repository includes a GitHub Actions build/test workflow; it has not been executed on GitHub yet.

Private uploads and local backups live under App_Data and are excluded from Git and release output. The `/health/live` route is a liveness endpoint, not a database readiness assertion.
