# Delp — Tool Specifications

Binding spec for every tool. Read `docs/CONVENTIONS.md` first; the Base64 tool
is the reference implementation. Each entry gives the `[Tool]` attribute values,
the Core API sketch, UI, edge cases, and required tests. File layout and naming
follow CONVENTIONS.md exactly.

Legend: `id — Name` · category · Order · keywords.

---

## Batch A — Encoding & Decoding (`ToolCategory.Encoding`)

### base64 — Base64 Encode / Decode · 10 — **DONE (reference implementation)**

### url-encode — URL Percent-Encoding · 20 · `url,percent,escape,uri,querystring`
**Core** `UrlEncodeTool`: `Encode(string, UrlEncodeMode mode)` /
`Decode(string, UrlEncodeMode)`. Modes: `Component` (`Uri.EscapeDataString`),
`FormData` (`WebUtility.UrlEncode`, space→`+`), `PreserveUriChars` (encode like
Component but keep `:/?#[]@!$&'()*+,;=` — build from a safe-char set).
Decode: `Uri.UnescapeDataString`; FormData decode maps `+`→space first.
**UI** Base64View pattern: two bidirectional panes DECODED / ENCODED; mode via
3 RadioButtons top.
**Edge** malformed `%` sequences on decode → `FormatException("Invalid percent
sequence '%q1' at position 12")` (find first bad triplet yourself); unicode
(emoji) round-trips; empty → empty.
**Tests** round-trip per mode, space handling difference, reserved chars per
mode, malformed decode throws with position, emoji round-trip.

### html-entities — HTML Entity Encode / Decode · 30 · `html,entities,escape,amp,nbsp`
**Core** `HtmlEntityTool`: `Encode(string, bool nonAsciiToNumeric)`,
`Decode(string)`. Encode = `WebUtility.HtmlEncode`, then if flag: replace every
char > 0x7F with `&#xXXXX;` (handle surrogate pairs → single codepoint entity).
Decode = `WebUtility.HtmlDecode`.
**UI** bidirectional TEXT / ENCODED panes; one CheckBox "Encode non-ASCII as
numeric entities".
**Edge** named entities (`&nbsp;`, `&mdash;`) decode; double-encoding visible
(`&amp;amp;`); astral plane chars encode as one `&#x1F600;` not two surrogates.
**Tests** basic `<div class="a">` round-trip, named entity decode, numeric flag
on emoji, decode of unknown entity `&foo;` passes through unchanged.

### jwt-decoder — JWT Decoder · 40 · `jwt,token,bearer,oauth,claims,jose`
**Core** `JwtTool.Decode(string token)` → record `JwtParts(string HeaderJson,
string PayloadJson, string SignatureB64, IReadOnlyList<JwtClaim> Claims)`;
`JwtClaim(string Name, string Value, string? Note)`. Split on `.` (must be 3
parts; 2 allowed = unsecured, signature empty). Base64url-decode, pretty-print
via `JsonSerializer` (indented). For registered claims `exp,iat,nbf` add Note =
local datetime + for `exp` either "expired N ago" or "expires in N"
(humanize: s/min/h/d). No signature verification (out of scope, say so in UI).
**UI** token input (TextBox.Mono, ~90px, top-docked); below a `TabControl`:
HEADER (readonly Json editor via `CodeEditors.Create("Json", readOnly:true)`),
PAYLOAD (same), CLAIMS (ItemsControl table Name | Value | Note; Note in
`Brush.Warning` when expired, `Brush.Fg2` otherwise). Copy buttons on header
and payload tabs. A `Text.Sub` line: "Decode only — signatures are not verified."
**Edge** whitespace/`Bearer ` prefix stripped automatically; wrong part count →
clear error; invalid base64url → error naming which part; non-JSON payload →
show raw string.
**Tests** decode known token (construct one in the test from JSON parts),
Bearer prefix strip, 2-part unsecured token, exp note says expired for past
timestamp, malformed part errors.

### unicode-escape — Unicode Escape / Unescape · 50 · `unicode,escape,codepoint,\\u`
**Core** `UnicodeEscapeTool`: `Escape(string, bool nonAsciiOnly)` → `\uXXXX`
per UTF-16 unit (astral chars = two `\u` escapes; also emit `\n \r \t \\ \"`
as those short forms); `Unescape(string)` supporting `\uXXXX`,
`\U0001F600` (8-digit), `\xNN`, `\n \r \t \0 \\ \" \'`.
**UI** bidirectional TEXT / ESCAPED panes; CheckBox "Escape non-ASCII only"
(default on).
**Edge** lone trailing backslash → error with position; invalid hex digits →
error; `\U` above 0x10FFFF → error; round-trip emoji.
**Tests** escape/unescape round-trip incl. emoji + newline, nonAsciiOnly keeps
`abc`, `\U0001F600` unescapes to 😀, malformed sequences throw.

### binary-hex — Binary ↔ Hex ↔ Text · 60 · `binary,hex,bytes,bits,dump,decimal`
**Core** `BytesTool`: `FromText(string)→byte[]` (UTF-8);
`FromHex(string)` (strip whitespace, `0x`, commas; odd length → error);
`FromBinary(string)` (strip whitespace; length %8 → error);
`FromDecimalBytes(string)` (space/comma separated 0–255);
`ToText/ToHex(bytes, bool spaced, bool uppercase)/ToBinary(bytes, bool spaced)/ToDecimalBytes`.
`ToText` uses UTF-8 with replacement chars for invalid sequences.
**UI** four stacked mono panes: TEXT, HEX, BINARY, DECIMAL — editing any one
updates the other three (shared `Run` guard). Options row: CheckBoxes "Space
between bytes" (default on), "Uppercase hex". Each pane has Copy. Panes share
a `ScrollViewer` if needed; keep each ~4 rows (`MinHeight="64"`).
**Edge** invalid hex char → error with offending char; byte >255 in decimal;
binary with non-01; text with emoji round-trips.
**Tests** "Hi" → `48 69` / `01001000 01101001` / `72 105` and back; odd-length
hex throws; invalid binary throws; unicode round-trip.

### morse — Morse Code Translator · 70 · `morse,dot,dash,telegraph,itu`
**Core** `MorseTool`: ITU table A–Z, 0–9, `.,?'!/()&:;=+-_"$@`. `Encode(string,
bool skipUnknown)` — letters→`.-` groups separated by single space, words by
` / `. `Decode(string)` — accepts `/`, `|`, or 3+ spaces as word separators.
Unknown char: throw with the char, unless skipUnknown.
**UI** bidirectional TEXT / MORSE panes; CheckBox "Skip unsupported
characters". Case-insensitive; decode output uppercase.
**Edge** consecutive spaces in input collapse to one word gap; unknown morse
group → error naming the group; empty groups filtered.
**Tests** "SOS HELP" → `... --- ... / .... . .-.. .--.` and back; punctuation;
unknown char throws vs skips; decode `|` separator.

### rot13 — ROT13 / Caesar Cipher · 80 · `rot13,caesar,cipher,shift`
**Core** `Rot13Tool.Shift(string, int n)` — letters only, both cases, preserve
everything else; n normalized mod 26 (negatives ok).
**UI** bidirectional PLAIN / SHIFTED panes; shift amount TextBox (default 13)
+ a `Text.Sub` note that 13 is its own inverse; shifted pane decodes with `26-n`.
**Edge** non-letters untouched; n=0/26 identity; large/negative n.
**Tests** rot13 round-trip, "abc"+3→"def", negative shift, digits untouched.

---

## Batch B — Hashing & Crypto (`ToolCategory.Hashing`)

### hash-generator — Hash Generator · 10 · `md5,sha,sha256,sha512,digest,checksum`
**Core** `HashTool.ComputeAll(byte[] data)` → `IReadOnlyList<HashResult(string
Algorithm, string Hex)>` for MD5, SHA-1, SHA-256, SHA-384, SHA-512 (use the
static `MD5.HashData` style APIs). Also `Compute(string algorithm, Stream)` for
files (incremental, works on big files).
**UI** input TextBox.Mono (top, ~100px) OR file: a "Hash a file…" Button
(OpenFileDialog) + drag-drop onto the input (`AllowDrop`, handle FileDrop).
Below: ItemsControl of rows [ALGO label | hex TextBox readonly mono | Copy].
CheckBox "Uppercase". File hashing async with "Hashing…" state on rows.
**Edge** empty text still hashes (known digests); file locked/missing → inline
error; text is UTF-8.
**Tests** known vectors: MD5("")=d41d8cd98f00b204e9800998ecf8427e,
SHA-256("abc")=ba7816bf…ad (full string), uppercase flag, stream vs bytes equal.

### hmac — HMAC Generator · 20 · `hmac,signature,sha256,key,mac`
**Core** `HmacTool.Compute(string algorithm, byte[] key, byte[] message)` →
byte[]; algorithms MD5, SHA-1, SHA-256, SHA-384, SHA-512 (`IncrementalHash` or
`HMACSHA256` classes). Helpers to interpret key/message input as UTF-8, hex, or
Base64 (`InputInterpretation` enum + `ParseInput(string, InputInterpretation)`).
**UI** MESSAGE pane (mono multiline); KEY row: TextBox + ComboBox
(UTF-8/Hex/Base64); algorithm ComboBox (default SHA-256); OUTPUT readonly row
+ RadioButtons Hex/Base64 + Copy. Live update.
**Edge** invalid hex/base64 key → inline error; empty key allowed (warn note).
**Tests** RFC 4231 test case 2 (key "Jefe", data "what do ya want for
nothing?") for SHA-256; hex-key parsing; base64 output correctness.

### pbkdf2 — PBKDF2 Password Hash · 30 · `pbkdf2,password,kdf,derive,rfc2898`
**Core** `Pbkdf2Tool.Derive(string password, byte[] salt, int iterations,
string hashAlgo, int lengthBytes)` → byte[] via `Rfc2898DeriveBytes.Pbkdf2`.
`GenerateSalt(int bytes)` via `RandomNumberGenerator`. Also
`FormatPhc(...)` → `$pbkdf2-sha256$i=600000$<b64salt>$<b64hash>` string.
**UI** password TextBox; SALT row: TextBox (hex) + "Generate" Button (16
bytes); iterations TextBox (default 600000); algo ComboBox (SHA-1/256/512,
default 256); length TextBox (default 32). OUTPUT: hex, Base64, and PHC string
rows each with Copy. Compute via Button (`Button.Primary`, "Derive") — not
live (it's slow); run in Task.Run, disable button while running.
**Edge** iterations < 1000 → show `Brush.Warning` note "below OWASP minimum";
non-numeric fields → inline error; empty password allowed.
**Tests** RFC 6070-style vector (password "password", salt "salt" ASCII, 1
iter, SHA-1, 20 bytes = 0c60c80f961f0e71f3a9b524af6012062fe037a6); PHC format
shape; salt length respected.

### file-checksum — File Checksum Verifier · 40 · `checksum,verify,file,integrity,sha256`
**Core** reuse `HashTool.Compute(algorithm, Stream)`. Add
`ChecksumTool.Verify(string actualHex, string expectedHex)` → bool
(case/whitespace/`*`-prefix insensitive).
**UI** file picker Button + drag-drop zone (a `Card` Border, dashed feel via
`Text.Sub` "Drop a file here or browse…", shows chosen file name + size);
algorithm ComboBox (SHA-256 default); computed hash readonly row + Copy;
EXPECTED TextBox — on input, show ✓ "Match" (`Brush.Success`) or ✗ "No match"
(`Brush.Danger`). Async with progress note for big files (just "Hashing…").
**Edge** file disappears mid-hash → inline error; expected with `sha256:`
prefix or `*filename` suffix (common in .sha256 files) still matches.
**Tests** Verify normalization cases; hashing a temp file matches HashData of
its bytes.

---

## Batch C — UUID & ID Generation (`ToolCategory.Hashing`)

Shared Core: `UuidFormat.Apply(Guid, UuidStyle(bool Uppercase, bool Braces,
bool NoHyphens))` + `UuidBatch(Func<Guid> gen, int count)` → string list.
Shared UI shape for all UUID tools: options row (count TextBox default 1 max
1000, CheckBoxes Uppercase/Braces/No hyphens + per-version options), Generate
`Button.Primary`, OUTPUT mono readonly pane (one per line) + Copy + "Copy as
JSON array" Button. Implement each version in `UuidTools.cs` (one file, class
per version is fine) in `Delp.Core/Tools/Hashing/`; views one file per tool.
RFC 9562 is the authority — hand-roll v1/v2/v3/v5/v6/v8 (set version AND
variant bits correctly; note `Guid` byte order: use the `Guid(int,short,short,
byte[8])` ctor or `Guid.Parse` of the formatted hex string you build).
`UUIDNext` package exists but hand-rolling with tests is preferred for v1/v2/v6.

### uuid-v1 — UUID v1 (Time-based) · 110 · `uuid,guid,v1,time,mac,rfc9562`
60-bit timestamp = 100ns intervals since 1582-10-15; clock sequence random per
batch; node = random 48 bits with multicast bit set (privacy, the RFC-blessed
alternative to MAC) — plus CheckBox "Use real MAC address" (first operational
`NetworkInterface`'s `GetPhysicalAddress()`, fall back to random if none).
Show decoded timestamp of the first UUID as a `Text.Sub` note.
**Tests** version/variant bits, timestamp round-trip decode ≈ now, node
multicast bit set when random, uniqueness in batch of 100.

### uuid-v2 — UUID v2 (DCE Security) · 120 · `uuid,guid,v2,dce,posix,rfc9562`
v1 layout but: `time_low` replaced by 32-bit local ID, `clock_seq_low`
replaced by domain byte (0=Person, 1=Group, 2=Org). UI: domain ComboBox +
local ID TextBox (default 1000). `Text.Sub` note: "Rarely implemented —
Delp generates the full RFC 9562 layout." **Tests** version/variant/domain
byte/embedded local id round-trip.

### uuid-v3 — UUID v3 (MD5 Name-based) · 130 · `uuid,guid,v3,md5,namespace,rfc9562`
Namespace ComboBox: DNS `6ba7b810-9dad-11d1-80b4-00c04fd430c8`, URL `…b811…`,
OID `…b812…`, X.500 `…b814…`, Custom (TextBox appears). NAME input. Hash
namespace-bytes(network order)+name(UTF-8), set version/variant. Live update,
no Generate button (deterministic). **Tests** RFC vector: DNS +
"www.example.com" → `5df41881-3aed-3515-88a7-2f4a814cf09e`; custom namespace
parse error.

### uuid-v4 — UUID v4 (Random) · 140 · `uuid,guid,v4,random,batch` · Order 140
`Guid.NewGuid()`. The standard batch UI. **Tests** version/variant bits,
batch count respected, formatting options.

### uuid-v5 — UUID v5 (SHA-1 Name-based) · 150 · `uuid,guid,v5,sha1,namespace,rfc9562`
Same UI/Core shape as v3 with SHA-1. **Tests** RFC vector: DNS +
"www.example.com" → `2ed6657d-e927-568b-95e1-2665a8aea6a2`.

### uuid-v6 — UUID v6 (Sortable time-based) · 160 · `uuid,guid,v6,sortable,rfc9562`
v1 fields with timestamp reordered high→low so lexical order = time order.
Same options as v1 (random node default). Note shows decoded timestamp.
**Tests** version bits, two sequential generations sort ascending as strings,
timestamp decode.

### uuid-v7 — UUID v7 (Unix epoch, sortable) · 170 · `uuid,guid,v7,epoch,sortable,rfc9562`
`Guid.CreateVersion7()` (built into .NET). Batch UI + a DECODE section:
paste a v7 UUID → embedded ms timestamp as local + UTC datetime.
**Tests** version bits, monotonic string sort across batch, decode of the
timestamp within ±5s of now, decode rejects non-v7.

### uuid-v8 — UUID v8 (Custom) · 180 · `uuid,guid,v8,custom,vendor,rfc9562`
122 free bits. UI: one TextBox "Custom data (hex, up to 32 digits)" — zero-pad
right; generator overwrites version nibble (8) and variant bits; plus "Random
fill" CheckBox (default on when input empty). Show resulting layout note.
**Tests** version/variant forced correctly, custom hex payload survives in the
non-reserved bit positions, invalid hex throws.

### random-string — Random String Generator · 190 · `random,string,secure,token,secret`
**Core** `RandomStringTool.Generate(RandomStringOptions(int Length, bool Lower,
bool Upper, bool Digits, bool Symbols, string? Custom, bool ExcludeAmbiguous))`
using `RandomNumberGenerator.GetInt32` (rejection-free uniform). Ambiguous set:
`Il1O0o`. `EntropyBits` = length × log2(alphabet size).
**UI** length TextBox (32), count TextBox (5), charset CheckBoxes + custom
TextBox, exclude-ambiguous CheckBox; Generate button; output pane + Copy;
`Text.Sub` shows "≈ N bits of entropy per string".
**Tests** length respected, only-selected-charset chars appear, ambiguous
excluded, custom alphabet used, entropy math, empty alphabet throws.

### nanoid — Nano ID Generator · 200 · `nanoid,id,short,url-safe`
**Core** `NanoIdTool.Generate(int size = 21, string alphabet = default64)` —
default alphabet `A-Za-z0-9_-`, crypto RNG, unbiased via rejection sampling
(mask technique). **UI** size TextBox, custom alphabet TextBox, count, Generate,
output + Copy; note on collision odds for current settings (`Text.Sub`,
use the standard `sqrt` approximation, e.g. "~T years at 1000 IDs/hour for 1%
probability" — a simple canned formula is fine).
**Tests** default size/alphabet, custom alphabet only, uniqueness sanity in
1000, size 0/negative throws, alphabet >256 chars throws.

---

## Batch D — Data Format: JSON / YAML / TOML (`ToolCategory.DataFormat`)

### json-format — JSON Formatter & Validator · 10 · `json,format,pretty,minify,validate`
**Core** `JsonFormatTool`: `Format(string, JsonFormatOptions(int IndentSize,
bool UseTabs, bool SortKeys, bool EscapeNonAscii))` and `Minify(string)` —
parse with `JsonNode.Parse` (preserve order, allow trailing commas OFF,
comments OFF — strict), custom recursive writer (Utf8JsonWriter with custom
indent via post-processing is fiddly — write your own StringBuilder emitter;
it also enables SortKeys). `Validate(string)` → null or
`JsonError(int Line, int Col, string Message)` (catch `JsonException`, use its
LineNumber/BytePositionInLine).
**UI** two `CodeEditors.Create("Json")` editors side-by-side in star columns
(input editable, output readonly), inside `Card.Editor` borders, section
labels INPUT/OUTPUT. Toolbar: Format (`Button.Primary`), Minify, indent
ComboBox (2/4/Tab), CheckBoxes Sort keys / Escape non-ASCII. Status line
bottom: green "Valid JSON — 1.2 KB" or red error with line:col. Debounced
live validate (300 ms) on typing; Format/Minify on click.
**Edge** big docs (1 MB) stay responsive (Task.Run + debounce); duplicate keys
(JsonNode throws — surface clearly); numbers preserved exactly as written
(emit raw text via `JsonValue.ToJsonString` careful — acceptable: normalize).
**Tests** format nested sample (stable expected string), minify round-trip,
sort keys recursive, error line/col for `{"a":}`, escape non-ASCII flag.

### jsonpath — JSONPath Query · 20 · `jsonpath,query,json,filter,$..`
**Core** `JsonPathTool.Query(string json, string path)` →
`JsonPathResult(int Count, string ResultJson)` using `JsonPath.Net`
(`Json.Path.JsonPath.Parse`) over `JsonNode` — results emitted as a
pretty-printed JSON array of matches (reuse `JsonFormatTool.Format`).
**UI** path TextBox top (mono, default `$`), INPUT Json editor left / RESULTS
readonly Json editor right; status "N matches"; error inline (bad path vs bad
JSON distinguished). `Text.Sub` cheat-sheet line under path box:
`$.store.book[0].title · $..price · $.items[?(@.price > 10)]`.
**Tests** root, recursive descent, array index/slice, filter expression, no
matches → empty array + count 0, invalid path throws with message.

### json-yaml — JSON ↔ YAML Converter · 30 · `json,yaml,convert,yml`
**Core** `JsonYamlTool`: `JsonToYaml(string)` — `JsonNode` walk → build object
graph (Dictionary/List/scalars with correct types) → YamlDotNet Serializer
(block style, indent 2). `YamlToJson(string)` — YamlDotNet Deserializer to
generic graph → JsonNode → pretty JSON. Numbers/bools/null preserved as types;
strings that look like numbers stay quoted in YAML (serializer handles).
**UI** bidirectional editors: JSON (Json syntax) left, YAML (plain) right,
live with 300 ms debounce, `Run` guard; error line under the offending side.
**Edge** YAML anchors/aliases resolve on YAML→JSON; multi-document YAML →
error "multiple documents not supported" (or convert first + note); duplicate
YAML keys → YamlDotNet error surfaced.
**Tests** object/array/scalar round-trips both ways, type preservation
(42 vs "42"), nested structures, anchor resolution, invalid input errors.

### yaml-format — YAML Formatter · 40 · `yaml,format,lint,yml,validate`
**Core** `YamlFormatTool.Format(string, int indent)` — parse via
`YamlStream`, re-emit canonical block style with chosen indent (YamlDotNet
Serializer with `WithIndentedSequences`); `Validate(string)` → error with
line/col from YamlDotNet's `YamlException` (it carries Start marks).
**UI** INPUT/OUTPUT editors, Format button, indent ComboBox (2/4), status line
valid/error like json-format.
**Edge** comments are lost on re-emit — put a permanent `Text.Sub` note
"Comments are not preserved."; multi-doc streams re-emit all docs with `---`.
**Tests** messy flow-style input → canonical block output, indent option,
multi-doc, error line/col, empty doc.

### toml-parse — TOML Parser · 50 · `toml,config,parse,validate,cargo`
**Core** `TomlTool`: `TomlToJson(string)` — `Tomlyn.Toml.ToModel` →
walk `TomlTable` graph → JsonNode → pretty JSON (dates → ISO strings);
`Validate(string)` → first diagnostic message with line/col (Tomlyn
`DiagnosticsBag`); `JsonToToml(string)` — JsonNode → TomlTable → `Toml.FromModel`
(root must be an object; JSON null → error "TOML has no null").
**UI** bidirectional TOML / JSON editors (TOML plain, JSON Json-syntax),
live-debounced; status line.
**Tests** tables/arrays-of-tables/inline tables → JSON shape, datetime
handling, both directions, null rejection, diagnostics line info.

---

## Batch E — Data Format: XML / CSV / SQL / GraphQL (`ToolCategory.DataFormat`)

### xml-format — XML Formatter & Validator · 60 · `xml,format,pretty,minify,validate`
**Core** `XmlFormatTool`: `Format(string, XmlFormatOptions(int IndentSize, bool
UseTabs, bool OmitDeclaration))` — `XDocument.Parse(text,
LoadOptions.PreserveWhitespace? no — default)`, write via `XmlWriter` with
settings; `Minify(string)`; `Validate(string)` → `XmlError(int Line, int Col,
string Message)` from `XmlException`.
**UI** identical shape to json-format (editors plain syntax; toolbar Format /
Minify / indent ComboBox / CheckBox "Omit XML declaration"; live validate).
**Edge** preserve CDATA; preserve comments; DTDs: set
`XmlResolver = null`, `DtdProcessing.Prohibit` → friendly error (XXE safety).
**Tests** format sample (stable expected), minify, CDATA preserved, error
line/col, DTD input rejected safely.

### xml-json — XML ↔ JSON Converter · 70 · `xml,json,convert`
**Core** `XmlJsonTool`: `XmlToJson(string)` — attributes → `"@attr"`, text
content → `"#text"` (or direct string when element has no attrs/children),
repeated sibling elements → array; `JsonToXml(string, string rootName)` —
reverse mapping, arrays → repeated elements, scalars → text; invalid element
names sanitized (`item` fallback + `name` attribute). Document the mapping in
a `Text.Sub` note.
**UI** bidirectional editors XML / JSON, root-name TextBox (default "root",
used only JSON→XML), live-debounced.
**Tests** attrs/@, #text mixed content, sibling array both ways, root name,
JSON scalar at root, round-trip stability on a representative doc.

### csv-json — CSV ↔ JSON Converter · 80 · `csv,json,convert,delimiter,tsv`
**Core** `CsvJsonTool` (CsvHelper): `CsvToJson(string, CsvOptions(char?
Delimiter /*null=auto: try , ; \t |*/, bool HasHeader, bool InferTypes))` →
JSON array of objects (no header → `col1..colN`); type inference: int, double
(invariant), bool, null for empty. `JsonToCsv(string, char delimiter)` — array
of flat objects; union of keys = columns; nested values → JSON-stringified
cell; proper quoting via CsvHelper.
**UI** bidirectional editors CSV (plain) / JSON (Json); options: delimiter
ComboBox (Auto/,/;/Tab/|), CheckBoxes header + infer types; live-debounced;
status "N rows × M columns".
**Edge** quoted fields with embedded delimiter/newlines; ragged rows → error
with row number; BOM stripped; CRLF/LF both fine.
**Tests** round-trip, auto-detect `;`, quotes/newlines in fields, no-header
mode, type inference on/off, ragged row error.

### sql-format — SQL Formatter · 90 · `sql,format,pretty,query,minify`
**Core** `SqlFormatTool.Format(string, SqlFormatOptions(bool UppercaseKeywords,
int IndentSize))` + `Minify(string)`. Hand-rolled, dependency-free,
dialect-agnostic: tokenizer (single/double-quoted strings, `--` and `/* */`
comments, numbers, identifiers incl. `[bracketed]`/backticks, punctuation) —
then layout: newline before major clauses (SELECT FROM WHERE GROUP BY HAVING
ORDER BY LIMIT UNION JOIN variants INSERT UPDATE DELETE SET VALUES WITH),
SELECT-list items one per line indented, AND/OR at line starts indented under
WHERE, parenthesized subqueries indented one level. Keep it deterministic and
conservative — never reorder/alter tokens; strings/comments pass through
verbatim. Minify: collapse whitespace outside strings/comments to single
spaces, strip comments (option not needed — always strip in minify).
**UI** json-format shape: INPUT/OUTPUT plain editors, Format/Minify buttons,
Uppercase keywords CheckBox (default on), indent ComboBox.
**Tests** 5 representative statements (simple select, join+where+and, insert
values, subquery, CTE) with exact expected output pinned; strings with
keywords inside untouched; comments preserved on format, stripped on minify.

### graphql-format — GraphQL Formatter · 100 · `graphql,format,query,schema,gql`
**Core** `GraphQlTool.Format(string)` — GraphQL-Parser:
`Parser.Parse(text)` → `SDLPrinter` (indent 2) to string; `Minify(string)` →
strip insignificant whitespace (printer with no indentation or hand-collapse);
`Validate(string)` → syntax error message with location from
`GraphQLSyntaxErrorException`.
**UI** INPUT/OUTPUT plain editors, Format/Minify buttons, live validate status.
**Tests** query + variables + fragments format stably, schema SDL formats,
minify, syntax error carries line info.

---

## Batch F — Web Dev: Colors & Minifiers (`ToolCategory.WebDev`)

### color-convert — Color Converter · 10 · `color,hex,rgb,hsl,hsb,hsv,css`
**Core** `ColorTool`: `Parse(string)` accepting `#rgb #rgba #rrggbb #rrggbbaa`,
`rgb(a)(…)` (0-255 or %), `hsl(a)(…)`, `hsb/hsv(…)`, bare `rrggbb`; →
record `ParsedColor(byte R,G,B,A)`. Converters `ToHex(bool alpha)`, `ToRgbCss`,
`ToHslCss`, `ToHsb` + the math both directions (single source of truth,
invariant culture, round to 1 decimal). Also `Luminance` + `ContrastRatio(a,b)`
(WCAG) for a bonus "contrast vs black/white" line.
**UI** one input TextBox (any format) + live swatch (Border 64×64, rounded,
shows color incl. alpha checkerboard not needed — solid ok) + R/G/B/A
TextBoxes (0-255) that also drive it. OUTPUT rows: HEX, RGB, HSL, HSB each
readonly + Copy. `Text.Sub`: contrast ratio vs white and black with AA/AAA
badges.
**Tests** all parse formats, hex↔hsl↔rgb known triples (e.g. #336699 → hsl
210 50% 40%), alpha handling, invalid input throws, contrast of black/white=21.

### css-minify — CSS Minifier / Beautifier · 20 · `css,minify,beautify,format`
**Core** `CssTool`: `Minify(string)` → `NUglify.Uglify.Css` (surface
`result.HasErrors` messages); `Beautify(string, int indent)` → hand-rolled:
tokenize respecting strings/comments/parens → one selector per line, `{`
same line, one declaration per line indented, blank line between rules,
preserve `/*!` comments, nested at-rules indent. Deterministic.
**UI** INPUT/OUTPUT plain editors; buttons Minify / Beautify; status shows
size before → after (− %).
**Tests** minify sample, beautify sample pinned, string with `{` inside
untouched, media query nesting, savings math.

### js-minify — JavaScript Minifier · 30 · `javascript,js,minify,uglify`
**Core** `JsTool.Minify(string)` → `NUglify.Uglify.Js`; map NUglify errors
(line/col/message) into result record `MinifyResult(string? Code,
IReadOnlyList<string> Errors, int BeforeBytes, int AfterBytes)`.
**UI** INPUT/OUTPUT plain editors, Minify button, size savings status, errors
listed inline (`Text.Error`, each on a line). Permanent `Text.Sub` note:
"Classic minifier — very new syntax (e.g. class fields, `??=`) may be
unsupported."
**Tests** basic function minifies smaller, error surfaces line info, unicode
strings survive, empty input.

### html-minify — HTML Minifier · 40 · `html,minify,compress`
**Core** `HtmlTool.Minify(string, HtmlMinifyOptions(bool RemoveComments, bool
CollapseWhitespace))` → `NUglify.Uglify.Html` with settings mapped; result
record like js-minify.
**UI** INPUT/OUTPUT plain editors, options CheckBoxes (both default on),
Minify button, savings status.
**Tests** comment removal on/off, whitespace collapse, `<pre>` content
preserved, savings math.

### lorem-ipsum — Lorem Ipsum Generator · 60 · `lorem,ipsum,placeholder,text,dummy`
**Core** `LoremTool.Generate(LoremOptions(LoremUnit Unit /*Words,Sentences,
Paragraphs*/, int Count, bool StartClassic, bool HtmlParagraphs, int? Seed))` —
embedded classic lorem word corpus (~70 words); sentences 6–14 words,
capitalized, period; paragraphs 4–7 sentences; seeded `Random` for
reproducibility (unseeded = random).
**UI** unit RadioButtons, count TextBox, CheckBoxes "Start with 'Lorem ipsum
dolor sit amet…'" (default on) and "Wrap paragraphs in <p>", Generate button,
output pane + Copy + word/char count status.
**Tests** seeded output deterministic, unit counts respected, classic start,
`<p>` wrapping, count 0 → error.

---

## Batch G — Web Dev: Visual (`ToolCategory.WebDev`)

### markdown-preview — Markdown Live Preview · 50 · `markdown,md,preview,render`
**Core** `MarkdownTool.ToHtml(string)` — Markdig with
`UseAdvancedExtensions()` (tables, task lists, autolinks…); `WrapDocument
(string bodyHtml)` → full HTML doc with embedded dark CSS (readable: ~66ch
line length, `#1E2126` bg / `#F2F4F7` text / `#0A84FF` links, mono blocks
with subtle card bg, table borders) — CSS lives as a const string.
**UI** left: plain editor (input); right: `WebView2` inside `Card.Editor`.
Live render debounced 300 ms via `NavigateToString(WrapDocument(html))`.
Initialize WebView2 async in Loaded (CoreWebView2 EnsureCoreWebView2Async with
default env; if it throws, show inline error "WebView2 runtime unavailable"
in place of preview). Disable context menus/devtools via settings. "Copy HTML"
button (body only).
**Edge** script tags render inert (WebView2 sandbox is fine for local
preview but set `Settings.IsScriptEnabled = false`); huge docs debounce.
**Tests** (Core only) headings/bold/table/tasklist produce expected HTML
fragments, WrapDocument contains CSS + body.

### data-uri — Data URI Converter · 70 · `datauri,base64,inline,image,mime`
**Core** `DataUriTool`: `Encode(byte[] data, string mimeType)` →
`data:<mime>;base64,<payload>`; `EncodeText(string text, string mimeType)`
(charset=utf-8, percent-encoded not base64 — offer both via bool);
`Decode(string dataUri)` → record `DataUriParts(string MimeType, bool IsBase64,
byte[] Data)`; `GuessMime(string fileExtension)` — embedded map (~40 common:
png jpg gif webp svg ico css js json woff2 pdf txt html …, fallback
`application/octet-stream`).
**UI** TabControl: ENCODE (file picker + drag-drop like file-checksum, or
text input + mime TextBox auto-filled from extension; output mono pane + Copy
+ size note incl. "+33% vs original") / DECODE (input pane → shows mime, size,
and if `image/*` a preview `Image` from bytes; "Save as…" button).
**Edge** >2 MB file → warning note (URI will be huge) but proceed; malformed
data URI → clear error; base64 vs percent forms both decode.
**Tests** encode/decode round-trip bytes, text percent-encoding form, mime
guess, malformed inputs throw.

### svg-path — SVG Path Visualizer · 80 · `svg,path,d,vector,bezier,visualize`
**Core** `SvgPathTool.Analyze(string d)` → record `SvgPathInfo(double MinX,
MinY, Width, Height, int CommandCount, IReadOnlyList<string> Commands)` —
WPF's `Geometry.Parse` uses the same grammar; do bounds via
`Geometry.Parse(d).Bounds` **in the App layer**; Core does a light tokenizer
for command list/validation (letters MmLlHhVvCcSsQqTtAaZz + number runs) so
it stays testable without WPF.
**UI** path TextBox.Mono top (~80px, seeded with a sample heart path);
canvas: `Path` element inside a `Viewbox` (Stretch=Uniform) inside
`Card.Editor` — `Data` bound from parse; options: Fill CheckBox (on),
Stroke CheckBox (on, 2px `Brush.Accent`, fill `Brush.AccentSoft`); status:
bounds + command count; parse error inline.
**Edge** relative vs absolute commands; arcs; scientific notation numbers;
invalid command letter → error naming it.
**Tests** (Core tokenizer) command extraction, invalid letter throws, number
parsing incl. `1e-5`, empty path.

### placeholder-image — Placeholder Image Generator · 90 · `placeholder,image,dummy,png,mock`
**Core** `PlaceholderTool.Options` record (Width, Height, Background hex,
Foreground hex, string? Label /*null → "W×H"*/, ImageKind Png|Jpeg). Rendering
is WPF so it lives in the **App layer** (code-behind or a small
`Delp.App/Tools/WebDev/PlaceholderRenderer.cs`): `DrawingVisual` — bg rect,
centered label (Segoe UI, size = clamp(min(w,h)/5, 10, 96)) → 
`RenderTargetBitmap` → Png/JpegBitmapEncoder bytes. Core just validates
options (1 ≤ w,h ≤ 4096) and computes default label — testable.
**UI** width/height TextBoxes (600×400), bg/fg color TextBoxes (defaults
`#2A2E34` / `#8A919B`) with mini swatches, label TextBox (placeholder shows
W×H), format RadioButtons, live preview `Image` (checker not needed), buttons
"Save PNG…"/"Copy image" (Clipboard.SetImage).
**Tests** (Core) validation ranges, default label text, hex color validation.

### url-parser — URL Parser & Builder · 100 · `url,parse,query,params,builder,punycode`
**Core** `UrlTool.Parse(string)` → record `UrlParts(string Scheme, string Host,
string HostUnicode /*IdnMapping both ways*/, int? Port, string Path,
IReadOnlyList<QueryParam(string Key, string Value)> Query, string Fragment,
string? UserInfo)` — query parsed by hand to preserve order/duplicates/empty
values, values percent-decoded. `Build(UrlParts)` → canonical URL with proper
re-encoding.
**UI** URL input top; below, a two-column "table" (ItemsControl): each part
labeled + selectable readonly TextBox; QUERY section: editable rows Key/Value
+ delete button per row + "Add parameter"; REBUILT URL readonly pane at
bottom + Copy, updating live as rows edit.
**Edge** no scheme → assume https w/ note; punycode hosts show both forms;
`?a&b=` (flag & empty) preserved; fragment with `?` inside.
**Tests** parse/build round-trip, duplicate keys order preserved, IDN host
both directions, port default elision, flag params.

---

## Batch H — Text Processing: Analysis (`ToolCategory.TextProcessing`)

### case-convert — Case Converter · 10 · `case,camel,snake,kebab,pascal,title`
**Core** `CaseTool.Tokenize(string)` — split on whitespace, `_-./`, and
lower→Upper boundaries + letter/digit boundaries (`HTTPServer2Go` →
`http, server, 2, go`; acronym runs stay one token until the last cap before a
lower). Converters: `camelCase, PascalCase, snake_case, SCREAMING_SNAKE,
kebab-case, Train-Case, Title Case, Sentence case, lowercase, UPPERCASE,
dot.case, path/case` — each a method using the shared tokenizer.
**UI** input pane top; ItemsControl of 12 rows [style name (`Text.Section`) |
converted readonly mono TextBox | Copy], live.
**Tests** tokenizer edge cases (acronyms, digits, mixed separators), every
converter on a shared fixture, empty input, unicode letters.

### regex-test — Regex Tester · 20 · `regex,regexp,pattern,match,test,replace`
**Core** `RegexTool.Run(string pattern, string input, RegexToolOptions(bool
IgnoreCase, bool Multiline, bool Singleline, bool IgnoreWhitespace))` →
`RegexRunResult(IReadOnlyList<MatchInfo> Matches, string? Error)`;
`MatchInfo(int Index, int Length, string Value, IReadOnlyList<GroupInfo>
Groups)`; `GroupInfo(string Name, string Value, bool Success)`. Always
construct `Regex` with 2 s timeout; catch `ArgumentException` (bad pattern) and
`RegexMatchTimeoutException` → Error. `Replace(pattern, input, replacement,
options)` → string with same protections.
**UI** PATTERN TextBox (mono) + flags CheckBoxes row + ".NET flavor"
`Text.Sub` tag. TEST TEXT: `RichTextBox` (readonly=false is hard to highlight
live — instead: plain TextBox.Mono input + a readonly `RichTextBox` "MATCHES
VIEW" below it showing the text with match runs highlighted
(`Brush.AccentSoft` background runs, alternating with a second tint for
adjacent matches)). MATCHES panel right or below (responsive `Grid`):
ListBox — `Match 1 [4..9] "value"` with expandable group lines indented.
TabControl second tab REPLACE: replacement TextBox + live result pane.
Status: "N matches in 12 ms" or the error.
**Perf** debounce 300 ms; input >1 MB → only first 1 MB processed + note.
**Tests** groups (named/numbered), flags behavior, timeout on catastrophic
pattern `(a+)+$` vs `aaaa…b` (assert Error not hang), replace with `$1`,
invalid pattern error.

### regex-library — Regex Pattern Library · 30 · `regex,patterns,library,cheatsheet,common`
**Core** `RegexLibrary.All` — static list of `RegexEntry(string Name, string
Pattern, string Description, string Example, string Category)` — ≥ 22 entries:
email, URL (http/s), IPv4, IPv6, UUID, ISO date, ISO datetime, 24h time,
semver, hex color, slug, US phone, E.164 phone, credit card (generic 13-19),
IBAN (generic), MAC address, Windows file path, HTML tag, trailing whitespace,
duplicated word, number (int/float), Base64 string, JWT shape. Each pattern
must compile — a test enforces it — and match its own Example.
**UI** search TextBox top (`TextBox.Search`, filters name/description);
master-detail: ListBox left (Name + Category subline), detail right: Name
(`Text.Title`-ish but smaller), description, PATTERN readonly mono + Copy,
EXAMPLE readonly mono showing the example and a live "matches ✓" check.
**Tests** every entry compiles with timeout, every Example matches its
Pattern, names unique, search helper (if any) filters.

### string-diff — String Diff · 40 · `diff,compare,text,delta,changes`
**Core** `DiffTool.Compute(string oldText, string newText, DiffToolOptions(
bool IgnoreCase, bool IgnoreWhitespace))` → DiffPlex:
`SideBySideDiffBuilder` → map into serializable records
`DiffPane(IReadOnlyList<DiffLine(int? Number, string Text, DiffKind Kind
/*Unchanged,Inserted,Deleted,Modified,Imaginary*/, IReadOnlyList<DiffPiece>
SubPieces)>)` for both sides + summary counts (+N −M lines).
**UI** two input panes top (OLD/NEW, each ~120px); below, side-by-side
readonly result: two synchronized `ItemsControl`s inside one `ScrollViewer`
(shared Grid, two columns) — line rows: line number (`Brush.Fg2`, right-
aligned 36px) + text mono; row background `#2E57D9A3`-style tints: use
`Brush.Success`/`Brush.Danger` at low opacity via `Border Opacity` — define
local `SolidColorBrush` in code from theme colors with alpha 0x30 (allowed:
derive from theme brushes, don't invent new hues). Options CheckBoxes; summary
status "+12 −4".
**Tests** insert/delete/modify detection, ignore-case, ignore-whitespace,
identical inputs → all Unchanged, empty sides.

### line-sort — Line Sorter & Deduplicator · 50 · `sort,dedupe,lines,unique,shuffle`
**Core** `LineTool.Process(string text, LineToolOptions(SortMode Mode
/*None,Asc,Desc,Natural,Length,Numeric/*, bool CaseInsensitive, bool Dedupe,
bool TrimLines, bool RemoveEmpty, bool Reverse, bool Shuffle, int? Seed))` →
`LineResult(string Text, int Before, int After)`. Natural sort: compare digit
runs numerically (hand-roll comparer). Numeric: parse leading number
(invariant), non-numeric lines sort last stable. Shuffle after sort if both
set (document: shuffle wins); seeded shuffle for tests.
**UI** INPUT/OUTPUT panes; options in a `WrapPanel`: sort ComboBox, CheckBoxes
(case-insensitive, dedupe, trim, remove empty, reverse, shuffle); live; status
"120 → 87 lines".
**Tests** each mode incl. natural (`a2 < a10`), dedupe case-insensitive, order
of operations (trim→filter→sort→dedupe→reverse), seeded shuffle deterministic.

### text-stats — Text Statistics · 60 · `count,words,characters,lines,statistics`
**Core** `TextStatsTool.Analyze(string)` → record with: Chars, CharsNoSpaces,
Words (unicode-aware split), UniqueWords (case-insensitive), Lines,
NonEmptyLines, Sentences (split on `.!?` followed by space/EOL, ignore common
abbreviations `e.g. i.e. etc. Mr. Dr.`), Paragraphs (blank-line separated),
Utf8Bytes, AvgWordLength, ReadingTimeSeconds (200 wpm),
`IReadOnlyList<(string Word,int Count)> TopWords(int n)` excluding a small
stopword set (~40 English).
**UI** input pane left/top; stats as a 2-col label/value ItemsControl; TOP
WORDS section: top 10 as `word × 12` lines. Live (debounce 300 ms).
**Tests** fixture text with known counts for every stat, sentence
abbreviation handling, empty text all zeros, reading time math.

---

## Batch I — Text Processing: Conversion (`ToolCategory.TextProcessing`)

### string-escape — String Escape / Unescape · 70 · `escape,unescape,json,csv,sql,quotes`
**Core** `EscapeTool` with per-target `Escape`/`Unescape`: JSON
(`JsonEncodedText`-style; unescape via JsonDocument of `"..."`), XML/HTML
(five entities), CSV (RFC 4180 quoting), C# (verbatim off: standard escapes),
JavaScript (single-quote string), SQL (single-quote doubling), Regex
(`Regex.Escape`/`Unescape`), URL (component). Enum `EscapeTarget` + dispatch.
**UI** target ComboBox top; bidirectional PLAIN / ESCAPED panes (targets
where unescape is meaningless — Regex? it has Unescape; all support both).
**Tests** per target: escape fixture + round-trip where defined; CSV field
with quote+comma+newline; SQL `O'Brien`; JSON control chars.

### whitespace — Whitespace Visualizer & Cleaner · 80 · `whitespace,tabs,spaces,trailing,invisible`
**Core** `WhitespaceTool`: `Visualize(string)` → replace space→`·` tab→`→`
CR→`␍` LF→`␊` (keep real newlines too), NBSP→`⍽`, zero-width chars→`�`-style
markers `‹ZWSP›`; `Clean(string, WhitespaceCleanOptions(bool TrimTrailing,
bool TrimLeading, bool CollapseSpaces, bool TabsToSpaces, int TabWidth, bool
SpacesToTabs, bool RemoveEmptyLines, bool CollapseEmptyLines, LineEnding
Normalize /*None,Lf,CrLf*/, bool StripZeroWidth))` → cleaned + change count.
**UI** INPUT pane; VISUALIZED readonly pane (mono, `Brush.Fg2`-ish glyphs are
fine as text); CLEANED readonly pane + Copy; options WrapPanel of CheckBoxes +
tab width TextBox + line-ending ComboBox; status "N changes".
**Tests** each option isolated, tab↔spaces round trips at width 4, zero-width
stripping, mixed CRLF/LF normalize, visualize glyph mapping.

### number-base — Number Base Converter · 90 · `binary,octal,decimal,hex,radix,base`
**Core** `BaseTool`: `Parse(string, int? baseHint)` → `BigInteger` — auto
prefix detect `0x 0b 0o` (and bare digits use hint/10); accepts `_`/space
separators, optional leading `-`. `ToBase(BigInteger, int radix, bool
uppercase, int groupSize /*0=none*/)` for radix 2–36 (hand-roll division loop —
BigInteger has no base-N ToString).
**UI** four linked mono TextBoxes (BINARY/OCTAL/DECIMAL/HEX) — edit any,
others update (shared Run guard); plus CUSTOM row: radix TextBox (2-36) +
value. Options: Uppercase CheckBox, "Group digits" CheckBox (4 for bin/hex,
3 for dec). Status: bit length + byte count.
**Edge** huge numbers (hundreds of digits) fine via BigInteger; invalid digit
for radix → error naming digit & position; negative numbers.
**Tests** cross-conversions with knowns, prefixes, grouping output, negatives,
invalid digit error, radix 36, big 256-bit value.

### unix-time — Unix Timestamp ↔ Date · 100 · `unix,epoch,timestamp,date,utc`
**Core** `EpochTool`: `Detect(string)` → `(long Value, EpochUnit Unit)` —
digits-only heuristic: ≥17 digits → µs? use: 10 digits→s, 13→ms, 16→µs, 17+→
100ns ticks? Keep: s/ms/µs by length 10/13/16 (also 9-or-fewer=s). `ToDate
(long, EpochUnit)` → DateTimeOffset (range-check → error). `FromDate
(DateTimeOffset)` → record with seconds & millis. `Humanize(DateTimeOffset,
DateTimeOffset now)` → "3 days ago"/"in 2 h".
**UI** NOW card: live current epoch s + ms (DispatcherTimer 1 s, stop when
unloaded via Unloaded event) + Copy each. EPOCH→DATE: input TextBox → rows:
Local ISO, UTC ISO, RFC 1123, relative; unit auto-detected note + manual unit
ComboBox (Auto/s/ms/µs). DATE→EPOCH: TextBox accepting ISO/`yyyy-MM-dd
HH:mm[:ss]` (parse `DateTimeOffset.TryParse` invariant then local styles) +
"Now" button → seconds + ms rows with Copy.
**Tests** detection heuristics, round trips, range errors (year 30000),
humanize buckets, parse formats.

### epoch-batch — Epoch Batch Converter · 110 · `epoch,batch,timestamps,convert,logs`
**Core** `EpochBatchTool.Convert(string multiline, EpochUnit? forceUnit)` →
rows `EpochRow(string Input, string? LocalIso, string? UtcIso, string? Error)` —
per line: trim, extract first integer token (lines may be `1712345678, foo`),
reuse EpochTool. `ToCsv(rows)` and `ToTable(rows)` (aligned plain text).
**UI** INPUT pane (one per line); OUTPUT readonly pane — aligned text table
`input → local → utc` (mono); unit ComboBox (Auto/s/ms/µs); buttons Copy
table / Copy CSV; status "42 converted, 3 errors" with errors inline in rows.
**Tests** mixed units auto per line, garbage lines produce Error rows not
crashes, CSV shape, forced unit.

### url-slug — URL Slug Generator · 120 · `slug,url,seo,kebab,permalink`
**Core** `SlugTool.Make(string, SlugOptions(char Separator /*'-','_'*/, bool
Lowercase, int? MaxLength, bool RemoveStopwords))` — Unicode NFD normalize,
strip combining marks (é→e), ß→ss, æ→ae, ø→o, đ→d + small explicit map;
non-alphanumeric → separator; collapse repeats; trim separators; stopwords
(~30 English: a,the,and,or,of…) removed word-wise when flag set; MaxLength
cuts at word boundary.
**UI** input TextBox (single line, but accept paste of multi-line → first
line); OUTPUT readonly + Copy; options: separator RadioButtons, CheckBoxes
lowercase (on)/remove stopwords, max length TextBox (blank = none). Live.
**Tests** diacritics table, "Hello, World & Friends!" → `hello-world-friends`,
stopwords, max length word boundary, underscore separator, CJK passthrough
(kept as-is — document).

### unicode-inspect — Unicode String Inspector · 130 · `unicode,codepoint,utf8,grapheme,invisible,debug`
**Core** `UnicodeTool.Inspect(string)` → `UnicodeReport(int Utf16Units, int
Codepoints, int Graphemes, int Utf8Bytes, IReadOnlyList<CharInfo> Chars)`;
`CharInfo(string Glyph, string CodepointHex /*U+1F600*/, string Utf8Hex,
UnicodeCategory Category, bool Invisible /*zero-width, bidi controls, NBSP,
variation selectors*/, string? Warning)` — iterate by codepoint (char.
ConvertToUtf32), graphemes via `StringInfo.GetTextElementEnumerator`.
**UI** input TextBox top; summary line (4 counts); ItemsControl table rows:
glyph (boxed, `Card` mini Border 28×28 centered; invisible chars show their
abbreviation like `ZWSP` in `Brush.Warning`) | U+XXXX | UTF-8 bytes |
category. Rows with warnings tinted. Cap display at first 500 codepoints +
note.
**Tests** emoji ZWJ family counts (1 grapheme, N codepoints), invisible
detection (ZWSP, RLO), UTF-8 byte hex, empty string.

---

## Batch J — Dev Utilities: Generators (`ToolCategory.DevUtilities`)

### qr-code — QR Code Generator · 10 · `qr,qrcode,barcode,wifi,link`
**Core** `QrTool.CreatePng(string content, int pixelsPerModule, EccLevel level)`
→ byte[] (QRCoder: `QRCodeGenerator` + `PngByteQRCode`, map ECC enum). Helper
`WifiPayload(ssid, password, WifiAuth auth, bool hidden)` → `WIFI:T:WPA;S:…;;`
string (escape `;,:"\`).
**UI** TabControl: TEXT/URL (multiline TextBox) | WI-FI (SSID, password,
auth ComboBox WPA/WEP/None, hidden CheckBox). Shared below: ECC ComboBox
(L/M/Q/H default M), size ComboBox (S/M/L → 6/10/16 px per module), live QR
`Image` (bytes → BitmapImage) on white `Border` padding 12 (quiet zone),
buttons "Save PNG…" / "Copy image".
**Tests** (Core) png bytes start with PNG magic, wifi payload escaping, empty
content throws, ECC levels produce different sizes.

### cron-parse — Cron Expression Parser · 20 · `cron,crontab,schedule,quartz`
**Core** `CronTool.Explain(string expr)` → `CronReport(string Human,
IReadOnlyList<DateTime> NextLocal /*10*/, IReadOnlyList<(string Field, string
Value, string Meaning)> Fields, bool HasSeconds)` — detect 5 vs 6 fields;
human text via CronExpressionDescriptor (`ExpressionDescriptor.GetDescription`);
occurrences via Cronos (`CronExpression.Parse` with `CronFormat.IncludeSeconds`
when 6 fields) from `DateTime.UtcNow` → convert to local; per-field meanings
hand-written (minute/hour/day-of-month/month/day-of-week + optional seconds).
**UI** expression TextBox (mono, default `*/15 9-17 * * 1-5`); HUMAN line
(`Text.Sub` but bigger — 14px accent-ish); FIELDS table (3-col ItemsControl);
NEXT 10 RUNS list (local time + relative). Live; errors inline. Note: "5-field
standard cron; 6th leading field = seconds (Quartz-style)."
**Tests** human text for fixtures, next-occurrence correctness for a fixed
`from` time (Cronos accepts from param — expose it for tests), 6-field
seconds, invalid expr error, field table values.

### git-branch — Git Branch Name Generator · 30 · `git,branch,name,slug,ticket`
**Core** `BranchTool.Make(string description, BranchOptions(string Type
/*feature|bugfix|hotfix|chore|release|custom*/, string? Ticket, string
Template /*"{type}/{ticket}-{slug}" default*/, int MaxLength /*60*/))` —
slugify via SlugTool rules but also enforce git ref rules: no `..`, no
leading/trailing `/. -`, no `~^:?*[\`, no `@{`, no `.lock` suffix; empty
segments collapse; ticket uppercased if matches `[A-Za-z]+-\d+`.
`Validate(string branchName)` → list of violations (for a second "check a
name" mini-mode).
**UI** description TextBox; ticket TextBox (optional); type ComboBox +
editable custom; template TextBox (advanced, default shown); OUTPUT readonly
+ Copy + `git checkout -b <name>` line + Copy. Below divider: CHECK A NAME
input → ✓ valid or violation list.
**Tests** template substitution, ticket normalization, git-illegal chars
stripped, length trim at word boundary, validator catches each rule.

### semver — SemVer Comparator · 40 · `semver,version,compare,range,precedence`
**Core** `SemverTool` (Semver package, `SemVersion`): `Parse(string)` →
breakdown record; `Compare(string a, string b)` → -1/0/1 + explanation string
("differ at minor: 2 < 5"; prerelease rules: "1.0.0-alpha < 1.0.0 (prerelease
precedes release)"); `Satisfies(string version, string range)` — implement
common range ops by hand on SemVersion: exact, `^x.y.z`, `~x.y.z`, `>=`, `>`,
`<=`, `<`, `=`, and space-joined AND (document supported subset in UI).
**UI** two version TextBoxes side by side → big comparison verdict line
(`1.2.3  <  1.3.0` with accent on operator) + explanation + per-part
breakdown table (major/minor/patch/prerelease/build with per-row differ
highlight). RANGE CHECK section: version + range inputs → ✓/✗ + note listing
supported operators.
**Tests** SemVer 2.0 precedence fixtures incl. prerelease chains
(alpha < alpha.1 < alpha.beta < beta < beta.2 < beta.11 < rc.1 < release),
build metadata ignored in compare, each range operator, invalid version error.

### ascii-art — ASCII Art Text · 50 · `ascii,art,figlet,banner,text`
**Core** `AsciiArtTool.Render(string text, string fontName)` — Figgle:
map font names to `FiggleFonts` properties via a static dictionary of ~15
fonts (Standard, Big, Small, Slant, SmSlant, Banner, Doom, Block, Bubble,
Digital, Ivrit, Mini, Script, Shadow, Starwars — verify each exists on
`FiggleFonts` at compile time). `FontNames` list for the UI.
**UI** input TextBox (single line); font ComboBox; readonly mono output pane
(no wrap, horizontal scroll) + Copy; live.
**Tests** render "Hi" per 3 fonts non-empty & multi-line, unknown font throws,
FontNames all resolve.

---

## Batch K — Dev Utilities: References & Network (`ToolCategory.DevUtilities`)

### http-status — HTTP Status Codes · 60 · `http,status,codes,404,500,reference`
**Core** `HttpStatusData.All` — static `IReadOnlyList<HttpStatusEntry(int Code,
string Name, string Class, string Summary, string When /*guidance*/, string?
Rfc)>` — ALL standard codes 100–511 (IANA registry) + 418, 425, 451; Summary
1 sentence, When 1–2 sentences of practical guidance ("Return when the client
sent a syntactically valid request that fails business validation → prefer
422 over 400 when…"-style). ≥ 60 entries. `Search(string)` filters code
prefix/name/summary.
**UI** search TextBox + class filter (ComboBox All/1xx…5xx); master-detail:
ListBox of `code Name` grouped by class; detail: big code + name, class badge,
summary, guidance, RFC line.
**Tests** completeness (contains 100,101,200…511 specific list), unique codes,
search by "teapot" and "404", every entry has non-empty fields.

### mime-lookup — MIME Type Lookup · 70 · `mime,content-type,extension,media`
**Core** `MimeData`: static map ext→mime (≥ 180 common: web, images, av,
fonts, docs, archives, code, data) + reverse mime→extensions;
`LookupByExtension(".png")`, `LookupByMime("image/png")`,
`Search(string)` → entries matching either side.
**UI** one search TextBox ("Type an extension or MIME type…"); results
ItemsControl rows `.ext ↔ type/subtype` + Copy on the mime; detail-less flat
list is fine; status "N results".
**Tests** both directions, multi-ext mimes (jpeg: .jpg/.jpeg), search
substring, unknown → empty, case-insensitivity, leading-dot optional.

### port-lookup — Port Number Reference · 80 · `port,tcp,udp,well-known,service`
**Core** `PortData.All` — ≥ 130 entries `PortEntry(int Port, string Protocol
/*TCP,UDP,TCP/UDP*/, string Service, string Description)` — well-known +
registered dev-relevant (20,21,22,23,25,53,67,68,80,110,123,143,161,194,443,
445,465,514,587,631,636,993,995,1080,1433,1521,1723,2049,2181,25565,27017,
3000,3306,3389,4200,5000,5044,5173,5432,5672,5900,6379,6443,8000,8080,8443,
9000,9092,9200,11211, etc. incl. K8s/Elastic/Kafka/Redis/Mongo/Postgres/
MySQL/RDP/VNC and common dev servers). `Search(string)` matches port exact/
prefix or service/description substring.
**UI** search TextBox; results ItemsControl rows: port (mono, right-aligned) |
proto badge | service | description. Status count.
**Tests** key ports present with right service, search "postgres" and "54",
uniqueness of (port,protocol,service) tuples, all descriptions non-empty.

### ip-info — IP Address Info · 90 · `ip,ipv4,ipv6,cidr,subnet,private`
**Core** `IpTool`: `Analyze(string)` — `IPAddress.Parse`; report record:
Version, canonical form, integer form (v4: uint; v6: BigInteger hex),
binary dotted form (v4), PTR name (`in-addr.arpa`/`ip6.arpa`),
classification (loopback, private RFC1918, link-local, CGNAT 100.64/10,
multicast, documentation ranges, unique-local, IPv4-mapped v6, global) —
implement range checks by hand. `AnalyzeCidr(string cidr)` → network,
broadcast (v4), first/last usable, host count (BigInteger for v6), prefix
mask dotted (v4). `LocalAdapters()` (App-callable but keep in Core via
`NetworkInterface` — it's testable enough to smoke-test non-throwing) →
name, description, IPv4s with prefix, IPv6s, status.
**UI** input TextBox (IP or CIDR, auto-detect `/`); ANALYSIS as label/value
rows; classification as a colored badge line (`Brush.Warning` for
private/special, `Brush.Success` for global). LOCAL ADAPTERS section:
refresh Button + ItemsControl cards (only Up adapters by default, CheckBox
"show all"). 100% offline — no geolocation; note says so.
**Tests** v4/v6 parse + classifications for each special range, CIDR math
fixtures (10.0.0.0/24 → .255 broadcast, 254 usable; /31 → 2 usable per RFC
3021; v6 /64 count), PTR forms, invalid input error.

### ssl-decode — SSL/TLS Certificate Decoder · 100 · `ssl,tls,certificate,x509,pem,expiry`
**Core** `CertTool.DecodePem(string pemOrBase64)` →
`IReadOnlyList<CertInfo>` (handle multiple PEM blocks; also bare base64 DER):
Subject CN + full DN, Issuer, NotBefore/NotAfter (+ DaysRemaining, IsExpired),
SANs (parse extension 2.5.29.17 via `X509SubjectAlternativeNameExtension` on
modern .NET), key algorithm + size (RSA/ECDSA curve), signature algorithm,
serial (colon hex), SHA-1 + SHA-256 thumbprints (colon hex), IsCa (basic
constraints), EKUs (friendly names), self-signed flag (subject==issuer).
`FetchFromHost(string host, int port=443)` → same, via `TcpClient` +
`SslStream` with a callback that captures the chain and returns true
(5 s timeout, SNI = host). **Network is allowed for this fetch mode only.**
**UI** TabControl: PASTE PEM (mono pane → decode live) | FROM HOST (host
TextBox + port TextBox + "Fetch" `Button.Primary`, async, note "connects to
the host"). Result below (both tabs share): per-cert expandable-free stacked
Cards: title line CN + validity badge (green "valid 87d" / red "EXPIRED"),
label/value rows for everything, Copy on serial/thumbprints.
**Tests** decode a fixture self-signed PEM (generate one **in the test** via
`RSA.Create()` + `CertificateRequest` so no binary fixture needed): all fields,
SAN extraction, expiry math, multi-PEM chain order, garbage input error.
No network in tests.

---

## Batch L — Popular Additions (mixed categories)

### password-generator — Password Generator · Hashing · 50 · `password,generate,secure,passphrase,strength`
**Core** `PasswordTool`: `Generate(PasswordOptions(int Length, bool Lower,
Upper, Digits, Symbols, bool ExcludeAmbiguous, bool RequireEachClass))` —
crypto RNG; RequireEachClass: generate then reject-and-retry until each
selected class present (cap 1000 tries). `GeneratePassphrase(PassphraseOptions
(int Words, char Separator, bool Capitalize, bool AppendNumber))` — embedded
word list: 1296 short common English words (4-6 letters, EFF-short-style —
generate a clean list, all lowercase, unique) in `PassphraseWords.cs`.
`EntropyBits(options)` for both modes (passphrase: words × log2(1296) + number
bits) + `StrengthLabel` (Weak <50 / Fair <70 / Strong <90 / Excellent ≥90).
**UI** TabControl: PASSWORD (length TextBox 20, class CheckBoxes, exclude-
ambiguous, require-each; count 5) | PASSPHRASE (words TextBox 5, separator
ComboBox `- . _ space`, capitalize CheckBox, append-number CheckBox).
Generate `Button.Primary`; output pane + Copy; entropy line with colored
strength label (`Brush.Danger/Warning/Success/Accent`).
**Tests** class guarantees hold across 200 gens, entropy math, word list size
= 1296 & all unique/lowercase, passphrase shape, ambiguous exclusion.

### mock-data — Mock Data Generator · DevUtilities · 110 · `mock,fake,faker,test data,seed,json`
**Core** `MockDataTool`: field kinds enum: FirstName, LastName, FullName,
Email, Username, Phone, StreetAddress, City, State, ZipCode, Country, Company,
JobTitle, Uuid, Bool, IntRange(min,max), DecimalRange(min,max,decimals),
DateBetween(from,to), IsoDateTime, Ipv4, Url, HexColor, LoremWords(n),
Password. Embedded data arrays in `MockCorpus.cs`: ≥100 first names (mixed
origins), ≥100 last names, ≥60 cities, US states, ≥15 countries, ≥40 company
patterns ("{Noun} Labs", real-ish nouns), ≥30 job titles, ≥25 street names,
email = name-based + domain pool. Deterministic via `Random(seed)` (seed
optional → random). `Generate(IReadOnlyList<FieldSpec(string Name, FieldKind
Kind, string? Options)>, int rows, int? seed)` → `List<Dictionary<string,
object?>>`; emitters: `ToJson(rows)` (pretty array), `ToCsv(rows)`,
`ToSqlInserts(rows, string table)` (quoted strings, N rows per statement=1).
Correlate FullName/Email/Username within a row (same underlying person) —
generate person once per row.
**UI** field rows editor (ItemsControl): [name TextBox | kind ComboBox |
options TextBox (visible for ranged kinds, e.g. `1..100`, `2020-01-01..
2025-12-31`) | ✕ delete]; "+ Add field" Button; default starter schema
(id: Uuid, firstName, lastName, email, city, createdAt: IsoDateTime). Row
count TextBox (10), seed TextBox (blank=random), format RadioButtons
JSON/CSV/SQL (+ table name TextBox for SQL); Generate `Button.Primary`;
output editor (Json syntax when JSON) + Copy. Status "10 rows in 3 ms".
**Tests** seeded determinism, row correlation (email contains last or first
name), each kind produces sane values, range parsing, all three emitters
(SQL quoting: O'Brien), corpus sizes.

### json-types — JSON → C# / TypeScript Types · DataFormat · 110 · `json,csharp,typescript,types,quicktype,codegen`
**Core** `JsonTypesTool`: `Infer(string json, string rootName)` → internal
schema (object props with merged types across array elements; number → int
if all integral else double; null-only → object?; mixed → object). Emitters:
`ToCSharp(schema, CSharpOptions(bool Records /*default true*/, bool
JsonPropertyNames /*default true — original names differ from PascalCase*/))`
— nested types flattened to top-level with de-duplicated PascalCase names;
`ToTypeScript(schema, TsOptions(bool Interfaces /*vs type aliases*/))` —
optional props (`?`) when a key is missing in some array elements. Keys that
aren't valid identifiers → quoted (TS) / attribute-mapped (C#).
**UI** INPUT Json editor left; right: TabControl C# / TYPESCRIPT readonly
plain editors + Copy each; root name TextBox (default "Root"); options
CheckBoxes per active tab (records / JsonPropertyName; interfaces). Live
debounced.
**Tests** nested objects/arrays, optionality from missing keys, int vs double,
name collisions (`data.user` & `meta.user` → User, User2), kebab-case keys
mapping, empty array → `object[]`/`unknown[]`, invalid JSON error.

### basic-auth — Basic Auth Header Builder · DevUtilities · 120 · `basic,auth,authorization,header,curl`
**Core** `BasicAuthTool`: `Encode(string user, string password)` →
`Authorization: Basic <b64(user:pass)>` (return record with raw b64 + full
header + curl flag `-H "Authorization: Basic …"` + `curl -u` form);
`Decode(string headerOrB64)` → (user, password) — accept full header, `Basic
xxx`, or bare b64; user may not contain `:` on encode (error).
**UI** ENCODE: user + password TextBoxes → three readonly rows (header, curl
-H, curl -u) each with Copy. DECODE: input TextBox → user/password rows.
`Text.Sub` warning: "Base64 is encoding, not encryption — only use over HTTPS."
**Tests** RFC example (Aladdin:open sesame → QWxhZGRpbjpvcGVuIHNlc2FtZQ==),
decode variants, colon-in-username error, unicode password round-trip.

---

## Batch M — Interactive & Reference Additions (mixed categories)

### color-blotter — Screen Color Picker · WebDev · 15 · `eyedropper,color picker,screen,pixel,blotter`
**Core** `ScreenColorTool` (self-contained — do NOT reference ColorTool, it may
not exist yet): `Formats(byte r, byte g, byte b)` → record with `Hex`
(`#RRGGBB`), `Rgb` (`rgb(r, g, b)`), `Hsl` (`hsl(h, s%, l%)` — own math,
invariant, 1 decimal). Fully testable.
**App layer** The picking flow (allowed exception to the UserControl-only
rule: helper `Window` classes may live in your tool folder):
"Pick from screen" `Button.Primary` → opens a borderless, transparent,
topmost, full-virtual-screen overlay Window (Left/Top/Width/Height from
`SystemParameters.VirtualScreenLeft/Top/Width/Height`, `AllowsTransparency=
True`, `Background` = `#01000000` so it hit-tests, `Cursor=Cross`,
`ShowInTaskbar=False`). While the mouse moves, sample the pixel under the
cursor via P/Invoke `GetCursorPos` (physical px) + `GetDC(IntPtr.Zero)` /
`GetPixel` / `ReleaseDC`, and show a small floating info card near the cursor
(a `Border` inside the overlay positioned in code; remember to convert
physical px → DIPs using `VisualTreeHelper.GetDpi` for positioning): 40×40
swatch + live hex label. Left-click captures and closes; Esc cancels.
Throttle sampling with a `DispatcherTimer` at ~30 ms rather than raw
MouseMove spam.
**UI (tool view)** the pick button + current color: big swatch (72×72) with
HEX / RGB / HSL readonly rows + Copy each; HISTORY: session list (newest
first, max 24) of small swatch rows — click a row to re-select, Copy per row.
`Text.Sub` note: "Multi-monitor supported. Esc cancels picking."
**Tests** (Core only) hex/rgb/hsl formatting knowns (pure white, black,
#336699 → hsl(210, 50%, 40%)), rounding.

### code-cheatsheet — Programming Cheat Sheet · DevUtilities · 130 · `cheatsheet,snippets,sorting,patterns,examples,learning`
**Core** `CodeCheatSheetData.All` — static `IReadOnlyList<CheatTopic(string
Id, string Title, string Category, string Explanation /*2-4 sentences: what it
is, when to use, complexity where relevant*/, IReadOnlyList<CodeSnippet(string
Language, string Code)>)>`. Minimum content (quality over stubs — every
snippet must be correct, idiomatic, runnable-in-context code):
- Categories & topics (≥ 18 topics): **Algorithms**: bubble sort, insertion
  sort, merge sort, quicksort, binary search; **Data structures**: stack,
  queue, hash map usage, linked list, binary tree + in/pre/post traversal;
  **Language constructs**: class + inheritance, interface/trait, struct/record,
  enum, generics, closure/lambda, error handling (try/catch + custom error),
  async/await; **Patterns**: singleton, factory, observer, builder;
  **Everyday**: read/write a text file, parse JSON, HTTP GET request,
  string formatting.
- Languages per topic: C#, Python, JavaScript, TypeScript, Java, C++, Go,
  Rust — at least 6 of these per topic (all 8 where sensible); keep language
  order consistent everywhere.
Split the content across several partial-class files by category
(CodeCheatSheetData.Algorithms.cs etc.) so no single file is enormous.
**UI** search TextBox (matches title/category/explanation); master-detail:
ListBox left grouped by Category; detail right: Title, Explanation
(`Text.Sub`), then a `TabControl` — one tab per language, each a readonly
mono pane (TextBox.Mono readonly or plain CodeEditors.Create) + Copy button.
Remember last-selected language across topics (a static field is fine).
**Tests** ≥18 topics, ≥6 languages per topic, language sets consistent, no
empty/whitespace snippet, ids unique, every snippet < 60 lines, search works.

### shell-cheatsheet — Shell Command Cheat Sheet · DevUtilities · 140 · `bash,powershell,linux,unix,commands,cheatsheet,terminal`
**Core** `ShellCheatSheetData.All` — static `IReadOnlyList<ShellEntry(string
Task /*"Find text in files recursively"*/, string Category, string Bash,
string PowerShell, string? Notes /*gotchas, flags worth knowing*/)>`.
≥ 70 entries across categories: Files & directories (list, find by name,
copy/move/delete recursive, size of dir, tail/head, watch a file, symlinks,
permissions/chmod↔icacls), Text processing (grep↔Select-String, sed-replace↔
-replace, sort/uniq, cut/awk column, count lines, diff), Processes (list,
kill by name/port, top), Network (open ports listening, curl↔Invoke-RestMethod,
download file, DNS lookup, trace route, my IP), Archives (tar/zip both ways),
Environment (vars set/read, PATH, which↔Get-Command), System (disk free,
memory, uptime, services, scheduled tasks/cron), Git one-liners (undo last
commit, amend, stash, prune branches), Misc (history search, aliases,
redirect stderr, chaining, here-docs). Commands must be correct for bash and
PowerShell 5.1+/7 respectively.
**UI** search TextBox + category ComboBox filter; results as stacked Cards:
task title, then two labeled mono rows `bash $` and `PS >` each with Copy,
Notes line (`Text.Sub`) when present. Status "N of 70 shown".
**Tests** ≥70 entries, categories non-empty, both commands non-empty for
every entry, tasks unique, search matches task/commands, no tabs/trailing
whitespace in commands.

### text-nlp — NLP Text Processor · TextProcessing · 140 · `nlp,stopwords,stemming,tokens,ngrams,frequency`
**Core** `NlpTool` (zero dependencies):
`Process(string text, NlpOptions(bool Lowercase, bool RemoveStopwords,
bool RemovePunctuation, bool RemoveNumbers, bool Stem, string?
ExtraStopwords /*comma/space separated user additions*/))` →
`NlpResult(string ProcessedText, IReadOnlyList<string> Tokens,
IReadOnlyList<(string Term,int Count)> Frequencies /*desc, then alpha*/,
int SentenceCount)`. Tokenization: unicode word boundaries (letters+digits+
apostrophes); processed text preserves original line breaks, applies the
pipeline in order lowercase → strip punct/numbers → stopwords → stem.
Embedded English stopword list (~175 standard words) in NlpStopwords.cs.
Stemming: classic **Porter stemmer**, hand-implemented faithfully (steps
1a–5b) in PorterStemmer.cs.
`Ngrams(IReadOnlyList<string> tokens, int n)` → `(string Gram,int Count)`
list desc (n = 2 or 3), grams joined with a space.
**UI** INPUT pane top (~40% height); options WrapPanel of CheckBoxes
(lowercase, remove stopwords, remove punctuation, remove numbers, stem
(Porter)) + "Extra stopwords" TextBox; below a TabControl: PROCESSED
(readonly pane + Copy) | TOKENS (readonly pane, one per line, + count in tab
header if easy — else status line) | FREQUENCIES (ItemsControl rows `term ×
count`, top 100) | BIGRAMS (top 50) | TRIGRAMS (top 50). Live, 300 ms
debounce. Status: "512 tokens · 289 unique · 14 sentences".
**Tests** Porter vectors (caresses→caress, ponies→poni, relational→relat,
conditional→condit, rational→ration, flying→fli, dies→die), stopword removal
incl. user extras, pipeline order (stemming after stopwords), ngram counts,
frequency ordering, empty input.
