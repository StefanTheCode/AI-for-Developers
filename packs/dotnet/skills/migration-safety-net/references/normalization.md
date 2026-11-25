# Normalization rules for parity tests

Old WebForms HTML and new Razor/Blazor HTML will never be byte-identical. Compare what the user sees and what clients depend on - not the markup.

## Always remove

- Hidden fields: `__VIEWSTATE`, `__VIEWSTATEGENERATOR`, `__EVENTVALIDATION`, `__EVENTTARGET`, `__EVENTARGUMENT`, `__LASTFOCUS`, `__RequestVerificationToken`
- `<script>`, `<style>`, `<noscript>`, HTML comments
- `id`, `name`, `class`, `style` attributes (WebForms generates `ctl00_MainContent_...`)
- Whitespace differences (collapse to one space, trim)
- Response headers except: `Content-Type` (media type only), `Location` (path + query only, no host)

## Compare by type

| compare | What is compared |
|---|---|
| `status` | status code |
| `status+location` | status code + redirect path, with a mapping table for renamed login URLs |
| `html` | visible text of the `select` element (default `main`), plus `href` and `src` values (path only), in document order |
| `json` | parsed JSON, object keys sorted, numbers as decimals, property names case-insensitive |
| `bytes-type` | content type + body length > 0 (images, files) |

## Per-page ignore rules

Add `"ignore": ["css selector", ...]` to a scenario for things that change on every request or on purpose:

- dates and times ("Generated 2026-11-25 15:22"), counters, "last login"
- random recommendations, ads, A/B test blocks
- the site header if only the layout changed (better: keep header text identical)

## Known, expected differences (log them, don't hide them)

| Old (.NET Framework) | New (.NET 10) | Usually |
|---|---|---|
| `[Authorize]` on an API with Forms auth -> 302 to the login page | 401 | accept, tell API consumers |
| `Accept: application/xml` -> XML | JSON only (unless XML formatters are added) | decide per API |
| `/Default.aspx` and `/` both serve the home page | only what you map | map both |
| Trailing whitespace / `&nbsp;` in GridView empty cells | empty | normalize |
| `DateTime` in JSON without offset (`2026-11-25T15:22:00`) | System.Text.Json writes the same for `Unspecified` kind, `Z` for UTC | check per field |
| Decimal `12.5` vs `12.50` | depends on the serializer | compare as numbers |
