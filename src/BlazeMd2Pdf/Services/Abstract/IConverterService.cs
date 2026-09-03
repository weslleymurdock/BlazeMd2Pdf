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
    /// Reads text from the specified PDF stream.
    /// </summary>
    /// <param name="stream">The stream containing PDF content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extracted text.</returns>
    Task<string> ReadPdfAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts Markdown content to a PDF document.
    /// </summary>
    /// <param name="markdown">The Markdown content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated PDF bytes.</returns>
    Task<byte[]> ConvertMarkdownToPdfAsync(string markdown, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts PDF content to Markdown content.
    /// </summary>
    /// <param name="stream">The stream containing PDF content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The converted Markdown content.</returns>
    Task<string> ConvertPdfToMarkdownAsync(Stream stream, CancellationToken cancellationToken = default);
}
