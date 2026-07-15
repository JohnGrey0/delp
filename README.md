# Delp — developer toolbox

59 developer tools in one native Windows app — consolidated, capability-dense tools (one UUID generator covers all 8 versions; one formatter covers JSON/YAML/XML/SQL/GraphQL; and so on). Lives in your system tray;
press **Ctrl+Alt+Space** anywhere to open the quick panel, or expand it into
the full window. Windows 11 acrylic "glass" UI, dark theme, everything
offline (the sole exception: the SSL decoder's optional fetch-from-host mode).

## Tools

- **Encoding & Decoding** — Base64 / Base32 / Base58 / Ascii85, URL, HTML
  entities, JWT decoder, unicode escapes, binary/hex/text, Morse, ROT-N
- **Hashing & Generation** — MD5–SHA-512, HMAC, PBKDF2, file checksums,
  password + passphrase generator, UUID v1–v8, random strings, Nano ID,
  TOTP / 2FA codes
- **Data Format** — JSON/YAML/XML/TOML format+validate, JSON tree viewer,
  JSONPath + XPath, converters (JSON↔YAML/XML/CSV), table converter
  (Markdown/CSV/SQL/ASCII/LaTeX), SQL + GraphQL formatters, JSON →
  C#/TypeScript types
- **Web Development** — color converter + WCAG contrast & color-blindness
  checks, screen color picker (eyedropper), CSS/JS/HTML minifiers, cURL → code
  converter, image converter (+ favicon/.ico), Markdown live preview, data
  URIs, SVG path visualizer, placeholder images, URL parser/builder, Lorem
  Ipsum
- **Text Processing** — case converter, regex tester + pattern library, diff,
  line sort/dedupe/filter/number, text stats, escapes, whitespace tools,
  number bases, date & time converter (timezones, durations, epoch), slugs,
  Unicode inspector, NLP processor (stopwords, Porter stemming, n-grams),
  text → Python list/JSON/CSV/SQL
- **Developer Utilities** — QR codes, cron parser, git branch names, semver,
  ASCII art, code screenshot generator, .gitignore generator, Docker run ↔
  Compose, reference lookup (HTTP status / MIME / ports / user-agent / chmod /
  byte sizes), IP + CIDR info, SSL/TLS certificate decoder, mock data
  generator, Basic auth builder, code linter (Roslyn-powered C#), programming,
  shell, and keyboard-shortcut (vim/VS Code/JetBrains) cheat sheets

## Get it

- **Installer (recommended):** `dist\delp-setup.msi` — per-user wizard, no
  admin required, Start Menu shortcut, clean uninstall. Lowest memory use.
- **Portable:** `dist\portable\delp.exe` — single self-contained file, no
  installation, no .NET required. Copy anywhere and run.
- **Portable lite:** `dist\portable-lite\delp.exe` (~40 MB) — same app, but
  requires the .NET 10 Desktop Runtime on the machine (Windows offers the
  download automatically on first launch if it's missing). Keep
  `WebView2Loader.dll` next to it.

Why the sizes: the app's own code and data are only a few MB. The
self-contained builds embed the entire .NET runtime + WPF (~76 MB) so they
run on any Windows machine with zero prerequisites; the Roslyn compiler that
powers the C# linter adds ~24 MB more.

## Build from source

Requires the .NET 10 SDK.

```powershell
dotnet build Delp.sln                 # debug build
dotnet test Delp.Core.Tests           # ~3,200 unit tests
delp.exe --smoke                      # constructs every tool view headlessly + UX lint

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
dotnet tool run wix -- build Package.wxs -ext WixToolset.UI.wixext `
  -ext WixToolset.Util.wixext -arch x64 `
  -d "PublishDir=$((Resolve-Path ..\dist\app).Path)" -o ..\dist\delp-setup.msi
```

## Architecture

Three projects: `Delp.Core` (pure, fully-tested tool logic), `Delp.App`
(WPF shell + one thin view per tool), `Delp.Core.Tests` (xunit). A tool is a
`[Tool]`-attributed UserControl discovered by reflection — no registry files.
See `docs/CONVENTIONS.md` and `docs/TOOLSPEC.md` for the full contract.
Settings (favorites, collapsed sidebar groups) live in
`%LOCALAPPDATA%\Delp\settings.json`.
