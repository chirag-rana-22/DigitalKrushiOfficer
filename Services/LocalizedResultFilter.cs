using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.RegularExpressions;

namespace Digital_Krushi_Officer.Services;
public class LocalizedResultFilter(UiText ui) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Controller is Controller controller)
        {
            if (context.Result is ViewResult)
            {
                foreach (var state in controller.ModelState.Values)
                {
                    var errors = state.Errors.Select(x => x.ErrorMessage).ToArray();
                    if (errors.Length == 0) continue;
                    state.Errors.Clear();
                    foreach (var error in errors) state.Errors.Add(Validation(error, ui.Language));
                }
            }
            if (context.Result is ViewResult) foreach (var key in new[] { "Success", "Error", "Warning" })
                if (controller.TempData.Peek(key) is string message) controller.TempData[key] = ui.Text(message);
        }
        if (context.Result is ObjectResult { Value: string text } result) result.Value = ui.Text(text);
        if (context.Result is ContentResult content && content.ContentType?.StartsWith("text/plain") == true) content.Content = ui.Text(content.Content);
        await next();
    }
    public static string Validation(string value, string language)
    {
        var exact = TranslationCatalog.Translate(value, language);
        if (exact != value) return exact;
        foreach (var (pattern, template) in new[] {
            (@"^The (.+) field is required\.$", "{0} is required."),
            (@"^The (.+) field is not a valid e-mail address\.$", "Enter a valid email address for {0}."),
            (@"^The field (.+) must be between (.+) and (.+)\.$", "{0} must be between {1} and {2}."),
            (@"^The field (.+) must be a string with a maximum length of '?(\d+)'?\.$", "{0} must be at most {1} characters."),
            (@"^The field (.+) must be a string with a minimum length of (\d+) and a maximum length of (\d+)\.$", "{0} must contain {1} to {2} characters.") })
        {
            var match = Regex.Match(value, pattern);
            if (match.Success) return string.Format(TranslationCatalog.Translate(template, language), match.Groups.Cast<Group>().Skip(1).Select(x => TranslationCatalog.Translate(x.Value, language)).Cast<object>().ToArray());
        }
        return value;
    }
}
