using System.Net;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Services;
using Xunit;

public class ReminderLanguageTests
{
    static readonly Uri Website = new("https://dko.test");
    static CropTask Task(string kind = "Inspection") => new() { Kind = kind, Title = "Weekly crop and soil check", DueDate = new DateTime(2026,10,14), FarmerCrop = new() { Crop = new() { CropName = "Bajra" }, FieldName = "ખેતર ૧" } };
    [Fact] public void GujaratiEmailIncludesLocalizedBodyDatesActionsAndSystemTasks() {
        var body = WebUtility.HtmlDecode(ReminderLanguage.Email(new() { Name = "ભાવિક", PreferredLanguage = "Gujarati" }, [Task()], new DateTime(2026,10,14), Website));
        Assert.Contains("આજના ખેતીના કામ",body);Assert.Contains("14 ઑક્ટોબર 2026",body);Assert.Contains("પાક અને જમીનની સાપ્તાહિક તપાસ",body);Assert.Contains("બાજરી",body);Assert.Contains("ખેતરની વાસ્તવિક સ્થિતિ તપાસો",body);Assert.Contains("બધાં કામ જુઓ, પૂર્ણ કરો અથવા તારીખ બદલો",body);Assert.Contains("ઇમેઇલ યાદ અપાવવાનો વિકલ્પ બંધ કરો",body);
        Assert.DoesNotContain("these are your",body,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("Check actual field conditions",body);Assert.DoesNotContain("Weekly crop and soil check",body);Assert.Contains("lang='gu'",body);
    }
    [Theory] [InlineData("Preparation")] [InlineData("Sowing")] [InlineData("Inspection")] [InlineData("HarvestPreparation")] [InlineData("HarvestCheck")]
    public void EveryGeneratedTaskHasGujaratiTitleAndNotes(string kind) {
        var task=Task(kind);task.Notes="English notes";
        Assert.NotEqual(task.Title,ReminderLanguage.TaskTitle(task,"Gujarati"));Assert.DoesNotContain("English notes",ReminderLanguage.TaskNotes(task,"Gujarati"));
    }
    [Theory] [InlineData("Hindi","आज के खेती के काम","सभी काम देखें, पूरा करें या तारीख बदलें","lang='hi'")] [InlineData("English","Today's farming tasks","View all tasks, complete or reschedule","lang='en'")]
    public void OtherLanguagesLocalizeWholeEmail(string language,string heading,string link,string attribute) {
        var body=WebUtility.HtmlDecode(ReminderLanguage.Email(new(){Name="Farmer",PreferredLanguage=language},[Task()],new DateTime(2026,10,14),Website));Assert.Contains(heading,body);Assert.Contains(link,body);Assert.Contains(attribute,body);
    }
    [Fact] public void CustomTextIsPreservedAndHtmlEncoded() {
        var task=Task("Custom");task.Title="મારો ખાસ રિમાઇન્ડર <script>bad</script>";task.FarmerCrop!.CustomCropName="મારો પાક";
        var body=ReminderLanguage.Email(new(){Name="<b>Name</b>",PreferredLanguage="Gujarati"},[task],new DateTime(2026,10,14),Website);
        Assert.Contains("&lt;script&gt;",body);Assert.DoesNotContain("<script>",body);Assert.Equal(task.Title,ReminderLanguage.TaskTitle(task,"Gujarati"));Assert.Equal("મારો પાક",ReminderLanguage.CropName(task.FarmerCrop,"Gujarati"));Assert.Equal("English",ReminderLanguage.Normalize(null));
    }
}
