using System.Globalization;
using System.Net;
using Digital_Krushi_Officer.Models;
namespace Digital_Krushi_Officer.Services;

public static class ReminderLanguage
{
    public static string Normalize(string? language) => SiteLanguage.Normalize(language);
    public static string Text(string? language, string english, string hindi, string gujarati) => Normalize(language) switch { "English" => english, "Hindi" => hindi, "Gujarati" => gujarati, _ => TranslationCatalog.Translate(english, "Marathi") };
    public static string Heading(string? language) => Text(language, "Today's farming tasks", "आज के खेती के काम", "આજના ખેતીના કામ");
    public static string Date(DateTime date, string? language)
    {
        string[] gu = ["જાન્યુઆરી", "ફેબ્રુઆરી", "માર્ચ", "એપ્રિલ", "મે", "જૂન", "જુલાઈ", "ઑગસ્ટ", "સપ્ટેમ્બર", "ઑક્ટોબર", "નવેમ્બર", "ડિસેમ્બર"];
        string[] hi = ["जनवरी", "फ़रवरी", "मार्च", "अप्रैल", "मई", "जून", "जुलाई", "अगस्त", "सितंबर", "अक्टूबर", "नवंबर", "दिसंबर"];
        return Normalize(language) switch { "Gujarati" => $"{date.Day} {gu[date.Month-1]} {date.Year}", "Hindi" => $"{date.Day} {hi[date.Month-1]} {date.Year}", "Marathi" => date.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("mr-IN")), _ => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) };
    }
    public static string TaskTitle(CropTask task, string? language) => task.Kind switch {
        "Preparation" => Text(language, "Review field preparation", "खेत की तैयारी की जाँच करें", "ખેતરની તૈયારી તપાસો"),
        "Sowing" => Text(language, "Confirm sowing", "बुवाई की पुष्टि करें", "વાવણીની પુષ્ટિ કરો"),
        "Inspection" => Text(language, "Weekly crop and soil check", "फसल और मिट्टी की साप्ताहिक जाँच", "પાક અને જમીનની સાપ્તાહિક તપાસ"),
        "HarvestPreparation" => Text(language, "Prepare for possible harvest", "संभावित कटाई की तैयारी करें", "સંભવિત કાપણીની તૈયારી કરો"),
        "HarvestCheck" => Text(language, "Check readiness for harvest", "कटाई के लिए फसल तैयार है या नहीं, जाँचें", "કાપણી માટે પાક તૈયાર છે કે નહીં તે તપાસો"),
        _ => task.Title // Farmer-written text is kept exactly as entered; it is not translated or sent to an AI provider.
    };
    public static string TaskNotes(CropTask task, string? language) => Normalize(language) == "Marathi" && task.Kind != "Custom" ? TranslationCatalog.Translate(task.Notes, "Marathi") : task.Kind switch {
        "Preparation" => Text(language, task.Notes ?? "", "खेत, बुवाई की सामग्री और मिट्टी जाँच की सलाह की समीक्षा करें। तारीख अपनी वास्तविक योजना के अनुसार बदलें।", "ખેતર, વાવણીની સામગ્રી અને જમીનના પરીક્ષણની સલાહ તપાસો. તમારી વાસ્તવિક યોજના મુજબ તારીખ બદલો."),
        "Sowing" => Text(language, task.Notes ?? "", "बुवाई हुई या नहीं, दर्ज करें। योजना बदली हो तो फसल की तारीखें बदलें।", "વાવણી થઈ છે કે નહીં તે નોંધો. વાવણીની યોજના બદલાઈ હોય તો પાકની તારીખો બદલો."),
        "Inspection" => Text(language, task.Notes ?? "", "पौधों की बढ़वार, पत्तों में बदलाव और मिट्टी की नमी जाँचें। समस्या दिखे तो AI से पूछें। यह सिंचाई या रसायन डालने का निर्देश नहीं है।", "છોડનો વિકાસ, પાનમાં ફેરફાર અને જમીનની ભેજ તપાસો. સમસ્યા દેખાય તો AIને પૂછો. આ પાણી કે રસાયણ આપવાનો નિર્દેશ નથી."),
        "HarvestPreparation" => Text(language, task.Notes ?? "", "फसल की परिपक्वता जाँचें और जरूरत हो तो मजदूर, उपकरण और भंडारण की तैयारी करें। तारीख अनुमानित है।", "પાકની પરિપક્વતા તપાસો અને જરૂર હોય તો મજૂરો, સાધનો તથા સંગ્રહની તૈયારી કરો. તારીખ અંદાજિત છે."),
        "HarvestCheck" => Text(language, task.Notes ?? "", "फसल तैयार है? वास्तविक परिपक्वता जाँचकर ही कटाई की पुष्टि करें; अन्यथा काम और अनुमानित तारीख आगे करें।", "પાક તૈયાર છે? વાસ્તવિક પરિપક્વતા તપાસ્યા પછી જ કાપણીની પુષ્ટિ કરો; નહીં તો કામ અને અંદાજિત તારીખ આગળ કરો."),
        _ => task.Notes ?? ""
    };
    public static string Status(string status, string? language) => status switch {
        "Pending" => Text(language, status, "बाकी", "બાકી"), "Done" => Text(language, status, "पूरा", "પૂર્ણ"),
        "Skipped" => Text(language, status, "छोड़ा गया", "છોડી દીધું"), "Cancelled" => Text(language, status, "रद्द", "રદ"), _ => status
    };
    public static string CropName(TblFarmerCrop crop, string? language)
    {
        return crop.CustomCropName != null ? crop.DisplayName : TranslationCatalog.Translate(crop.DisplayName, Normalize(language));
    }

    public static string Email(TblUser farmer, IReadOnlyList<CropTask> tasks, DateTime today, Uri publicUrl)
    {
        var language = farmer.PreferredLanguage;
        string T(string en, string hi, string gu) => Text(language, en, hi, gu);
        string E(string? text) => WebUtility.HtmlEncode(text ?? "");
        var date = Date(today, language);
        var introduction = string.Format(TranslationCatalog.Translate("These are your due or overdue planned tasks for {0} (India time).", language), date);
        var list = string.Join("", tasks.Select(task => $"<li><b>{E(CropName(task.FarmerCrop!, language))} — {E(task.FarmerCrop!.FieldName)}</b>: {E(TaskTitle(task, language))} ({E(Date(task.DueDate, language))})</li>"));
        var caution = T("Check actual field conditions before acting. Harvest dates are estimates.", "काम करने से पहले खेत की वास्तविक स्थिति जाँचें। कटाई की तारीखें अनुमानित हैं।", "કામ કરતાં પહેલાં ખેતરની વાસ્તવિક સ્થિતિ તપાસો. કાપણીની તારીખો અંદાજિત છે.");
        var link = T("View all tasks, complete or reschedule", "सभी काम देखें, पूरा करें या तारीख बदलें", "બધાં કામ જુઓ, પૂર્ણ કરો અથવા તારીખ બદલો");
        var preference = T("You opted in to crop reminders. To stop, open My Crops → Edit details and turn email reminders off.", "आपने फसल के ईमेल रिमाइंडर चालू किए हैं। बंद करने के लिए फसल की जानकारी बदलने वाले पेज पर ईमेल रिमाइंडर का विकल्प बंद करें।", "તમે પાક માટે ઇમેઇલ યાદ અપાવવાનું ચાલુ કર્યું છે. બંધ કરવા માટે પાકની વિગતો બદલવાના પાનામાં જઈને ઇમેઇલ યાદ અપાવવાનો વિકલ્પ બંધ કરો.");
        var code = SiteLanguage.Code(language);
        return $"<html lang='{code}'><body><h2>{E(Heading(language))}</h2><p>{E(farmer.Name)}, {E(introduction)}</p><ul>{list}</ul><p>{E(caution)}</p><p><a href='{E(publicUrl.GetLeftPart(UriPartial.Authority))}/CropPlan/Today'>{E(link)}</a></p><p>{E(preference)}</p></body></html>";
    }
}
