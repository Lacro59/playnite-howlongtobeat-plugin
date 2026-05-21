# Git Commit Message Convention

## 1. Template Structure

Each commit must follow this structure to keep history readable and
support changelog automation:

```text
<type>(<scope>): <description>

<body>

<footer>
```

---

## 2. Writing Guide

### Header

- **Type**: `fix`, `feat`, `docs`, `style`, `refactor`, `perf`, `test`,
  `chore`, `ci`, `build`
- **Scope**: optional. Indicates the impacted module or file
  (for example: `api`, `middleware`, `ui`)
- **Description**:
  - use imperative mood (for example: `add` instead of `added`)
  - no leading capital letter
  - no trailing period
  - maximum length: 50 characters

### Body (optional)

- explain the **why** and the **how**, not only the "what"
- always separate it from the header with one blank line
- maximum length: 72 characters per line

### Footer

- **GitHub issue reference**:
  - **When an issue exists**: include the full GitHub issue URL in the
    footer, using the keywords from section 3 (`Fixes`, `Closes`, `Ref`,
    `See`, and so on).
  - **When there is no associated issue**: do **not** invent a URL or
    placeholder link. Omit the issue line entirely. The header (and
    optional body) must still describe the change clearly enough to
    stand alone in the history.
- **Trailer policy**: do not add tool-generated trailers
  (for example: `Made-with: Cursor`)

---

## 3. GitHub Cheat Sheet (Keywords)

> [!IMPORTANT]
> The examples below are templates. Replace bracketed values with your
> project values.

### Common Actions

- **Close an issue**
  - Keywords: `Fixes`, `Closes`
  - Example:

    ```text
    Fixes https://github.com/[USER]/[REPO]/issues/[ID]
    ```

- **Reference an issue**
  - Keywords: `Ref`, `See`
  - Example:

    ```text
    Ref https://github.com/[USER]/[REPO]/issues/[ID]
    ```

- **Multiple issues**
  - Example:

    ```text
    Fixes [URL_1], Fixes [URL_2]
    ```

---

## 4. Examples

### Simple fix

```text
fix(ui): resolve button misalignment on login page
```

### Detailed fix with body and footer

```text
fix(api): handle null values in user profile update

The API was throwing a 500 error when the 'bio' field was sent as null.
Added a null-check validator before database persistence.

Fixes https://github.com/Lacro59/playnite-gameactivity-plugin/issues/456
```

### Change without a tracked issue

No `Fixes` / `Closes` / `Ref` line in the footer; the header and body
carry the full intent.

```text
chore(build): bump target framework reference in csproj

Aligns the project file with the solution-wide SDK version used locally.
```

---

**Last updated**: 2026-05-04  
**Version**: 1.5
