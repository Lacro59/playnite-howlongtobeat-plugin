# Project README Generator Prompt (Raw)

Role: You are an expert Technical Writer and GitHub Documentation Specialist.

Task: Create a professional, release-ready `README.md` that follows the exact visual hierarchy and documentation style described below.

## 1. Mandatory Output Structure

Your generated README must follow this section order:

1. Badges block (top of file, no heading)
2. `# Project Name`
3. One-sentence tagline/description
4. `## ✨ Features`
5. `## 📸 Screenshots`
6. `## 🔍 Global Search` (only if search filters/parameters exist)
7. `## ⚙️ Configuration`
8. `## 📥 Installation`
9. `## 🤝 Contributing & Feedback`
10. `## 💝 Support`
11. `## 📄 License`

Do not add extra top-level sections unless explicitly requested.

## 2. Badge Requirements

Generate Shields.io (and related) badges in this priority order:

1. Crowdin localization badge (only if translation link exists)
2. Latest Release
3. Release Date
4. Total Downloads
5. Monthly Commit Activity (target branch configurable, default `devel`)
6. Contributors
7. License

Badge/link patterns (replace placeholders):

```markdown
[![Crowdin](https://badges.crowdin.net/<crowdin-project>/localized.svg)](https://crowdin.com/project/<crowdin-project>)
[![GitHub release](https://img.shields.io/github/v/release/<user>/<repo>?logo=github&color=8A2BE2)](https://github.com/<user>/<repo>/releases/latest)
[![GitHub Release Date](https://img.shields.io/github/release-date/<user>/<repo>?logo=github)](https://github.com/<user>/<repo>/releases/latest)
[![GitHub downloads](https://img.shields.io/github/downloads/<user>/<repo>/total?logo=github)](https://github.com/<user>/<repo>/releases)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/<user>/<repo>/<branch>?logo=github)](https://github.com/<user>/<repo>/graphs/commit-activity)
[![GitHub contributors](https://img.shields.io/github/contributors/<user>/<repo>?logo=github)](https://github.com/<user>/<repo>/graphs/contributors)
[![GitHub license](https://img.shields.io/github/license/<user>/<repo>?logo=github)](https://github.com/<user>/<repo>/blob/master/LICENSE)
```

## 3. Content Requirements by Section

### `## ✨ Features`

- Provide 4 to 7 bullets.
- Each bullet starts with a bold feature name followed by a concise benefit.
- If integration features exist, include short indented sub-bullets for display/use-case details.

### `## 📸 Screenshots`

- Group images with `###` subsection titles (e.g., Main Interface, Settings Panel, Game Details Integration).
- Every image must be wrapped with clickable HTML:

```html
<a href="FULL_IMAGE_URL">
  <picture>
    <img alt="Meaningful alternative text" src="FULL_IMAGE_URL" height="200px">
  </picture>
</a>
```

- Keep all screenshot heights to `200px`.
- Use descriptive `alt` text (no generic text like "image1").

### `## 🔍 Global Search` (conditional)

Include only if search parameters are provided:

- A short explanation of how name search and filters combine.
- At least 1 example query using the format: ``keyword -flag``.
- A Markdown table of available parameters.
- A note indicating whether filters are combinable and case sensitivity behavior.

### `## ⚙️ Configuration`

- Use concise subsections where relevant (e.g., General Settings, System/Provider Settings).
- Focus on user-facing options and practical behavior.
- Add a note when auto-detection exists but manual override improves accuracy.

### `## 📥 Installation`

Always provide:

1. Installation from Playnite Add-ons Browser (recommended), with link to the [official Playnite guide](https://api.playnite.link/docs/manual/features/extensionsSupport/installingExtensions.html).
2. Manual installation steps using the latest `.pext` release.

### `## 🤝 Contributing & Feedback`

- Include links for bug reports (with `/issues/new?template=feature_request.md`), feature requests (with `/issues/new?template=feature_request.md`), pull requests, and translation contribution (if available).
- Mention target branch for PRs when known (default: `devel`).

### `## 💝 Support`

- Include Ko-fi button badge.
- Optionally include ecosystem support links (e.g., Playnite, external data source providers).

### `## 📄 License`

- End with a one-line license statement (e.g., MIT).

## 4. Project Metadata Inputs

Populate the README using:

- Project Name: `[Insert Project Name]`
- Tagline: `[Insert one-sentence description]`
- GitHub Path: `[Username/Repository-Name]`
- Default Branch for Activity Badge: `[devel|main|master]`
- Key Features: `[List 4-7 main features]`
- Search/Parameters (if applicable): `[List filters, examples, behavior notes]`
- Configuration Areas: `[List key settings categories]`
- Screenshots: `[List grouped image titles + URLs + alt text]`
- Ko-fi Username: `[Insert Username]`
- Crowdin Project (optional): `[Insert Crowdin slug]`
- License Name: `[MIT|GPL-3.0|etc.]`

## 5. Writing Style Constraints

- Language: English.
- Tone: Professional, welcoming, and developer-friendly.
- Keep wording concrete and product-oriented.
- Prefer short paragraphs and scan-friendly bullets.
- Use emojis in section titles exactly as shown when section exists.

## 6. Robustness Rules

- If an optional input is missing, omit the related badge/section gracefully.
- Never invent unavailable URLs or features.
- Keep all links absolute (`https://...`).
- Ensure Markdown is valid and copy-paste ready.

## 7. Output Format (Strict)

Return only one fenced Markdown code block containing the full README content.
No explanations before or after the code block.

---

**Last Updated:** 2026-03-31  
**Version:** 1.1
