#!/usr/bin/env python3
"""Smoke test for HowLongToBeat search used by the Playnite plugin.

Mirrors HowLongToBeatApi.DiscoverSearchUrlAsync → GetAuthToken → ApiSearch:
1. Discover the POST /api/... path from site _app-*.js bundles
2. Call {endpoint}/init for token + hp headers
3. POST a search for a known game and validate JSON results

Exit codes:
  0 — success
  1 — discovery / auth / search / schema failure (API broken for the plugin)
  2 — HTTP 429 rate limited (transient; do not open an \"API down\" issue)
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import time
import urllib.error
import urllib.request
from typing import Any, Dict, List, Optional, Tuple
from urllib.parse import urljoin

BASE_URL = "https://howlongtobeat.com"
USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
    "AppleWebKit/537.36 (KHTML, like Gecko) "
    "Chrome/131.0.0.0 Safari/537.36"
)
ACCEPT = "application/json, text/javascript, */*; q=0.01"
SCRIPT_SRC_RE = re.compile(
    r"""<script[^>]*src=["']([^"']+)["'][^>]*>""",
    re.IGNORECASE,
)
FETCH_POST_API_RE = re.compile(
    r"""fetch\s*\(\s*["']/api/([a-zA-Z0-9_/]+)[^"']*["']\s*,\s*\{[^}]*method:\s*["']POST["'][^}]*\}""",
    re.IGNORECASE | re.DOTALL,
)

EXIT_OK = 0
EXIT_BROKEN = 1
EXIT_RATE_LIMITED = 2


class RateLimitedError(Exception):
    """Raised when HowLongToBeat returns HTTP 429."""


def log(message: str) -> None:
    print(message, flush=True)


def gha_error(message: str) -> None:
    # GitHub Actions annotation (harmless locally).
    safe = message.replace("\n", "%0A")
    print(f"::error::{safe}", flush=True)


def http_request(
    method: str,
    url: str,
    *,
    headers: Optional[Dict[str, str]] = None,
    body: Optional[bytes] = None,
    timeout: float = 30.0,
) -> Tuple[int, str]:
    req_headers = {
        "User-Agent": USER_AGENT,
        "Accept": ACCEPT,
    }
    if headers:
        req_headers.update(headers)

    request = urllib.request.Request(url, data=body, headers=req_headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            status = int(getattr(response, "status", 200) or 200)
            raw = response.read()
            text = raw.decode("utf-8", errors="replace")
            if status == 429:
                raise RateLimitedError(f"HTTP 429 for {method} {url}")
            return status, text
    except urllib.error.HTTPError as exc:
        raw = exc.read() if exc.fp is not None else b""
        text = raw.decode("utf-8", errors="replace")
        if exc.code == 429:
            raise RateLimitedError(f"HTTP 429 for {method} {url}") from exc
        return int(exc.code), text


def extract_script_urls(html: str) -> List[str]:
    urls = SCRIPT_SRC_RE.findall(html or "")
    # Prefer _app- bundles first (same ordering idea as the plugin).
    preferred = [u for u in urls if "_app-" in u]
    rest = [u for u in urls if "_app-" not in u]
    ordered: List[str] = []
    seen = set()
    for url in preferred + rest:
        if not url or url in seen:
            continue
        seen.add(url)
        ordered.append(url)
    return ordered


def absolute_url(path_or_url: str) -> str:
    if path_or_url.startswith("http://") or path_or_url.startswith("https://"):
        return path_or_url
    return urljoin(BASE_URL + "/", path_or_url.lstrip("/"))


def discover_search_endpoint(timeout: float) -> str:
    log(f"GET {BASE_URL}/")
    status, html = http_request("GET", BASE_URL + "/", timeout=timeout)
    if status < 200 or status >= 300:
        raise RuntimeError(f"Homepage HTTP {status}")

    script_urls = extract_script_urls(html)
    if not script_urls:
        raise RuntimeError("No <script src> found on homepage")

    log(f"Found {len(script_urls)} script URL(s); scanning for POST /api/...")
    for script_url in script_urls:
        full_url = absolute_url(script_url)
        try:
            status, script = http_request("GET", full_url, timeout=timeout)
        except RateLimitedError:
            raise
        except Exception as exc:  # noqa: BLE001 — continue scanning other scripts
            log(f"Skip script {full_url}: {exc}")
            continue

        if status < 200 or status >= 300:
            log(f"Skip script HTTP {status}: {full_url}")
            continue

        match = FETCH_POST_API_RE.search(script)
        if not match:
            continue

        suffix = (match.group(1) or "").strip("/")
        if not suffix:
            continue

        first_segment = suffix.split("/", 1)[0]
        if first_segment.lower() == "find":
            log(f"Ignoring legacy endpoint suffix 'find' in {full_url}")
            continue

        endpoint = "/api/" + suffix
        log(f"Discovered endpoint '{endpoint}' from {full_url}")
        return endpoint

    raise RuntimeError("No search POST /api/... endpoint discovered from scripts")


def fetch_auth(endpoint: str, timeout: float) -> Dict[str, str]:
    init_url = f"{BASE_URL}{endpoint}/init?t={int(time.time() * 1000)}"
    log(f"GET auth init {init_url}")
    status, body = http_request(
        "GET",
        init_url,
        headers={"Referer": BASE_URL},
        timeout=timeout,
    )
    if status < 200 or status >= 300:
        raise RuntimeError(f"Auth init HTTP {status}")

    try:
        data = json.loads(body)
    except json.JSONDecodeError as exc:
        raise RuntimeError("Auth init response is not JSON") from exc

    token = data.get("token")
    hp_key = data.get("hpKey")
    hp_val = data.get("hpVal")
    if not token or not hp_key or not hp_val:
        raise RuntimeError("Auth init missing token/hpKey/hpVal")

    return {"Token": str(token), "Hpkey": str(hp_key), "Hpval": str(hp_val)}


def build_search_body(game_name: str, hp_key: str, hp_val: str) -> Dict[str, Any]:
    terms = [part for part in game_name.split(" ") if part]
    if not terms:
        terms = [game_name]

    body: Dict[str, Any] = {
        "searchType": "games",
        "searchTerms": terms,
        "searchPage": 1,
        "size": 20,
        "searchOptions": {
            "games": {
                "userId": 0,
                "platform": "",
                "sortCategory": "popular",
                "rangeCategory": "main",
                "rangeTime": {"min": 0, "max": 0},
                "gameplay": {
                    "perspective": "",
                    "flow": "",
                    "genre": "",
                    "difficulty": "",
                },
                "rangeYear": {"min": "", "max": ""},
                "modifier": "",
            },
            "users": {"sortCategory": "postcount"},
            "lists": {"sortCategory": "follows"},
            "filter": "",
            "sort": 0,
            "randomizer": 0,
        },
        "useCache": True,
        hp_key: hp_val,
    }
    return body


def post_search(
    endpoint: str,
    game_name: str,
    auth: Dict[str, str],
    timeout: float,
) -> Dict[str, Any]:
    url = BASE_URL + endpoint
    payload = build_search_body(game_name, auth["Hpkey"], auth["Hpval"])
    raw = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    headers = {
        "Origin": BASE_URL,
        "Referer": BASE_URL,
        "Content-Type": "application/json",
        "x-auth-token": auth["Token"],
        "x-hp-key": auth["Hpkey"],
        "x-hp-val": auth["Hpval"],
    }
    log(f"POST search {url} game='{game_name}'")
    status, body = http_request("POST", url, headers=headers, body=raw, timeout=timeout)
    if status < 200 or status >= 300:
        raise RuntimeError(f"Search POST HTTP {status}: {body[:300]}")

    try:
        data = json.loads(body)
    except json.JSONDecodeError as exc:
        raise RuntimeError("Search response is not JSON") from exc

    if not isinstance(data, dict):
        raise RuntimeError("Search response JSON root is not an object")
    return data


def validate_search_result(result: Dict[str, Any], game_name: str) -> None:
    rows = result.get("data")
    if not isinstance(rows, list) or len(rows) == 0:
        raise RuntimeError("Search JSON has empty or missing 'data'")

    needle = game_name.casefold()
    matched = False
    for row in rows:
        if not isinstance(row, dict):
            continue
        if "game_id" not in row or "game_name" not in row:
            continue
        name = str(row.get("game_name") or "")
        if needle in name.casefold():
            matched = True
            log(f"OK: found game_id={row.get('game_id')} game_name={name!r}")
            break

    if not matched:
        # Schema may still be usable; require at least one well-formed row.
        first = next((r for r in rows if isinstance(r, dict)), None)
        if first is None or "game_id" not in first or "game_name" not in first:
            raise RuntimeError("Search rows missing game_id/game_name")
        log(
            f"WARN: no row name contains {game_name!r}; "
            f"accepted first row game_name={first.get('game_name')!r}"
        )


def parse_args(argv: Optional[List[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="HowLongToBeat search smoke test (plugin-aligned)."
    )
    parser.add_argument(
        "--game",
        default="Celeste",
        help="Game name to search (default: Celeste)",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=30.0,
        help="HTTP timeout in seconds (default: 30)",
    )
    return parser.parse_args(argv)


def main(argv: Optional[List[str]] = None) -> int:
    args = parse_args(argv)
    try:
        endpoint = discover_search_endpoint(args.timeout)
        auth = fetch_auth(endpoint, args.timeout)
        result = post_search(endpoint, args.game, auth, args.timeout)
        validate_search_result(result, args.game)
        log("Smoke test passed.")
        return EXIT_OK
    except RateLimitedError as exc:
        gha_error(str(exc))
        log(f"RATE_LIMITED: {exc}")
        return EXIT_RATE_LIMITED
    except Exception as exc:  # noqa: BLE001 — top-level CI exit mapping
        gha_error(str(exc))
        log(f"FAILED: {exc}")
        return EXIT_BROKEN


if __name__ == "__main__":
    sys.exit(main())
