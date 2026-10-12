using System.Text.RegularExpressions;
namespace Digital_Krushi_Officer.Services;
// This small deterministic path keeps frequent price follow-ups working even when the LLM is unavailable.
// Unrecognised commodities/locations still go through the model's structured extraction.
public static class MarketConversation {
    private static readonly Dictionary<string,string[]> Crops=new() {
        ["Onion"]=["onion","onions","onoine","onoin","onoinе","dungli","dungળી","dungali","ડુંગળી","प्याज","प्याज़","pyaj","pyaaz"],
        ["Tomato"]=["tomato","tomatoes","tameta","tametu","tamatar","ટામેટા","ટામેટાં","टमाटर"],
        ["Potato"]=["potato","potatoes","batata","બટાટા","आलू","aloo"],
        ["Wheat"]=["wheat","ghau","ઘઉં","गेहूं","गेहूँ"],
        ["Cotton"]=["cotton","kapas","કપાસ","कपास"],
        ["Groundnut"]=["groundnut","peanut","magfali","મગફળી","मूंगफली"],
        ["Garlic"]=["garlic","lasan","લસણ","लहसुन"],
        ["Bajra(Pearl Millet/Cumbu)"]=["bajra","bajri","બાજરી","बाजरा"],
        ["Maize"]=["maize","makai","મકાઈ","मक्का"],
        ["Rice"]=["rice","chawal","ચોખા","चावल"]
    };
    private static readonly Dictionary<string,string[]> Districts=new() {
        ["Junagadh"]=["junagadh","junagarh","junagadhh","જૂનાગઢ","जूनागढ़","जूनागढ"],
        ["Rajkot"]=["rajkot","રાજકોટ","राजकोट"], ["Ahmedabad"]=["ahmedabad","અમદાવાદ","अहमदाबाद"],
        ["Surat"]=["surat","સુરત","सूरत"], ["Amreli"]=["amreli","અમરેલી","अमरेली"],
        ["Jamnagar"]=["jamnagar","જામનગર","जामनगर"]
    };
    private static bool Has(string text,IEnumerable<string> terms)=>terms.Any(t=>Regex.IsMatch(text,@"(?<![\p{L}])"+Regex.Escape(t)+@"(?![\p{L}])",RegexOptions.IgnoreCase));
    private static string Find(string text,Dictionary<string,string[]> map)=>map.FirstOrDefault(p=>Has(text,p.Value)).Key??"";
    private static bool PriceIntent(string text)=>Has(text,["price","prices","prize","rate","rates","bhav","bhaav","bhave","daam","dam","भाव","दाम","कीमत","ભાવ","કિંમત"]);
    private static bool Unsupported(string text)=>Has(text,["retail","msp","forecast","yesterday","tomorrow","historical","खुदरा","कल","છૂટક","આવતીકાલ"]);
    private static bool IsRepair(string text)=>Regex.IsMatch(text.Trim(),@"^(pan try to karo|try again|please try|abe mara question no answer ap|mara question no answer ap|to pchi ke to kem nathi|fuck you|fuck yoy)[.!? ]*$",RegexOptions.IgnoreCase);
    public static MarketPriceRequest? Resolve(string text,IReadOnlyList<AIConversationTurn>? history) {
        if(Unsupported(text)||Regex.IsMatch(text,@"\d"))return null;
        if(Crops.Count(p=>Has(text,p.Value))>1)return null;
        if(Regex.IsMatch(text,@"\b(in|at|near)\s+",RegexOptions.IgnoreCase) && Find(text,Districts).Length==0)return null;
        if(Has(text,["maharashtra","punjab","rajasthan","delhi","karnataka","compare","versus"]))return null;
        var currentCrop=Find(text,Crops);var currentDistrict=Find(text,Districts);
        // Do not steal disease/advice questions which happen to mention market prices.
        if(Has(text,["disease","pesticide","dose","spray","yellow","wilt","रोग","दवा","पीले","રોગ","દવા"]))return null;
        var previous=history?.TakeLast(6).ToList()??new();
        var lastPrice=previous.FindLastIndex(t=>PriceIntent(t.Question));
        var explicitPrice=PriceIntent(text);
        var shortFollowup=text.Length<90 && (currentCrop.Length>0 || currentDistrict.Length>0 || IsRepair(text) || Has(text,["gujarat","ગુજરાત","गुजरात"]));
        // A intervening unrelated question stops implicit price carryover.
        var contextActive=lastPrice>=0 && previous.Skip(lastPrice+1).All(t=>PriceIntent(t.Question)||Find(t.Question,Crops).Length>0||Find(t.Question,Districts).Length>0||IsRepair(t.Question)||Has(t.Question,["gujarat","ગુજરાત","गुजरात"]));
        if(!explicitPrice && !(contextActive && shortFollowup))return null;
        // Unsupported/unknown price topics are left to semantic extraction, not matched to a stale crop.
        if(explicitPrice && currentCrop.Length==0)return null;
        var crop=currentCrop;var district=currentDistrict;
        if(lastPrice>=0 && contextActive)foreach(var turn in previous.AsEnumerable().Reverse()) {if(crop.Length==0)crop=Find(turn.Question,Crops);if(district.Length==0)district=Find(turn.Question,Districts);}
        if(crop.Length==0)return null;
        // Only these explicitly recognised Gujarat districts are mapped. No arbitrary location is guessed.
        var state=district.Length>0||Has(text,["gujarat","ગુજરાત","गुजरात"])?"Gujarat":"";
        // Let the model handle named submarkets, other states, or comparisons instead of silently broadening scope.
        if(Has(text,["market","mandi","apmc","મંડી","मंडी"]) && currentDistrict.Length==0 && text.Length>60)return null;
        return new(crop,state,district,"");
    }
    public static MarketPriceRequest Normalize(MarketPriceRequest r) {
        var crop=Find(r.Commodity,Crops);var district=Find(r.District,Districts);
        var state=r.State.Trim();if(state.Length==0 && district.Length>0)state="Gujarat";
        return r with {Commodity=crop.Length>0?crop:r.Commodity,State=state,District=district.Length>0?district:r.District};
    }
}
