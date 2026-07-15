# Delp — Contribution Conventions (READ FIRST)

Delp is a native Windows (WPF, .NET 10) developer toolbox: 59 tools in one
tray-first glass-UI app. This document is the binding contract for every tool
implementation. The reference implementation is the **Base64 tool** — read all
three of these files before writing any code:

- `Delp.Core/Tools/Encoding/Base64Tool.cs` (core logic)
- `Delp.App/Tools/Encoding/Base64View.xaml` + `.xaml.cs` (view + wiring)
- `Delp.Core.Tests/Tools/Encoding/Base64ToolTests.cs` (tests)

## Architecture

| Project | Purpose | Rules |
|---|---|---|
| `Delp.Core` | All tool logic as pure, UI-free static classes | Fully unit-testable; no WPF references |
| `Delp.App` | WPF shell + one thin `UserControl` per tool | Views only wire events to Core calls |
| `Delp.Core.Tests` | xunit tests for Core | Must stay green |

A tool = three (or four) new files, mirroring the Base64 layout:

```
Delp.Core/Tools/<Category>/<Name>Tool.cs
Delp.App/Tools/<Category>/<Name>View.xaml
Delp.App/Tools/<Category>/<Name>View.xaml.cs
Delp.Core.Tests/Tools/<Category>/<Name>ToolTests.cs
```

Namespaces mirror folders: `Delp.Core.Tools.Encoding`, `Delp.App.Tools.Encoding`, …
Category folder names: `Encoding`, `Hashing`, `DataFormat`, `WebDev`,
`TextProcessing`, `DevUtilities`.

### Registration — never edit shared files

Put a `[Tool]` attribute on the view's class; the shell discovers it by
reflection. There is **no registry file**:

```csharp
[Tool("base64", "Base64 Encode / Decode", ToolCategory.Encoding,
    "Convert text to and from Base64, with an optional URL-safe alphabet.",
    Keywords = "base64,b64,encode,decode", Order = 10)]
public partial class Base64View : UserControl
```

Use the exact `id`, `Name`, `ToolCategory`, and `Order` given in your
TOOLSPEC.md section. Description: one sentence, sentence case, ends with a period.

### Files you MUST NOT touch

- Any `.csproj`, the `.sln`, `Directory.*.props`
- `Delp.App/App.xaml(.cs)`, `Delp.App/Theme/*`, `Delp.App/Infrastructure/*`,
  `Delp.App/Windows/*`, `Delp.App/Controls/*`, `Delp.App/Assets/*`
- Any existing tool or test that isn't yours

All packages you need are already referenced. **If something seems missing,
implement it by hand in `Delp.Core` — do not add packages.**

### Available packages

Delp.Core: `YamlDotNet`, `Tomlyn`, `CsvHelper`, `NUglify`, `Markdig`,
`QRCoder`, `Figgle`, `DiffPlex`, `Cronos`, `CronExpressionDescriptor`,
`Semver`, `JsonPath.Net`, `UUIDNext`, `GraphQL-Parser`; plus the full BCL
(`System.Text.Json`, `System.Security.Cryptography`, `System.Xml.Linq`, …).
Delp.App additionally: `AvalonEdit`, `Microsoft.Web.WebView2`.

## UI rules (the app must feel like ONE product)

The app renders on a dark **acrylic glass** backdrop. Tool views sit inside a
translucent card in the main window and inside a compact 430×620 flyout.

1. Root element: `DockPanel` or `Grid` with `Margin="16"`, no background set.
2. **Responsive from ~400×460 up.** No fixed widths over 360; panes use star
   sizing. Options rows use `WrapPanel` if they could overflow 380 px.
3. **Only theme resources — never hardcode a color.** The vocabulary:
   - Text styles: `Text.Title`, `Text.Sub`, `Text.Section` (uppercase pane
     labels like `INPUT`), `Text.Error`, `Text.Mono`
   - Brushes: `Brush.Fg0/Fg1/Fg2`, `Brush.Accent`, `Brush.Danger`,
     `Brush.Success`, `Brush.Warning`, `Brush.Card`, `Brush.Border`
   - Buttons: implicit style, `Button.Primary` (one per view max), `Button.Icon`
   - Inputs: implicit `TextBox`, `TextBox.Mono` (multiline mono IO panes),
     `TextBox.Search`, implicit `ComboBox`/`CheckBox`/`RadioButton`/`TabControl`
   - Containers: `Card`, `Card.Editor` (Border styles)
4. Layout pattern (copy Base64View): options top-docked; error `TextBlock`
   (`Text.Error`, collapsed by default) bottom-docked; IO panes fill the rest.
   Every pane gets an uppercase `Text.Section` label and, for outputs, a Copy
   button wired to `Ui.Copy(text, button)` (it flashes "Copied ✓" for you).
5. Large / syntax-highlighted IO: `CodeEditors.Create("Json")` (AvalonEdit,
   dark-tuned JSON; pass `null` for plain) hosted inside a
   `Border Style="Card.Editor"`. Add the editor in code-behind constructor.
   Only "Json" is dark-tuned — do not use other syntax names; plain is better
   than unreadable light-theme colors.
6. Live conversion on `TextChanged` with the reentrancy-guard `Run(...)`
   pattern from Base64View. Errors go to the inline error TextBlock — never
   MessageBox, never throw across the UI boundary.
7. Anything potentially slow (file IO, hashing big files, parsing megabytes)
   runs in `Task.Run` with `async void` handlers guarded by try/catch;
   debounce continuous typing with a 300 ms `DispatcherTimer` when conversion
   is expensive.
8. Don't use `DataGrid`, `ListView`/`GridView`, `Expander`, `GroupBox`,
   `Slider`, or `DatePicker` (unstyled for dark glass). Tables = `ItemsControl`
   / `ListBox` with `Grid` rows. Numeric input = `TextBox` + validation.
9. All regexes over user input: `RegexOptions` + `TimeSpan.FromSeconds(2)`
   match timeout.
10. No network calls unless your spec section explicitly allows it.

## Testing & verification

- Every Core class gets xunit tests: happy path, edge cases (empty input,
  unicode, huge-ish input where relevant), and at least one malformed-input
  case asserting the documented exception/result.
- Before finishing: `dotnet build Delp.sln` → **0 errors, 0 new warnings**,
  and `dotnet test Delp.Core.Tests` → all green. Fix what you break.
- Commit all your work to your branch with a descriptive message when done.

## Core API conventions

- Static class `<Name>Tool` (or a small set of statics) with pure methods.
- User-facing failures throw `FormatException` (or `ArgumentException`) with a
  message that makes sense to a human — views display `ex.Message` verbatim.
- Prefer returning result records over tuples for multi-value outputs.
- Culture: `CultureInfo.InvariantCulture` for all machine formatting/parsing;
  local culture only when displaying dates/times to the user.
