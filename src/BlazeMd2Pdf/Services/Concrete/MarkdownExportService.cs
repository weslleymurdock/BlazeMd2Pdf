using BlazeMd2Pdf.Services.Abstract;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazeMd2Pdf.Services.Concrete;

/// <summary>Exports rendered Markdown through the browser's HTML, canvas, and PDF rendering engines.</summary>
public sealed class MarkdownExportService(IJSRuntime jsRuntime) : IMarkdownExportService
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    /// <inheritdoc />
    public Task ExportAsync(
        ElementReference element,
        MarkdownExportFormat format,
        string fileName,
        MarkdownExportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        cancellationToken.ThrowIfCancellationRequested();

        return _jsRuntime.InvokeVoidAsync(
            "blazeMd2Pdf.exportMarkdown",
            cancellationToken,
            element,
            format.ToString().ToLowerInvariant(),
            fileName,
            options).AsTask();
    }
}
