using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Digital_Krushi_Officer.Services;

public record LanguageOption(string Name, string Code, string NativeName);
public static class SiteLanguage
{
    public const string CookieName = "DKO.Language";
    public static readonly LanguageOption[] Options = [new("English", "en", "English"), new("Hindi", "hi", "हिन्दी"), new("Gujarati", "gu", "ગુજરાતી"), new("Marathi", "mr", "मराठी")];
    public static bool Supported(string? name) => Options.Any(x => x.Name == name);
    public static string Normalize(string? name) => Options.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?.Name ?? "English";
    public static string Code(string? name) => Options.First(x => x.Name == Normalize(name)).Code;
    public static string Current(HttpContext context) => Normalize(context.Items[CookieName]?.ToString() ?? context.User.FindFirstValue("Language") ?? context.Request.Cookies[CookieName]);
    public static async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var language = Normalize(context.User.Identity?.IsAuthenticated == true ? context.User.FindFirstValue("Language") : context.Request.Cookies[CookieName]);
        context.Items[CookieName] = language;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(Code(language) + "-IN");
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        context.Response.Headers.ContentLanguage = Code(language);
        await next(context);
    }
}

// Only application-authored strings are translated. User content is never looked up or sent to a translation provider.
public class TranslationCatalog
{
    private static readonly Dictionary<string, Dictionary<string, string>> entries = Load();
    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        using var stream = typeof(TranslationCatalog).Assembly.GetManifestResourceStream("Digital_Krushi_Officer.Resources.ui.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)!.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.First().Key, x => x.First().Value, StringComparer.OrdinalIgnoreCase);
    }
    public IReadOnlyDictionary<string, string> For(string language) => SiteLanguage.Normalize(language) == "English" ? new Dictionary<string,string>() : entries.ToDictionary(x => x.Key, x => x.Value.GetValueOrDefault(SiteLanguage.Code(language), x.Key));
    public string Text(string? value, string language)
        => Translate(value, language);
    public static string Translate(string? value, string language)
    {
        if (string.IsNullOrEmpty(value) || SiteLanguage.Normalize(language) == "English") return value ?? "";
        var key = Regex.Replace(value.Trim(), @"\s+", " ");
        if (entries.TryGetValue(key, out var translations)) return translations.GetValueOrDefault(SiteLanguage.Code(language), value);
        var withoutIcon = Regex.Replace(key, @"^[^\p{L}\p{N}]+", "");
        if (entries.TryGetValue(withoutIcon, out translations)) return key[..(key.Length-withoutIcon.Length)] + translations.GetValueOrDefault(SiteLanguage.Code(language), withoutIcon);
        var match = Regex.Match(key, @"^(?<prefix>[^\p{L}\p{N}]*)(?<text>.*?[\p{L}\p{N}])(?<suffix>[^\p{L}\p{N}]*)$");
        if (match.Success && entries.TryGetValue(match.Groups["text"].Value, out translations))
            return match.Groups["prefix"].Value + translations.GetValueOrDefault(SiteLanguage.Code(language), match.Groups["text"].Value) + match.Groups["suffix"].Value;
        return value;
    }
}
public class UiText(IHttpContextAccessor accessor, TranslationCatalog catalog)
{
    public string Language => accessor.HttpContext == null ? "English" : SiteLanguage.Current(accessor.HttpContext);
    public string Code => SiteLanguage.Code(Language);
    public string Text(string? value) => catalog.Text(value, Language);
    public IReadOnlyDictionary<string, string> ClientStrings => catalog.For(Language);
}
