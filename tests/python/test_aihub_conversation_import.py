from __future__ import annotations

import hashlib
import unittest

from python.aihub.conversation_import import (
    classify,
    merge_transcript_payloads,
    parse_transcript,
    redact_text,
)


class ConversationImportTests(unittest.TestCase):
    def test_redacts_constructed_secret_material(self) -> None:
        openai_token = "sk-" + "proj-" + ("A" * 28)
        github_token = "github_" + "pat_" + ("B" * 28)
        slack_token = "xox" + "b-" + ("C" * 28)
        text = f"openai={openai_token} github={github_token} slack={slack_token}"

        redacted, count = redact_text(text)

        self.assertEqual(count, 3)
        self.assertNotIn(openai_token, redacted)
        self.assertNotIn(github_token, redacted)
        self.assertNotIn(slack_token, redacted)
        self.assertEqual(redacted.count("[REDACTED_SECRET]"), 3)

    def test_parse_transcript_tracks_roles_tags_and_redactions(self) -> None:
        token = "sk-" + "proj-" + ("D" * 28)
        lines = [
            "You said",
            "Build the WinUI profile wheel and Azure OIDC plan.",
            "Copilot said",
            f"Created the GitHub workflow. Credential: {token}",
        ]

        turns, redactions = parse_transcript(lines, source="chat-a.txt")

        self.assertEqual(len(turns), 2)
        self.assertEqual([turn.role for turn in turns], ["user", "assistant"])
        self.assertIn("ui", turns[0].tags)
        self.assertIn("azure", turns[0].tags)
        self.assertIn("github", turns[1].tags)
        self.assertEqual(redactions, 1)
        self.assertIn("[REDACTED_SECRET]", turns[1].text)
        self.assertNotIn(token, turns[1].text)

    def test_merge_two_transcripts_is_deterministic_except_timestamp(self) -> None:
        first = [
            "You said",
            "Run the development what-if.",
            "Assistant said",
            "Prepared the Bicep plan.",
        ]
        second = [
            "User said",
            "Run the development what-if.",
            "ChatGPT said",
            "Preserved evidence in SharePoint.",
        ]

        one = merge_transcript_payloads(
            [("chat-a.txt", first), ("chat-b.txt", second)]
        )
        two = merge_transcript_payloads(
            [("chat-a.txt", first), ("chat-b.txt", second)]
        )

        one.pop("generatedUtc")
        two.pop("generatedUtc")
        self.assertEqual(one, two)
        self.assertEqual(one["summary"]["sourceCount"], 2)
        self.assertEqual(one["summary"]["turnCount"], 4)
        self.assertEqual(one["summary"]["userTurnCount"], 2)
        self.assertEqual(one["summary"]["assistantTurnCount"], 2)
        self.assertEqual(one["uniqueRequirements"][0]["count"], 2)
        self.assertEqual(
            [source["sha256"] for source in one["sources"]],
            [
                hashlib.sha256("\n".join(first).encode("utf-8")).hexdigest(),
                hashlib.sha256("\n".join(second).encode("utf-8")).hexdigest(),
            ],
        )

    def test_import_never_claims_private_reasoning_or_object_merge(self) -> None:
        payload = merge_transcript_payloads(
            [
                (
                    "export.txt",
                    [
                        "You said",
                        "Consolidate the HELIOS public project history.",
                        "Assistant said",
                        "Generated a redacted control record.",
                    ],
                )
            ]
        )

        self.assertFalse(payload["privateReasoningImported"])
        self.assertFalse(payload["sourceConversationObjectsMerged"])
        self.assertEqual(
            payload["scope"],
            "user-supplied-or-exported-transcript-text-only",
        )

    def test_classification_is_bounded_to_known_components(self) -> None:
        tags = classify(
            "Use WinRE, BitLocker, Azure Bicep, GitHub, Slack, AIHub, MCP, and WinUI."
        )
        self.assertEqual(
            set(tags),
            {
                "windows",
                "storage",
                "security",
                "ai-ml",
                "runtime",
                "azure",
                "github",
                "collaboration",
                "ui",
            },
        )


if __name__ == "__main__":
    unittest.main()
