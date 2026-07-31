# Digital Krushi Officer - Setup Commands
# Run these in order in the project folder terminal

# 1. Restore NuGet packages
dotnet restore

# 2. Create EF Core initial migration
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# 3. Apply migration to create database
dotnet ef database update

# 4. Run the application
dotnet run

# =========================================
# DEFAULT LOGIN CREDENTIALS
# =========================================
# Admin:
#   Email: admin@digitalkrushi.com
#   Password: Admin@123
#
# Register as Farmer or Officer via /Account/Register
# =========================================

# IMPORTANT: Before running, update appsettings.json:
# - ConnectionStrings:DefaultConnection -> your MySQL password
# - AISettings:GeminiApiKey -> your Gemini API key (optional)
# - AISettings:OpenWeatherApiKey -> your OpenWeather key (optional)
# - EmailSettings -> your SMTP credentials (optional)
