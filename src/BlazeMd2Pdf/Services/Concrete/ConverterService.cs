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

/// <summary>Provides local document conversion operations.</summary>
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
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
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
        return await ConvertPdfToMarkdownAsync(stream, new MarkdownConversionOptions(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> ConvertMarkdownToPdfAsync(string markdown, PdfConversionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);
        cancellationToken.ThrowIfCancellationRequested();
        var fontBytes = await LoadFontAsync(options.FontKey, cancellationToken);
        return BuildPdf(markdown, fontBytes, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ConvertPdfToMarkdownAsync(Stream stream, MarkdownConversionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);
        cancellationToken.ThrowIfCancellationRequested();
        await using var pdfBytes = new MemoryStream();
        await stream.CopyToAsync(pdfBytes, cancellationToken);
        pdfBytes.Position = 0;
        return ExtractPdfText(pdfBytes, options, cancellationToken);
    }

    /// <summary>Loads the bytes of the requested open document font.</summary>
    /// <param name="fontKey">The stable font identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The TrueType font bytes.</returns>
    private async Task<byte[]> LoadFontAsync(string fontKey, CancellationToken cancellationToken) => fontKey switch
    {
        "liberation-sans" => _liberationSans ??= await httpClient.GetByteArrayAsync(LiberationSansUrl, cancellationToken),
        "liberation-serif" => _liberationSerif ??= await httpClient.GetByteArrayAsync(LiberationSerifUrl, cancellationToken),
        "liberation-mono" => _liberationMono ??= await httpClient.GetByteArrayAsync(LiberationMonoUrl, cancellationToken),
        "noto-sans" => _notoSans ??= await httpClient.GetByteArrayAsync(NotoSansUrl, cancellationToken),
        _ => throw new ArgumentException($"Unknown document font '{fontKey}'.", nameof(fontKey))
    };

    /// <summary>Extracts PDF text and turns physical lines into readable Markdown paragraphs.</summary>
    /// <param name="stream">The in-memory PDF stream.</param>
    /// <param name="options">The Markdown formatting options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated Markdown.</returns>
    private static string ExtractPdfText(Stream stream, MarkdownConversionOptions options, CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(stream);
        var pages = new List<string>();
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            pages.Add(ContentOrderTextExtractor.GetText(page, true));
        }

        var output = new StringBuilder();
        for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            if (pageIndex > 0 && options.PreservePageBreaks)
            {
                output.AppendLine();
                output.AppendLine("---");
                output.AppendLine();
            }
            AppendPageAsMarkdown(output, pages[pageIndex], options);
        }
        return output.ToString().Trim();
    }

    /// <summary>Converts one extracted PDF page into readable Markdown blocks.</summary>
    /// <param name="output">The destination builder.</param>
    /// <param name="text">The extracted page text.</param>
    /// <param name="options">The Markdown formatting options.</param>
    private static void AppendPageAsMarkdown(StringBuilder output, string text, MarkdownConversionOptions options)
    {
        var lines = text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        if (options.PreserveLineBreaks)
        {
            foreach (var line in lines)
            {
                var value = line.TrimEnd();
                if (value.Length > 0)
                {
                    output.Append(value);
                    output.Append("  ");
                }
                output.AppendLine();
            }
            return;
        }

        var paragraph = new List<string>();
        foreach (var line in lines)
        {
            var value = line.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                AppendParagraph(output, paragraph, options.ParagraphSpacing);
                paragraph.Clear();
            }
            else
            {
                paragraph.Add(value);
            }
        }
        AppendParagraph(output, paragraph, options.ParagraphSpacing);
    }

    /// <summary>Appends one extracted paragraph with the requested blank-line spacing.</summary>
    /// <param name="output">The destination builder.</param>
    /// <param name="paragraph">The paragraph lines.</param>
    /// <param name="spacing">The number of blank lines.</param>
    private static void AppendParagraph(StringBuilder output, List<string> paragraph, int spacing)
    {
        if (paragraph.Count == 0)
        {
            return;
        }
        output.Append(string.Join(' ', paragraph.Select(NormalizeWhitespace)));
        output.AppendLine();
        for (var index = 0; index < spacing; index++)
        {
            output.AppendLine();
        }
    }

    /// <summary>Builds a styled PDF using semantic Markdown HTML and a deterministic page layout.</summary>
    /// <param name="markdown">The Markdown source.</param>
    /// <param name="fontBytes">The selected TrueType font bytes.</param>
    /// <param name="options">The PDF presentation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated PDF bytes.</returns>
    private static byte[] BuildPdf(string markdown, byte[] fontBytes, PdfConversionOptions options, CancellationToken cancellationToken)
    {
        var html = Markdig.Markdown.ToHtml(markdown, MarkdownPipeline);
        var document = new HtmlParser().ParseDocument(html);
        var builder = new PdfDocumentBuilder();
        var font = builder.AddTrueTypeFont(fontBytes);
        var page = builder.AddPage(595d, 842d);
        var layout = new PdfLayout(builder, page, font, options, cancellationToken);
        if (document.Body is not null)
        {
            foreach (var child in document.Body.Children)
            {
                layout.RenderBlock(child);
            }
        }
        return builder.Build();
    }

    /// <summary>Represents one styled inline text run.</summary>
    private readonly record struct PdfTextRun(string Text, double FontSize, bool Bold, bool Italic, bool Code, bool Link);

    /// <summary>Provides safe block and line layout for a PDF page.</summary>
    private sealed class PdfLayout
    {
        private const double PageWidth = 595d;
        private const double PageHeight = 842d;
        private readonly PdfDocumentBuilder _builder;
        private readonly PdfDocumentBuilder.AddedFont _font;
        private readonly PdfConversionOptions _options;
        private readonly CancellationToken _cancellationToken;
        private readonly double _horizontalMargin;
        private readonly double _topMargin;
        private readonly double _bottomMargin;
        private readonly double _contentWidth;
        private PdfPageBuilder _page;
        private double _y;

        /// <summary>Initializes the PDF layout.</summary>
        /// <param name="builder">The document builder.</param>
        /// <param name="page">The first page.</param>
        /// <param name="font">The selected font.</param>
        /// <param name="options">The presentation options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public PdfLayout(PdfDocumentBuilder builder, PdfPageBuilder page, PdfDocumentBuilder.AddedFont font, PdfConversionOptions options, CancellationToken cancellationToken)
        {
            _builder = builder;
            _page = page;
            _font = font;
            _options = options;
            _cancellationToken = cancellationToken;
            _horizontalMargin = MmToPoints(options.HorizontalMargin);
            _topMargin = MmToPoints(options.TopMargin);
            _bottomMargin = MmToPoints(options.BottomMargin);
            _contentWidth = Math.Max(1d, PageWidth - (_horizontalMargin * 2d));
            _y = PageHeight - _topMargin;
        }

        /// <summary>Renders one block-level HTML element.</summary>
        /// <param name="element">The HTML element.</param>
        public void RenderBlock(IElement element)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            switch (element.LocalName.ToLowerInvariant())
            {
                case "h1": RenderParagraph(element, 24, true, 1.05, _options.HeadingSpacing, _options.HeadingSpacing, true); break;
                case "h2": RenderParagraph(element, 20, true, 1.1, _options.HeadingSpacing, _options.HeadingSpacing * 0.8, true); break;
                case "h3": RenderParagraph(element, 17, true, 1.15, _options.HeadingSpacing * 0.8, _options.HeadingSpacing * 0.7, true); break;
                case "h4": RenderParagraph(element, 15, true, 1.2, _options.HeadingSpacing * 0.7, _options.HeadingSpacing * 0.6, true); break;
                case "h5": RenderParagraph(element, 13, true, 1.25, _options.HeadingSpacing * 0.6, _options.HeadingSpacing * 0.5, true); break;
                case "h6": RenderParagraph(element, 12, true, 1.3, _options.HeadingSpacing * 0.5, _options.HeadingSpacing * 0.5, true); break;
                case "p": RenderParagraph(element, _options.FontSize, false, _options.LineSpacing, 0, _options.ParagraphSpacing, false); break;
                case "blockquote": RenderQuote(element); break;
                case "ul": RenderList(element, false, 0); break;
                case "ol": RenderList(element, true, 0); break;
                case "pre": RenderCodeBlock(element); break;
                case "hr": RenderRule(); break;
                case "table": RenderTable(element); break;
                default:
                    foreach (var child in element.Children)
                    {
                        RenderBlock(child);
                    }
                    break;
            }
        }

        /// <summary>Renders a paragraph or heading with measured line wrapping.</summary>
        /// <param name="element">The source element.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="bold">Whether the block is bold.</param>
        /// <param name="lineSpacing">The line-height multiplier.</param>
        /// <param name="before">The space before the block.</param>
        /// <param name="after">The space after the block.</param>
        /// <param name="heading">Whether the block is a heading.</param>
        private void RenderParagraph(IElement element, double fontSize, bool bold, double lineSpacing, double before, double after, bool heading)
        {
            var runs = new List<PdfTextRun>();
            AppendInlineRuns(element, runs, fontSize, bold, false, false, false);
            var lines = WrapRuns(runs, _contentWidth);
            if (lines.Count == 0)
            {
                return;
            }

            var lineHeight = Math.Max(fontSize * lineSpacing, fontSize * 1.15d);
            var minimum = heading && _options.KeepHeadingWithNext ? lineHeight * 2d : lineHeight;
            EnsureSpace(before + minimum + after);
            _y -= before;
            for (var index = 0; index < lines.Count; index++)
            {
                EnsureSpace(lineHeight + after);
                DrawLine(lines[index], _horizontalMargin, _contentWidth, _options.Alignment, index < lines.Count - 1);
                _y -= lineHeight;
            }
            _y -= after;
        }

        /// <summary>Renders a block quote.</summary>
        /// <param name="element">The quote element.</param>
        private void RenderQuote(IElement element)
        {
            var runs = new List<PdfTextRun>();
            AppendInlineRuns(element, runs, _options.FontSize, false, true, false, false);
            const double indent = 18d;
            var lines = WrapRuns(runs, _contentWidth - indent);
            var lineHeight = LineHeight(_options.FontSize);
            EnsureSpace((lines.Count * lineHeight) + _options.ParagraphSpacing + 8d);
            var startY = _y;
            for (var index = 0; index < lines.Count; index++)
            {
                DrawLine(lines[index], _horizontalMargin + indent, _contentWidth - indent, DocumentTextAlignment.Left, false);
                _y -= lineHeight;
            }
            _page.DrawLine(new PdfPoint(_horizontalMargin + 5d, startY + 3d), new PdfPoint(_horizontalMargin + 5d, _y + lineHeight), 2);
            _y -= 8d;
        }

        /// <summary>Renders ordered and unordered lists with stable indentation.</summary>
        /// <param name="element">The list element.</param>
        /// <param name="ordered">Whether the list is ordered.</param>
        /// <param name="level">The nesting level.</param>
        private void RenderList(IElement element, bool ordered, int level)
        {
            var index = 1;
            var indent = 14d + (level * 18d);
            foreach (var item in element.Children.Where(child => child.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase)))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var prefix = ordered ? $"{index.ToString(CultureInfo.InvariantCulture)}. " : "• ";
                var runs = new List<PdfTextRun> { new(prefix, _options.FontSize, false, false, false, false) };
                AppendInlineRuns(item, runs, _options.FontSize, false, false, false, false);
                var lines = WrapRuns(runs, _contentWidth - indent);
                for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    EnsureSpace(LineHeight(_options.FontSize));
                    DrawLine(lines[lineIndex], _horizontalMargin + indent, _contentWidth - indent, _options.Alignment, lineIndex < lines.Count - 1);
                    _y -= LineHeight(_options.FontSize);
                }
                index++;
                foreach (var nested in item.Children.Where(child => child.LocalName is "ul" or "ol"))
                {
                    RenderList(nested, nested.LocalName.Equals("ol", StringComparison.OrdinalIgnoreCase), level + 1);
                }
            }
            _y -= _options.ParagraphSpacing * 0.5d;
        }

        /// <summary>Renders a code block with wrapping instead of allowing text to escape the page.</summary>
        /// <param name="element">The code element.</param>
        private void RenderCodeBlock(IElement element)
        {
            var text = element.QuerySelector("code")?.TextContent ?? element.TextContent;
            const double lineHeight = 12d;
            foreach (var sourceLine in text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
            {
                var run = new PdfTextRun(sourceLine, 9, false, false, true, false);
                var lines = WrapRuns([run], _contentWidth - 10d);
                foreach (var line in lines)
                {
                    EnsureSpace(lineHeight + 6d);
                    _page.DrawRectangle(new PdfPoint(_horizontalMargin - 3d, _y - 3d), _contentWidth + 6d, lineHeight + 5d, 0.5d);
                    DrawLine(line, _horizontalMargin + 5d, _contentWidth - 10d, DocumentTextAlignment.Left, false);
                    _y -= lineHeight;
                }
            }
            _y -= 8d;
        }

        /// <summary>Renders table rows as wrapped text blocks so cell content can never overlap.</summary>
        /// <param name="element">The table element.</param>
        private void RenderTable(IElement element)
        {
            var rows = element.QuerySelectorAll("tr")
                .Select(row => row.QuerySelectorAll("th,td").Select(cell => NormalizeWhitespace(cell.TextContent)).ToArray())
                .Where(row => row.Length > 0)
                .ToArray();
            if (rows.Length == 0)
            {
                return;
            }

            foreach (var row in rows)
            {
                var text = string.Join(" | ", row);
                var runs = new List<PdfTextRun> { new(text, _options.FontSize - 1, row == rows[0], false, false, false) };
                var lines = WrapRuns(runs, _contentWidth - 8d);
                for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    EnsureSpace(LineHeight(_options.FontSize - 1) + 2d);
                    DrawLine(lines[lineIndex], _horizontalMargin + 4d, _contentWidth - 8d, _options.Alignment, lineIndex < lines.Count - 1);
                    _y -= LineHeight(_options.FontSize - 1);
                }
                _page.DrawLine(new PdfPoint(_horizontalMargin, _y + 3d), new PdfPoint(_horizontalMargin + _contentWidth, _y + 3d), 0.5d);
                _y -= 3d;
            }
            _y -= _options.ParagraphSpacing;
        }

        /// <summary>Renders a horizontal rule.</summary>
        private void RenderRule()
        {
            EnsureSpace(16d);
            _page.DrawLine(new PdfPoint(_horizontalMargin, _y), new PdfPoint(_horizontalMargin + _contentWidth, _y), 0.7d);
            _y -= 16d;
        }

        /// <summary>Adds inline descendants to styled text runs.</summary>
        /// <param name="element">The current element.</param>
        /// <param name="runs">The destination collection.</param>
        /// <param name="fontSize">The inherited font size.</param>
        /// <param name="bold">Whether bold is inherited.</param>
        /// <param name="italic">Whether italic is inherited.</param>
        /// <param name="code">Whether code styling is inherited.</param>
        /// <param name="link">Whether link styling is inherited.</param>
        private static void AppendInlineRuns(IElement element, List<PdfTextRun> runs, double fontSize, bool bold, bool italic, bool code, bool link)
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
                if (tag == "br")
                {
                    runs.Add(new PdfTextRun("\n", fontSize, bold, italic, code, link));
                    continue;
                }
                AppendInlineRuns(child, runs,
                    tag switch
                    {
                        "small" => Math.Max(8d, fontSize - 2d),
                        "sup" or "sub" => fontSize * 0.75d,
                        _ => fontSize
                    },
                    bold || tag is "strong" or "b",
                    italic || tag is "em" or "i",
                    code || tag == "code",
                    link || tag == "a");
            }
        }

        /// <summary>Wraps styled runs into lines that fit completely inside the content rectangle.</summary>
        /// <param name="runs">The runs to wrap.</param>
        /// <param name="maxWidth">The maximum line width.</param>
        /// <returns>The wrapped lines.</returns>
        private List<List<PdfTextRun>> WrapRuns(IEnumerable<PdfTextRun> runs, double maxWidth)
        {
            var result = new List<List<PdfTextRun>>();
            var current = new List<PdfTextRun>();
            var width = 0d;
            foreach (var run in runs)
            {
                foreach (var token in Tokenize(run.Text))
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    if (token == "\n")
                    {
                        TrimLineEnd(current);
                        AddLine(result, current);
                        current = [];
                        width = 0;
                        continue;
                    }
                    var tokenWidth = MeasureWidth(token, run.FontSize);
                    if (tokenWidth <= maxWidth && width > 0 && width + tokenWidth > maxWidth)
                    {
                        TrimLineEnd(current);
                        AddLine(result, current);
                        current = [];
                        width = 0;
                    }
                    if (tokenWidth <= maxWidth)
                    {
                        current.Add(run with { Text = token });
                        width += tokenWidth;
                        continue;
                    }
                    foreach (var part in SplitLongToken(token, run.FontSize, maxWidth))
                    {
                        var partWidth = MeasureWidth(part, run.FontSize);
                        if (width > 0 && width + partWidth > maxWidth)
                        {
                            TrimLineEnd(current);
                            AddLine(result, current);
                            current = [];
                            width = 0;
                        }
                        current.Add(run with { Text = part });
                        width += partWidth;
                    }
                }
            }
            TrimLineEnd(current);
            AddLine(result, current);
            return result;
        }

        /// <summary>Splits a single oversized token so long URLs and code cannot escape the page.</summary>
        /// <param name="token">The oversized token.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <returns>Width-safe token fragments.</returns>
        private IEnumerable<string> SplitLongToken(string token, double fontSize, double maxWidth)
        {
            var buffer = new StringBuilder();
            foreach (var character in token)
            {
                buffer.Append(character);
                if (buffer.Length > 1 && MeasureWidth(buffer.ToString(), fontSize) > maxWidth)
                {
                    buffer.Length--;
                    yield return buffer.ToString();
                    buffer.Clear();
                    buffer.Append(character);
                }
            }
            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
            }
        }

        /// <summary>Removes trailing whitespace from a line before alignment is calculated.</summary>
        /// <param name="line">The line to normalize.</param>
        private static void TrimLineEnd(List<PdfTextRun> line)
        {
            while (line.Count > 0 && string.IsNullOrWhiteSpace(line[^1].Text))
            {
                line.RemoveAt(line.Count - 1);
            }
        }

        /// <summary>Adds a non-empty line to the result.</summary>
        /// <param name="result">The line collection.</param>
        /// <param name="line">The line.</param>
        private static void AddLine(List<List<PdfTextRun>> result, List<PdfTextRun> line)
        {
            if (line.Count > 0)
            {
                result.Add(line);
            }
        }

        /// <summary>Draws one already-wrapped line using the requested alignment.</summary>
        /// <param name="line">The line runs.</param>
        /// <param name="left">The left content coordinate.</param>
        /// <param name="maxWidth">The content width.</param>
        /// <param name="alignment">The requested alignment.</param>
        /// <param name="justify">Whether extra width should be distributed across spaces.</param>
        private void DrawLine(List<PdfTextRun> line, double left, double maxWidth, DocumentTextAlignment alignment, bool justify)
        {
            var naturalWidth = line.Sum(run => MeasureWidth(run.Text, run.FontSize));
            var extra = Math.Max(0d, maxWidth - naturalWidth);
            var spaceCount = line.Count(run => IsWhitespaceRun(run.Text));
            var useJustification = alignment == DocumentTextAlignment.Justify && justify && spaceCount > 0;
            var spaceExtra = useJustification ? extra / spaceCount : 0d;
            var x = alignment switch
            {
                DocumentTextAlignment.Center => left + (extra / 2d),
                DocumentTextAlignment.Right => left + extra,
                _ => left
            };

            foreach (var run in line)
            {
                var width = MeasureWidth(run.Text, run.FontSize);
                if (x + width > left + maxWidth + 0.01d)
                {
                    throw new InvalidOperationException("Internal PDF layout error: a line exceeded its content width.");
                }
                DrawRun(run, x, _y);
                x += width;
                if (useJustification && IsWhitespaceRun(run.Text))
                {
                    x += spaceExtra;
                }
            }
        }

        /// <summary>Draws one styled run.</summary>
        /// <param name="run">The run.</param>
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
                    _page.AddText(run.Text, run.FontSize, new PdfPoint(x + 0.3d, y), _font);
                }
                if (run.Italic || run.Link)
                {
                    var underlineY = y - Math.Max(1d, run.FontSize * 0.12d);
                    _page.DrawLine(new PdfPoint(x, underlineY), new PdfPoint(x + MeasureWidth(run.Text, run.FontSize), underlineY), 0.4d);
                }
                if (run.Link)
                {
                    _page.ResetColor();
                }
            }
            catch (InvalidOperationException exception) when (TryGetUnsupportedCharacter(exception, out var character))
            {
                throw new UnsupportedFontCharacterException(_options.FontKey, character);
            }
        }

        /// <summary>Measures text using PdfPig font metrics.</summary>
        /// <param name="text">The text.</param>
        /// <param name="fontSize">The font size.</param>
        /// <returns>The measured width.</returns>
        private double MeasureWidth(string text, double fontSize) => string.IsNullOrEmpty(text)
            ? 0d
            : _page.MeasureText(text, fontSize, new PdfPoint(0, 0), _font).Sum(letter => letter.Width);

        /// <summary>Ensures the requested vertical space is available inside the top and bottom margins.</summary>
        /// <param name="requiredHeight">The required height.</param>
        private void EnsureSpace(double requiredHeight)
        {
            if (_y - requiredHeight >= _bottomMargin)
            {
                return;
            }
            _page = _builder.AddPage(PageWidth, PageHeight);
            _y = PageHeight - _topMargin;
        }

        /// <summary>Calculates the line height.</summary>
        /// <param name="fontSize">The font size.</param>
        /// <returns>The line height.</returns>
        private double LineHeight(double fontSize) => Math.Max(fontSize * _options.LineSpacing, fontSize * 1.15d);

        /// <summary>Tokenizes text into words, whitespace runs, and explicit line breaks.</summary>
        /// <param name="text">The source text.</param>
        /// <returns>The tokens used by the line wrapper.</returns>
        private static IEnumerable<string> Tokenize(string text)
        {
            var normalized = text.Replace("\r", string.Empty, StringComparison.Ordinal);
            var buffer = new StringBuilder();
            var whitespace = false;

            foreach (var character in normalized)
            {
                if (character == '\n')
                {
                    if (buffer.Length > 0)
                    {
                        yield return buffer.ToString();
                        buffer.Clear();
                    }
                    whitespace = false;
                    yield return "\n";
                    continue;
                }

                var isWhitespace = char.IsWhiteSpace(character);
                if (buffer.Length > 0 && isWhitespace != whitespace)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }

                if (isWhitespace)
                {
                    buffer.Append(' ');
                }
                else
                {
                    buffer.Append(character);
                }
                whitespace = isWhitespace;
            }

            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
            }
        }

        /// <summary>Determines whether a run consists only of whitespace.</summary>
        /// <param name="text">The run text.</param>
        /// <returns><see langword="true"/> when the run contains only whitespace.</returns>
        private static bool IsWhitespaceRun(string text) => text.Length > 0 && text.All(char.IsWhiteSpace);

        /// <summary>Converts millimetres to PDF points.</summary>
        /// <param name="millimetres">The value in millimetres.</param>
        /// <returns>The value in points.</returns>
        private static double MmToPoints(double millimetres) => millimetres * 72d / 25.4d;
    }

    /// <summary>Normalizes extracted or HTML whitespace.</summary>
    /// <param name="text">The source text.</param>
    /// <returns>Normalized text.</returns>
    private static string NormalizeWhitespace(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Validates PDF generation settings.</summary>
    /// <param name="options">The settings to validate.</param>
    private static void Validate(PdfConversionOptions options)
    {
        if (options.FontSize is < 8 or > 18) throw new ArgumentOutOfRangeException(nameof(options.FontSize));
        if (options.LineSpacing is < 1 or > 2.5) throw new ArgumentOutOfRangeException(nameof(options.LineSpacing));
        if (options.HorizontalMargin is < 5 or > 60) throw new ArgumentOutOfRangeException(nameof(options.HorizontalMargin));
        if (options.TopMargin is < 5 or > 60) throw new ArgumentOutOfRangeException(nameof(options.TopMargin));
        if (options.BottomMargin is < 5 or > 60) throw new ArgumentOutOfRangeException(nameof(options.BottomMargin));
        if (options.ParagraphSpacing is < 0 or > 24) throw new ArgumentOutOfRangeException(nameof(options.ParagraphSpacing));
        if (options.HeadingSpacing is < 0 or > 30) throw new ArgumentOutOfRangeException(nameof(options.HeadingSpacing));
    }

    /// <summary>Validates Markdown generation settings.</summary>
    /// <param name="options">The settings to validate.</param>
    private static void Validate(MarkdownConversionOptions options)
    {
        if (options.ParagraphSpacing is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(options.ParagraphSpacing));
    }

    /// <summary>Extracts the first unsupported character from a PdfPig exception.</summary>
    /// <param name="exception">The PdfPig exception.</param>
    /// <param name="character">The unsupported character.</param>
    /// <returns><see langword="true"/> when a character was found.</returns>
    private static bool TryGetUnsupportedCharacter(InvalidOperationException exception, out char character)
    {
        const string marker = "The font does not contain a character: ";
        var start = exception.Message.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0 || start + marker.Length >= exception.Message.Length)
        {
            character = default;
            return false;
        }
        character = exception.Message[start + marker.Length];
        return true;
    }
}
