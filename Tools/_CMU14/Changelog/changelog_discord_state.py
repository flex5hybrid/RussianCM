"""Delivery journal on a separate branch, independent of upstream YAML and Actions history."""

import base64
import json
import os
import time
from pathlib import Path
from urllib.parse import quote

import requests


def empty_state():
    return {"version": 2, "baseline": [], "delivered": {}, "pending": None}


def load_state(raw):
    state = json.loads(raw)
    if isinstance(state, dict) and state.get("version") == 1:
        # Preserve the prepared payload verbatim, including confirmed chunks.
        state.pop("translations", None)
        state["version"] = 2
    return validate_state(state)


def validate_state(state):
    if (not isinstance(state, dict) or state.get("version") != 2
            or not isinstance(state.get("baseline"), list)
            or any(not isinstance(key, str) for key in state["baseline"])
            or not isinstance(state.get("delivered"), dict)
            or "pending" not in state):
        raise ValueError("Invalid Discord delivery journal; refusing to reset it")
    pending = state.get("pending")
    if pending is not None:
        if (not isinstance(pending, dict) or not isinstance(pending.get("keys"), list)
                or not pending["keys"] or any(not isinstance(key, str) for key in pending["keys"])
                or not isinstance(pending.get("chunks"), list)
                or not pending["chunks"]
                or not isinstance(pending.get("receipts"), list)
                or any(not isinstance(receipt, str) or not receipt for receipt in pending["receipts"])
                or len(pending["receipts"]) != pending.get("next")
                or any(not isinstance(chunk, str) for chunk in pending["chunks"])
                or not isinstance(pending.get("next"), int)
                or not 0 <= pending["next"] <= len(pending["chunks"])):
            raise ValueError("Invalid pending Discord delivery")
        if pending.get("in_flight") is not None and (
            pending["in_flight"] != pending["next"] or pending["next"] == len(pending["chunks"])
        ):
            raise ValueError("Invalid in-flight Discord delivery")
    return state


class FileStateStore:
    def __init__(self, path):
        self.path = Path(path)

    def load(self):
        if not self.path.exists():
            return None
        return load_state(self.path.read_text(encoding="utf-8"))

    def save(self, state):
        validate_state(state)
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temporary = self.path.with_suffix(".tmp")
        temporary.write_text(json.dumps(state, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        temporary.replace(self.path)


class GitHubStateStore:
    def __init__(self, repo, token, branch="codex/changelog-discord-state", session=None):
        self.branch = branch
        self.session = session or requests.Session()
        self.session.headers.update({"Authorization": f"Bearer {token}",
                                     "Accept": "application/vnd.github+json",
                                     "X-GitHub-Api-Version": "2022-11-28"})
        self.base = f"{os.environ.get('GITHUB_API_URL', 'https://api.github.com')}/repos/{repo}"
        self.path = "discord-delivery.json"
        self.sha = None
        self.last_write = None

    def call(self, method, path, **kwargs):
        if method != "GET":
            # A backlog requires two writes per Discord message. Leave room
            # under GitHub's per-minute content creation limit.
            if self.last_write is not None:
                time.sleep(max(0, 1 - (time.monotonic() - self.last_write)))
            self.last_write = time.monotonic()
        response = self.session.request(method, self.base + path, timeout=30, **kwargs)
        if response.status_code not in {200, 201, 404}:
            # Never print request URLs/headers or provider exception text containing credentials.
            raise RuntimeError(f"Delivery journal API returned HTTP {response.status_code}; stopped")
        return response

    def load(self):
        response = self.call("GET", f"/contents/{self.path}", params={"ref": self.branch})
        if response.status_code == 404:
            # Distinguish a missing branch from a deleted journal on an existing branch.
            ref = self.call("GET", f"/git/ref/heads/{quote(self.branch, safe='/')}")
            if ref.status_code == 200:
                raise RuntimeError("Delivery journal is missing on the state branch; refusing to reset")
            return None
        data = response.json()
        self.sha = data["sha"]
        if data.get("content"):
            raw = base64.b64decode(data["content"])
        else:
            blob = self.call("GET", f"/git/blobs/{self.sha}")
            if blob.status_code == 404:
                raise RuntimeError("Delivery journal blob is missing")
            raw = base64.b64decode(blob.json()["content"])
        return load_state(raw)

    def save(self, state):
        validate_state(state)
        if self.sha is None:
            # Create the state branch from an existing commit; no checkout or force-push.
            head = os.environ.get("GITHUB_SHA")
            if not head:
                raise RuntimeError("GITHUB_SHA is required to initialize the delivery journal")
            self.call("POST", "/git/refs", json={"ref": f"refs/heads/{self.branch}", "sha": head})
        body = {"message": "Record Discord changelog delivery [skip ci]", "branch": self.branch,
                "content": base64.b64encode(json.dumps(state, ensure_ascii=False).encode()).decode()}
        if self.sha:
            body["sha"] = self.sha
        response = self.call("PUT", f"/contents/{self.path}", json=body)
        if response.status_code == 404:
            raise RuntimeError("Cannot write delivery journal")
        self.sha = response.json()["content"]["sha"]
