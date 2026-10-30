from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from scripts import analyze_s03_comfort_log


def row(event: str, time: float, details: str) -> str:
    return json.dumps({"event": event, "time": time, "details": details})


class S03ComfortLogTests(unittest.TestCase):
    def test_complete_log_passes_with_summary(self) -> None:
        lines = [
            row("startup", 0.0, "probe_limit_minutes=21,raw_media=false"),
            row(
                "comfort_checkpoint",
                0.0,
                "minute=0,thermal_state=nominal",
            ),
        ]
        for window in range(1, 121):
            elapsed = window * 10.0
            lines.append(
                row(
                    "frame_window",
                    elapsed,
                    f"elapsed_seconds={elapsed:.1f},samples=300,avg_ms=33.333,"
                    "p50_ms=33.2,p95_ms=34.0,min_ms=32.0,max_ms=35.0,"
                    "thermal_state=nominal",
                )
            )
            if window in {30, 60, 120}:
                minute = window // 6
                lines.append(
                    row(
                        "comfort_checkpoint",
                        elapsed,
                        f"minute={minute},thermal_state=nominal",
                    )
                )

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "comfort.jsonl"
            path.write_text("\n".join(lines) + "\n", encoding="utf-8")
            summary = analyze_s03_comfort_log.analyze_rows(
                analyze_s03_comfort_log.load_rows(path)
            )

        self.assertTrue(summary.complete)
        self.assertEqual(summary.checkpoints, [0, 5, 10, 20])
        self.assertEqual(summary.frame_windows, 120)
        self.assertEqual(summary.total_frame_samples, 36_000)
        self.assertGreaterEqual(summary.covered_frame_seconds, 18 * 60)
        self.assertEqual(summary.worst_p95_ms, 34.0)

    def test_partial_log_remains_incomplete(self) -> None:
        rows = [
            {
                "event": "startup",
                "time": 0.0,
                "details": "probe_limit_minutes=21,raw_media=false",
            },
            {
                "event": "comfort_checkpoint",
                "time": 0.0,
                "details": "minute=0,thermal_state=unavailable",
            },
        ]

        summary = analyze_s03_comfort_log.analyze_rows(rows)

        self.assertFalse(summary.complete)
        self.assertEqual(summary.missing_checkpoints, [5, 10, 20])
        self.assertEqual(summary.frame_windows, 0)

    def test_private_identifier_material_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "comfort.jsonl"
            path.write_text(
                row(
                    "startup",
                    0.0,
                    "path=/Users/example/private",
                ),
                encoding="utf-8",
            )
            with self.assertRaises(analyze_s03_comfort_log.ComfortLogError):
                analyze_s03_comfort_log.load_rows(path)

    def test_inconsistent_frame_percentiles_are_rejected(self) -> None:
        rows = [
            {
                "event": "startup",
                "time": 0.0,
                "details": "probe_limit_minutes=21,raw_media=false",
            },
            {
                "event": "comfort_checkpoint",
                "time": 0.0,
                "details": "minute=0,thermal_state=nominal",
            },
            {
                "event": "frame_window",
                "time": 1.0,
                "details": "elapsed_seconds=1.0,samples=300,avg_ms=15.0,"
                "p50_ms=15.0,p95_ms=20.0,min_ms=10.0,max_ms=10.0,"
                "thermal_state=nominal",
            },
        ]

        with self.assertRaises(analyze_s03_comfort_log.ComfortLogError):
            analyze_s03_comfort_log.analyze_rows(rows)

    def test_checkpoint_before_elapsed_threshold_is_rejected(self) -> None:
        rows = [
            {
                "event": "startup",
                "time": 100.0,
                "details": "probe_limit_minutes=21,raw_media=false",
            },
            {
                "event": "comfort_checkpoint",
                "time": 100.0,
                "details": "minute=0,thermal_state=nominal",
            },
            {
                "event": "comfort_checkpoint",
                "time": 101.0,
                "details": "minute=20,thermal_state=nominal",
            },
        ]

        with self.assertRaises(analyze_s03_comfort_log.ComfortLogError):
            analyze_s03_comfort_log.analyze_rows(rows)

    def test_interrupted_run_is_not_complete(self) -> None:
        rows = [
            {
                "event": "startup",
                "time": 0.0,
                "details": "probe_limit_minutes=21,raw_media=false",
            },
            *[
                {
                    "event": "comfort_checkpoint",
                    "time": minute * 60.0,
                    "details": f"minute={minute},thermal_state=nominal",
                }
                for minute in (0, 5, 10, 20)
            ],
            {
                "event": "frame_window",
                "time": 1200.0,
                "details": "elapsed_seconds=1200.0,samples=300,avg_ms=3600.0,"
                "p50_ms=3599.0,p95_ms=3600.0,min_ms=3598.0,max_ms=3601.0,"
                "thermal_state=serious",
            },
            {
                "event": "focus_lost",
                "time": 1200.1,
                "details": "thermal_state=serious",
            },
        ]

        summary = analyze_s03_comfort_log.analyze_rows(rows)

        self.assertFalse(summary.complete)
        self.assertEqual(summary.interruptions, ["focus_lost"])


if __name__ == "__main__":
    unittest.main()
