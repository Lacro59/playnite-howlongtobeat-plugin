#!/usr/bin/env python3
"""Sync the HLTB Search Smoke badge line in README.md.

OK  → badge only
KO  → badge + link to open hltb-api-down issues and #254
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys

REPO = "Lacro59/playnite-howlongtobeat-plugin"
BADGE_IMG = (
    f"https://github.com/{REPO}/actions/workflows/hltb-search-smoke.yml/badge.svg"
)
BADGE_LINK = (
    f"https://github.com/{REPO}/actions/workflows/hltb-search-smoke.yml"
)
ISSUES_LINK = (
    f"https://github.com/{REPO}/issues"
    "?q=is%3Aissue+is%3Aopen+label%3Ahltb-api-down"
)
ISSUE_254 = f"https://github.com/{REPO}/issues/254"

BADGE_MARKDOWN = f"[![HLTB Search Smoke]({BADGE_IMG})]({BADGE_LINK})"
KO_SUFFIX = (
    f" — see open [hltb-api-down]({ISSUES_LINK}) issues "
    f"([#254]({ISSUE_254}))"
)

# Match the badge line even if a previous KO suffix is present.
# Do not let \s consume newlines (keeps the blank line before the H1).
LINE_RE = re.compile(
    r"^\[!\[HLTB Search Smoke\]\([^\)]+\)\]\([^\)]+\)(?: —.*)?[ \t]*$",
    re.MULTILINE,
)


def build_line(status: str) -> str:
    if status == "ok":
        return BADGE_MARKDOWN
    if status == "ko":
        return BADGE_MARKDOWN + KO_SUFFIX
    raise ValueError(f"Unsupported status: {status}")


def sync_readme(readme_path: pathlib.Path, status: str) -> bool:
    text = readme_path.read_text(encoding="utf-8")
    new_line = build_line(status)
    match = LINE_RE.search(text)
    if not match:
        raise RuntimeError(
            f"Could not find HLTB Search Smoke badge line in {readme_path}"
        )

    updated = text[: match.start()] + new_line + text[match.end() :]
    # Preserve a single trailing newline after the badge line block if present.
    if updated == text:
        return False

    readme_path.write_text(updated, encoding="utf-8", newline="\n")
    return True


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--status",
        choices=("ok", "ko"),
        required=True,
        help="ok = badge only; ko = badge + issue links",
    )
    parser.add_argument(
        "--readme",
        type=pathlib.Path,
        default=pathlib.Path("README.md"),
    )
    args = parser.parse_args(argv)

    changed = sync_readme(args.readme, args.status)
    print(f"README badge status={args.status} changed={changed}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
