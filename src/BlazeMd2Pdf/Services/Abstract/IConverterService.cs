namespace BlazeMd2Pdf.Services.Abstract;

/// <summary>
/// Describes a document font available to the PDF converter.
/// </summary>
/// <param name="Key">The stable font identifier.</param>
/// <param name="DisplayName">The user-facing font name.</param>
public sealed record DocumentFont(string Key, string DisplayName);

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
    /// Converts Markdown content to a PDF document using the selected font.
    /// </summary>
    /// <param name="markdown">The Markdown content.</param>
    /// <param name="fontKey">The key of the font to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated PDF bytes.</returns>
    Task<byte[]> ConvertMarkdownToPdfAsync(string markdown, string fontKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts PDF content to Markdown content.
    /// </summary>
    /// <param name="stream">The stream containing PDF content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The converted Markdown content.</returns>
    Task<string> ConvertPdfToMarkdownAsync(Stream stream, CancellationToken cancellationToken = default);
}
