namespace BlazeMd2Pdf.Services.Abstract;

/// <summary>
/// Describes a document font available to the PDF converter.
/// </summary>
/// <param name="Key">The stable font identifier.</param>
/// <param name="DisplayName">The user-facing font name.</param>
public sealed record DocumentFont(string Key, string DisplayName);

/// <summary>
/// Defines presentation options used when generating a PDF from Markdown.
/// </summary>
/// <param name="FontKey">The font identifier.</param>
/// <param name="FontSize">The body font size in points.</param>
/// <param name="LineSpacing">The line-height multiplier.</param>
/// <param name="PageMargin">The page margin in millimetres.</param>
/// <param name="ParagraphSpacing">The spacing after paragraphs in points.</param>
/// <param name="HeadingSpacing">The spacing around headings in points.</param>
/// <param name="KeepHeadingWithNext">Whether a heading should remain with its following content when possible.</param>
public sealed record PdfConversionOptions(
    string FontKey = "liberation-sans",
    double FontSize = 11,
    double LineSpacing = 1.35,
    double PageMargin = 20,
    double ParagraphSpacing = 8,
    double HeadingSpacing = 10,
    bool KeepHeadingWithNext = true);

/// <summary>
/// Defines presentation options used when generating Markdown from a PDF.
/// </summary>
/// <param name="PreservePageBreaks">Whether page boundaries are represented in the Markdown output.</param>
/// <param name="ParagraphSpacing">The number of blank lines between detected paragraphs.</param>
/// <param name="PreserveLineBreaks">Whether line breaks extracted from the PDF are retained.</param>
public sealed record MarkdownConversionOptions(
    bool PreservePageBreaks = true,
    int ParagraphSpacing = 1,
    bool PreserveLineBreaks = false);

/// <summary>
/// Indicates that the selected PDF font cannot render a character from the source document.
/// </summary>
public sealed class UnsupportedFontCharacterException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedFontCharacterException"/> class.
    /// </summary>
    /// <param name="font">The font that could not render the character.</param>
    /// <param name="character">The unsupported character.</param>
    public UnsupportedFontCharacterException(string font, char character)
        : base($"The font '{font}' does not contain the character '{character}' (U+{(int)character:X4}).")
    {
        Font = font;
        Character = character;
    }

    /// <summary>
    /// Gets the font that could not render the character.
    /// </summary>
    public string Font { get; }

    /// <summary>
    /// Gets the unsupported character.
    /// </summary>
    public char Character { get; }
}

/// <summary>
/// Provides document reading and conversion operations supported by the application.
/// </summary>
public interface IConverterService
{
    /// <summary>
    /// Gets the fonts available for PDF generation.
    /// </summary>
    IReadOnlyList<DocumentFont> AvailableFonts { get; }

    /// <summary>
    /// Reads Markdown content from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing Markdown content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Markdown content.</returns>
    Task<string> ReadMarkdownAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads text from the specified PDF stream.
    /// </summary>
    /// <param name="stream">The stream containing PDF content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extracted text.</returns>
    Task<string> ReadPdfAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts Markdown content to a PDF document using the supplied presentation options.
    /// </summary>
    /// <param name="markdown">The Markdown content.</param>
    /// <param name="options">The PDF presentation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated PDF bytes.</returns>
    Task<byte[]> ConvertMarkdownToPdfAsync(string markdown, PdfConversionOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts PDF content to Markdown using the supplied presentation options.
    /// </summary>
    /// <param name="stream">The stream containing PDF content.</param>
    /// <param name="options">The Markdown presentation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The converted Markdown content.</returns>
    Task<string> ConvertPdfToMarkdownAsync(Stream stream, MarkdownConversionOptions options, CancellationToken cancellationToken = default);
}
