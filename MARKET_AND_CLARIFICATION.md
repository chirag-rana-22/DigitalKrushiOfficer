# AI clarification and mandi-price integration — 10 October 2026

Implemented clarification-first photo handling. The model may ask up to two targeted questions/request clearer photos without creating an officer task. Clarification turns are saved as AwaitingDetails. After two clarification turns within recent context, continued uncertainty routes to an officer; clearly complex cases can route immediately. Ordinary supported answers remain direct. This routing still depends on model assessment and does not guarantee diagnostic accuracy.

Officer escalation now stores a separate farmer-visible PublicSummary of observations/uncertainty and safe next steps. The private diagnostic draft remains hidden from farmers. Assigned officers receive earlier conversation text and photos up to the assigned query; future messages and unrelated conversations are not included in this access. The PublicAdvisorySummary migration was applied locally.

Market price intent is extracted from the same conversation, including Hindi/Gujarati follow-ups. Price answers are assembled from provider records, not generated price numbers. The integration targets the official AGMARKNET data.gov.in resource:
https://www.data.gov.in/resource/current-daily-price-various-commodities-various-markets-mandi

It requests commodity/state/district/market filters; validates returned location/commodity, dates and min/modal/max ranges; shows variety, location, report date and INR/quintal. Old reports are explicitly labelled not today's confirmed price. Retail, MSP, historical and forecast prices are not inferred from wholesale reports. At most eight records from the newest date in the returned matching reports are displayed. Provider coverage and commodity naming can produce no matches; these produce a clear no-data response rather than invented prices.

Verified records are cached for five minutes. Provider requests time out after ten seconds. Empty/error responses are not cached. Market-client HTTP logging is disabled because the provider requires an API key in the URL query. All credentials stay server-side.

## Activation still needed

The user does not yet have a data.gov.in API key. Therefore live numeric prices are NOT enabled or live-validated. Missing-key behaviour was verified with the real chatbot. Add a personal authorized key to the ignored appsettings.Local.json:

```json
"MarketPrices": {
  "ApiKey": "YOUR_DATA_GOV_IN_KEY"
}
```

Merge this section into the existing JSON, preserving the database/Gemini settings. Restart the app after editing local configuration. Alternatively set MarketPrices__ApiKey in the deployment environment. Once configured, run a live lookup for a known commodity and mandi and compare it against the source, including date and units. No third-party credentials or scraped prices were used. Unauthenticated source probes did not provide usable access.

## Verification

- 91 automated tests passed, including all previous regressions. New checks cover valid/invalid/older price records, missing key/location, upstream failures, clarification limits, complex-case escalation, public summary separation, and officer access to earlier photos while denying future/unrelated photos.
- Real Gemini/browser checks: Hindi tomato-price question asks location; Rajkot/Gujarat follow-up returns honest source-unavailable message without escalation; unclear photo asks for details without escalation; difficult conflicting-report case escalates with a separate public summary.
- Observed response times for those four requests: 4.4, 3.4, 4.6 and 5.9 seconds. These are a small sample, not a latency guarantee; numeric price-provider latency was not measured without a key.
- Evidence: artifacts/market-clarification (ignored). Only QA IDs 88–91 and their one uploaded photo were deleted after checks; existing user records were preserved.
- Replies still display after validation of the complete structured result. Token-by-token streaming has not been implemented; private draft text is not exposed before routing validation. The existing immediate question/pending state remains.
- No deployment or GitHub push performed.
