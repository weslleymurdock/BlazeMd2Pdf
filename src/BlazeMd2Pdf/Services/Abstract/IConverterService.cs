namespace BlazeMd2Pdf.Services.Abstract;

/// <summary>
/// Provides document reading and conversion operations supported by the application.
/// </summary>
public interface IConverterService
{
    /// <summary>
    /// Reads Markdown content from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing Markdown content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Markdown content.</returns>
    Task<string> ReadMarkdownAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads PDF content and converts its text to Markdown.
    /// </summary>
    /// <param name="stream">The stream containing PDF content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extracted Markdown content.</returns>
    Task<string> ReadPdfAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts Markdown content to a PDF document.
    /// </summary>
    /// <param name="markdown">The Markdown content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream containing the generated PDF.</returns>
    Task<Stream> ConvertMarkdownToPdfAsync(string markdown, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts PDF content to Markdown content.
    /// </summary>
    /// <param name="stream">The stream containing PDF content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The converted Markdown content.</returns>
    Task<string> ConvertPdfToMarkdownAsync(Stream stream, CancellationToken cancellationToken = default);
}
