#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AI Changelog Generator — генерирует публичные релизные заметки из GitHub PR
с помощью Gemini 2.0 Flash и отправляет в Discord.

Запускается после существующего шага с YAML-ченджлогом.
"""

import json
import os
import re
import time
from dataclasses import dataclass, field
from typing import Any, Optional

import requests

import config_changelog as cfg


# ─── Dataclasses ────────────────────────────────────────────────────────────────

@dataclass
class CommitInfo:
    sha: str
    message: str


@dataclass
class VideoAttachment:
    url: str
    filename: str
    content_type: str


@dataclass
class PRInfo:
    number: int
    title: str
    body: str
    html_url: str
    labels: list[str]
    commits: list[CommitInfo]
    videos: list[VideoAttachment] = field(default_factory=list)
    author: str = ""


@dataclass
class ChangelogResult:
    features: list[str]
    fixes: list[str]
    improvements: list[str]
    videos: dict[str, str]  # описание -> url видео


# ─── GitHub Client ─────────────────────────────────────────────────────────────

class GitHubClient:
    """Обёртка над GitHub REST API."""

    def __init__(self, token: str, repo: str):
        self.token = token
        self.repo = repo
        self.base = cfg.GITHUB_API_URL
        self.session = requests.Session()
        self.session.headers.update({
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "AI-Changelog-Generator/1.0",
        })
        if self.token:
            self.session.headers["Authorization"] = f"Bearer {self.token}"

    def _get(self, path: str, **kwargs) -> requests.Response:
        url = f"{self.base}/repos/{self.repo}{path}"
        resp = self.session.get(url, timeout=30, **kwargs)
        resp.raise_for_status()
        return resp

    def _get_json(self, path: str, **kwargs) -> Any:
        return self._get(path, **kwargs).json()

    def _paginate(self, path: str, **kwargs) -> list[Any]:
        """Fetch all pages of a list endpoint."""
        results = []
        url = f"{self.base}/repos/{self.repo}{path}"
        kwargs.setdefault("timeout", 30)

        while url:
            resp = self.session.get(url, **kwargs)
            resp.raise_for_status()
            data = resp.json()

            if isinstance(data, list):
                results.extend(data)
            elif isinstance(data, dict) and "items" in data:
                results.extend(data["items"])
            else:
                break

            url = None
            if "next" in resp.links:
                url = resp.links["next"]["url"]
                kwargs.pop("params", None)
            elif isinstance(data, list) and len(data) < 30:
                break

        return results

    # ── Определение границы: последний successful workflow run ──────────────────

    def get_last_successful_run_sha(self, current_run_id: str) -> tuple[str, str, str]:
        """
        Находит SHA и ID последнего успешного workflow run (не текущего).
        Возвращает (sha, run_id, tag_name).
        """
        runs = self._paginate(
            "/actions/runs",
            params={"status": "success", "per_page": 20},
        )

        for run in runs:
            if str(run["id"]) != str(current_run_id):
                sha = run["head_commit"]["id"]
                tag_name = self._get_nearest_tag(sha)
                print(f"  [GITHUB] Last successful run: #{run['id']} SHA={sha} tag={tag_name}")
                return sha, str(run["id"]), tag_name

        return "", "", ""

    def _get_nearest_tag(self, sha: str) -> str:
        """
        Находит ближайший тег, который <= данному SHA.
        Использует compare API: сравнивает тег с SHA.
        """
        try:
            # Получаем список тегов
            refs = self._get_json("/git/refs/tags", params={"per_page": 100})
            if not refs:
                return ""

            tag_names = [
                r["ref"].replace("refs/tags/", "")
                for r in refs
                if r["ref"].startswith("refs/tags/")
            ]

            # Для каждого тега — получить его commit SHA
            for tag_name in reversed(tag_names):
                try:
                    tag_ref = self._get_json(f"/git/refs/tags/{tag_name}")
                    tag_sha = tag_ref["object"]["sha"]

                    # Сравнить: tag_sha...sha (base...head)
                    comp = self._get_json(
                        f"/compare/{tag_sha}...{sha}",
                        params={"per_page": 1},
                    )

                    # statuses: ahead, behind, identical
                    # ahead = 0  → тег <= sha (подходит как верхняя граница)
                    # behind > 0 → тег > sha
                    if comp.get("ahead_by", -1) == 0:
                        print(f"  [GITHUB] Nearest tag for {sha[:8]}: {tag_name}")
                        return tag_name
                except Exception:
                    pass

        except Exception as e:
            print(f"  [GITHUB] Tag lookup failed: {e}")

        return ""

    # ── Получение коммитов между двумя SHA ─────────────────────────────────────

    def get_commits_between(self, base_sha: str, head_sha: str) -> list[dict]:
        """
        Получает все коммиты между base_sha и head_sha через GitHub Compare API.
        base — старый (от последнего run), head — новый (текущий HEAD).
        Возвращает список коммитов с их данными.
        """
        if not base_sha:
            print("  [GITHUB] No base SHA, fetching commits from last 100...")
            base_sha = "HEAD~100"

        try:
            comp = self._get_json(f"/compare/{base_sha}...{head_sha}")
            commits = comp.get("commits", [])
            ahead = comp.get("ahead_by", 0)
            behind = comp.get("behind_by", 0)
            print(f"  [GITHUB] Compare {base_sha[:8]}...{head_sha[:8]}: "
                  f"ahead={ahead}, behind={behind}, total_commits={len(commits)}")
            return commits
        except Exception as e:
            print(f"  [GITHUB] Compare API failed: {e}, falling back to recent commits")
            return []

    # ── Получение PR по номерам коммитов ───────────────────────────────────────

    def get_pr_for_commit(self, sha: str) -> Optional[dict]:
        """
        Находит PR, который содержит данный коммит (через Statuses API или
        через поиск /search/commits).

        GitHub не имеет прямого API "какой PR содержит этот коммит",
        поэтому используем поиск.
        """
        try:
            # Search for commits — но требует auth с repo scope
            results = self.session.get(
                f"{self.base}/search/commits",
                params={
                    "q": f"{sha} repo:{self.repo}",
                    "per_page": 5,
                },
                headers={
                    "Accept": "application/vnd.github.cloak-preview+json",
                },
                timeout=30,
            )
            if results.status_code == 200:
                items = results.json().get("items", [])
                for item in items:
                    if item.get("sha") == sha:
                        pr_url = item.get("repository", {}).get("pull_request_url")
                        if pr_url:
                            return self._get_json(pr_url.replace(self.base, ""))
            return None
        except Exception:
            return None

    def get_associated_prs(self, commit_sha: str) -> Optional[list[dict]]:
        """
        Получает PR, ассоциированные с коммитом, через /commits/{sha}/pulls.
        """
        try:
            resp = self._get(f"/commits/{commit_sha}/pulls")
            return resp.json()
        except Exception:
            return None

    # ── Получение всех PR между двумя SHA ───────────────────────────────────────

    def get_prs_between_shhas(self, base_sha: str, head_sha: str) -> list[PRInfo]:
        """
        Главный метод: получает PR, смерженные между двумя SHA.

        Алгоритм:
        1. Получить все коммиты через compare API (base...head)
        2. Для каждого коммита — найти ассоциированный PR
        3. Собрать уникальные PR
        4. Для каждого PR — получить title, body, commits, labels, videos
        """
        commits_data = self.get_commits_between(base_sha, head_sha)

        if not commits_data:
            print("  [GITHUB] No commits found between SHAs, trying PR list fallback")
            return self._get_merged_prs_fallback(head_sha)

        # Собрать уникальные PR по merge commit SHA
        pr_map: dict[int, dict] = {}
        seen_commits = set()

        for commit in commits_data:
            sha = commit["sha"]
            if sha in seen_commits:
                continue
            seen_commits.add(sha)

            # Попробовать получить PR через associated PRs
            prs = self.get_associated_prs(sha)
            if prs:
                for pr in prs:
                    pr_num = pr.get("number")
                    if pr_num and pr.get("merged_at"):
                        if pr_num not in pr_map:
                            pr_map[pr_num] = pr

        if not pr_map:
            print("  [GITHUB] No merged PRs found via commits, trying search fallback")
            return self._get_merged_prs_fallback(head_sha)

        print(f"  [GITHUB] Found {len(pr_map)} unique merged PRs")

        # Обогатить каждый PR
        result = []
        for pr_num, pr in pr_map.items():
            try:
                # Получить полные данные PR (важно: с labels и body)
                full_pr = self._get_json(f"/pulls/{pr_num}")
                labels = [l["name"] for l in full_pr.get("labels", [])]
                videos = self._extract_videos_from_body(full_pr.get("body", "") or "")

                # Получить коммиты этого PR
                pr_commits_resp = self._get_json(f"/pulls/{pr_num}/commits")
                commit_infos = [
                    CommitInfo(
                        sha=c["sha"],
                        message=c["commit"]["message"].split("\n")[0],
                    )
                    for c in pr_commits_resp
                ]

                pr_info = PRInfo(
                    number=pr_num,
                    title=full_pr.get("title", ""),
                    body=full_pr.get("body", "") or "",
                    html_url=full_pr.get("html_url", ""),
                    labels=labels,
                    commits=commit_infos,
                    videos=videos,
                    author=full_pr.get("user", {}).get("login", "unknown"),
                )
                result.append(pr_info)

            except Exception as e:
                print(f"  [WARNING] Failed to enrich PR #{pr_num}: {e}")

        return result

    def _get_merged_prs_fallback(self, head_sha: str) -> list[PRInfo]:
        """
        Fallback: если compare API не сработал — берём все merged PR
        отсортированные по updated_at и берём последние N.
        Ограничено — работает если PR обновляются при merge.
        """
        try:
            prs = self._paginate(
                "/pulls",
                params={"state": "closed", "sort": "updated", "direction": "desc", "per_page": 50},
            )

            result = []
            for pr in prs:
                if not pr.get("merged_at"):
                    continue

                try:
                    labels = [l["name"] for l in pr.get("labels", [])]
                    videos = self._extract_videos_from_body(pr.get("body", "") or "")
                    pr_commits = self._get_json(f"/pulls/{pr['number']}/commits")

                    pr_info = PRInfo(
                        number=pr["number"],
                        title=pr.get("title", ""),
                        body=pr.get("body", "") or "",
                        html_url=pr.get("html_url", ""),
                        labels=labels,
                        commits=[
                            CommitInfo(
                                sha=c["sha"],
                                message=c["commit"]["message"].split("\n")[0],
                            )
                            for c in pr_commits
                        ],
                        videos=videos,
                        author=pr.get("user", {}).get("login", "unknown"),
                    )
                    result.append(pr_info)

                except Exception as e:
                    print(f"  [WARNING] Failed PR #{pr['number']}: {e}")

                if len(result) >= 30:  # Limit
                    break

            print(f"  [GITHUB] Fallback found {len(result)} merged PRs")
            return result

        except Exception as e:
            print(f"  [GITHUB] PR fallback failed: {e}")
            return []

    # ── Поиск тега по SHA ──────────────────────────────────────────────────────

    def get_latest_tag(self) -> str:
        """Получить самый новый тег (по семантической версии)."""
        try:
            refs = self._get_json("/git/refs/tags", params={"per_page": 100})
            if not refs:
                return ""

            tag_names = [
                r["ref"].replace("refs/tags/", "")
                for r in refs
                if r["ref"].startswith("refs/tags/")
            ]

            # Семантическая сортировка
            def parse_tag(name: str) -> tuple:
                parts = re.split(r"[.\-_]", name.lstrip("vV"))
                return tuple(int(m) if m.isdigit() else m for m in parts)

            tag_names.sort(key=parse_tag, reverse=True)
            return tag_names[0] if tag_names else ""

        except Exception as e:
            print(f"  [GITHUB] Tag fetch failed: {e}")
            return ""

    # ── Извлечение видео из body PR ────────────────────────────────────────────

    def _extract_videos_from_body(self, body: str) -> list[VideoAttachment]:
        """Извлекает URL видео из текста body PR."""
        videos = []
        seen = set()

        patterns = [
            # Прямые ссылки на медиа
            r"https?://[^\s<>\"\']+\.(?:mp4|webm|mov)(?:\?[^\s<>\"\']*)?",
            r"https?://[^\s<>\"\']+\.gif(?:\?[^\s<>\"\']*)?",
            # YouTube / Vimeo
            r"https?://(?:www\.)?(?:youtube\.com/watch\?v=|youtu\.be/|vimeo\.com/)[^\s<>\"\']+",
            # Imgur gifv
            r"https?://[^\s<>\"\']+\.gifv",
        ]

        for pattern in patterns:
            for match in re.finditer(pattern, body, re.IGNORECASE):
                url = match.group().rstrip(").,")
                if url not in seen:
                    seen.add(url)
                    filename = url.split("/")[-1].split("?")[0]
                    videos.append(VideoAttachment(
                        url=url,
                        filename=filename,
                        content_type=self._guess_content_type(url),
                    ))

        return videos

    def _guess_content_type(self, url: str) -> str:
        lower = url.lower()
        if ".mp4" in lower:
            return "video/mp4"
        if ".webm" in lower:
            return "video/webm"
        if ".mov" in lower:
            return "video/quicktime"
        if ".gif" in lower or ".gifv" in lower:
            return "image/gif"
        if "youtube" in lower or "youtu.be" in lower:
            return "video/youtube"
        if "vimeo" in lower:
            return "video/vimeo"
        return "video/mp4"


# ─── Фильтрация ────────────────────────────────────────────────────────────────

def should_skip_pr(pr: PRInfo) -> bool:
    """Проверяет, нужно ли пропустить PR."""
    pr_labels_lower = [l.lower() for l in pr.labels]
    for skip_label in cfg.SKIP_PR_LABELS:
        if skip_label.lower() in pr_labels_lower:
            print(f"  [FILTER] PR #{pr.number} пропущен: метка '{skip_label}'")
            return True

    body_lower = (pr.body + " " + pr.title).lower()
    for keyword in cfg.SKIP_KEYWORDS:
        if keyword.lower() in body_lower:
            print(f"  [FILTER] PR #{pr.number} пропущен: keyword '{keyword}'")
            return True

    return False


def filter_commits(pr: PRInfo) -> list[CommitInfo]:
    """Убирает коммиты с техническими префиксами."""
    filtered = []
    for commit in pr.commits:
        msg_lower = commit.message.lower()
        for prefix in cfg.SKIP_COMMIT_PREFIXES:
            if msg_lower.startswith(prefix.lower()):
                print(f"  [FILTER-COMMIT] '{commit.message[:60]}' ({prefix})")
                break
        else:
            filtered.append(commit)
    return filtered


# ─── Промпт для Gemini ────────────────────────────────────────────────────────

def build_pr_data(pr: PRInfo) -> str:
    """Формирует текстовый блок PR для промпта."""
    videos_info = ""
    if pr.videos:
        video_urls = [v.url for v in pr.videos]
        videos_info = f"\n[ВИДЕО: {', '.join(video_urls)}]"

    commits_info = "\n".join(
        f"  - {c.message[:200]}" for c in pr.commits
    ) if pr.commits else "  (нет коммитов)"

    return (
        f"--- PR #{pr.number} ---\n"
        f"Название: {pr.title}\n"
        f"Описание: {pr.body[:1000] if pr.body else '(нет описания)'}\n"
        f"Автор: {pr.author}\n"
        f"Коммиты:\n{commits_info}\n"
        f"{videos_info}\n"
        f"Ссылка: {pr.html_url}\n"
    )


# ─── Gemini Client ─────────────────────────────────────────────────────────────

class GeminiClient:
    """Обёртка над Gemini API."""

    def __init__(self, api_key: str):
        self.api_key = api_key
        self.url = cfg.GEMINI_API_URL

    def generate_changelog(self, pr_list: list[PRInfo]) -> ChangelogResult:
        """Отправляет PR в Gemini и получает структурированный changelog."""
        pr_blocks = "\n".join(build_pr_data(pr) for pr in pr_list)
        user_prompt = cfg.GEMINI_USER_PROMPT_TEMPLATE.format(pr_data=pr_blocks)

        payload = {
            "contents": [{"parts": [{"text": user_prompt}]}],
            "generationConfig": {
                "temperature": 0.3,
                "maxOutputTokens": 8192,
                "topP": 0.8,
                "topK": 40,
            },
            "systemInstruction": {
                "parts": [{"text": cfg.GEMINI_SYSTEM_PROMPT}]
            },
        }

        headers = {
            "Content-Type": "application/json",
            "x-goog-api-key": self.api_key,
        }

        print(f"  [GEMINI] Отправляю {len(pr_list)} PR в Gemini...")
        start = time.time()

        resp = requests.post(
            self.url, json=payload, headers=headers, timeout=120
        )
        elapsed = time.time() - start
        print(f"  [GEMINI] Ответ за {elapsed:.1f}с (status={resp.status_code})")

        if resp.status_code != 200:
            print(f"  [GEMINI] Ошибка: {resp.text[:500]}")
            resp.raise_for_status()

        raw_text = resp.json()["candidates"][0]["content"]["parts"][0]["text"]
        return self._parse_response(raw_text, pr_list)

    def _parse_response(self, raw_text: str, pr_list: list[PRInfo]) -> ChangelogResult:
        """Парсит ответ Gemini и валидирует данные."""
        text = raw_text.strip()

        # Убрать markdown-обёртку ```json ... ```
        if "```" in text:
            lines = text.split("\n")
            text = "\n".join(
                line for line in lines
                if not line.strip().startswith("```")
            )

        # Найти границы JSON-объекта
        start = text.find("{")
        end = text.rfind("}") + 1
        if start >= 0 and end > start:
            text = text[start:end]

        try:
            parsed = json.loads(text)
        except json.JSONDecodeError as e:
            print(f"  [GEMINI] JSON parse error: {e}")
            print(f"  [GEMINI] Raw:\n{raw_text[:800]}")
            return ChangelogResult(features=[], fixes=[], improvements=[], videos={})

        features = parsed.get("features", []) or []
        fixes = parsed.get("fixes", []) or []
        improvements = parsed.get("improvements", []) or []
        videos: dict = parsed.get("videos", {}) or {}

        # Привязать видео к фичам
        features = self._attach_videos(features, videos)

        return ChangelogResult(
            features=features,
            fixes=fixes,
            improvements=improvements,
            videos=videos,
        )

    def _attach_videos(self, features: list[str], videos: dict[str, str]) -> list[str]:
        """Добавляет [Демонстрация] к фичам, для которых есть видео."""
        if not videos:
            return features

        result = []
        for feature in features:
            matched_url = None
            for key, url in videos.items():
                if key.lower() in feature.lower():
                    matched_url = url
                    break

            if matched_url:
                display_url = self._format_video_url(matched_url)
                result.append(f"[Демонстрация] {feature}\n► {display_url}")
            else:
                result.append(feature)

        return result

    def _format_video_url(self, url: str) -> str:
        """Форматирует URL для отображения в Discord."""
        if url.lower().endswith((".gif", ".gifv", ".mp4", ".webm", ".mov")):
            return url
        if "youtube" in url.lower() or "youtu.be" in url.lower():
            return url
        if "vimeo" in url.lower():
            return url
        return url


# ─── Discord Sender ───────────────────────────────────────────────────────────

class DiscordSender:
    """Отправка в Discord webhook."""

    def __init__(self, webhook_url: str):
        self.webhook_url = webhook_url
        self.session = requests.Session()

    def send(self, content: str = "", embeds: list[dict] = None) -> bool:
        body = {
            "content": content,
            "embeds": embeds or [],
            "allowed_mentions": {"parse": []},
        }

        retry_count = 0
        while True:
            resp = self.session.post(self.webhook_url, json=body, timeout=30)
            if resp.status_code == 429:
                retry_count += 1
                if retry_count > 20:
                    print("[DISCORD] Слишком много retry, отмена")
                    return False
                retry_after = resp.json().get("retry_after", 5)
                print(f"[DISCORD] Rate limited, жду {retry_after}с")
                time.sleep(retry_after)
                continue
            if resp.status_code >= 400:
                print(f"[DISCORD] Ошибка {resp.status_code}: {resp.text[:300]}")
                return False
            print("[DISCORD] Отправлено")
            return True

    def build_embed(
        self,
        tag_name: str,
        result: ChangelogResult,
        repo: str,
        pr_count: int,
    ) -> dict:
        """Формирует Discord embed."""
        embed = {
            "title": f"🎉 {tag_name} — Релиз",
            "url": f"https://github.com/{repo}/releases/tag/{tag_name}",
            "color": 5814783,
            "footer": {
                "text": f"AI-generated changelog • {pr_count} PR",
            },
            "fields": [],
        }

        parts = []

        if result.features:
            items = "\n".join(
                f"▸ {f.split(chr(10))[0]}" for f in result.features[:8]
            )
            if len(result.features) > 8:
                items += f"\n▸ ...и ещё {len(result.features) - 8}"
            parts.append(f"**✨ Новые функции**\n{items}\n")

        if result.fixes:
            items = "\n".join(
                f"▸ {f.split(chr(10))[0]}" for f in result.fixes[:8]
            )
            if len(result.fixes) > 8:
                items += f"\n▸ ...и ещё {len(result.fixes) - 8}"
            parts.append(f"**🐛 Исправления**\n{items}\n")

        if result.improvements:
            items = "\n".join(
                f"▸ {f.split(chr(10))[0]}" for f in result.improvements[:8]
            )
            if len(result.improvements) > 8:
                items += f"\n▸ ...и ещё {len(result.improvements) - 8}"
            parts.append(f"**⚙️ Изменения**\n{items}\n")

        if not parts:
            parts.append("*Нет изменений для отображения.*")

        embed["description"] = "\n".join(parts)

        # Видео-поля
        video_features = [f for f in result.features if "[Демонстрация]" in f]
        if video_features:
            lines = []
            for feature in video_features:
                feat_parts = feature.split("\n")
                desc = feat_parts[0].replace("[Демонстрация] ", "")
                url = feat_parts[1].replace("► ", "") if len(feat_parts) > 1 else ""
                lines.append(f"**{desc}**\n► {url}")

            embed["fields"].append({
                "name": "🎥 Демонстрации",
                "value": "\n\n".join(lines)[:1024],
                "inline": False,
            })

            # Thumbnail — первое прямое видео
            for feature in video_features:
                parts_2 = feature.split("\n")
                if len(parts_2) > 1:
                    url = parts_2[1].replace("► ", "")
                    ext = url.lower()
                    if ext.endswith((".gif", ".gifv")):
                        embed["image"] = {"url": url}
                        break
                    elif ext.endswith((".mp4", ".webm", ".mov")):
                        embed["video"] = {"url": url}
                        break

        return embed

    def send_changelog(
        self,
        tag_name: str,
        result: ChangelogResult,
        repo: str,
        pr_count: int,
    ):
        if not result.features and not result.fixes and not result.improvements:
            print("[DISCORD] Нечего отправлять")
            return

        embed = self.build_embed(tag_name, result, repo, pr_count)
        self.send("", embeds=[embed])


# ─── Main ──────────────────────────────────────────────────────────────────────

def _resolve_git_ref(ref: str) -> str:
    """Resolve a git ref (SHA, tag, HEAD~N) to an actual SHA using local git."""
    import subprocess
    try:
        result = subprocess.run(
            ["git", "rev-parse", ref],
            capture_output=True,
            text=True,
            timeout=10,
            cwd=os.path.dirname(os.path.abspath(__file__)) + "/..",
        )
        if result.returncode == 0:
            return result.stdout.strip()
    except Exception:
        pass
    return ref  # Return original if resolution fails


def main():
    print("=" * 60)
    print("AI Changelog Generator — запуск")
    print("=" * 60)

    # ── Проверка env ────────────────────────────────────────────────────────────
    webhook_url = cfg.DISCORD_WEBHOOK_URL
    if not webhook_url:
        print("[ERROR] DISCORD_WEBHOOK_URL не задан, пропуск")
        return

    api_key = cfg.GEMINI_API_KEY
    if not api_key:
        print("[ERROR] GEMINI_API_KEY не задан, пропуск")
        return

    repo = cfg.GITHUB_REPOSITORY or os.environ.get("GITHUB_REPOSITORY", "")
    if not repo:
        print("[ERROR] GITHUB_REPOSITORY не задан")
        return

    token = cfg.GITHUB_TOKEN or os.environ.get("GITHUB_TOKEN", "")

    github = GitHubClient(token, repo)

    # ── HEAD SHA ────────────────────────────────────────────────────────────────
    head_sha = os.environ.get("GITHUB_SHA", "").strip()
    head_input = os.environ.get("INPUT_HEAD_SHA", "").strip()
    if head_input:
        resolved = _resolve_git_ref(head_input)
        head_sha = resolved
        print(f"[GIT] HEAD resolve: {head_input} -> {head_sha[:8] if head_sha else 'FAILED'}")

    if not head_sha:
        # Попробовать через локальный git
        try:
            head_sha = _resolve_git_ref("HEAD")
            print(f"[GIT] HEAD resolved: {head_sha[:8]}")
        except Exception:
            pass

    if not head_sha:
        print("[GITHUB] HEAD SHA не определена, пропуск")
        return

    # ── Base SHA ────────────────────────────────────────────────────────────────
    base_sha = os.environ.get("INPUT_BASE_SHA", "").strip()
    base_input_raw = base_sha  # для логирования

    if not base_sha:
        current_run_id = os.environ.get("GITHUB_RUN_ID", "")
        last_sha, last_run_id, tag_from_run = github.get_last_successful_run_sha(
            current_run_id
        )
        if last_sha:
            base_sha = last_sha
            tag_name = tag_from_run
        else:
            # Fallback: предыдущий тег
            tag_name = github.get_latest_tag()
            if tag_name:
                try:
                    tag_ref = github._get_json(f"/git/refs/tags/{tag_name}")
                    base_sha = tag_ref["object"]["sha"]
                except Exception:
                    base_sha = ""

    if not base_sha:
        print("[GITHUB] Base SHA не определена, пропуск")
        return

    # Разрешить git-ссылки (HEAD~N, теги и т.д.)
    if base_sha.startswith("HEAD") or not all(c in "0123456789abcdef" for c in base_sha[:8]):
        resolved = _resolve_git_ref(base_sha)
        print(f"[GIT] Base resolve: {base_sha} → {resolved[:8] if resolved else 'FAILED'}")
        base_sha = resolved

    print(f"[GITHUB] Base:  {base_sha[:8] if base_sha else 'N/A'}")
    print(f"[GITHUB] HEAD:  {head_sha[:8] if head_sha else 'N/A'}")

    # ── Тег ─────────────────────────────────────────────────────────────────────
    tag_name = tag_name or github.get_latest_tag()
    if not tag_name:
        tag_name = os.environ.get("INPUT_TAG_NAME", "Latest Release")
    print(f"[GITHUB] Тег релиза: {tag_name}")

    # ── Получить PR между SHA ───────────────────────────────────────────────────
    print("[GITHUB] Получаю PR между base и HEAD...")
    prs = github.get_prs_between_shhas(base_sha, head_sha)
    print(f"[GITHUB] Найдено {len(prs)} PR")

    if not prs:
        print("[STEP] Нет PR для обработки, пропуск Gemini")
        return

    # ── Фильтрация ──────────────────────────────────────────────────────────────
    filtered = []
    for pr in prs:
        if should_skip_pr(pr):
            continue
        pr.commits = filter_commits(pr)
        if pr.commits or pr.body:
            filtered.append(pr)

    print(f"[FILTER] После фильтрации: {len(filtered)} PR")

    if not filtered:
        print("[STEP] Нет PR после фильтра, пропуск")
        return

    # ── Gemini ─────────────────────────────────────────────────────────────────
    gemini = GeminiClient(api_key)
    result = gemini.generate_changelog(filtered)

    total = len(result.features) + len(result.fixes) + len(result.improvements)
    print(f"[GEMINI] Сгенерировано {total} пунктов:")
    print(f"  Новые функции: {len(result.features)}")
    print(f"  Исправления:   {len(result.fixes)}")
    print(f"  Изменения:     {len(result.improvements)}")
    print(f"  Видео:         {len(result.videos)}")

    if result.features:
        for f in result.features:
            print(f"    ✨ {f.split(chr(10))[0][:80]}")

    # ── Discord ─────────────────────────────────────────────────────────────────
    sender = DiscordSender(webhook_url)
    sender.send_changelog(tag_name, result, repo, len(filtered))

    print("=" * 60)
    print("AI Changelog Generator — завершён")
    print("=" * 60)


if __name__ == "__main__":
    main()
