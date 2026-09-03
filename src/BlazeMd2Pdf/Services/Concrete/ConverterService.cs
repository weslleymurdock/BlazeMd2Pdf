using System.Text;
using System.Text.RegularExpressions;
using BlazeMd2Pdf.Services.Abstract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Writer;

namespace BlazeMd2Pdf.Services.Concrete;

/// <summary>
/// Provides local, client-side document conversion operations.
/// </summary>
public sealed partial class ConverterService : Abstract.IConverterService
{
    /// <inheritdoc />
    public async Task<string> ReadMarkdownAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return await ReadTextAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ReadPdfAsync(Stream stream, CancellationToken cancellationToken = default) =>
        ConvertPdfToMarkdownAsync(stream, cancellationToken);

    /// <inheritdoc />
    public async Task<Stream> ConvertMarkdownToPdfAsync(string markdown, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        cancellationToken.ThrowIfCancellationRequested();

        var output = new MemoryStream();
        await Task.Run(() => BuildPdf(markdown, output, cancellationToken), cancellationToken);
        output.Position = 0;
        return output;
    }

    /// <inheritdoc />
    public async Task<string> ConvertPdfToMarkdownAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        if (stream.CanSeek)
            stream.Position = 0;

        return await Task.Run(() => ExtractPdfText(stream, cancellationToken), cancellationToken);
    }

    private static async Task<string> ReadTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string ExtractPdfText(Stream stream, CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var line in GetLines(page))
                builder.AppendLine(line);
            builder.AppendLine();
        }

        return builder.ToString().Trim();
    }

    private static IEnumerable<string> GetLines(Page page)
    {
        var words = page.GetWords().ToList();
        var lines = new List<(double Y, List<Word> Words)>();

        foreach (var word in words.OrderByDescending(x => x.BoundingBox.Bottom))
        {
            var line = lines.FirstOrDefault(x => Math.Abs(x.Y - word.BoundingBox.Bottom) <= 2);
            if (line.Words is null)
                lines.Add((word.BoundingBox.Bottom, [word]));
            else
                line.Words.Add(word);
        }

        return lines.Select(x => string.Join(" ", x.Words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
    }

    private static void BuildPdf(string markdown, Stream output, CancellationToken cancellationToken)
    {
        using var builder = new PdfDocumentBuilder(output, disposeStream: false);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);
        const double margin = 50;
        const double fontSize = 11;
        const double lineHeight = 16;
        var y = page.PageSize.Height - margin;

        foreach (var sourceLine in MarkdownLineRegex().Split(markdown.Replace("\r\n", "\n")))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = sourceLine.TrimEnd();
            var text = MarkdownSyntaxRegex().Replace(line, string.Empty);
            if (string.IsNullOrWhiteSpace(text))
            {
                y -= lineHeight;
                continue;
            }

            if (y < margin)
            {
                page = builder.AddPage(PageSize.A4);
                y = page.PageSize.Height - margin;
            }

            page.AddText(text, fontSize, new PdfPoint(margin, y), font);
            y -= lineHeight;
        }

        builder.Build();
    }

    [GeneratedRegex("\\n")]
    private static partial Regex MarkdownLineRegex();

    [GeneratedRegex("^\\s{0,3}(#{1,6}\\s+|[-*+]\\s+|\\d+[.)]\\s+|>\\s+|`{1,3}|[*_~])|[*_~]+$")]
    private static partial Regex MarkdownSyntaxRegex();
}
