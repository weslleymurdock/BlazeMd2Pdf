using System.Text;
using BlazeMd2Pdf.Services.Abstract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Core;

namespace BlazeMd2Pdf.Services.Concrete;

/// <summary>
/// Provides local document conversion operations.
/// </summary>
public sealed class ConverterService : IConverterService
{
    /// <inheritdoc />
    public async Task<string> ReadMarkdownAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ReadPdfAsync(Stream stream, CancellationToken cancellationToken = default) =>
        ConvertPdfToMarkdownAsync(stream, cancellationToken);

    /// <inheritdoc />
    public Task<byte[]> ConvertMarkdownToPdfAsync(string markdown, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(BuildPdf(markdown, cancellationToken));
    }

    /// <inheritdoc />
    public Task<string> ConvertPdfToMarkdownAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return Task.FromResult(ExtractPdfText(stream, cancellationToken));
    }

    /// <summary>
    /// Extracts text from all pages of a PDF document in content order.
    /// </summary>
    /// <param name="stream">The stream containing the PDF document.</param>
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
    /// Creates a PDF containing the readable text represented by the Markdown source.
    /// </summary>
    /// <param name="markdown">The Markdown source.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated PDF bytes.</returns>
    private static byte[] BuildPdf(string markdown, CancellationToken cancellationToken)
    {
        var builder = new PdfDocumentBuilder();
        var regularFont = builder.AddStandard14Font(Standard14Font.Helvetica);
        var boldFont = builder.AddStandard14Font(Standard14Font.HelveticaBold);
        var page = builder.AddPage(PageSize.A4);
        const double margin = 50;
        const double lineHeight = 16;
        const double headingLineHeight = 22;
        var y = page.PageSize.Height - margin;

        foreach (var sourceLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = sourceLine.TrimEnd();
            var (text, fontSize, font, spacing) = ParseMarkdownLine(line, regularFont, boldFont);

            if (string.IsNullOrWhiteSpace(text))
            {
                y -= lineHeight;
                continue;
            }

            if (y < margin + spacing)
            {
                page = builder.AddPage(PageSize.A4);
                y = page.PageSize.Height - margin;
            }

            page.AddText(text, fontSize, new PdfPoint(margin, y), font);
            y -= spacing;
        }

        return builder.Build();
    }

    /// <summary>
    /// Converts one Markdown source line into PDF text and basic presentation settings.
    /// </summary>
    /// <param name="line">The Markdown source line.</param>
    /// <param name="regularFont">The regular PDF font.</param>
    /// <param name="boldFont">The bold PDF font.</param>
    /// <returns>The PDF text, font size, font, and line spacing.</returns>
    private static (string Text, double FontSize, PdfDocumentBuilder.AddedFont Font, double Spacing) ParseMarkdownLine(
        string line,
        PdfDocumentBuilder.AddedFont regularFont,
        PdfDocumentBuilder.AddedFont boldFont)
    {
        var trimmed = line.TrimStart();
        var headingLength = 0;

        while (headingLength < trimmed.Length && headingLength < 6 && trimmed[headingLength] == '#')
        {
            headingLength++;
        }

        if (headingLength > 0 && headingLength < trimmed.Length && char.IsWhiteSpace(trimmed[headingLength]))
        {
            var heading = trimmed[(headingLength + 1)..].Trim();
            return (heading, 16, boldFont, headingLineHeight);
        }

        var text = trimmed.StartsWith("> ", StringComparison.Ordinal)
            ? trimmed[2..]
            : trimmed;

        if (text.StartsWith("- ", StringComparison.Ordinal) ||
            text.StartsWith("* ", StringComparison.Ordinal) ||
            text.StartsWith("+ ", StringComparison.Ordinal))
        {
            text = $"- {text[2..]}";
        }

        return (text, 11, regularFont, lineHeight);
    }
}
