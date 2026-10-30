#!/usr/bin/env python3
"""Redact local signing and device identifiers from S02 command output."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPLACEMENTS = (
    (re.compile(r"/Users/[^/\s]+"), "$USER_HOME"),
    (
        re.compile(r"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", re.IGNORECASE),
        "<redacted-email>",
    ),
    (re.compile(r"(?i)(Apple Development:)[^\r\n]+"), r"\1 <redacted-identity>"),
    (re.compile(r"(?i)(DEVELOPMENT_TEAM\s*=\s*)[A-Z0-9]{10}"), r"\1<redacted-team-id>"),
    (
        re.compile(r"(?i)(team(?:\s+identifier|\s+id)?\s*[:=]\s*)[A-Z0-9]{10}"),
        r"\1<redacted-team-id>",
    ),
    (
        re.compile(r"(?i)(certificate(?:\s+identity)?\s*[:=]\s*)[^\r\n]+"),
        r"\1<redacted-identity>",
    ),
    (
        re.compile(r"(?i)(serial(?:\s+number)?\s*[:=]\s*)[^\s,;]+"),
        r"\1<redacted-serial>",
    ),
    (re.compile(r"(?i)(udid\s*[:=]\s*)[0-9a-f-]{20,}"), r"\1<redacted-udid>"),
    (
        re.compile(r"(?i)(\bid\s*[=:]\s*)[0-9a-f]{8}-[0-9a-f-]{16,}"),
        r"\1<redacted-device-id>",
    ),
    (re.compile(r"\b[0-9A-Fa-f]{8}-[0-9A-Fa-f-]{20,}\b"), "<redacted-device-id>"),
)


def sanitize(text: str) -> str:
    for pattern, replacement in REPLACEMENTS:
        text = pattern.sub(replacement, text)
    return text


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("path", nargs="?", type=Path)
    arguments = parser.parse_args()
    if arguments.path is None:
        source = sys.stdin.read()
    else:
        source = arguments.path.read_text(encoding="utf-8", errors="replace")
    sys.stdout.write(sanitize(source))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
