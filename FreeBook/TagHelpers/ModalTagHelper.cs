using FreeBook.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;

namespace FreeBook.TagHelpers
{
    /// <summary>
    /// Renders the Bootstrap modal shell (header, close button and body) so views
    /// only have to supply their form content.
    /// </summary>
    [HtmlTargetElement("modal")]
    public class ModalTagHelper : TagHelper
    {
        private readonly IStringLocalizer<ResourceWeb> _localizer;

        public ModalTagHelper(IStringLocalizer<ResourceWeb> localizer)
        {
            _localizer = localizer;
        }

        /// <summary>Id of the modal root element.</summary>
        public string? Id { get; set; }

        /// <summary>Heading text shown in the modal header.</summary>
        public string? Title { get; set; }

        /// <summary>Optional id for the heading; also used for aria-labelledby.</summary>
        public string? TitleId { get; set; }

        /// <summary>Optional dialog size suffix, e.g. "lg" or "sm".</summary>
        public string? Size { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var childContent = await output.GetChildContentAsync();

            var dialog = new TagBuilder("div");
            dialog.AddCssClass("modal-dialog");
            if (!string.IsNullOrWhiteSpace(Size))
            {
                dialog.AddCssClass($"modal-{Size}");
            }

            var content = new TagBuilder("div");
            content.AddCssClass("modal-content");

            var header = new TagBuilder("div");
            header.AddCssClass("modal-header");

            var title = new TagBuilder("h5");
            title.AddCssClass("modal-title");
            if (!string.IsNullOrWhiteSpace(TitleId))
            {
                title.Attributes["id"] = TitleId;
            }
            title.InnerHtml.Append(Title ?? string.Empty);

            var close = new TagBuilder("button");
            close.Attributes["type"] = "button";
            close.Attributes["aria-label"] = _localizer["lbClose"].Value;
            close.Attributes["data-bs-dismiss"] = "modal";
            close.AddCssClass("btn-close");

            header.InnerHtml.AppendHtml(title);
            header.InnerHtml.AppendHtml(close);

            var body = new TagBuilder("div");
            body.AddCssClass("modal-body");
            body.InnerHtml.AppendHtml(childContent);

            content.InnerHtml.AppendHtml(header);
            content.InnerHtml.AppendHtml(body);
            dialog.InnerHtml.AppendHtml(content);

            var modal = new TagBuilder("div");
            modal.AddCssClass("modal");
            modal.AddCssClass("fade");
            if (!string.IsNullOrWhiteSpace(Id))
            {
                modal.Attributes["id"] = Id;
            }
            if (!string.IsNullOrWhiteSpace(TitleId))
            {
                modal.Attributes["aria-labelledby"] = TitleId;
            }
            modal.Attributes["tabindex"] = "-1";
            modal.Attributes["aria-hidden"] = "true";
            modal.InnerHtml.AppendHtml(dialog);

            output.TagName = null;
            output.Attributes.Clear();
            output.Content.SetHtmlContent(modal);
        }
    }
}
