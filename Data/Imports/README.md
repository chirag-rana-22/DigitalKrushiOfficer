# Catalog import — 9 October 2026

## Applied to local DigitalKrushiOfficer database

- Crops: all eight records from the legacy SQL file already existed. ImportLegacyCrops.sql imports only missing names, case/whitespace-insensitively, and preserves existing records and IDs. Running it twice left eight master crops and did not alter farmer planting records.
- Schemes: refreshed the five named legacy entries with current descriptive information and official links, retaining their IDs. Removed the unsupported shared sample deadline of 31 March 2026. NULL LastDate means the application window must be checked on the official portal; it does not mean enrollment is open everywhere.
- No DROP, TRUNCATE or DELETE is used. The original destructive full-database SQL file was not executed.
- Backup of both catalog tables: App_Data/backups/catalog-before-20261009.sql (ignored private local file).

## Sources checked

| Entry | Official source | Notes |
|---|---|---|
| PM-KISAN | https://www.pmindia.gov.in/en/news_updates/cabinet-approves-continuation-of-the-pm-kisan-scheme-from-2026-27-to-2030-31-with-a-financial-outlay-of-rs-3-15-lakh-crore/?comment=disable and https://www.pib.gov.in/newsite/PrintRelease.aspx?lang=2&reg=48&relid=190752 | Eligible landholding families, exclusions and verification; links to https://pmkisan.gov.in/ for the official service. |
| Soil Health Card Scheme | https://www.nic.gov.in/project/soil-health-card-portal/ | Soil nutrient testing/card; official service https://soilhealth.dac.gov.in/. |
| Kisan Credit Card | https://www.myscheme.gov.in/schemes/kcc | Cultivation/allied credit; bank assessment applies. No fixed rate or loan approval is promised. |
| Crop Insurance (PMFBY) | https://secure.mygov.in/campaigns/pmfby/ and https://www.pmfby.gov.in/faq | Notified crop/area/season rules; directory entry does not confirm Gujarat enrollment. Official service https://pmfby.gov.in/home. |
| Agricultural Mechanization | https://agrimachinery.nic.in/ | Machinery assistance; state/component-specific allocations and windows must be checked. |

## Reuse

Select the intended application database, back up TblCrop and TblGovernmentScheme, then execute ImportLegacyCrops.sql or RefreshGovernmentSchemes.sql using a MariaDB client. Review official scheme information again before rerunning the scheme refresh in future: that script intentionally updates the five named entries, including descriptions, links and dates. It leaves unrelated entries alone.

The crop catalog is separate from My Crops. Farmers select one of the eight available crop types, then enter their own land area and dates. The import never invents planting records for farmers.
