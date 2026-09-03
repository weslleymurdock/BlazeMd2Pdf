namespace BlazeMd2Pdf.Services.Abstract;

/// <summary>Describes a document font available to the PDF converter.</summary>
/// <param name="Key">The stable font identifier.</param>
/// <param name="DisplayName">The user-facing font name.</param>
public sealed record DocumentFont(string Key, string DisplayName);

/// <summary>Defines supported horizontal text alignment modes.</summary>
public enum DocumentTextAlignment
{
    /// <summary>Aligns text to the left content edge.</summary>
    Left,
    /// <summary>Centers text inside the content area.</summary>
    Center,
    /// <summary>Aligns text to the right content edge.</summary>
    Right,
    /// <summary>Expands non-final paragraph lines to both content edges.</summary>
    Justify
}

/// <summary>Defines presentation options used when generating a PDF from Markdown.</summary>
public sealed class PdfConversionOptions
{
    /// <summary>Gets or sets the font identifier.</summary>
    public string FontKey { get; set; } = "liberation-sans";
    /// <summary>Gets or sets the body font size in points.</summary>
    public double FontSize { get; set; } = 11;
    /// <summary>Gets or sets the line-height multiplier.</summary>
    public double LineSpacing { get; set; } = 1.35;
    /// <summary>Gets or sets the horizontal left and right margin in millimetres.</summary>
    public double HorizontalMargin { get; set; } = 20;
    /// <summary>Gets or sets the top margin in millimetres.</summary>
    public double TopMargin { get; set; } = 20;
    /// <summary>Gets or sets the bottom margin in millimetres.</summary>
    public double BottomMargin { get; set; } = 20;
    /// <summary>Gets or sets the paragraph spacing in points.</summary>
    public double ParagraphSpacing { get; set; } = 8;
    /// <summary>Gets or sets the heading spacing in points.</summary>
    public double HeadingSpacing { get; set; } = 10;
    /// <summary>Gets or sets the paragraph text alignment.</summary>
    public DocumentTextAlignment Alignment { get; set; } = DocumentTextAlignment.Justify;
    /// <summary>Gets or sets whether headings should remain with following content when possible.</summary>
    public bool KeepHeadingWithNext { get; set; } = true;

    /// <summary>Gets or sets the legacy page margin used by older callers.</summary>
    public double PageMargin
    {
        get => HorizontalMargin;
        set => HorizontalMargin = value;
    }
}

/// <summary>Defines presentation options used when generating Markdown from a PDF.</summary>
public sealed class MarkdownConversionOptions
{
    /// <summary>Gets or sets whether page boundaries are represented in the Markdown output.</summary>
    public bool PreservePageBreaks { get; set; } = true;
    /// <summary>Gets or sets the number of blank lines between detected paragraphs.</summary>
    public int ParagraphSpacing { get; set; } = 1;
    /// <summary>Gets or sets whether extracted visual line breaks are retained.</summary>
    public bool PreserveLineBreaks { get; set; } = true;
    /// <summary>Gets or sets whether HTML alignment metadata is emitted to preserve PDF alignment.</summary>
    public bool PreserveAlignment { get; set; } = true;
}

/// <summary>Indicates that the selected PDF font cannot render a character from the source document.</summary>
public sealed class UnsupportedFontCharacterException : InvalidOperationException
{
    /// <summary>Initializes an instance of the exception.</summary>
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

/// <summary>Provides document reading and conversion operations supported by the application.</summary>
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
