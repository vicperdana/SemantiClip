using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Markdig;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Linq;
using System.Text;

namespace SemanticClip.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportController : ControllerBase
    {
        [HttpPost("export")]
        public IActionResult Export([FromBody] ExportRequest request)
        {
            var format = (request.Format ?? "docx").Trim().ToLowerInvariant();
            var content = request.MarkdownContent ?? request.Content ?? string.Empty;
            var baseName = string.IsNullOrWhiteSpace(request.FileName)
                ? $"Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}"
                : Path.GetFileNameWithoutExtension(request.FileName);

            if (string.IsNullOrWhiteSpace(content))
                return BadRequest("Content is required.");

            // Helper to set page size if supplied
            void ApplyPageSettings(Section section)
            {
                if (!string.IsNullOrWhiteSpace(request.PageSize))
                {
                    var ps = request.PageSize.Trim().ToLowerInvariant();
                    if (ps is "a4") section.PageSetup.PageSize = PageSize.A4;
                    else if (ps is "letter") section.PageSetup.PageSize = PageSize.Letter;
                }
            }

            switch (format)
            {
                case "docx":
                {
                    using var doc = new Document();
                    var section = doc.AddSection();
                    ApplyPageSettings(section);

                    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                    var mdDoc = Markdown.Parse(content, pipeline);
                    foreach (var block in mdDoc)
                        AddMarkdownBlockSpire(doc, section, block);

                    using var ms = new MemoryStream();
                    doc.SaveToStream(ms, FileFormat.Docx);
                    ms.Position = 0;
                    var fileName = baseName + ".docx";
                    return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
                }
                case "pdf":
                {
                    using var doc = new Document();
                    var section = doc.AddSection();
                    ApplyPageSettings(section);

                    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                    var mdDoc = Markdown.Parse(content, pipeline);
                    foreach (var block in mdDoc)
                        AddMarkdownBlockSpire(doc, section, block);

                    using var ms = new MemoryStream();
                    doc.SaveToStream(ms, FileFormat.PDF);
                    ms.Position = 0;
                    var fileName = baseName + ".pdf";
                    return File(ms.ToArray(), "application/pdf", fileName);
                }
                case "md":
                {
                    var bytes = Encoding.UTF8.GetBytes(content);
                    var fileName = baseName + ".md";
                    return File(bytes, "text/markdown", fileName);
                }
                case "txt":
                {
                    // Basic plain text export (markdown preserved as-is)
                    var bytes = Encoding.UTF8.GetBytes(content);
                    var fileName = baseName + ".txt";
                    return File(bytes, "text/plain", fileName);
                }
                default:
                    return BadRequest("Unsupported export format. Use docx, pdf, md, or txt.");
            }
        }

        [HttpPost("docx")]
        public IActionResult ExportToDocx([FromBody] ExportRequest request)
        {
            var doc = new Document();
            var section = doc.AddSection();

            // Apply PageSize if provided (kept parity with multi-format endpoint)
            var ps = request.PageSize?.Trim().ToLowerInvariant();
            if (ps == "a4") section.PageSetup.PageSize = PageSize.A4;
            else if (ps == "letter") section.PageSetup.PageSize = PageSize.Letter;

            var content = request.MarkdownContent ?? request.Content ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(content))
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                var mdDoc = Markdown.Parse(content, pipeline);
                foreach (var block in mdDoc)
                {
                    AddMarkdownBlockSpire(doc, section, block);
                }
            }

            using var ms = new MemoryStream();
            doc.SaveToStream(ms, FileFormat.Docx);
            ms.Position = 0;
            var fileName = string.IsNullOrWhiteSpace(request.FileName) ? $"Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx" : request.FileName;
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        // Spire.Doc: Add markdown block to section
        private void AddMarkdownBlockSpire(Document doc, Section section, MarkdownObject block)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    var para = section.AddParagraph();
                    para.AppendText(GetInlineTextSpire(heading.Inline));
                    para.ApplyStyle(heading.Level switch
                    {
                        1 => BuiltinStyle.Heading1,
                        2 => BuiltinStyle.Heading2,
                        3 => BuiltinStyle.Heading3,
                        4 => BuiltinStyle.Heading4,
                        5 => BuiltinStyle.Heading5,
                        _ => BuiltinStyle.Heading6
                    });
                    break;
                case ListBlock list:
                    foreach (ListItemBlock item in list)
                    {
                        foreach (var subBlock in item)
                        {
                            var listPara = section.AddParagraph();
                            if (subBlock is ParagraphBlock paragraphInList)
                            {
                                AddMarkdownInlinesSpire(listPara, paragraphInList.Inline);
                            }
                            else
                            {
                                listPara.AppendText(GetInlineTextSpire((subBlock as ParagraphBlock)?.Inline));
                            }
                            if (list.IsOrdered)
                                listPara.ListFormat.ApplyNumberedStyle();
                            else
                                listPara.ListFormat.ApplyBulletStyle();
                        }
                    }
                    break;
                case ParagraphBlock paraBlock:
                    var p = section.AddParagraph();
                    AddMarkdownInlinesSpire(p, paraBlock.Inline);
                    break;
                case FencedCodeBlock code:
                    var codeP = section.AddParagraph();
                    var codeRun = codeP.AppendText(code.Lines.ToString());
                    codeRun.CharacterFormat.FontName = "Consolas";
                    break;
                case QuoteBlock quote:
                    foreach (var quoteBlock in quote)
                    {
                        var quotePara = section.AddParagraph();
                        if (quoteBlock is ParagraphBlock quoteParagraph)
                        {
                            AddMarkdownInlinesSpire(quotePara, quoteParagraph.Inline);
                            quotePara.Format.LeftIndent = 36; // Indent blockquotes
                        }
                    }
                    break;
                // Add more block types as needed
                default:
                    section.AddParagraph().AppendText(block?.ToString() ?? string.Empty);
                    break;
            }
        }

        private void AddMarkdownInlinesSpire(Spire.Doc.Documents.Paragraph para, ContainerInline? container)
        {
            if (container == null) return;
            foreach (var inline in container)
            {
                if (inline is LiteralInline lit)
                {
                    para.AppendText(lit.Content.ToString());
                }
                else if (inline is EmphasisInline emph)
                {
                    var emphText = GetInlineTextSpire(emph);
                    var run = para.AppendText(emphText);
                    if (emph.DelimiterChar == '*' && emph.DelimiterCount == 2)
                        run.CharacterFormat.Bold = true;
                    else if (emph.DelimiterChar == '*' && emph.DelimiterCount == 1)
                        run.CharacterFormat.Italic = true;
                    else if (emph.DelimiterChar == '_' && emph.DelimiterCount == 1)
                        run.CharacterFormat.Italic = true;
                    else if (emph.DelimiterChar == '_' && emph.DelimiterCount == 2)
                        run.CharacterFormat.Bold = true;
                }
                else if (inline is LineBreakInline)
                {
                    para.AppendBreak(BreakType.LineBreak);
                }
                else if (inline is CodeInline code)
                {
                    var run = para.AppendText(code.Content);
                    run.CharacterFormat.FontName = "Consolas";
                }
                else if (inline is LinkInline link)
                {
                    var display = GetInlineTextSpire(link);
                    if (string.IsNullOrWhiteSpace(display))
                        display = link.Title ?? link.Url ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(link.Url))
                    {
                        // Append clickable web hyperlink
                        para.AppendHyperlink(link.Url, display, HyperlinkType.WebLink);
                    }
                    else
                    {
                        para.AppendText(display);
                    }
                }
                // Add more inline types as needed
            }
        }

        private string GetInlineTextSpire(ContainerInline? container)
        {
            if (container == null) return string.Empty;
            var text = string.Empty;
            foreach (var inline in container)
            {
                if (inline is LiteralInline lit)
                    text += lit.Content.ToString();
                else if (inline is EmphasisInline emph)
                    text += string.Concat(emph.Select(x => (x as LiteralInline)?.Content.ToString()));
                else if (inline is CodeInline code)
                    text += code.Content;
                // Add more inline types as needed
            }
            return text;
        }
    }

    public class ExportRequest
    {
        public string? Content { get; set; }
        public string? MarkdownContent { get; set; }
        public string? FileName { get; set; }
        public string? Format { get; set; } // docx|pdf|md|txt
        public string? PageSize { get; set; } // A4|Letter
    }
}
