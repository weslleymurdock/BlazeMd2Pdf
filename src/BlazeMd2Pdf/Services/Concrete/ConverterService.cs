using System.Globalization;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using BlazeMd2Pdf.Services.Abstract;
using Markdig;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Writer;

namespace BlazeMd2Pdf.Services.Concrete;

/// <summary>
/// Provides local document conversion operations.
/// </summary>
/// <param name="httpClient">The HTTP client used to retrieve the open document fonts.</param>
public sealed class ConverterService(HttpClient httpClient) : IConverterService
{
    private const string LiberationSansUrl = "https://raw.githubusercontent.com/shantigilbert/liberation-fonts-ttf/master/LiberationSans-Regular.ttf";
    private const string LiberationSerifUrl = "https://raw.githubusercontent.com/shantigilbert/liberation-fonts-ttf/master/LiberationSerif-Regular.ttf";
    private const string LiberationMonoUrl = "https://raw.githubusercontent.com/shantigilbert/liberation-fonts-ttf/master/LiberationMono-Regular.ttf";
    private const string NotoSansUrl = "https://raw.githubusercontent.com/notofonts/noto-fonts/main/hinted/ttf/NotoSans/NotoSans-Regular.ttf";

    private static readonly DocumentFont[] Fonts =
    [
        new("liberation-sans", "Arial-compatible — Liberation Sans"),
        new("liberation-serif", "Times New Roman-compatible — Liberation Serif"),
        new("liberation-mono", "Courier New-compatible — Liberation Mono"),
        new("noto-sans", "Broad Unicode coverage — Noto Sans")
    ];

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private byte[]? _liberationSans;
    private byte[]? _liberationSerif;
    private byte[]? _liberationMono;
    private byte[]? _notoSans;

    /// <inheritdoc />
    public IReadOnlyList<DocumentFont> AvailableFonts => Fonts;

    /// <inheritdoc />
    public async Task<string> ReadMarkdownAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ReadPdfAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        await using var pdfBytes = new MemoryStream();
        await stream.CopyToAsync(pdfBytes, cancellationToken);
        pdfBytes.Position = 0;

        return ExtractPdfText(pdfBytes, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> ConvertMarkdownToPdfAsync(
        string markdown,
        string fontKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(fontKey);
        cancellationToken.ThrowIfCancellationRequested();

        var fontBytes = await LoadFontAsync(fontKey, cancellationToken);
        return BuildPdf(markdown, fontBytes, fontKey, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ConvertPdfToMarkdownAsync(Stream stream, CancellationToken cancellationToken = default) =>
        ReadPdfAsync(stream, cancellationToken);

    /// <summary>
    /// Loads the bytes of the requested open document font.
    /// </summary>
    /// <param name="fontKey">The stable font identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The TrueType font bytes.</returns>
    private async Task<byte[]> LoadFontAsync(string fontKey, CancellationToken cancellationToken)
    {
        return fontKey switch
        {
            "liberation-sans" => _liberationSans ??= await httpClient.GetByteArrayAsync(LiberationSansUrl, cancellationToken),
            "liberation-serif" => _liberationSerif ??= await httpClient.GetByteArrayAsync(LiberationSerifUrl, cancellationToken),
            "liberation-mono" => _liberationMono ??= await httpClient.GetByteArrayAsync(LiberationMonoUrl, cancellationToken),
            "noto-sans" => _notoSans ??= await httpClient.GetByteArrayAsync(NotoSansUrl, cancellationToken),
            _ => throw new ArgumentException($"Unknown document font '{fontKey}'.", nameof(fontKey))
        };
    }

    /// <summary>
    /// Extracts text from all PDF pages in content order.
    /// </summary>
    /// <param name="stream">The in-memory PDF stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extracted text separated by page breaks.</returns>
    private static string ExtractPdfText(Stream stream, CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();
        var firstPage = true;

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!firstPage)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append(ContentOrderTextExtractor.GetText(page, true));
            firstPage = false;
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// Builds a styled PDF by first rendering Markdown to semantic HTML and then laying out that HTML.
    /// </summary>
    /// <param name="markdown">The Markdown source.</param>
    /// <param name="fontBytes">The selected TrueType font bytes.</param>
    /// <param name="fontKey">The selected font identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated PDF bytes.</returns>
    private static byte[] BuildPdf(
        string markdown,
        byte[] fontBytes,
        string fontKey,
        CancellationToken cancellationToken)
    {
        var html = Markdig.Markdown.ToHtml(markdown, MarkdownPipeline);
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var builder = new PdfDocumentBuilder();
        var font = builder.AddTrueTypeFont(fontBytes);
        var page = builder.AddPage(595d, 842d);
        var layout = new PdfLayout(builder, page, font, fontKey, cancellationToken);

        foreach (var child in document.Body?.Children ?? [])
        {
            layout.RenderBlock(child);
        }

        return builder.Build();
    }

    /// <summary>
    /// Represents a styled piece of inline document content.
    /// </summary>
    private readonly record struct PdfTextRun(string Text, double FontSize, bool Bold, bool Italic, bool Code, bool Link);

    /// <summary>
    /// Performs semantic HTML to PDF layout while preserving Markdown presentation semantics.
    /// </summary>
    private sealed class PdfLayout
    {
        private const double PageWidth = 595d;
        private const double PageHeight = 842d;
        private const double Margin = 50d;
        private const double ContentWidth = PageWidth - (Margin * 2);

        private readonly PdfDocumentBuilder _builder;
        private readonly PdfDocumentBuilder.AddedFont _font;
        private readonly string _fontKey;
        private readonly CancellationToken _cancellationToken;
        private PdfPageBuilder _page;
        private double _y;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfLayout"/> class.
        /// </summary>
        /// <param name="builder">The PDF document builder.</param>
        /// <param name="page">The first PDF page.</param>
        /// <param name="font">The selected document font.</param>
        /// <param name="fontKey">The selected font identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public PdfLayout(
            PdfDocumentBuilder builder,
            PdfPageBuilder page,
            PdfDocumentBuilder.AddedFont font,
            string fontKey,
            CancellationToken cancellationToken)
        {
            _builder = builder;
            _page = page;
            _font = font;
            _fontKey = fontKey;
            _cancellationToken = cancellationToken;
            _y = PageHeight - Margin;
        }

        /// <summary>
        /// Renders a block-level HTML element.
        /// </summary>
        /// <param name="element">The HTML element.</param>
        public void RenderBlock(IElement element)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            switch (element.LocalName.ToLowerInvariant())
            {
                case "h1": RenderParagraph(element, 24d, true, 14d, 8d); break;
                case "h2": RenderParagraph(element, 20d, true, 12d, 7d); break;
                case "h3": RenderParagraph(element, 17d, true, 10d, 6d); break;
                case "h4": RenderParagraph(element, 15d, true, 9d, 5d); break;
                case "h5": RenderParagraph(element, 13d, true, 8d, 4d); break;
                case "h6": RenderParagraph(element, 12d, true, 7d, 4d); break;
                case "p": RenderParagraph(element, 11d, false, 5d, 7d); break;
                case "blockquote": RenderQuote(element); break;
                case "ul": RenderList(element, false); break;
                case "ol": RenderList(element, true); break;
                case "pre": RenderCodeBlock(element); break;
                case "hr": EnsureSpace(12d); _page.DrawLine(new PdfPoint(Margin, _y), new PdfPoint(PageWidth - Margin, _y), 1); _y -= 12d; break;
                case "table": RenderTable(element); break;
                default:
                    foreach (var child in element.Children)
                    {
                        RenderBlock(child);
                    }
                    break;
            }
        }

        /// <summary>
        /// Renders a normal paragraph or heading with inline formatting and automatic wrapping.
        /// </summary>
        /// <param name="element">The HTML element.</param>
        /// <param name="fontSize">The base font size.</param>
        /// <param name="bold">Whether the complete block is bold.</param>
        /// <param name="before">Vertical space before the block.</param>
        /// <param name="after">Vertical space after the block.</param>
        private void RenderParagraph(IElement element, double fontSize, bool bold, double before, double after)
        {
            EnsureSpace(before + fontSize + after);
            var runs = new List<PdfTextRun>();
            AppendInlineRuns(element, runs, fontSize, bold, false, false, false);
            RenderRuns(runs, 0);
            _y -= after;
        }

        /// <summary>
        /// Renders a block quote with an indentation and separator line.
        /// </summary>
        /// <param name="element">The block quote element.</param>
        private void RenderQuote(IElement element)
        {
            EnsureSpace(12d);
            var startY = _y;
            var runs = new List<PdfTextRun>();
            AppendInlineRuns(element, runs, 11d, false, true, false, false);
            RenderRuns(runs, 16d);
            _page.DrawLine(new PdfPoint(Margin + 2d, startY + 2d), new PdfPoint(Margin + 2d, _y + 12d), 2);
            _y -= 8d;
        }

        /// <summary>
        /// Renders an ordered or unordered list with nested indentation.
        /// </summary>
        /// <param name="element">The list element.</param>
        /// <param name="ordered">Whether the list is ordered.</param>
        private void RenderList(IElement element, bool ordered)
        {
            var index = 1;
            foreach (var item in element.Children.Where(child => child.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase)))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var prefix = ordered
                    ? $"{index.ToString(CultureInfo.InvariantCulture)}. "
                    : "• ";
                var runs = new List<PdfTextRun>
                {
                    new(prefix, 11d, false, false, false, false)
                };
                AppendInlineRuns(item, runs, 11d, false, false, false, false);
                EnsureSpace(18d);
                RenderRuns(runs, 0, Margin + 12d);
                index++;

                foreach (var nested in item.Children.Where(child => child.LocalName is "ul" or "ol"))
                {
                    RenderList(nested, nested.LocalName.Equals("ol", StringComparison.OrdinalIgnoreCase));
                }
            }

            _y -= 5d;
        }

        /// <summary>
        /// Renders fenced or indented Markdown code as a monospaced block.
        /// </summary>
        /// <param name="element">The code block element.</param>
        private void RenderCodeBlock(IElement element)
        {
            var text = element.QuerySelector("code")?.TextContent ?? element.TextContent;
            var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            const double fontSize = 9d;
            const double lineHeight = 13d;

            foreach (var line in lines)
            {
                EnsureSpace(lineHeight + 8d);
                _page.DrawRectangle(new PdfPoint(Margin - 4d, _y - 3d), ContentWidth + 8d, lineHeight + 4d, 0.5d);
                DrawRun(new PdfTextRun(line, fontSize, false, false, true, false), Margin, _y);
                _y -= lineHeight;
            }

            _y -= 8d;
        }

        /// <summary>
        /// Renders a Markdown table as a readable grid with wrapped cell content.
        /// </summary>
        /// <param name="element">The table element.</param>
        private void RenderTable(IElement element)
        {
            var rows = element.QuerySelectorAll("tr")
                .Select(row => row.QuerySelectorAll("th,td").Select(cell => cell.TextContent.Trim()).ToArray())
                .Where(row => row.Length > 0)
                .ToArray();

            if (rows.Length == 0)
            {
                return;
            }

            var columnCount = rows.Max(row => row.Length);
            var widths = new double[columnCount];
            for (var column = 0; column < columnCount; column++)
            {
                widths[column] = Math.Max(70d, ContentWidth / columnCount);
            }

            foreach (var row in rows)
            {
                EnsureSpace(22d);
                var x = Margin;
                var maxHeight = 18d;
                for (var column = 0; column < columnCount; column++)
                {
                    var text = column < row.Length ? row[column] : string.Empty;
                    var cellWidth = widths[column];
                    var cellRuns = new List<PdfTextRun> { new(text, 9.5d, rows[0] == row, false, false, false) };
                    RenderRuns(cellRuns, 0, x + 4d, cellWidth - 8d);
                    _page.DrawRectangle(new PdfPoint(x, _y - maxHeight + 3d), cellWidth, maxHeight, 0.5d);
                    x += cellWidth;
                }
                _y -= maxHeight;
            }
            _y -= 8d;
        }

        /// <summary>
        /// Adds inline HTML descendants to a sequence of styled text runs.
        /// </summary>
        /// <param name="element">The current element.</param>
        /// <param name="runs">The destination run collection.</param>
        /// <param name="fontSize">The inherited font size.</param>
        /// <param name="bold">Whether bold is inherited.</param>
        /// <param name="italic">Whether italic is inherited.</param>
        /// <param name="code">Whether code styling is inherited.</param>
        /// <param name="link">Whether link styling is inherited.</param>
        private static void AppendInlineRuns(
            IElement element,
            List<PdfTextRun> runs,
            double fontSize,
            bool bold,
            bool italic,
            bool code,
            bool link)
        {
            foreach (var node in element.ChildNodes)
            {
                if (node is IText text)
                {
                    var value = NormalizeWhitespace(text.Data);
                    if (!string.IsNullOrEmpty(value))
                    {
                        runs.Add(new PdfTextRun(value, fontSize, bold, italic, code, link));
                    }
                    continue;
                }

                if (node is not IElement child)
                {
                    continue;
                }

                var tag = child.LocalName.ToLowerInvariant();
                if (tag is "ul" or "ol" or "pre" or "table")
                {
                    continue;
                }

                var childBold = bold || tag is "strong" or "b";
                var childItalic = italic || tag is "em" or "i";
                var childCode = code || tag == "code";
                var childLink = link || tag == "a";
                var childSize = tag switch
                {
                    "small" => Math.Max(8d, fontSize - 2d),
                    "sup" => fontSize * 0.75d,
                    "sub" => fontSize * 0.75d,
                    _ => fontSize
                };

                if (tag == "br")
                {
                    runs.Add(new PdfTextRun("\n", fontSize, bold, italic, code, link));
                    continue;
                }

                AppendInlineRuns(child, runs, childSize, childBold, childItalic, childCode, childLink);
            }
        }

        /// <summary>
        /// Renders styled runs with word wrapping.
        /// </summary>
        /// <param name="runs">The runs to render.</param>
        /// <param name="indent">The additional left indentation.</param>
        /// <param name="left">The left edge of the content.</param>
        /// <param name="maxWidth">The maximum line width.</param>
        private void RenderRuns(List<PdfTextRun> runs, double indent, double left = Margin, double maxWidth = ContentWidth)
        {
            var x = left + indent;
            foreach (var run in runs)
            {
                foreach (var token in Tokenize(run.Text))
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    if (token == "\n")
                    {
                        _y -= LineHeight(run.FontSize);
                        x = left + indent;
                        EnsureSpace(LineHeight(run.FontSize));
                        continue;
                    }

                    var width = MeasureWidth(token, run.FontSize);
                    if (x > left + indent && x + width > left + maxWidth)
                    {
                        _y -= LineHeight(run.FontSize);
                        x = left + indent;
                        EnsureSpace(LineHeight(run.FontSize));
                    }

                    DrawRun(run with { Text = token }, x, _y);
                    x += width;
                }
            }
            _y -= 4d;
        }

        /// <summary>
        /// Draws one styled run and adds visual emphasis for bold, italic, code, and links.
        /// </summary>
        /// <param name="run">The run to draw.</param>
        /// <param name="x">The horizontal position.</param>
        /// <param name="y">The baseline.</param>
        private void DrawRun(PdfTextRun run, double x, double y)
        {
            try
            {
                if (run.Link)
                {
                    _page.SetTextAndFillColor(35, 90, 160);
                }

                _page.AddText(run.Text, run.FontSize, new PdfPoint(x, y), _font);

                if (run.Bold)
                {
                    _page.AddText(run.Text, run.FontSize, new PdfPoint(x + 0.35d, y), _font);
                }

                if (run.Italic || run.Link)
                {
                    var underlineY = y - Math.Max(1d, run.FontSize * 0.12d);
                    _page.DrawLine(new PdfPoint(x, underlineY), new PdfPoint(x + MeasureWidth(run.Text, run.FontSize), underlineY), 0.5d);
                }

                if (run.Link)
                {
                    _page.ResetColor();
                }
            }
            catch (InvalidOperationException exception) when (TryGetUnsupportedCharacter(exception, out var character))
            {
                throw new UnsupportedFontCharacterException(_fontKey, character);
            }
        }

        /// <summary>
        /// Measures a run using PdfPig's font metrics.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="fontSize">The font size.</param>
        /// <returns>The measured width.</returns>
        private double MeasureWidth(string text, double fontSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0d;
            }

            return _page.MeasureText(text, fontSize, new PdfPoint(0, 0), _font).Sum(letter => letter.Width);
        }

        /// <summary>
        /// Ensures that the requested vertical space is available on the current page.
        /// </summary>
        /// <param name="requiredHeight">The required height.</param>
        private void EnsureSpace(double requiredHeight)
        {
            if (_y - requiredHeight >= Margin)
            {
                return;
            }

            _page = _builder.AddPage(PageWidth, PageHeight);
            _y = PageHeight - Margin;
        }

        /// <summary>
        /// Calculates a readable line height from a font size.
        /// </summary>
        /// <param name="fontSize">The font size.</param>
        /// <returns>The line height.</returns>
        private static double LineHeight(double fontSize) => Math.Max(13d, fontSize * 1.35d);

        /// <summary>
        /// Splits text into words and whitespace while preserving explicit line breaks.
        /// </summary>
        /// <param name="text">The text to tokenize.</param>
        /// <returns>The tokens.</returns>
        private static IEnumerable<string> Tokenize(string text)
        {
            var normalized = text.Replace("\r", string.Empty, StringComparison.Ordinal);
            var buffer = new StringBuilder();
            foreach (var character in normalized)
            {
                if (character == '\n')
                {
                    if (buffer.Length > 0)
                    {
                        yield return buffer.ToString();
                        buffer.Clear();
                    }
                    yield return "\n";
                }
                else if (char.IsWhiteSpace(character))
                {
                    buffer.Append(' ');
                }
                else
                {
                    buffer.Append(character);
                }
            }

            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
            }
        }

        /// <summary>
        /// Normalizes HTML text-node whitespace without collapsing explicit paragraph structure.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <returns>The normalized text.</returns>
        private static string NormalizeWhitespace(string text) =>
            string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Extracts the first unsupported character from a PdfPig font exception.
    /// </summary>
    /// <param name="exception">The PdfPig exception.</param>
    /// <param name="character">The unsupported character.</param>
    /// <returns><see langword="true"/> when an unsupported character was found.</returns>
    private static bool TryGetUnsupportedCharacter(InvalidOperationException exception, out char character)
    {
        const string marker = "The font does not contain a character: ";
        var message = exception.Message;
        var start = message.IndexOf(marker, StringComparison.Ordinal);

        if (start < 0)
        {
            character = default;
            return false;
        }

        start += marker.Length;
        if (start >= message.Length)
        {
            character = default;
            return false;
        }

        character = message[start];
        return true;
    }
}
