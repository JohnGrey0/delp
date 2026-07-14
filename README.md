# Delp — developer toolbox

75 developer tools in one native Windows app. Lives in your system tray;
press **Ctrl+Alt+Space** anywhere to open the quick panel, or expand it into
the full window. Windows 11 acrylic "glass" UI, dark theme, everything
offline (the sole exception: the SSL decoder's optional fetch-from-host mode).

## Tools

- **Encoding & Decoding** — Base64, URL, HTML entities, JWT decoder, unicode
  escapes, binary/hex/text, Morse, ROT-N
- **Hashing & Generation** — MD5–SHA-512, HMAC, PBKDF2, file checksums,
  password + passphrase generator, UUID v1–v8, random strings, Nano ID
- **Data Format** — JSON/YAML/XML/TOML format+validate, JSONPath, converters
  (JSON↔YAML/XML/CSV), SQL + GraphQL formatters, JSON → C#/TypeScript types
- **Web Development** — color converter, screen color picker (eyedropper),
  CSS/JS/HTML minifiers, Markdown live preview, data URIs, SVG path
  visualizer, placeholder images, URL parser/builder, Lorem Ipsum
- **Text Processing** — case converter, regex tester + pattern library, diff,
  line sort/dedupe, text stats, escapes, whitespace tools, number bases,
  epoch/timestamp tools, slugs, Unicode inspector, NLP processor (stopwords,
  Porter stemming, n-grams), text → Python list/JSON/CSV/SQL
- **Developer Utilities** — QR codes, cron parser, git branch names, semver,
  ASCII art, HTTP status / MIME / port references, IP + CIDR info, SSL/TLS
  certificate decoder, mock data generator, Basic auth builder, code linter
  (Roslyn-powered C#), programming + shell cheat sheets

## Get it

- **Installer (recommended):** `dist\delp-setup.msi` — per-user wizard, no
  admin required, Start Menu shortcut, clean uninstall. Lowest memory use.
- **Portable:** `dist\portable\delp.exe` — single self-contained file, no
  installation, no .NET required. Copy anywhere and run.

## Build from source

Requires the .NET 10 SDK.

```powershell
dotnet build Delp.sln                 # debug build
dotnet test Delp.Core.Tests           # ~2,700 unit tests
delp.exe --smoke                      # constructs all 75 tool views headlessly

# portable single-file exe -> dist\portable\delp.exe
dotnet publish Delp.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
  -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -p:SatelliteResourceLanguages=en -o dist\portable

# MSI (WiX ships as a repo-local dotnet tool)
dotnet publish Delp.App -c Release -r win-x64 --self-contained true `
  -p:PublishReadyToRun=true -p:DebugType=None `
  -p:SatelliteResourceLanguages=en -o dist\app
cd installer
dotnet tool run wix -- build Package.wxs -ext WixToolset.UI.wixext -arch x64 `
  -d "PublishDir=$((Resolve-Path ..\dist\app).Path)" -o ..\dist\delp-setup.msi
```

## Architecture

Three projects: `Delp.Core` (pure, fully-tested tool logic), `Delp.App`
(WPF shell + one thin view per tool), `Delp.Core.Tests` (xunit). A tool is a
`[Tool]`-attributed UserControl discovered by reflection — no registry files.
See `docs/CONVENTIONS.md` and `docs/TOOLSPEC.md` for the full contract.
Settings (favorites, collapsed sidebar groups) live in
`%LOCALAPPDATA%\Delp\settings.json`.
