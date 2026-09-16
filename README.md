# Senice

Senice is a desktop tool for Windows that strips comments and emoji from source files — without changing program behavior, formatting, encoding, or line endings.

It uses [Tree-sitter](https://tree-sitter.github.io/) grammars to locate comments and strings precisely, with a safe lexical fallback for any language without a grammar. The result reads like hand-written, clean production code: comment bodies are removed while newlines, indentation, shebangs, BOM, and `\r\n`/`\n` endings are preserved.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![C#](https://img.shields.io/badge/C%23-12-239120)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D6)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Precise comment removal** via Tree-sitter grammars for 30+ languages (C#, C/C++, Java, JavaScript, TypeScript, Python, Go, Rust, PHP, Ruby, HTML, CSS and more).
- **Safe fallback** — languages without a grammar fall back to a lexical scanner that understands strings, char literals, and escape sequences.
- **Emoji stripping** — remove emoji from comments and/or string literals using a full Unicode emoji range table with ZWJ and variation-selector joining.
- **Behavior-preserving rewrites** — only the comment text is deleted; `\r\n`/`\n` newlines, surrounding code, and formatting stay untouched. `#!` shebang lines are preserved by default.
- **Encoding & line-ending fidelity** — UTF-8 (with/without BOM), UTF-16, UTF-32 and ANSI are detected and re-encoded byte-for-byte (BOM preserved).
- **Backups** — every modified file gets a side-by-side `.senice.bak` plus a recoverable copy in `%TEMP%\Senice\Backups`.
- **Side-by-side diff** — inspect before/after for any changed file.
- **Drag & drop** — drop a file or a whole folder onto the window and process it in parallel.
- **Light & dark themes** — switch between a light and a dark UI at any time with a single checkbox.

## Downloads

A ready-to-run single-file executable is published under `release/` (self-contained, no runtime required):

```
release/
  bin/            # the application
    Senice.exe    # ~75 MB, Win x64, runs standalone
  source/         # complete source code + tests
  README.md
```

## Quick start

```
# Run the app
release\bin\Senice.exe

# Or build from source
dotnet restore Senice.slnx
dotnet build   Senice.slnx
dotnet test    Senice.slnx
```

Drag a file or folder onto the window, tweak the options, and press **Run**.

### Single-file publish

```powershell
dotnet publish src\Senice.App\Senice.App.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```

## Options

| Option | Description |
| --- | --- |
| Remove comments | Strip `//`, `/* */`, `#`, `--`, `<!-- -->` … bodies |
| Remove emoji from comments | Also strip emoji inside comments |
| Remove emoji from strings | Also strip emoji inside string literals |
| Preserve shebang lines | Keep `#!` first-line markers (`#!/usr/bin/env python3`) |
| Backup before writing | Write `{file}.senice.bak` + recoverable copy before changing a file |
| Skip uncertain files | When Tree-sitter and the lexical scan disagree, leave the file untouched |
| Include hidden files | Recurse into hidden directories and process hidden files |
| Max depth | Folder recursion limit |
| Dark theme | Toggle between the dark and the light UI |

## Supported languages

`agda`, `bash`, `c`, `c-sharp`, `cpp`, `css`, `embedded-template`, `go`, `haskell`, `html`, `java`, `javascript`, `jsdoc`, `json`, `julia`, `ocaml`, `php`, `python`, `ql`, `razor`, `ruby`, `rust`, `scala`, `swift`, `toml`, `tsq`, `tsx`, `typescript`, `verilog`

Other languages are still handled by the built-in lexical fallback (C#, JS, and other `//`/`/* */`/`#`-style comments, including `""`/`''` strings with escapes).

## How it works

1. **Scan** — the file is parsed with the matching Tree-sitter grammar; comment/string nodes are located (`comment`, `line_comment`, `block_comment`, …). On parse errors or missing grammars, a lexical scanner takes over.
2. **Plan** — comment and emoji spans become `EditOperation`s (`TextRange` + kind). A `SourceRewriter` applies them sorted by position, skipping overlaps, and preserves only the newline characters of removed blocks.
3. **Rewrite** — output is re-encoded with the detected encoding and BOM, and written atomically (temp file + replace).
4. **Diff** — a linear-space Myers diff compares the backup with the result for the built-in diff viewer.

### Project layout

```
src/
  Senice.App/          WPF shell (MVVM, DI via Microsoft.Extensions.Hosting)
  Senice.Core/         engine: scanning, rewriting, emoji, encodings, diffing, processing
  Senice.Infrastructure/ Tree-sitter integration, file system, backups, wiring
tests/
  Senice.Core.Tests/        engine unit tests
  Senice.Infrastructure.Tests/  Tree-sitter + pipeline tests
```

## Tests

78 tests covering the rewriter, Myers diff, encoding/BOM round-trips, the emoji scanner, lexical and Tree-sitter comment location, and the full processing pipeline.

## License

MIT
AAAAAA
A
A
A
A
A
A
A
A
A
A
A
A
AAAAA
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A
A