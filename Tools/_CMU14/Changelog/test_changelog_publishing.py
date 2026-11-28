import datetime as dt
import json
import os
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

import requests
import yaml

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

import actions_changelogs_since_last_run as discord
import update_changelog as assembler
import update_changelog_parts as parts
from changelog_discord_state import FileStateStore, GitHubStateStore, empty_state


def entry(number=1, source="CMU", timestamp="2026-10-01T12:00:00Z", message="Исправлены ошибки."):
    e = {"author": "author", "id": number, "time": timestamp,
         "url": f"https://github.com/example/game/pull/{number}",
         "changes": [{"type": "Fix", "message": message}], "source": source}
    e["key"] = discord.delivery_key(source, e)
    return e


class AssemblyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.file = self.root / "CMU.yml"
        self.parts = self.root / "Parts"
        self.parts.mkdir()
        self.save(self.file, {"Name": "CMU", "Order": -1, "Entries": [entry(30)]})

    def save(self, path, data):
        path.write_text(yaml.safe_dump(data, allow_unicode=True, sort_keys=False), encoding="utf-8")

    def add_part(self, name="part.yml", **changes):
        part = entry(31)
        part.pop("id")
        part.update(changes)
        self.save(self.parts / name, part)

    def test_real_cli_regression_no_undefined_variables_or_duplicate_append(self):
        self.add_part()
        # Use the imported function so the bundled Python path works on Windows too.
        with patch.object(sys, "argv", ["update_changelog", str(self.file), str(self.parts), "--category", "CMU"]):
            assembler.main()
        data = assembler.load_yaml(self.file)
        self.assertEqual([e["id"] for e in data["Entries"]], [30, 31])
        self.assertEqual(data["Name"], "CMU")
        self.assertEqual(data["Order"], -1)
        self.assertFalse(list(self.parts.glob("*.yml")))

    def test_duplicate_url_combines_changes_and_preserves_metadata(self):
        self.add_part(url=entry(30)["url"], author="other", time="2026-10-02T00:00:00Z",
                      changes=[{"type": "Add", "message": "Added a map."}])
        assembler.assemble(str(self.file), str(self.parts), "CMU")
        retained, = assembler.load_yaml(self.file)["Entries"]
        self.assertEqual((retained["author"], retained["time"], retained["id"]),
                         ("author", "2026-10-01T12:00:00Z", 30))
        self.assertEqual(len(retained["changes"]), 2)
        self.assertFalse(list(self.parts.glob("*.yml")))

    def test_existing_duplicate_url_merges_distinct_changes(self):
        a, b = entry(30), entry(31, message="Добавлены карты.")
        b["url"] = a["url"]
        self.save(self.file, {"Entries": [a, b]})
        assembler.assemble(str(self.file), str(self.parts), "CMU")
        entries = assembler.load_yaml(self.file)["Entries"]
        self.assertEqual(len(entries), 1)
        self.assertEqual(len(entries[0]["changes"]), 2)
        self.assertEqual(entries[0]["id"], 31)

    def test_colliding_upstream_ids_receive_new_unique_ids(self):
        a, b = entry(30), entry(31)
        b["id"] = 30
        self.save(self.file, {"Entries": [a, b]})
        assembler.assemble(str(self.file), str(self.parts), "CMU")
        self.assertEqual([e["id"] for e in assembler.load_yaml(self.file)["Entries"]], [30, 31])

    def test_duplicate_url_carrying_another_pr_id_does_not_create_collision(self):
        a, b, duplicate = entry(30), entry(31), entry(31)
        duplicate["url"] = a["url"]
        self.save(self.file, {"Entries": [a, b, duplicate]})
        assembler.assemble(str(self.file), str(self.parts), "CMU")
        entries = assembler.load_yaml(self.file)["Entries"]
        self.assertEqual(len(entries), 2)
        self.assertEqual(len({e["id"] for e in entries}), 2)
        self.assertEqual(max(e["id"] for e in entries), 32)

    def test_invalid_part_leaves_changelog_and_all_parts_intact(self):
        self.add_part("a.yml")
        self.add_part("b.yml", changes=[{"type": "Fix", "message": 123}])
        before = self.file.read_bytes()
        with self.assertRaises(ValueError):
            assembler.assemble(str(self.file), str(self.parts), "CMU")
        self.assertEqual(self.file.read_bytes(), before)
        self.assertEqual(len(list(self.parts.glob("*.yml"))), 2)

    def test_other_category_is_retained(self):
        self.add_part(category="Admin")
        assembler.assemble(str(self.file), str(self.parts), "CMU")
        self.assertTrue((self.parts / "part.yml").exists())

    def test_no_op_after_first_assembly_preserves_bytes(self):
        self.add_part()
        assembler.assemble(str(self.file), str(self.parts), "CMU")
        before = self.file.read_bytes()
        assembler.assemble(str(self.file), str(self.parts), "CMU")
        self.assertEqual(self.file.read_bytes(), before)

    def test_upstream_merge_restores_ids_and_assigns_old_arrival_a_new_id(self):
        previous = {"Entries": [entry(90), entry(100)]}
        old, removed = previous["Entries"]
        upstream = entry(101, timestamp="2025-01-01")
        upstream.update(id=1, url="https://github.com/upstream/game/pull/90")
        self.save(self.file, {"Entries": [{**old, "id": 1}, upstream]})
        assembler.assemble(str(self.file), str(self.parts), "CMU", previous=previous)
        incoming, retained = assembler.load_yaml(self.file)["Entries"]
        self.assertEqual(retained["id"], old["id"])
        self.assertGreater(incoming["id"], removed["id"])

    def test_removed_high_water_id_is_not_reused(self):
        previous = {"Entries": [entry(30), entry(80)]}
        assembler.assemble(str(self.file), str(self.parts), "CMU", previous=previous)
        self.assertEqual(assembler.load_yaml(self.file)["LastId"], 80)
        self.add_part()
        assembler.assemble(str(self.file), str(self.parts), "CMU")
        self.assertEqual(max(e["id"] for e in assembler.load_yaml(self.file)["Entries"]), 81)

    def test_file_replacement_retains_downstream_prs(self):
        local = entry(30)
        upstream = entry(1, timestamp="2024-01-01")
        upstream["url"] = "https://github.com/upstream/game/pull/30"
        self.save(self.file, {"Entries": [upstream]})
        assembler.assemble(str(self.file), str(self.parts), "CMU",
                           previous={"Entries": [local]}, repository="example/game")
        self.assertEqual({e["url"] for e in assembler.load_yaml(self.file)["Entries"]},
                         {local["url"], upstream["url"]})

    def test_all_categories_consume_tagged_parts_into_correct_files(self):
        for category, filename in assembler.CHANGELOG_CATEGORIES.items():
            if filename != "CMU":
                self.save(self.root / f"{filename}.yml", {"Entries": []})
            self.add_part(f"{category}.yml", category=category)
        with patch.object(sys, "argv", ["assemble", str(self.file), str(self.parts), "--category", "CMU", "--all"]):
            assembler.main()
        for filename in assembler.CHANGELOG_CATEGORIES.values():
            expected = 2 if filename == "CMU" else 1
            self.assertEqual(len(assembler.load_yaml(str(self.root / f"{filename}.yml"))["Entries"]), expected)
        self.assertFalse(list(self.parts.glob("*.yml")))


class SelectionTests(unittest.TestCase):
    def test_identity_survives_upstream_renumber_and_timestamp_rewrite(self):
        old = entry()
        new = entry()
        new.update(id=900, time="2026-10-04", author="new author")
        self.assertEqual(discord.delivery_key("CMU", old), discord.delivery_key("CMU", new))

    def test_same_pr_number_from_another_repository_is_new(self):
        old, new = entry(), entry()
        new["url"] = "https://github.com/upstream/game/pull/1"
        self.assertNotEqual(discord.delivery_key("CMU", old), discord.delivery_key("CMU", new))

    def test_sections_have_independent_identity(self):
        self.assertNotEqual(entry(source="CMU")["key"], entry(source="RMC14")["key"])

    def test_late_upstream_entry_with_old_time_is_delivered(self):
        old = entry(1, timestamp="2025-01-01")
        state = discord.initial_state([old], now=dt.datetime(2026, 10, 6, tzinfo=dt.timezone.utc))
        imported = entry(2, timestamp="2025-02-01")
        self.assertEqual(discord.select_entries([old, imported], state), [imported])

    def test_backfill_includes_baseline_but_never_delivered_entries(self):
        old = entry(1, timestamp="2026-08-01")
        done = entry(2, timestamp="2026-08-02")
        state = discord.initial_state([old, done], now=dt.datetime(2026, 10, 6, tzinfo=dt.timezone.utc))
        state["delivered"][done["key"]] = "confirmed"
        self.assertEqual(discord.select_entries([old, done], state, "2026-07-01"), [old])

    def test_manual_timestamp_normalization(self):
        a, b = entry(), entry()
        a["url"] = b["url"] = None
        b["time"] = "2026-10-01T12:00:00.0000000+00:00"
        self.assertEqual(discord.delivery_key("CMU", a), discord.delivery_key("CMU", b))

    def test_different_manual_entries_from_same_author_and_time_are_not_lost(self):
        a, b = entry(), entry(message="Добавлены карты.")
        a["url"] = b["url"] = None
        self.assertNotEqual(discord.delivery_key("CMU", a), discord.delivery_key("CMU", b))

    def test_undated_historical_entry_has_stable_content_identity(self):
        a, b = entry(), entry()
        a.pop("time")
        b.pop("time")
        a["url"] = b["url"] = None
        b["id"] = 500
        self.assertEqual(discord.delivery_key("Maps", a), discord.delivery_key("Maps", b))
        self.assertEqual(discord.entry_date(a).year, 1)

    def test_real_files_all_sections_and_invalid_historical_dates(self):
        entries = discord.load_entries(discord.DEFAULT_FILES)
        self.assertEqual({e["source"] for e in entries}, set(discord.SECTION_NAMES))
        self.assertEqual(len({e["key"] for e in entries}), len(entries))
        for filename in discord.DEFAULT_FILES:
            with self.subTest(filename=filename):
                raw = assembler.load_yaml(filename)["Entries"]
                self.assertEqual(len(raw), len({e["id"] for e in raw}))
                for entry in raw:
                    self.assertIsInstance(entry["author"], str)
                    for change in entry["changes"]:
                        self.assertIn(change["type"], assembler.CHANGE_TYPES)
                        self.assertIsInstance(change["message"], str)


class DeliveryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.store = FileStateStore(Path(self.temp.name) / "journal.json")
        self.state = empty_state()
        self.state["pending"] = {"keys": ["key"], "chunks": ["Первое", "Второе"], "next": 0, "receipts": []}
        self.store.save(self.state)

    def test_partial_rejection_resumes_confirmed_payload_without_duplicates(self):
        sender = Mock(side_effect=["receipt1", discord.DeliveryRejected("HTTP 400")])
        with self.assertRaises(discord.DeliveryRejected):
            discord.publish_pending(self.state, self.store, sender)
        recovered = self.store.load()
        self.assertEqual(recovered["pending"]["next"], 1)
        self.assertEqual(recovered["delivered"], {})
        sender = Mock(return_value="receipt2")
        discord.publish_pending(recovered, self.store, sender)
        sender.assert_called_once_with("Второе")
        self.assertIn("key", self.store.load()["delivered"])
        self.assertIsNone(self.store.load()["pending"])

    def test_timeout_pauses_future_runs_until_operator_verifies_channel(self):
        with self.assertRaises(RuntimeError):
            discord.publish_pending(self.state, self.store, Mock(side_effect=RuntimeError("timeout")))
        recovered = self.store.load()
        sender = Mock()
        with self.assertRaises(RuntimeError):
            discord.publish_pending(recovered, self.store, sender)
        sender.assert_not_called()
        discord.resolve_ambiguous(recovered, "delivered", self.store)
        sender.return_value = "receipt2"
        discord.publish_pending(recovered, self.store, sender)
        sender.assert_called_once_with("Второе")

    def test_operator_retry_after_confirmed_absence(self):
        self.state["pending"]["in_flight"] = 0
        discord.resolve_ambiguous(self.state, "retry", self.store)
        self.assertIsNone(self.store.load()["pending"]["in_flight"])
        self.assertEqual(self.store.load()["pending"]["next"], 0)

    def test_corrupt_journal_is_not_treated_as_empty(self):
        self.store.path.write_text("{}", encoding="utf-8")
        with self.assertRaises(ValueError):
            self.store.load()

    def test_discord_wait_confirmation_and_mentions_disabled(self):
        response = Mock(status_code=200)
        response.json.return_value = {"id": "123"}
        with patch.object(discord.requests, "post", return_value=response) as post:
            self.assertEqual(discord.send_discord_webhook("Текст", "https://example.invalid/token"), "123")
        self.assertEqual(post.call_args.kwargs["params"], {"wait": "true"})
        self.assertEqual(post.call_args.kwargs["json"]["allowed_mentions"], {"parse": []})

    def test_discord_429_retries_but_timeout_does_not(self):
        rate_limit, success = Mock(status_code=429), Mock(status_code=200)
        rate_limit.json.return_value = {"retry_after": 0}
        success.json.return_value = {"id": "id"}
        with patch.object(discord.requests, "post", side_effect=[rate_limit, success]) as post:
            discord.send_discord_webhook("Текст", "https://example.invalid/token")
            self.assertEqual(post.call_count, 2)
        with patch.object(discord.requests, "post", side_effect=requests.Timeout("SECRET_URL")) as post:
            with self.assertRaisesRegex(RuntimeError, "paused") as context:
                discord.send_discord_webhook("Текст", "https://example.invalid/token")
            self.assertNotIn("SECRET_URL", str(context.exception))
            self.assertEqual(post.call_count, 1)

    def test_long_messages_preserve_all_text_and_repeat_links(self):
        message = ("Очень длинная запись 🛠️ " * 300).rstrip()
        chunks = discord.entry_chunks(entry(message=message))
        self.assertGreater(len(chunks), 1)
        for chunk in chunks:
            self.assertLessEqual(discord.discord_length(chunk), 2000)
            self.assertIn("https://github.com/example/game/pull/1", chunk)
        fragments = []
        for chunk in chunks:
            for line in chunk.splitlines()[1:-1]:
                fragments.append(line[len("🔧 "):])
        self.assertEqual("".join(fragments), message)

    def test_dry_run_has_no_discord_or_journal_writes(self):
        self.state["pending"] = None
        self.store.save(self.state)
        before = self.store.path.read_bytes()
        output = Path(self.temp.name) / "preview.md"
        with patch.object(sys, "argv", ["publisher", "--dry-run", "--state-file", str(self.store.path), "--output", str(output)]), \
                patch.object(discord, "load_entries", return_value=[entry()]), \
                patch.object(discord.requests, "post", side_effect=AssertionError("network forbidden")):
            discord.main()
        self.assertEqual(self.store.path.read_bytes(), before)
        self.assertIn("Исправлены ошибки.", output.read_text(encoding="utf-8"))

    def test_missing_journal_on_existing_branch_does_not_reset(self):
        session = Mock()
        missing, branch = Mock(status_code=404), Mock(status_code=200)
        session.request.side_effect = [missing, branch]
        with self.assertRaisesRegex(RuntimeError, "refusing to reset"):
            GitHubStateStore("repo/name", "test", session=session).load()

    def test_v1_preview_and_resume_preserve_partial_queue_and_delivered_keys(self):
        self.state.update(version=1, baseline=["baseline"], delivered={"previous": "2026-09-10"},
                          translations={"old": "translation"})
        self.state["pending"].update(next=1, in_flight=1, receipts=["receipt1"])
        self.store.path.write_text(json.dumps(self.state), encoding="utf-8")
        before = self.store.path.read_bytes()
        output = self.store.path.with_name("preview.md")
        argv = ["publisher", "--state-file", str(self.store.path)]
        english = entry(message="Fixed radio @everyone.")
        with patch.object(discord, "load_entries", return_value=[english]), patch.dict(
            os.environ, {"DISCORD_WEBHOOK_URL": "https://example.invalid"}, clear=True
        ), patch.object(discord.requests, "post") as post:
            with patch.object(sys, "argv", argv + ["--dry-run", "--output", str(output)]):
                discord.main()
            post.assert_not_called()
            self.assertEqual(self.store.path.read_bytes(), before)
            preview = output.read_text(encoding="utf-8")
            self.assertIn("Второе", preview)
            self.assertNotIn("\nПервое\n", preview)
            self.assertIn(english["changes"][0]["message"], preview)
            self.assertIn("требует сверки", preview)
            post.return_value = Mock(status_code=200, **{"json.return_value": {"id": "123"}})
            with patch.object(sys, "argv", argv + ["--resolve-ambiguous", "delivered"]):
                discord.main()
            post.assert_called_once()
            self.assertIn(english["changes"][0]["message"], post.call_args.kwargs["json"]["content"])
        migrated = self.store.load()
        self.assertEqual(migrated["version"], 2)
        self.assertNotIn("translations", migrated)
        self.assertEqual(migrated["baseline"], ["baseline"])
        self.assertEqual(migrated["delivered"]["previous"], "2026-09-10")
        self.assertIn("key", migrated["delivered"])
        self.assertIn(english["key"], migrated["delivered"])

    def test_requested_backfill_stays_eligible_when_existing_queue_is_paused(self):
        historical = entry()
        self.state["baseline"] = [historical["key"]]
        self.state["pending"]["in_flight"] = 0
        self.store.save(self.state)
        argv = ["publisher", "--state-file", str(self.store.path), "--start-date", "2026-09-30"]
        with patch.object(discord, "load_entries", return_value=[historical]), patch.dict(
            os.environ, {"DISCORD_WEBHOOK_URL": "https://example.invalid"}, clear=True
        ), patch.object(sys, "argv", argv):
            with self.assertRaisesRegex(RuntimeError, "verification"):
                discord.main()
        self.assertEqual(discord.select_entries([historical], self.store.load()), [historical])


class ParserTests(unittest.TestCase):
    def test_equal_pr_numbers_from_two_repositories_do_not_collide(self):
        with tempfile.TemporaryDirectory() as directory:
            for repo in ["local/game", "upstream/game"]:
                self.assertTrue(parts.write_part(directory, 12, "author", [], "2026-10-01",
                                                 f"https://github.com/{repo}/pull/12", "CMU", []))
            self.assertEqual(len(list(Path(directory).glob("*.yml"))), 2)

    def test_template_comments_and_empty_blocks_are_ignored(self):
        body = "<!-- :cl:\nadd: Added fun! -->\n:cl: Author\n- fix: Исправлена связь.\n"
        self.assertEqual(parts.parse_cl_block(body, "fallback"),
                         ("Author", [{"type": "Fix", "message": "Исправлена связь."}]))
        self.assertIsNone(parts.parse_cl_block(":cl:\n- add: Added fun!", "fallback"))
        self.assertIsNone(parts.parse_cl_block(":cl:\r\n- fix: Исправлено что-то сломанное!\r\n", "fallback"))
        self.assertEqual(parts.parse_cl_block("🆑\r\n- fix: Fixed A\r\n## Other\r\n- add: unrelated", "fallback"),
                         ("fallback", [{"type": "Fix", "message": "Fixed A"}]))
        self.assertEqual(parts.parse_cl_block("```md\n:cl:\n- fix: Example.\n```\n:cl:\n- fix: Actual.", "fallback"),
                         ("fallback", [{"type": "Fix", "message": "Actual."}]))

    def test_scan_uses_local_repo_cursor_not_newer_upstream_time(self):
        local, upstream = entry(), entry(2, timestamp="2026-10-05")
        upstream["url"] = "https://github.com/upstream/game/pull/2"
        self.assertEqual(parts.get_scan_start([local, upstream], "example/game", None), "2026-09-24T12:00:00Z")

    def test_scan_paginates_closed_prs_filters_merge_time_and_stops_at_cutoff(self):
        session = Mock()
        page1, page2 = Mock(links={"next": {}}), Mock(links={"next": {}})
        session.get.side_effect = [page1, page2]
        merged = {"number": 11, "updated_at": "2026-10-02", "merged_at": "2026-10-01"}
        page1.json.return_value = [{**merged, "number": 10, "merged_at": None},
                                   {**merged, "number": 9, "merged_at": "2026-09-01"}, merged]
        page2.json.return_value = [merged, {**merged, "updated_at": "2026-09-20"}]
        self.assertEqual(parts.get_merged_prs(session, "repo/name", "2026-10-01", "master"), [merged])
        self.assertEqual(session.get.call_count, 2)
        self.assertEqual(session.get.call_args.args[0], f"{parts.GITHUB_API_URL}/repos/repo/name/pulls")
        self.assertEqual(session.get.call_args.kwargs["params"]["base"], "master")
        self.assertEqual(session.get.call_args.kwargs["timeout"], 30)


class PipelineTests(unittest.TestCase):
    def test_collect_assemble_preview_deliver_and_retry_without_provider_key(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            parts_dir = root / "Parts"
            parts_dir.mkdir()
            files = [str(root / f"{name}.yml") for name in assembler.CHANGELOG_CATEGORIES.values()]
            for file in files:
                Path(file).write_text("Entries: []\n", encoding="utf-8")
            upstream = entry(1, timestamp="2024-01-01", message="Upstream arrived late.")
            upstream.update(url="https://github.com/upstream/game/pull/1")
            Path(files[0]).write_text(yaml.safe_dump({"Entries": [upstream]}), encoding="utf-8")

            def merged(number, body, labels=()):
                return {"number": number, "html_url": f"https://github.com/example/game/pull/{number}",
                        "body": body, "user": {"login": "author"}, "merged_at": "2026-10-01T12:00:00Z",
                        "labels": [{"name": label} for label in labels]}

            prs = [merged(1, ":cl: Алиса\n- fix: Исправлена связь."), merged(
                2, ":cl:\n- add: Added a радио.", ["Changelog: Maps", "Intent: Experimental"]),
                merged(3, "No changelog"), merged(4, ":cl:\n- fix: Fixed chat.")]
            journal = FileStateStore(root / "journal.json")
            journal.save(empty_state())
            preview = root / "preview.md"
            collect = ["collect", str(parts_dir), "--changelog-file", files[0], "--all", "--category", "CMU"]
            assemble = ["assemble", files[0], str(parts_dir), "--all"]
            deliver = ["deliver", "--state-file", str(journal.path), "--files", *files]
            env = {"GITHUB_TOKEN": "fake", "GITHUB_REPOSITORY": "example/game",
                   "START_DATE": "2026-10-01", "DISCORD_WEBHOOK_URL": "https://example.invalid"}
            with patch.dict(os.environ, env, clear=True), patch.object(parts, "get_merged_prs", return_value=prs):
                for _ in range(2):
                    with patch.object(sys, "argv", collect):
                        parts.main()
                    with patch.object(sys, "argv", assemble):
                        assembler.main()
                self.assertEqual(len(discord.load_entries(files)), 4)
                self.assertEqual(len(assembler.load_yaml(files[3])["Entries"]), 1)
                self.assertEqual(list(parts_dir.iterdir()), [])
                before = journal.path.read_bytes()
                with patch.object(sys, "argv", deliver + ["--dry-run", "--output", str(preview)]), patch.object(
                    discord.requests, "post"
                ) as post:
                    discord.main()
                    post.assert_not_called()
                self.assertEqual(journal.path.read_bytes(), before)
                for expected in ("Алиса", "Исправлена связь.", "Fixed chat.", "Added a радио.", "Upstream arrived late."):
                    self.assertIn(expected, preview.read_text(encoding="utf-8"))

                accepted = Mock(status_code=200, **{"json.return_value": {"id": "123"}})
                rejected = Mock(status_code=400)
                with patch.object(sys, "argv", deliver), patch.object(
                    discord.requests, "post", side_effect=[accepted, rejected]
                ) as post:
                    with self.assertRaises(discord.DeliveryRejected):
                        discord.main()
                    sent = post.call_args_list[0].kwargs["json"]["content"]
                with patch.object(sys, "argv", deliver), patch.object(
                    discord.requests, "post", return_value=accepted
                ) as post:
                    discord.main()
                    discord.main()
                    self.assertEqual(post.call_count, 3)
                    self.assertNotIn(sent, [call.kwargs["json"]["content"] for call in post.call_args_list])
                self.assertEqual(len(journal.load()["delivered"]), 4)

    def test_both_publish_workflows_use_one_commit_and_only_wait_for_assembly(self):
        workflows = Path(__file__).resolve().parents[3] / ".github" / "workflows"
        workflow = yaml.load((workflows / "update-changelog.yml").read_text(), Loader=yaml.BaseLoader)
        self.assertEqual(workflow["on"]["workflow_call"]["inputs"]["assembly_only"]["default"], "true")
        self.assertIn("inputs.assembly_only != true", workflow["jobs"]["discord"]["if"])
        self.assertIn("inputs.dry_run != true", workflow["jobs"]["discord"]["if"])
        self.assertEqual(workflow["on"]["workflow_run"]["workflows"], ["Publish", "Fast Publish"])
        self.assertEqual(workflow["jobs"]["discord"]["concurrency"]["queue"], "max")
        for filename in ("publish.yml", "publish-fast.yml"):
            with self.subTest(workflow=filename):
                jobs = yaml.load((workflows / filename).read_text(), Loader=yaml.BaseLoader)["jobs"]
                self.assertEqual(jobs["changelog"]["uses"], "./.github/workflows/update-changelog.yml")
                start = jobs["publish_start"]
                self.assertEqual(start["needs"], "changelog")
                self.assertIn("needs.changelog.outputs.commit_hash || github.sha", start["steps"][0]["with"]["ref"])
                self.assertIn("steps.checkout.outputs.commit", start["outputs"]["ref"])
                for name, job in jobs.items():
                    if name in ("changelog", "publish_start"):
                        continue
                    self.assertIn("publish_start", job["needs"])
                    self.assertIn("needs.publish_start.outputs.ref", job["steps"][0]["with"]["ref"])
                    for step in job["steps"]:
                        if "PUBLISH_TOKEN" in step.get("env", {}):
                            self.assertIn("needs.publish_start.outputs.ref", step["env"]["PUBLISH_VERSION"])


if __name__ == "__main__":
    unittest.main()
