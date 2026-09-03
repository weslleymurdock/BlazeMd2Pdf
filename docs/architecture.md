# Project Architecture

## Application

`src/BlazeMd2Pdf` is a .NET 10 Blazor WebAssembly application. MudBlazor 9.9.0 supplies the UI, while PdfPig 0.1.16 provides PDF parsing and writing.

## Service layer

`Services/Abstract/IConverterService.cs` defines the application conversion contract. `Services/Concrete/ConverterService.cs` implements Markdown reading, PDF text extraction, Markdown-to-PDF generation, and PDF-to-Markdown conversion.

Browser file streams are copied asynchronously into memory before PdfPig opens a PDF. PdfPig's PDF parser is synchronous, so this avoids synchronous reads directly against the browser-provided stream while preserving cancellation during the asynchronous copy.

Markdown-to-PDF uses embedded TrueType font bytes loaded asynchronously from open document-font distributions. The available families are Liberation Sans (metric-compatible with Arial), Liberation Serif (metric-compatible with Times New Roman), Liberation Mono (metric-compatible with Courier New), and Noto Sans for broader Unicode coverage. The UI exposes the font selector when PdfPig reports that the selected font lacks a source character.

## Pages

- `/tomarkdown` accepts PDF files and displays extracted Markdown in a dialog.
- `/topdf` accepts Markdown files and generates a PDF, with the result presented through the PDF viewer dialog.

## Controls

- `Controls/MarkdownViewer.razor` is the Markdown preview dialog.
- `Controls/PdfViewer.razor` is the generated PDF result dialog.

## Font licensing

The Liberation font family is distributed under the SIL Open Font License. The converter retrieves the published font files at runtime rather than bundling proprietary Microsoft font files such as Arial or Times New Roman. This keeps the application distributable without redistributing proprietary fonts.

Noto Sans is provided as the broader-Unicode fallback. No font can guarantee every Unicode character, so the converter reports unsupported characters instead of silently replacing them.

## Dependencies

The application currently references AngleSharp 1.7.2, MarkItDown 0.0.1, ASP.NET Core Blazor WebAssembly 10.0.11, MudBlazor 9.9.0, MudBlazor FontIcons MaterialSymbols 1.5.0, MudBlazor.Markdown 9.0.0, and PdfPig 0.1.16.

Always verify APIs against these installed versions before changing implementation details.
