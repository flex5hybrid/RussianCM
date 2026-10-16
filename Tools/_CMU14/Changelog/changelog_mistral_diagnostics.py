"""Small service probe and strictly filtered rate-limit diagnostics, never delivery."""

import math
import os
import re
import sys
import time

import requests


LIMIT_HEADER = re.compile(
    r"x-ratelimit-(?:limit|remaining|reset)-(?:requests|tokens)(?:-(?:second|minute|hour|day|month))?"
    r"|x-ratelimit-(?:limit|remaining|reset)-(?:rpm|tpm|rps)"
)
ERROR_TYPES = {"rate_limited", "rate_limit_error", "authentication_error", "invalid_request_error", "server_error"}
ERROR_CODES = {"unknown_model", "invalid_api_key", "insufficient_quota", "rate_limit_exceeded"}


def safe_limits(response):
    result = {}
    for name, value in response.headers.items():
        name = name.lower()
        value = str(value).strip()
        if LIMIT_HEADER.fullmatch(name) and re.fullmatch(r"-?[0-9]{1,18}(?:[.][0-9]{1,6})?", value):
            if math.isfinite(float(value)):
                result[name] = value
    return result


def zero_limits(response):
    return [name for name, value in safe_limits(response).items()
            if name.startswith("x-ratelimit-limit-") and float(value) == 0]


def print_diagnostics(response):
    details = safe_limits(response)
    try:
        data = response.json()
    except (ValueError, TypeError):
        data = {}
    if isinstance(data, dict):
        error = data.get("error", data)
        if isinstance(error, dict):
            kind, code = error.get("type"), error.get("code")
            if isinstance(kind, str) and kind in ERROR_TYPES:
                details["error_type"] = kind
            if (isinstance(code, (str, int)) and not isinstance(code, bool)
                    and (str(code) in ERROR_CODES or re.fullmatch(r"[0-9]{1,8}", str(code)))):
                details["error_code"] = str(code)
    if details:
        print("Mistral diagnostics: " + "; ".join(f"{k}={v}" for k, v in sorted(details.items())), flush=True)


def run_probe():
    token = os.environ.get("MISTRAL_API_KEY")
    if not token:
        print("MISTRAL_API_KEY is missing; create a Studio API key and update the repository secret")
        return 1
    model = os.environ.get("CHANGELOG_TRANSLATION_MODEL") or "mistral-small-latest"
    if not re.fullmatch(r"[a-z0-9][a-z0-9.-]{0,99}", model):
        print("Invalid translation model identifier")
        return 1
    headers = {"Authorization": f"Bearer {token}"}
    print(f"Checking Mistral model={model}; fixed test text only, no changelog or Discord", flush=True)
    try:
        catalog = requests.get("https://api.mistral.ai/v1/models", headers=headers, timeout=20)
        print(f"Model catalog HTTP {catalog.status_code}", flush=True)
        print_diagnostics(catalog)
        if catalog.status_code != 200:
            print("Cannot read model catalog; check the Studio key, workspace access and account limits")
            return 1
        data = catalog.json()
        models = data.get("data", []) if isinstance(data, dict) else []
        listed = any(isinstance(row, dict) and (row.get("id") == model or
                     isinstance(row.get("aliases"), list) and model in row["aliases"]) for row in models)
        print(f"Configured model listed in catalog: {listed}", flush=True)
        # Catalog and completion may share a per-second request limit.
        time.sleep(2)
        response = requests.post("https://api.mistral.ai/v1/chat/completions", headers=headers, timeout=30,
            json={"model": model, "temperature": 0, "max_tokens": 32,
                  "messages": [{"role": "user", "content": "Translate Hello into Russian. Reply with one word."}]})
        print(f"Tiny completion HTTP {response.status_code}; max_tokens=32", flush=True)
        print_diagnostics(response)
        if response.status_code == 200:
            print("Mistral accepts a tiny request. Inspect the changelog request size and current token usage.")
            return 0
        if zero_limits(response):
            print("Mistral reports a zero API limit. Waiting or replacing a key in the same workspace cannot enable access.")
        elif response.status_code == 429:
            print("Even a tiny request is rate-limited. Check model/account/workspace limits and monthly usage; batch size alone does not explain this refusal.")
        else:
            print("Check the Studio API key, model availability and workspace permissions.")
        return 1
    except (requests.RequestException, ValueError, TypeError):
        print("Mistral probe connection or JSON error; provider body and credentials were not logged")
        return 1


if __name__ == "__main__":
    sys.exit(run_probe())
