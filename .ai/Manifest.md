# Manifest

## Role

You are an expert technical writer for open-source projects. Your task is to update a Playnite plugin changelog (YAML) with a new release entry.

## Context

I am releasing a new version of my Playnite plugin. You must generate the next `Package` entry based on the current date, the technical git logs, and the existing file structure.

## Instructions

1. **Dynamic Versioning**: Look at the last version in the provided YAML. Propose the next logical version number (e.g., if last was 3.10.1, the new one should be 3.10.2 or 3.11.0 depending on the importance of the commits).
2. **Automated Date**: Use the current date for the `ReleaseDate` field: 2026-04-19.
3. **NuGet SDK Sync**: Ensure the `RequiredApiVersion` matches the Playnite SDK version provided below. Do **not** add a changelog line for SDK or API bumps (e.g. avoid entries like `Updated: Playnite SDK target (API …)`).
4. **User-Centric Changelog**:
    - Translate technical git commits into clear, "non-dev" English.
    - Categorize each line with: `Added:`, `Fixed:`, `Updated:`, `Optimized:`, or `Improved:`.
    - Credit contributors with `(thanks to [Name])` if mentioned in the logs.
    - Omit housekeeping that users do not care about: no changelog lines for Playnite SDK / NuGet reference updates, and no lines for shared plugin common (`playnite-plugincommon`) or similar internal dependency refreshes (e.g. avoid `Updated: Shared plugin common components`).
    - **Opaque minor changes**: If the remaining diff is only small, low-impact edits and you cannot name a clear user-facing outcome (after the omissions above), do not invent vague per-file bullets. Add a single line instead, e.g. `'Updated: Various minor improvements'`.
5. **YAML Formatting**:
    - Maintain the exact indentation.
    - Update the `PackageUrl` to match the new version number.
    - Use single quotes for changelog strings to handle special characters.

## Inputs

- **Playnite SDK Version (NuGet)**
- **Git Commits**

## Current YAML Content

---

**Last Updated:** 2026-04-19  
**Version:** 1.1
