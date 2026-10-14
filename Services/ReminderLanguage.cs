using System.Globalization;
using System.Net;
using Digital_Krushi_Officer.Models;
namespace Digital_Krushi_Officer.Services;

public static class ReminderLanguage
{
    public static string Normalize(string? language) => language?.Trim().ToLowerInvariant() switch { "english" => "English", "hindi" => "Hindi", _ => "Gujarati" };
    public static string Text(string? language, string english, string hindi, string gujarati) => Normalize(language) switch { "English" => english, "Hindi" => hindi, _ => gujarati };
    public static string Heading(string? language) => Text(language, "Today's farming tasks", "आज के खेती के काम", "આજના ખેતીના કામ");
    public static string Date(DateTime date, string? language)
    {
        string[] gu = ["જાન્યુઆરી", "ફેબ્રુઆરી", "માર્ચ", "એપ્રિલ", "મે", "જૂન", "જુલાઈ", "ઑગસ્ટ", "સપ્ટેમ્બર", "ઑક્ટોબર", "નવેમ્બર", "ડિસેમ્બર"];
        string[] hi = ["जनवरी", "फ़रवरी", "मार्च", "अप्रैल", "मई", "जून", "जुलाई", "अगस्त", "सितंबर", "अक्टूबर", "नवंबर", "दिसंबर"];
        return Normalize(language) switch { "Gujarati" => $"{date.Day} {gu[date.Month-1]} {date.Year}", "Hindi" => $"{date.Day} {hi[date.Month-1]} {date.Year}", _ => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) };
    }
    public static string TaskTitle(CropTask task, string? language) => task.Kind switch {
        "Preparation" => Text(language, "Review field preparation", "खेत की तैयारी की जाँच करें", "ખેતરની તૈયારી તપાસો"),
        "Sowing" => Text(language, "Confirm sowing", "बुवाई की पुष्टि करें", "વાવણીની પુષ્ટિ કરો"),
        "Inspection" => Text(language, "Weekly crop and soil check", "फसल और मिट्टी की साप्ताहिक जाँच", "પાક અને જમીનની સાપ્તાહિક તપાસ"),
        "HarvestPreparation" => Text(language, "Prepare for possible harvest", "संभावित कटाई की तैयारी करें", "સંભવિત કાપણીની તૈયારી કરો"),
        "HarvestCheck" => Text(language, "Check readiness for harvest", "कटाई के लिए फसल तैयार है या नहीं, जाँचें", "કાપણી માટે પાક તૈયાર છે કે નહીં તે તપાસો"),
        _ => task.Title // Farmer-written text is kept exactly as entered; it is not translated or sent to an AI provider.
    };
    public static string TaskNotes(CropTask task, string? language) => task.Kind switch {
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
        if (Normalize(language) != "Gujarati" || crop.CustomCropName != null) return crop.DisplayName;
        return crop.DisplayName switch {
            "Bajra" => "બાજરી", "Wheat" => "ઘઉં", "Rice" => "ડાંગર", "Cotton" => "કપાસ", "Groundnut" => "મગફળી", "Castor" => "દિવેલા", "Sugarcane" => "શેરડી", "Turmeric" => "હળદર",
            "Tomato" => "ટામેટાં", "Onion" => "ડુંગળી", "Potato" => "બટાકા", "Garlic" => "લસણ", "Ginger" => "આદુ", "Cumin" => "જીરું", "Coriander" => "ધાણા", "Fennel" => "વરિયાળી",
            "Chickpea (Gram)" => "ચણા", "Pigeon pea (Tur)" => "તુવેર", "Green gram (Moong)" => "મગ", "Black gram (Urad)" => "અડદ", "Maize" => "મકાઈ", "Jowar (Sorghum)" => "જુવાર",
            "Okra (Bhindi)" => "ભીંડા", "Brinjal (Eggplant)" => "રીંગણ", "Chilli" => "મરચાં", "Mango" => "કેરી", "Banana" => "કેળાં", "Papaya" => "પપૈયાં", "Guava" => "જામફળ",
            "Pomegranate" => "દાડમ", "Lemon" => "લીંબુ", "Watermelon" => "તરબૂચ", "Muskmelon" => "ટેટી", "Sesame (Til)" => "તલ", "Mustard" => "રાઈ", "Fenugreek (Methi)" => "મેથી",
            "Ragi (Finger millet)" => "નાગલી", "Barley" => "જવ", "Oats" => "ઓટ્સ", "Foxtail millet" => "કાંગ", "Little millet" => "સામો", "Kodo millet" => "કોદરી", "Barnyard millet" => "મોરૈયો", "Proso millet" => "ચીનો",
            "Lentil (Masoor)" => "મસૂર", "Cowpea" => "ચોળા", "Field pea" => "વટાણા", "Kidney bean (Rajma)" => "રાજમા", "Horse gram" => "કળથી", "Soybean" => "સોયાબીન", "Sunflower" => "સૂર્યમુખી", "Safflower" => "કુસુમ", "Linseed" => "અળસી",
            "Cabbage" => "કોબી", "Cauliflower" => "ફૂલકોબી", "Capsicum" => "કેપ્સિકમ", "Carrot" => "ગાજર", "Radish" => "મૂળા", "Beetroot" => "બીટ", "Cucumber" => "કાકડી", "Bottle gourd" => "દૂધી", "Bitter gourd" => "કારેલા", "Ridge gourd" => "તુરીયા", "Pumpkin" => "કોળું", "Ash gourd" => "સફેદ કોળું",
            "French bean" => "ફણસી", "Cluster bean (Guar)" => "ગુવાર", "Garden pea" => "વટાણા", "Spinach" => "પાલક", "Amaranth" => "તાંદળજો", "Ajwain" => "અજમો", "Grapes" => "દ્રાક્ષ", "Sweet lime" => "મોસંબી", "Orange" => "નારંગી", "Sapota (Chikoo)" => "ચીકુ", "Strawberry" => "સ્ટ્રોબેરી", "Custard apple" => "સીતાફળ", "Dragon fruit" => "ડ્રેગન ફ્રૂટ", "Coconut" => "નાળિયેર", "Date palm" => "ખજૂર",
            "Lucerne (Alfalfa)" => "રજકો", "Berseem" => "બરસીમ", "Napier grass" => "નેપિયર ઘાસ", "Marigold" => "ગલગોટા", "Rose" => "ગુલાબ", "Jasmine" => "મોગરો", "Chrysanthemum" => "સેવંતી", "Jute" => "શણ", _ => crop.DisplayName
        };
    }
    public static string Email(TblUser farmer, IReadOnlyList<CropTask> tasks, DateTime today, Uri publicUrl)
    {
        var language = farmer.PreferredLanguage;
        string T(string en, string hi, string gu) => Text(language, en, hi, gu);
        string E(string? text) => WebUtility.HtmlEncode(text ?? "");
        var date = Date(today, language);
        var introduction = T($"These are your due or overdue planned tasks for {date} (India time).", $"{date} (भारत समय) के लिए आपके आज के या पिछली तारीख से बाकी खेती के काम नीचे हैं।", $"{date} (ભારતીય સમય) માટે તમારા આજના અથવા અગાઉની તારીખથી બાકી રહેલા ખેતીના કામ નીચે આપેલ છે.");
        var list = string.Join("", tasks.Select(task => $"<li><b>{E(CropName(task.FarmerCrop!, language))} — {E(task.FarmerCrop!.FieldName)}</b>: {E(TaskTitle(task, language))} ({E(Date(task.DueDate, language))})</li>"));
        var caution = T("Check actual field conditions before acting. Harvest dates are estimates.", "काम करने से पहले खेत की वास्तविक स्थिति जाँचें। कटाई की तारीखें अनुमानित हैं।", "કામ કરતાં પહેલાં ખેતરની વાસ્તવિક સ્થિતિ તપાસો. કાપણીની તારીખો અંદાજિત છે.");
        var link = T("View all tasks, complete or reschedule", "सभी काम देखें, पूरा करें या तारीख बदलें", "બધાં કામ જુઓ, પૂર્ણ કરો અથવા તારીખ બદલો");
        var preference = T("You opted in to crop reminders. To stop, open My Crops → Edit details and turn email reminders off.", "आपने फसल के ईमेल रिमाइंडर चालू किए हैं। बंद करने के लिए फसल की जानकारी बदलने वाले पेज पर ईमेल रिमाइंडर का विकल्प बंद करें।", "તમે પાક માટે ઇમેઇલ યાદ અપાવવાનું ચાલુ કર્યું છે. બંધ કરવા માટે પાકની વિગતો બદલવાના પાનામાં જઈને ઇમેઇલ યાદ અપાવવાનો વિકલ્પ બંધ કરો.");
        var code = Normalize(language) switch { "English" => "en", "Hindi" => "hi", _ => "gu" };
        return $"<html lang='{code}'><body><h2>{E(Heading(language))}</h2><p>{E(farmer.Name)}, {E(introduction)}</p><ul>{list}</ul><p>{E(caution)}</p><p><a href='{E(publicUrl.GetLeftPart(UriPartial.Authority))}/CropPlan/Today'>{E(link)}</a></p><p>{E(preference)}</p></body></html>";
    }
}
