namespace BlazeMd2Pdf.Services.Abstract;

/// <summary>Defines browser-based export operations for rendered Markdown documents.</summary>
public interface IMarkdownExportService
{
    /// <summary>Exports the supplied rendered Markdown element to a downloadable file.</summary>
    /// <param name="element">The rendered Markdown element.</param>
    /// <param name="format">The requested output format.</param>
    /// <param name="fileName">The output file name.</param>
    /// <param name="options">The export presentation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ExportAsync(
        object element,
        MarkdownExportFormat format,
        string fileName,
        MarkdownExportOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>Defines the output formats supported by the Markdown export pipeline.</summary>
public enum MarkdownExportFormat
{
    /// <summary>Exports the rendered document as PDF.</summary>
    Pdf,
    /// <summary>Exports the rendered document as HTML.</summary>
    Html,
    /// <summary>Exports the rendered document as PNG.</summary>
    Png,
    /// <summary>Exports the rendered document as JPEG.</summary>
    Jpeg
}
