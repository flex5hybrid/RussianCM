import contextlib
import io
import os
import unittest
from unittest.mock import Mock, patch

import requests

from changelog_mistral_diagnostics import print_diagnostics, run_probe, safe_limits, zero_limits
from changelog_translation import RussianTranslator, TranslationRateLimitError


def response(status, headers=None, data=None):
    return Mock(status_code=status, headers=headers or {}, json=Mock(return_value=data or {}))


class DiagnosticTests(unittest.TestCase):
    def test_only_numeric_rate_limit_headers_and_known_error_fields_are_printed(self):
        result = response(429, headers={"X-RateLimit-Limit-Tokens-Minute": "20000",
            "X-RateLimit-Remaining-Requests-Minute": "0", "Authorization": "PRIVATE",
            "x-ratelimit-limit-tokens-month": "PRIVATE", "x-ratelimit-secret": "PRIVATE"},
            data={"type": "rate_limited", "code": "1300", "message": "PRIVATE"})
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            print_diagnostics(result)
        self.assertNotIn("PRIVATE", output.getvalue())
        self.assertIn("error_code=1300", output.getvalue())
        self.assertEqual(len(safe_limits(result)), 2)
        self.assertEqual(zero_limits(result), [])

    def test_arbitrary_error_fields_are_not_logged(self):
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            print_diagnostics(response(429, data={"type": "PRIVATE", "code": "PRIVATE", "error": "PRIVATE"}))
        self.assertEqual(output.getvalue(), "")

    def test_zero_limit_stops_without_retry_but_exhausted_remaining_is_temporary(self):
        result = response(429, headers={"x-ratelimit-limit-requests-minute": "0"})
        with patch.dict(os.environ, {"MISTRAL_API_KEY": "PRIVATE"}), \
                patch("changelog_translation.requests.post", return_value=result) as post, \
                patch("changelog_translation.time.sleep") as sleep:
            with self.assertRaisesRegex(TranslationRateLimitError, "zero API limit"):
                RussianTranslator({}).translate(["Fixed radio."])
            post.assert_called_once()
            sleep.assert_not_called()
        self.assertEqual(zero_limits(response(429, headers={"x-ratelimit-limit-requests-minute": "1",
            "x-ratelimit-remaining-requests-minute": "0"})), [])

    def invoke_probe(self, catalog, completion=None):
        output = io.StringIO()
        with patch.dict(os.environ, {"MISTRAL_API_KEY": "PRIVATE", "CHANGELOG_TRANSLATION_MODEL": "mistral-small-latest"}), \
                patch("changelog_mistral_diagnostics.requests.get", return_value=catalog) as get, \
                patch("changelog_mistral_diagnostics.requests.post", return_value=completion) as post, \
                patch("changelog_mistral_diagnostics.time.sleep") as sleep, \
                contextlib.redirect_stdout(output):
            code = run_probe()
        self.assertNotIn("PRIVATE", output.getvalue())
        return code, output.getvalue(), get, post, sleep

    def test_probe_uses_only_tiny_fixed_payload(self):
        code, output, get, post, sleep = self.invoke_probe(response(200, data={"data": [
            {"id": "mistral-small-2603", "aliases": ["mistral-small-latest"]}]}), response(200))
        self.assertEqual(code, 0)
        self.assertIn("catalog: True", output)
        self.assertEqual(post.call_args.kwargs["json"]["max_tokens"], 32)
        self.assertEqual(post.call_args.kwargs["json"]["messages"], [
            {"role": "user", "content": "Translate Hello into Russian. Reply with one word."}])
        sleep.assert_called_once_with(2)
        self.assertEqual(post.call_count, 1)

    def test_probe_zero_limit_is_actionable_and_no_loop(self):
        code, output, _, post, _ = self.invoke_probe(response(200), response(429,
            headers={"x-ratelimit-limit-tokens-minute": "0"}, data={"message": "PRIVATE"}))
        self.assertEqual(code, 1)
        self.assertIn("zero API limit", output)
        post.assert_called_once()

    def test_probe_authentication_failure_never_calls_completion(self):
        code, _, _, post, _ = self.invoke_probe(response(401, data={"message": "PRIVATE"}))
        self.assertEqual(code, 1)
        post.assert_not_called()

    def test_probe_missing_key_and_network_errors_do_not_expose_secrets(self):
        with patch.dict(os.environ, {}, clear=True), patch("changelog_mistral_diagnostics.requests.get") as get:
            self.assertEqual(run_probe(), 1)
            get.assert_not_called()
        output = io.StringIO()
        with patch.dict(os.environ, {"MISTRAL_API_KEY": "PRIVATE"}), \
                patch("changelog_mistral_diagnostics.requests.get", side_effect=requests.ConnectionError("PRIVATE")), \
                contextlib.redirect_stdout(output):
            self.assertEqual(run_probe(), 1)
        self.assertNotIn("PRIVATE", output.getvalue())
