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
public sealed class PdfConversionOptions
{
    /// <summary>Gets or sets the font identifier.</summary>
    public string FontKey { get; set; } = "liberation-sans";

    /// <summary>Gets or sets the body font size in points.</summary>
    public double FontSize { get; set; } = 11;

    /// <summary>Gets or sets the line-height multiplier.</summary>
    public double LineSpacing { get; set; } = 1.35;

    /// <summary>Gets or sets the page margin in millimetres.</summary>
    public double PageMargin { get; set; } = 20;

    /// <summary>Gets or sets the spacing after paragraphs in points.</summary>
    public double ParagraphSpacing { get; set; } = 8;

    /// <summary>Gets or sets the spacing around headings in points.</summary>
    public double HeadingSpacing { get; set; } = 10;

    /// <summary>Gets or sets whether headings should remain with following content when possible.</summary>
    public bool KeepHeadingWithNext { get; set; } = true;
}

/// <summary>
/// Defines presentation options used when generating Markdown from a PDF.
/// </summary>
public sealed class MarkdownConversionOptions
{
    /// <summary>Gets or sets whether page boundaries are represented in the Markdown output.</summary>
    public bool PreservePageBreaks { get; set; } = true;

    /// <summary>Gets or sets the number of blank lines between detected paragraphs.</summary>
    public int ParagraphSpacing { get; set; } = 1;

    /// <summary>Gets or sets whether extracted line breaks are retained.</summary>
    public bool PreserveLineBreaks { get; set; }
}

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

    /// <summary>Gets the font that could not render the character.</summary>
    public string Font { get; }

    /// <summary>Gets the unsupported character.</summary>
    public char Character { get; }
}

/// <summary>
/// Provides document reading and conversion operations supported by the application.
/// </summary>
public interface IConverterService
{
    /// <summary>Gets the fonts available for PDF generation.</summary>
    IReadOnlyList<DocumentFont> AvailableFonts { get; }

    /// <summary>Reads Markdown content from the specified stream.</summary>
    /// <param name="stream">The stream containing Markdown content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Markdown content.</returns>
    Task<string> ReadMarkdownAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>Reads text from the specified PDF stream.</summary>
    /// <param name="stream">The stream containing PDF content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extracted text.</returns>
    Task<string> ReadPdfAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>Converts Markdown content to a PDF using the supplied presentation options.</summary>
    /// <param name="markdown">The Markdown content.</param>
    /// <param name="options">The PDF presentation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated PDF bytes.</returns>
    Task<byte[]> ConvertMarkdownToPdfAsync(string markdown, PdfConversionOptions options, CancellationToken cancellationToken = default);

    /// <summary>Converts PDF content to Markdown using the supplied presentation options.</summary>
    /// <param name="stream">The stream containing PDF content.</param>
    /// <param name="options">The Markdown presentation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The converted Markdown content.</returns>
    Task<string> ConvertPdfToMarkdownAsync(Stream stream, MarkdownConversionOptions options, CancellationToken cancellationToken = default);
}
