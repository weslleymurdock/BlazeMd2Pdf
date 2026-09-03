# BlazeMd2Pdf Agent Guide

## Project

BlazeMd2Pdf is a .NET 10 Blazor WebAssembly application that converts documents between Markdown and PDF. The first conversion goal is PDF to Markdown; Markdown to PDF is also exposed by the UI.

## Global rules

- Target .NET 10 and follow current .NET best practices.
- Prefer primary constructors when they improve clarity and are supported by the surrounding design.
- Use async/await correctly and propagate CancellationToken through asynchronous operations.
- Do not invent APIs, component parameters, methods, or library behavior. Verify the installed package versions in `src/BlazeMd2Pdf/BlazeMd2Pdf.csproj` and consult their official documentation before implementation.
- Use MudBlazor components for application pages and controls.
- Do not add unsupported Blazor attributes or parameters that produce compiler warnings.
- Keep the build clean: zero errors, zero warnings, and zero compiler messages where practical.
- All implementation code must have XML documentation in en-US, including appropriate `<summary>`, `<param>`, `<returns>`, `<exception>`, and related tags when applicable.
- Keep conversion work client-side unless the architecture is explicitly changed.
- Validate uploaded file extensions at the UI boundary before conversion.
- Keep commits small and focused. Do not push intermediate commits when a task explicitly requests a single final push.

## UI conventions

- Pages live under `src/BlazeMd2Pdf/Pages`.
- Reusable dialog components live under `src/BlazeMd2Pdf/Controls`.
- `MudDialogProvider` is registered by the main layout; dialog components should use the MudBlazor dialog APIs documented for the installed version.

## Conversion service

- The abstraction is `IConverterService` under `Services/Abstract`.
- The implementation is `ConverterService` under `Services/Concrete`.
- The service is registered in the DI container in `Program.cs`.
- Preserve stream ownership rules: callers own streams they pass to the service unless documentation explicitly states otherwise; returned streams are owned by the caller.
- Honor cancellation before and during potentially expensive conversion work.

## Verification

The CI workflow uses .NET 10 and builds `BlazeMd2Pdf.slnx` in Release mode. Before considering a change complete, run the equivalent build locally and inspect the GitHub Actions result after pushing.
