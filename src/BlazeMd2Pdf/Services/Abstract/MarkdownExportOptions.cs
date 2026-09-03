namespace BlazeMd2Pdf.Services.Abstract;

/// <summary>Defines the presentation options used by browser-based Markdown exports.</summary>
public sealed class MarkdownExportOptions
{
    /// <summary>Gets or sets the page format used by PDF export.</summary>
    public string PageFormat { get; set; } = "a4";

    /// <summary>Gets or sets the page orientation used by PDF export.</summary>
    public string Orientation { get; set; } = "portrait";

    /// <summary>Gets or sets the top page margin in millimetres.</summary>
    public double MarginTop { get; set; } = 20;

    /// <summary>Gets or sets the right page margin in millimetres.</summary>
    public double MarginRight { get; set; } = 20;

    /// <summary>Gets or sets the bottom page margin in millimetres.</summary>
    public double MarginBottom { get; set; } = 20;

    /// <summary>Gets or sets the left page margin in millimetres.</summary>
    public double MarginLeft { get; set; } = 20;

    /// <summary>Gets or sets the body font size in pixels.</summary>
    public double FontSize { get; set; } = 14;

    /// <summary>Gets or sets the CSS line-height multiplier.</summary>
    public double LineHeight { get; set; } = 1.6;

    /// <summary>Gets or sets the body font family.</summary>
    public string FontFamily { get; set; } = "Arial, Helvetica, sans-serif";

    /// <summary>Gets or sets the paragraph text alignment.</summary>
    public DocumentTextAlignment Alignment { get; set; } = DocumentTextAlignment.Left;

    /// <summary>Gets or sets the image quality used by PNG and JPEG export.</summary>
    public double ImageQuality { get; set; } = 0.95;

    /// <summary>Gets or sets the rasterization scale used by image and PDF export.</summary>
    public double Scale { get; set; } = 2;
}
