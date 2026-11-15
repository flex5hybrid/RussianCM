#!/usr/bin/env python3
# CMU14 Begin: downstream changelog collection.
"""
Extract changelog .yml parts from merged PRs via their :cl: blocks.

The scan cursor is derived from changelog entries belonging to this repository,
not from GitHub Actions run history. An overlap plus URL deduplication makes
re-runs, scheduled runs and reusable-workflow calls safe.

usage: update_changelog_parts.py <parts-dir> [--changelog-file FILE]
                                 [--category "Main"]
"""

import argparse
import datetime
import hashlib
import os
import re
import subprocess
from pathlib import Path

import requests
import yaml
from update_changelog import CATEGORY_MAIN, CHANGELOG_CATEGORIES, entry_key, load_yaml

GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")
FALLBACK_DATE = "2025-06-01T00:00:00Z"
INITIAL_LOOKBACK_DAYS = 30
REPOSITORY_OVERLAP_DAYS = 7

COMMENT_RE = re.compile(r"<!--.*?-->", re.DOTALL)
FENCE_RE = re.compile(r"^[ \t]*(`{3,}|~{3,})[^\n]*\n.*?^[ \t]*\1[ \t]*\r?$", re.MULTILINE | re.DOTALL)
HEADER_RE = re.compile(
    r"^[ \t]*(?::cl:|🆑)[ \t]*([^\n\r]*)\r?$", re.IGNORECASE | re.MULTILINE
)
ENTRY_RE = re.compile(
    r"^[ \t]*(?:[*-][ \t]*)?(add|remove|tweak|fix|map|code|admin):[ \t]*(\S[^\n\r]*)\r?$",
    re.IGNORECASE | re.MULTILINE,
)
BLOCK_END_RE = re.compile(r"^[ \t]*(?:#{1,6}[ \t]+|:cl:|🆑)", re.IGNORECASE | re.MULTILINE)

TYPE_MAP = {
    "add": "Add",
    "remove": "Remove",
    "tweak": "Tweak",
    "fix": "Fix",
    "map": "Map",
    "code": "Code",
    "admin": "Admin",
}
DEFAULT_MESSAGES = {
    "Added fun!",
    "Removed fun!",
    "Changed fun!",
    "Fixed fun!",
    "Mapped fun!",
    "Admin related change!",
    "Code related change for contributors!",
    "Добавлено что-то новое!",
    "Удалено что-то лишнее!",
    "Изменено что-то существующее!",
    "Исправлено что-то сломанное!",
    "Изменена карта!",
    "Изменение для администраторов!",
    "Изменение в коде для разработчиков!",
}


def make_session(token: str) -> requests.Session:
    sess = requests.Session()
    sess.headers.update(
        {
            "Authorization": f"Bearer {token}",
            "X-GitHub-Api-Version": "2022-11-28",
            "Accept": "application/vnd.github+json",
        }
    )
    return sess


def parse_time(value: str) -> datetime.datetime:
    # Historical upstream files contain dates such as 2024-1-4; normalize only
    # that known representation, while still rejecting genuinely invalid dates.
    value = re.sub(r"^(\d{4})-(\d{1,2})-(\d{1,2})(?=T|$)",
                   lambda m: f"{m[1]}-{int(m[2]):02d}-{int(m[3]):02d}", str(value))
    parsed = datetime.datetime.fromisoformat(value.replace("Z", "+00:00"))
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=datetime.timezone.utc)
    return parsed.astimezone(datetime.timezone.utc)


def format_time(value: datetime.datetime) -> str:
    return value.astimezone(datetime.timezone.utc).isoformat().replace("+00:00", "Z")


def get_scan_start(entries: list[dict], repo: str, explicit_start: str | None) -> str:
    if explicit_start:
        return format_time(parse_time(explicit_start))

    repo_prefix = f"https://github.com/{repo}/pull/".lower()
    repo_times = [
        parse_time(entry["time"])
        for entry in entries
        if str(entry.get("url", "")).lower().startswith(repo_prefix) and entry.get("time")
    ]
    if repo_times:
        latest = max(repo_times) - datetime.timedelta(days=REPOSITORY_OVERLAP_DAYS)
        return format_time(latest)

    all_times = [parse_time(entry["time"]) for entry in entries if entry.get("time")]
    if all_times:
        # Migration path for a changelog that previously only contained upstream
        # entries: import a bounded recent window of local PRs instead of all history.
        latest = max(all_times) - datetime.timedelta(days=INITIAL_LOOKBACK_DAYS)
        return format_time(latest)

    return FALLBACK_DATE


def get_merged_prs(sess: requests.Session, repo: str, since: str, base: str | None = None) -> list[dict]:
    cutoff = parse_time(since)
    prs = {}
    params = {"state": "closed", "sort": "updated", "direction": "desc", "per_page": 100}
    if base:
        params["base"] = base

    # A merge also updates the PR. The pulls API includes the body and merge
    # timestamp, with no search indexing delay or 1000-result search limit.
    page = 1
    while True:
        resp = sess.get(
            f"{GITHUB_API_URL}/repos/{repo}/pulls",
            params={**params, "page": page},
            timeout=30,
        )
        resp.raise_for_status()
        for pr in resp.json():
            if parse_time(pr["updated_at"]) < cutoff:
                return sorted(prs.values(), key=lambda pr: (pr["merged_at"], pr["number"]))
            if pr["merged_at"] and parse_time(pr["merged_at"]) >= cutoff:
                prs[pr["number"]] = pr
        if "next" not in resp.links:
            return sorted(prs.values(), key=lambda pr: (pr["merged_at"], pr["number"]))
        page += 1


def parse_cl_block(body: str, pr_author: str) -> tuple[str, list[dict]] | None:
    body = FENCE_RE.sub("", COMMENT_RE.sub("", body))
    match = HEADER_RE.search(body)
    if not match:
        return None

    author = (match.group(1) or "").strip() or pr_author
    block = BLOCK_END_RE.split(body[match.end():], maxsplit=1)[0]
    changes = [
        {"type": TYPE_MAP[m.group(1).lower()], "message": m.group(2).strip()}
        for m in ENTRY_RE.finditer(block)
        if m.group(2).strip() not in DEFAULT_MESSAGES
    ]
    return (author, changes) if changes else None


def get_seen_urls(entries: list[dict], parts_dir: str) -> set[str]:
    return {entry_key(e)[1].lower() for e in
            entries + [load_yaml(path) for path in Path(parts_dir).glob("*.yml")]
            if e.get("url")}


def write_part(
    parts_dir: str,
    pr_number: int,
    author: str,
    changes: list,
    time: str,
    url: str,
    category: str,
    labels: list[str],
) -> bool:
    part = {
        "author": author,
        "changes": changes,
        "time": time,
        "url": url,
        "category": category,
    }
    if labels:
        part["labels"] = labels

    # Local and upstream repositories can use the same PR number in the shared Parts directory.
    namespace = hashlib.sha256(url.rstrip("/").lower().encode()).hexdigest()[:12]
    path = os.path.join(parts_dir, f"pr-{pr_number}-{namespace}.yml")
    if os.path.exists(path):
        print(f"Part for PR #{pr_number} already exists, skipping.")
        return False

    with open(path, "w", encoding="utf-8") as f:
        yaml.safe_dump(part, f, allow_unicode=True, sort_keys=False)
    print(f"Wrote part for PR #{pr_number} by {author}")
    return True


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("parts_dir")
    parser.add_argument("--changelog-file", default="Resources/Changelog/CMU.yml")
    parser.add_argument("--category", default=CATEGORY_MAIN, choices=CHANGELOG_CATEGORIES)
    parser.add_argument("--all", action="store_true", help="Check all supported sections for existing PRs")
    parser.add_argument("--ref", help="Only collect PRs whose merge commit is in this git ref")
    args = parser.parse_args()

    token = os.environ["GITHUB_TOKEN"]
    repo = os.environ["GITHUB_REPOSITORY"]
    start = os.environ.get("START_DATE") or None

    target = Path(args.changelog_file)
    targets = [target.with_name(f"{name}.yml") for name in CHANGELOG_CATEGORIES.values()] if args.all else [target]
    entries = [entry for path in targets if path.exists() for entry in load_yaml(path).get("Entries", [])]
    since = get_scan_start(entries, repo, start)
    seen_urls = get_seen_urls(entries, args.parts_dir)

    print(f"Fetching PRs from {repo} merged since {since}")
    sess = make_session(token)
    base = os.environ.get("GITHUB_BASE_BRANCH")
    prs = get_merged_prs(sess, repo, since, base)
    print(f"Found {len(prs)} merged PRs")

    written = 0
    for pr in prs:
        url = pr["html_url"].rstrip("/")
        if url.lower() in seen_urls:
            continue

        body = pr.get("body") or ""
        result = parse_cl_block(body, pr["user"]["login"])
        if result is None:
            print(f"PR #{pr['number']}: no non-empty :cl: block, skipping")
            continue

        if args.ref and subprocess.run(
            ["git", "merge-base", "--is-ancestor", pr["merge_commit_sha"], args.ref],
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
        ).returncode != 0:
            # The branch can advance while we are reading the PR list.
            print(f"PR #{pr['number']}: merge not present in the checkout, skipping")
            continue
        author, changes = result
        time = format_time(parse_time(pr["merged_at"]))
        labels = [label["name"] for label in pr.get("labels", []) if label.get("name")]
        normalized_labels = {label.lower() for label in labels}
        categories = [c for c in CHANGELOG_CATEGORIES if f"changelog: {c.lower()}" in normalized_labels]
        if len(categories) > 1:
            raise ValueError(f"PR #{pr['number']} has multiple changelog category labels")
        if write_part(
            args.parts_dir,
            pr["number"],
            author,
            changes,
            time,
            url,
            next(iter(categories), args.category),
            labels,
        ):
            seen_urls.add(url.lower())
            written += 1

    print(f"Done. Wrote {written} parts from {len(prs)} PRs.")


if __name__ == "__main__":
    main()
# CMU14 End
