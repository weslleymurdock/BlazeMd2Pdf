using System.Text;
using BlazeMd2Pdf.Services.Abstract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Core;

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
    /// Creates a PDF containing the readable text represented by the Markdown source.
    /// </summary>
    /// <param name="markdown">The Markdown source.</param>
    /// <param name="fontBytes">The TrueType font bytes.</param>
    /// <param name="fontKey">The selected font identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated PDF bytes.</returns>
    private static byte[] BuildPdf(
        string markdown,
        byte[] fontBytes,
        string fontKey,
        CancellationToken cancellationToken)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddTrueTypeFont(fontBytes);
        var page = builder.AddPage(595d, 842d);
        const double margin = 50d;
        const double lineHeight = 16d;
        var y = page.PageSize.Height - margin;

        foreach (var sourceLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = NormalizeMarkdownLine(sourceLine);
            if (string.IsNullOrWhiteSpace(text))
            {
                y -= lineHeight;
                continue;
            }

            if (y < margin + lineHeight)
            {
                page = builder.AddPage(595d, 842d);
                y = page.PageSize.Height - margin;
            }

            try
            {
                page.AddText(text, 11, new PdfPoint(margin, y), font);
            }
            catch (InvalidOperationException exception) when (TryGetUnsupportedCharacter(exception, out var character))
            {
                throw new UnsupportedFontCharacterException(fontKey, character);
            }

            y -= lineHeight;
        }

        return builder.Build();
    }

    /// <summary>
    /// Removes Markdown-only line syntax while preserving readable text.
    /// </summary>
    /// <param name="line">The Markdown source line.</param>
    /// <returns>The text that should be written to the PDF.</returns>
    private static string NormalizeMarkdownLine(string line)
    {
        var text = line.Trim();

        while (text.StartsWith('#'))
        {
            text = text[1..].TrimStart();
        }

        if (text.StartsWith("> ", StringComparison.Ordinal))
        {
            text = text[2..];
        }

        if (text.StartsWith("- ", StringComparison.Ordinal) ||
            text.StartsWith("* ", StringComparison.Ordinal) ||
            text.StartsWith("+ ", StringComparison.Ordinal))
        {
            text = $"• {text[2..]}";
        }

        return text;
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
