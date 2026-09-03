# BlazeMd2Pdf Agent Guide

## Project

BlazeMd2Pdf is a .NET 10 Blazor WebAssembly application for converting and rendering documents around Markdown. The current product goal is a client-only application suitable for static hosting such as GitHub Pages.

The current conversion workflows are:

- PDF to Markdown through the existing conversion service.
- Markdown to PDF through the browser-rendered Markdown export surface.
- Markdown to HTML through the browser-rendered Markdown export surface.
- Markdown to PNG and JPEG through browser rasterization.

## Global rules

- Target .NET 10 and follow current .NET best practices.
- Prefer primary constructors when they improve clarity and are supported by the surrounding design.
- Use async/await correctly and propagate CancellationToken through asynchronous operations.
- Do not invent APIs, component parameters, methods, or library behavior. Verify the installed package versions in `src/BlazeMd2Pdf/BlazeMd2Pdf.csproj` and consult their official documentation before implementation.
- Use MudBlazor components for application pages and controls.
- Do not add unsupported Blazor attributes or parameters that produce compiler warnings.
- Keep the build clean: zero errors, zero warnings, and zero compiler messages where practical.
- All implementation code must have XML documentation in en-US, including appropriate `<summary>`, `<param>`, `<returns>`, `<exception>`, and related tags when applicable.
- Keep the core conversion and export work client-side so the application remains deployable as a static GitHub Pages site.
- Validate uploaded file extensions at the UI boundary before conversion.
- Keep commits small and focused. Do not push intermediate commits when a task explicitly requests a single final push.

## UI conventions

- Pages live under `src/BlazeMd2Pdf/Pages`.
- Reusable UI and rendering components live under `src/BlazeMd2Pdf/Controls`.
- `MudDialogProvider` is registered by the main layout; dialog components should use the MudBlazor APIs documented for the installed version.
- Keep format-specific pages thin. Shared Markdown upload, preview, options, and export behavior belongs in `Pages/MarkdownExportPage.razor`.

## Markdown rendering and export

- `Controls/MarkdownExportSurface.razor` is the canonical rendered Markdown DOM used for Markdown export.
- The intended pipeline is `Markdown -> MudBlazor.Markdown -> browser DOM/CSS -> HTML, PDF, PNG, or JPEG`.
- Do not reintroduce manual PDF text positioning for Markdown export unless the architecture is explicitly changed.
- `Services/Abstract/IMarkdownExportService.cs` defines the .NET export abstraction and `Services/Concrete/MarkdownExportService.cs` bridges it to browser JavaScript.
- `wwwroot/js/download.js` contains the browser-side export implementation.
- `wwwroot/css/markdown-export.css` contains the shared Markdown document styling used by the preview and exports.
- The current PDF export uses `html2pdf.js` and is intentionally browser-based. It is not a server-side Chromium `page.pdf()` implementation.
- Exact parity with the VS Code Markdown PDF extension's Chromium rendering would require a server-side browser renderer and must not be introduced as an implicit dependency of the GitHub Pages build.

## Conversion service

- The abstraction is `IConverterService` under `Services/Abstract`.
- The implementation is `ConverterService` under `Services/Concrete`.
- The service is registered in the DI container in `Program.cs`.
- Preserve stream ownership rules: callers own streams they pass to the service unless documentation explicitly states otherwise; returned streams are owned by the caller.
- Honor cancellation before and during potentially expensive conversion work.
- PdfPig parsing is synchronous, so browser-provided PDF streams must be copied asynchronously into memory before parsing.

## Dependencies

The main application uses ASP.NET Core Blazor WebAssembly 10.0.11, MudBlazor 9.9.0, MudBlazor FontIcons MaterialSymbols 1.5.0, MudBlazor.Markdown 9.0.0, AngleSharp 1.7.2, and PdfPig 0.1.16. Browser PDF/image export also uses `html2pdf.js` loaded from `wwwroot/index.html`.

Do not add the old `MarkItDown` package merely to implement Markdown-to-PDF. A document-to-Markdown engine can be considered later if additional input formats require it.

## Future integrations

An MCP integration may be added as an optional capability in a future goal. It must preserve the client-only/static-hosting baseline: the application must continue to work without an MCP server. MCP connectivity should be abstracted behind a dedicated service boundary rather than coupling the UI or conversion pipeline directly to Docker or a particular MCP implementation.

## Verification

The CI workflow uses .NET 10 and builds `BlazeMd2Pdf.slnx` in Release mode. Before considering implementation changes complete, run the equivalent build locally and inspect the GitHub Actions result after pushing.
