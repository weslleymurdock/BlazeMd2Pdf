# Project Architecture

## Application

`src/BlazeMd2Pdf` is a .NET 10 Blazor WebAssembly application. MudBlazor 9.9.0 supplies the UI, while PdfPig 0.1.16 provides PDF parsing and writing.

## Service layer

`Services/Abstract/IConverterService.cs` defines the application conversion contract. `Services/Concrete/ConverterService.cs` implements Markdown reading, PDF text extraction, Markdown-to-PDF generation, and PDF-to-Markdown conversion.

The service intentionally works with `Stream` and `string` values so the UI remains responsible for browser file selection and downloading.

## Pages

- `/tomarkdown` accepts PDF files and displays extracted Markdown in a dialog.
- `/topdf` accepts Markdown files and generates a PDF, with the result presented through the PDF viewer dialog.

## Controls

- `Controls/MarkdownViewer.razor` is the Markdown preview dialog.
- `Controls/PdfViewer.razor` is the generated PDF result dialog.

## Dependencies

The application currently references AngleSharp 1.7.2, MarkItDown 0.0.1, ASP.NET Core Blazor WebAssembly 10.0.11, MudBlazor 9.9.0, MudBlazor FontIcons MaterialSymbols 1.5.0, MudBlazor.Markdown 9.0.0, and PdfPig 0.1.16.

Always verify APIs against these installed versions before changing implementation details.
