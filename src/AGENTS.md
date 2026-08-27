        # WCount AI Agent Guide

        Purpose: concise, actionable information an AI coding agent needs to be productive in this repository. Split into CLI and library guidance, then project/build/test conventions.

        ## Big picture
        WCount is composed of two kinds of projects under `src/`:
        - CLI: `WCountCli` — a small command-line front-end that parses arguments, resolves services via DI and delegates file/stdin processing to `TextReaderLogic`.
        - Libraries: `WCountLib.Abstractions` (interfaces) and `WCountLib` (implementations). Libraries are packaged separately and intended to be reusable.

        Dependency Graph: `WCountCli` $\rightarrow$ `WCountLib` $\rightarrow$ `WCountLib.Abstractions`.
        
        Typical data flow (concrete example): `Program.cs` (CLI) parses flags and files → resolves `ITextReaderLogic` from `WCountLib.Abstractions.Logic` → `TextReaderLogic.ReadTextReaderAsync` (in `WCountLib.Logic`) reads input in 8KB buffers (`char[8192]`) → for each chunk it calls counters (`IWordCounter`, `ICharacterCounter`, `IByteCounter`) → results aggregated into `WCountInfo` and printed by `ResultPrintingHelper`.

        ## CLI (WCountCli) – what agents must know
        - Entry point: `WCountCli/Program.cs`. DI registrations live here (singletons: `IWordCounter`, `ICharacterCounter`, `IByteCounter`, `ITextReaderLogic`). Prefer resolving interfaces, not concrete types.
        - Arguments: uses `System.CommandLine`. Examples from `Program.cs`: `-w` (words), `-l` (lines), `-m` (chars), `-c` (bytes), `-v` (verbose). When running manually forward CLI args after `--` when using `dotnet run`.
        - Text I/O: `TextReaderLogic` (in `lib/WCountLib/Logic/`) reads stdin via `Console.In` and files via `File.OpenText(...)`, passing counts to the CLI. Breakpoints for debugging: `TextReaderLogic.ReadTextReaderAsync` and `ReadTextChunk`.
        - Output formatting: `WCountCli/Helpers/ResultPrintingHelper.cs` and `FormattingHelpers.cs` control human-readable output and localization strings (`Localizations/Resources.resx`).

        ## Libraries (WCountLib and WCountLib.Abstractions)
        - Abstractions: `WCountLib.Abstractions` contains interfaces (e.g., `IWordCounter`, `ICharacterCounter`, `IByteCounter`) — implementations must remain compatible with the contracts.
        - Implementation notes:
          - `WordCounter` counts whitespace-separated tokens via `string.Split(null, StringSplitOptions.RemoveEmptyEntries)` to match classic `wc` behaviour. It does not use parallelism. See `WCountLib/Counters/WordCounter.cs`.
          - `TextReaderLogic` (in `WCountLib/Logic/`) performs chunked counting and handles platform line endings (CRLF vs LF) in `ReadTextChunk` — changes here affect cross-platform behavior. `WCountInfo` (in `WCountLib.Abstractions/Models/`) holds the counting results.

        ## Build, pack, run and test (commands agents should use)
        Use the dotnet CLI from the repository `src/` directory. Example commands (PowerShell):

        ```powershell
        # build everything under src
        dotnet build

        # build a single project
        dotnet build .\WCountCli\WCountCli.csproj

        # run the CLI (pass args after --)
        dotnet run --project .\WCountCli -- -w -l path\to\file.txt

        # produce NuGet package for the CLI (Release)
        dotnet pack .\WCountCli -c Release

        # run tests (if a tests/ folder exists next to src/ or project-specific tests)
        dotnet test
        dotnet test .\tests\MyProject.Tests\MyProject.Tests.csproj

        # test CLI using files from the test-files directory
        dotnet run --project .\WCountCli -- -w -l ..\test-files\<filename>.txt
        ```

        Notes:
        - Projects are organized as separate subfolders under `src/` (e.g., `WCountCli`, `lib\WCountLib`). If this repository also contains a `tests/` folder, tests are typically separated from `src/` and reference the projects under `src/`.
        - Test assets: The `test-files/` folder contains text files that should be used for testing WCount CLI and functionality.
        - If tests pass before you change code, your change must preserve that behavior. Do not modify test code to make behavior match; instead adjust implementation or add regressions-free tests.
        
        ## Conventions & gotchas
        - Central package management: `Directory.Packages.props` pins NuGet versions. Update versions there for cross-project consistency.
        - Global usings: `GlobalUsings.cs` files provide common imports for each project — add new global usings there rather than per-file.
        - Nullable/reference semantics: projects enable nullable (`<Nullable>enable</Nullable>`). Many aggregates use `long?` for optional counts (see `WCountInfo`).
        - Encoding & bytes: byte counting uses `Encoding.Default` (system encoding) by design; falling back to `UTF8` only if `Encoding.Default` throws (`NotSupportedException` / `ArgumentException`). `TextReaderLogic.ResolveDefaultEncoding` centralises this policy.
        - Parallel patterns: `WordCounter` counts tokens via `string.Split` (single-threaded, no shared counter). When editing, preserve wc-compatible token semantics.
        - Line ending logic: `ReadTextChunk` tracks a `hasCharWasCR` flag to detect CRLF sequences on Windows — be careful when refactoring this logic.
        - External Libraries: 
          - In `WCountCli`, external libraries (e.g. `System.CommandLine`) are necessary and acceptable.
          - In `WCountLib`, external libraries should provide significant, non-trivial utility. If a dependency provides zero or negligible utility, agents should suggest its removal to the user.
        
        ## When you edit code — recommended checklist for agents
        1. Run `dotnet build` at `src/` and fix compilation issues.
        2. Run `dotnet test` (if tests exist). If tests fail and they passed before, investigate the implementation change; do not change tests to make them pass.
        3. Run the CLI on various files in `..\test-files\` (e.g. for edge cases like empty files, different line endings, or large words) to verify output formatting and totals. Example:

        ```powershell
        dotnet run --project .\WCountCli -- -w -l ..\test-files\<filename>.txt
        ```
        
        4. If you change public API surfaces (abstractions), update dependent projects and consider package versioning.

        ## Key files to inspect first
        - `WCountCli/Program.cs` — DI registration and argument handling
        - `lib/WCountLib/Logic/TextReaderLogic.cs` — chunked reading, encoding and line handling
        - `lib/WCountLib.Abstractions/Models/WCountInfo.cs` — counting result model
        - `lib/WCountLib.Abstractions/Logic/ITextReaderLogic.cs` — counting interface
        - `lib/WCountLib/Counters/WordCounter.cs` — wc-token counting
        - `Directory.Packages.props` — central package versions
        - `GlobalUsings.cs` files — shared imports per project

        Keep this file concise: prefer linking specific files above when you need to change behavior. If you add repo-level policies (formatting, testing), reflect them here so future agents follow the same checks.