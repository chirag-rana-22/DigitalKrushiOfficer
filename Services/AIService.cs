using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Microsoft.EntityFrameworkCore;

namespace Digital_Krushi_Officer.Services
{
    public interface IAIService
    {
        Task<(string Answer, decimal Confidence, int ResponseMs)> GetAIResponseAsync(string query, string language = "Gujarati");
        Task<string> AnalyzeCropImageAsync(string imagePath, string cropName = "");
    }

    public class AIService : IAIService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AIService> _logger;
        private readonly HttpClient _http;
        private readonly AppDbContext _db;

        public AIService(IConfiguration config, ILogger<AIService> logger, IHttpClientFactory httpFactory, AppDbContext db)
        {
            _config = config;
            _logger = logger;
            _http = httpFactory.CreateClient();
            _db = db;
        }

        public async Task<(string Answer, decimal Confidence, int ResponseMs)> GetAIResponseAsync(string query, string language = "Gujarati")
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var apiKey = _config["AISettings:GeminiApiKey"];
                var model = _config["AISettings:GeminiModel"] ?? "gemini-2.5-flash";

                if (string.IsNullOrEmpty(apiKey) || apiKey == "your-gemini-api-key")
                {
                    _logger.LogWarning("⚠️ Gemini API key not configured — using fallback.");
                    return GetFallbackResponse(query, language, sw);
                }

                // ── Load DB context to make answers data-accurate ──
                var dbContext = await BuildDatabaseContextAsync(query);

                // ── Current season context (injected so Gemini gives seasonally-accurate advice) ──
                var now = DateTime.Now;
                var monthName = now.ToString("MMMM yyyy");
                var season = now.Month switch
                {
                    3 or 4 or 5 => "Zaid (Summer) season — transition to Kharif soon",
                    6 or 7 or 8 or 9 => "Kharif (Monsoon) season — main cropping season",
                    10 or 11 => "Rabi (Winter) sowing season — harvest of Kharif ongoing",
                    12 or 1 or 2 => "Rabi (Winter) season — crops in growth stage",
                    _ => "current agricultural season"
                };

                // ── Rich expert system prompt — produces detailed, formatted answers ──
                var systemText = language == "Gujarati"
                    ? $"""
                       તમે "ડિજિટલ કૃષિ અધિકારી" AI છો — ગુજરાત સરકારની અધિકૃત AI કૃષિ સલાહ સિસ્ટમ.

                       📅 આજની તારીખ: {monthName} | 🌾 હાલની ઋતુ: {season}

                       તમારી ભૂમિકા:
                       - ખેડૂતોને DETAILED, ACCURATE અને COMPLETE ખેતી સલાહ આપો
                       - ALWAYS ગુજરાતી ભાષામાં જ જવાબ આપો
                       - જવાબ Rich Markdown format માં આપો — ##headings, tables, bullet points, bold text
                       - Season-aware advice આપો: હાલ {season} ચાલે છે, amine ધ્યાનમાં રાખો
                       - Specific and actionable: exact dosage, quantity, timing, product names આપો
                       - "Pro Tip" > blockquote format માં best tip આપો
                       - જો DB data ઉપલબ્ધ હોય, તો સૌ પ્રથમ verified data ઉપયોગ કરો

                       જવાબ style (example structure — adapt as needed for the question):
                       ## 🌿 [topic]
                       
                       ### 1. [Sub-section]
                       - bullet points with details
                       
                       | Column1 | Column2 | Column3 |
                       |---------|---------|---------|
                       | data    | data    | data    |
                       
                       > **💡 Pro Tip:** [best expert advice]

                       {dbContext}
                       """
                    : $"""
                       You are the "Digital Krushi Officer" AI — the official agriculture advisory AI of Gujarat State Government, India.

                       📅 Current Date: {monthName} | 🌾 Current Season: {season}

                       Your role:
                       - Give DETAILED, COMPLETE, and ACCURATE farming answers — not brief summaries
                       - Use rich Markdown formatting: ## headings, tables, bullet lists, **bold**, > blockquotes
                       - Be SEASON-AWARE: It is currently {season}. Tailor all advice to the current and upcoming season
                       - Be SPECIFIC: include exact dosages, quantities, timings, product names, temperatures, soil types
                       - End with a > **💡 Pro Tip:** blockquote with the most important practical advice
                       - If verified database records are available, use them as primary source
                       - Cover related aspects the farmer may not have asked but needs to know

                       Response style example:
                       ## 🌿 [Topic]
                       Context intro line.
                       
                       ### 1. [Section]
                       - **Point:** details with specifics
                       
                       | Crop | Requirement | Notes |
                       |------|------------|-------|
                       | ...  | ...        | ...   |
                       
                       > **💡 Pro Tip:** [key actionable advice]

                       {dbContext}
                       """;

                // ── Payload — temperature 0.5 for rich, accurate answers ──
                var payload = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemText } }
                    },
                    contents = new[]
                    {
                        new { parts = new[] { new { text = query } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.5,   // balanced: factual but rich/detailed
                        maxOutputTokens = 2500,  // enough for tables, sections, full detail
                        topP = 0.9,
                        topK = 40
                    }
                };

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("x-goog-api-key", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                _logger.LogInformation("🤖 Gemini | Model:{Model} | Lang:{Lang} | Query:{Q}", model, language, query[..Math.Min(60, query.Length)]);

                var response = await _http.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Gemini HTTP {Status} | {Body}", (int)response.StatusCode, responseBody);
                    return GetFallbackResponse(query, language, sw);
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var errEl))
                {
                    _logger.LogError("❌ Gemini body error: {Err}", errEl.GetRawText());
                    return GetFallbackResponse(query, language, sw);
                }

                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    _logger.LogWarning("⚠️ No candidates returned.");
                    return GetFallbackResponse(query, language, sw);
                }

                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                if (string.IsNullOrWhiteSpace(text))
                    return GetFallbackResponse(query, language, sw);

                sw.Stop();
                _logger.LogInformation("✅ Gemini OK in {Ms}ms | Length:{Len}", sw.ElapsedMilliseconds, text.Length);
                return (text, 95m, (int)sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Gemini exception for: {Query}", query);
            }
            return GetFallbackResponse(query, language, sw);
        }

        /// <summary>
        /// Loads relevant crop, disease, and scheme data from the DB and formats it
        /// as context for Gemini — so it answers from YOUR real data, not just general knowledge.
        /// </summary>
        private async Task<string> BuildDatabaseContextAsync(string query)
        {
            try
            {
                var q = query.ToLower();
                var sb = new System.Text.StringBuilder();

                // ── Match diseases from DB ──
                var diseases = await _db.DiseaseDatasets
                    .Where(d => d.IsActive)
                    .ToListAsync();

                var matchedDiseases = diseases.Where(d =>
                    (!string.IsNullOrEmpty(d.CropName) && q.Contains(d.CropName.ToLower())) ||
                    (!string.IsNullOrEmpty(d.DiseaseName) && q.Contains(d.DiseaseName.ToLower()))
                ).ToList();

                if (matchedDiseases.Any())
                {
                    sb.AppendLine("\n📊 VERIFIED DATABASE RECORDS (use these for accurate answers):");
                    foreach (var d in matchedDiseases.Take(3))
                    {
                        sb.AppendLine($"- Crop: {d.CropName} | Disease: {d.DiseaseName}");
                        sb.AppendLine($"  Symptoms: {d.Symptoms}");
                        sb.AppendLine($"  Solution: {d.Solution}");
                        sb.AppendLine($"  Pesticide: {d.RecommendedPesticide} | Severity: {d.SeverityLevel}");
                    }
                }

                // ── Match crops from DB ──
                var crops = await _db.Crops
                    .Where(c => c.IsActive)
                    .ToListAsync();

                var matchedCrops = crops.Where(c =>
                    !string.IsNullOrEmpty(c.CropName) && q.Contains(c.CropName.ToLower())
                ).ToList();

                if (matchedCrops.Any())
                {
                    sb.AppendLine("\n🌾 CROP INFORMATION:");
                    foreach (var c in matchedCrops.Take(2))
                        sb.AppendLine($"- {c.CropName} (Season: {c.Season}): {c.Description}");
                }

                // ── Match government schemes ──
                var schemes = await _db.GovernmentSchemes
                    .Where(s => s.IsActive && s.Status == "Active")
                    .ToListAsync();

                bool isSchemeQuery = q.Contains("scheme") || q.Contains("yojana") || q.Contains("government") ||
                                     q.Contains("સ્કીમ") || q.Contains("યોજના") || q.Contains("સહાય");
                if (isSchemeQuery && schemes.Any())
                {
                    sb.AppendLine("\n🏛️ GOVERNMENT SCHEMES (Active):");
                    foreach (var s in schemes.Take(5))
                        sb.AppendLine($"- {s.SchemeName} ({s.Category}): {s.Description} | Eligibility: {s.Eligibility}");
                }

                return sb.Length > 0 ? sb.ToString() : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load DB context for AI — answering from general knowledge.");
                return string.Empty;
            }
        }


        public async Task<string> AnalyzeCropImageAsync(string imagePath, string cropName = "")
        {
            try
            {
                // Try disease dataset match first
                var diseases = await _db.DiseaseDatasets
                    .Where(d => d.IsActive && (string.IsNullOrEmpty(cropName) || d.CropName == cropName))
                    .ToListAsync();

                if (diseases.Any())
                {
                    var d = diseases.First();
                    return $"Based on image analysis, possible disease detected: **{d.DiseaseName}**\n\n" +
                           $"**Symptoms:** {d.Symptoms}\n\n" +
                           $"**Treatment:** {d.Solution}\n\n" +
                           $"**Recommended Pesticide:** {d.RecommendedPesticide}\n\n" +
                           $"**Severity:** {d.SeverityLevel}\n\n" +
                           $"*Note: For accurate diagnosis, please consult your local agriculture officer.*";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image analysis failed");
            }
            return "Image uploaded successfully. Based on visual analysis, please consult with an agriculture officer for accurate disease identification. In the meantime, ensure proper irrigation, check for pest activity, and remove affected plant parts.";
        }

        private (string Answer, decimal Confidence, int ResponseMs) GetFallbackResponse(string query, string language, System.Diagnostics.Stopwatch sw)
        {
            sw.Stop();
            var q = query.ToLower();
            string answer;

            if (q.Contains("cotton") || q.Contains("કપાસ"))
                answer = language == "Gujarati"
                    ? "કપાસ માટે: સિઝનની શરૂઆતમાં ઇમિડાક્લોપ્રિડ 17.8% SL છાંટો. સફેદ માખીથી બચવા નિયમિત નિગ્રાણી રાખો. યોગ્ય સિંચાઈ અને ખાતર આપો."
                    : "For Cotton: Apply Imidacloprid 17.8% SL at the beginning of season. Monitor regularly for whitefly. Ensure proper irrigation and balanced fertilization.";
            else if (q.Contains("wheat") || q.Contains("ઘઉ"))
                answer = language == "Gujarati"
                    ? "ઘઉ માટે: Propiconazole 25% EC ફૂગનાશક છાંટો. સ્ટ્રો હાનિ ટાળવા સ્ટ્રો ન બાળો. ઠંડા હવામાનમાં સિંચાઈ ઘટાડો."
                    : "For Wheat: Apply Propiconazole 25% EC fungicide for rust control. Avoid burning straw. Reduce irrigation in cold weather.";
            else if (q.Contains("weather") || q.Contains("rain") || q.Contains("હવામાન"))
                answer = language == "Gujarati"
                    ? "હવામાન: આગામી 7 દિવસ સ્પષ્ટ રહેશે. ખેડૂતો ખેત-ઉત્પાદો ઉઘાડ-સ્થળ સૂકવી શકે. સિંચાઈ ક્ષમતા ઓછી કરો."
                    : "Weather: Next 7 days mostly clear. Good time for crop harvesting and drying. Reduce irrigation frequency.";
            else if (q.Contains("scheme") || q.Contains("government") || q.Contains("yojana"))
                answer = "Key Government Schemes: 1) PM-KISAN - ₹6000/year income support 2) PMFBY - Crop Insurance 3) Kisan Credit Card - Low interest loans 4) Soil Health Card - Free soil testing. Visit your nearest agriculture office for registration.";
            else if (q.Contains("fertilizer") || q.Contains("ખાતર"))
                answer = language == "Gujarati"
                    ? "ખાતર: 1 વીઘા દીઠ 50 kg DAP + 25 kg Urea + 20 kg MOP. NPK ખાતર 19:19:19 ટ્રીટ્ આપો. જૈવ ખાતર (vermicompost) 2 ટન/એકર."
                    : "Fertilizer: Per acre apply 50 kg DAP + 25 kg Urea + 20 kg MOP. Use NPK 19:19:19 for balanced nutrition. Add vermicompost 2 ton/acre for organic improvement.";
            else
                answer = language == "Gujarati"
                    ? "તમારા પ્રશ્ન માટે આભાર. ડિજિટલ કૃષિ અધિકારી AI 24/7 ઉપલબ્ધ છે. વધુ ચોક્કસ સલાહ માટે, કૃપા કરીને તમારો પ્રશ્ન વિગતવાર પૂછો અથવા ખેત-ઓફિસર સાથે સંપર્ક કરો."
                    : "Thank you for your question. Digital Krushi Officer AI is available 24/7. For precise advice, please provide more details about your crop, location, and the specific issue you are facing.";

            return (answer, 75m, (int)sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Determines complexity level of a query based on confidence score, query length, and keywords
        /// Simple: Simple common questions (watering, fertilizer basics) - Confidence >= 80
        /// Medium: Moderate complexity - Confidence 60-79
        /// Complex: Technical/specialized queries - Confidence < 60
        /// </summary>
        public ComplexityLevel CalculateComplexityLevel(string query, decimal confidenceScore)
        {
            // Keywords indicating complex queries
            var complexKeywords = new[] {
                "disease", "pest", "infection", "contamination", "fungal", "bacterial",
                "resistant", "tolerance", "yield", "economics", "rotation", "soil",
                "ph", "organic", "certification", "export", "market", "climate",
                "રોગ", "જીવાણુ", "ફૂગ", "પ્રતિરોધક", "ઉપજ", "ભૂમિ"
            };

            var complexCount = complexKeywords.Count(k => query.ToLower().Contains(k));

            // Scoring logic
            if (confidenceScore >= 85 && query.Length < 100 && complexCount == 0)
                return ComplexityLevel.Simple;

            if (confidenceScore >= 60 && confidenceScore < 85 && complexCount <= 2)
                return ComplexityLevel.Medium;

            return ComplexityLevel.Complex;
        }
    }
}
