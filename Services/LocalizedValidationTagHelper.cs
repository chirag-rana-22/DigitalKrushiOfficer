using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Digital_Krushi_Officer.Services;
[HtmlTargetElement("input")]
[HtmlTargetElement("select")]
[HtmlTargetElement("textarea")]
public class LocalizedValidationTagHelper(UiText ui) : TagHelper
{
    public override int Order => int.MaxValue;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        foreach (var attribute in output.Attributes.Where(x => x.Name.StartsWith("data-val-", StringComparison.Ordinal)).ToArray())
            output.Attributes.SetAttribute(attribute.Name, LocalizedResultFilter.Validation(attribute.Value.ToString() ?? "", ui.Language));
    }
}
