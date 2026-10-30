"""Offline S01/S02 repository policy checks with no third-party dependencies."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

REQUIRED_PATHS = (
    ".env.example",
    ".github/workflows/ci.yml",
    ".gitignore",
    "AGENTS.md",
    "backend/README.md",
    "backend/pyproject.toml",
    "backend/requirements-dev.txt",
    "backend/requirements-lock.txt",
    "backend/requirements.txt",
    "backend/src/omnilens_api/contracts.py",
    "backend/src/omnilens_api/main.py",
    "backend/src/omnilens_api/settings.py",
    "backend/tests/test_health.py",
    "client/README.md",
    "client/OmniLensXR/Assets/Tests/Editor/OmniLensXR.Tests.Editor.asmdef",
    "client/OmniLensXR/Assets/Tests/Editor/S01FoundationTests.cs",
    "client/OmniLensXR/Assets/Tests/Editor/S02IosConfigurationTests.cs",
    "client/OmniLensXR/Assets/Editor/S02IosBuild.cs",
    "client/OmniLensXR/Assets/OmniLens/S02/S02RuntimeProbe.cs",
    "client/OmniLensXR/Packages/manifest.json",
    "client/OmniLensXR/Packages/packages-lock.json",
    "client/OmniLensXR/ProjectSettings/ProjectVersion.txt",
    "client/OmniLensXR/ProjectSettings/UnityConnectSettings.asset",
    "config/toolchain-versions.json",
    "contracts/README.md",
    "contracts/health-response.schema.json",
    "docs/architecture/adr/ADR-0001-xr-foundation-toolchain.md",
    "docs/device-matrix/environment-inventory.md",
    "docs/evidence/S01/EV-S01-01.md",
    "docs/evidence/S01/EV-S01-02.md",
    "docs/evidence/S01/EV-S01-03.md",
    "docs/evidence/S01/EV-S01-04.md",
    "docs/evidence/S01/toolchain-installation.md",
    "docs/evidence/S02/implementation-plan.md",
    "docs/evidence/S02/EV-S02-01.md",
    "docs/evidence/S02/EV-S02-02.md",
    "docs/evidence/S02/EV-S02-03.md",
    "docs/evidence/S02/EV-S02-04.md",
    "docs/evidence/S02/sprint-report.md",
    "docs/licenses/initial-license-register.md",
    "scripts/run_unity_s01_checks.sh",
    "scripts/run_unity_s02_checks.sh",
    "scripts/collect_s02_device_log.sh",
    "scripts/sanitize_s02_evidence.py",
)

REQUIRED_IGNORE_RULES = (
    ".env",
    ".env.*",
    "!.env.example",
    "*.p12",
    "*.mobileprovision",
    "*.key",
    "*.pem",
    "**/Secrets/**",
    "**/UserSettings/**",
)

SECRET_SUFFIXES = {".p12", ".mobileprovision", ".key", ".pem"}
BINARY_SUFFIXES = {".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".pdf"}
EXCLUDED_DIRECTORY_NAMES = {
    ".git",
    ".mypy_cache",
    ".pytest_cache",
    ".ruff_cache",
    ".venv",
    "Build",
    "Builds",
    "Library",
    "Logs",
    "MemoryCaptures",
    "Obj",
    "Temp",
    "UserSettings",
    "__pycache__",
}
SECRET_PATTERNS = (
    re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
    re.compile(r"\bAKIA[0-9A-Z]{16}\b"),
    re.compile(
        r"(?i)\b(?:api[_-]?key|access[_-]?token|client[_-]?secret)\b"
        r"\s*[:=]\s*['\"]?[A-Za-z0-9_./+\-=]{16,}"
    ),
)


def project_files(root: Path = ROOT) -> list[Path]:
    files: list[Path] = []
    for path in root.rglob("*"):
        if not path.is_file() or any(
            part in EXCLUDED_DIRECTORY_NAMES for part in path.parts
        ):
            continue
        files.append(path)
    return files


def validate(root: Path = ROOT) -> list[str]:
    errors: list[str] = []

    for relative in REQUIRED_PATHS:
        if not (root / relative).is_file():
            errors.append(f"missing required file: {relative}")

    gitignore_path = root / ".gitignore"
    if gitignore_path.is_file():
        ignore_lines = {
            line.strip()
            for line in gitignore_path.read_text(encoding="utf-8").splitlines()
            if line.strip() and not line.lstrip().startswith("#")
        }
        for rule in REQUIRED_IGNORE_RULES:
            if rule not in ignore_lines:
                errors.append(f"missing .gitignore rule: {rule}")

    manifest_path = root / "config/toolchain-versions.json"
    if manifest_path.is_file():
        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            errors.append(f"invalid toolchain manifest JSON: {exc}")
        else:
            expected = {
                "unity": "6000.3.27f1",
                "cardboard": "1.36.0",
                "xcode": "27.1",
            }
            actual = {
                "unity": manifest.get("unity", {}).get("version"),
                "cardboard": manifest.get("cardboard", {}).get("version"),
                "xcode": manifest.get("apple", {}).get("xcode_version"),
            }
            for component, version in expected.items():
                if actual[component] != version:
                    errors.append(
                        f"unexpected {component} pin: {actual[component]!r}; expected {version}"
                    )
            if manifest.get("decision_status") != "provisional_not_device_verified":
                errors.append(
                    "toolchain decision must remain provisional until device evidence"
                )

    for path in project_files(root):
        relative = path.relative_to(root)
        if path.name == ".env" or (
            path.name.startswith(".env.") and path.name != ".env.example"
        ):
            errors.append(
                f"secret environment file must not be in repository: {relative}"
            )
        if path.suffix.lower() in SECRET_SUFFIXES:
            errors.append(
                f"credential/signing file must not be in repository: {relative}"
            )
        if path.suffix.lower() in BINARY_SUFFIXES:
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        for pattern in SECRET_PATTERNS:
            if pattern.search(text):
                errors.append(f"possible secret material in: {relative}")
                break

    return errors


def main() -> int:
    errors = validate()
    if errors:
        for error in errors:
            print(f"FAIL: {error}")
        return 1
    print("PASS: required S01/S02 repository files are present")
    print("PASS: toolchain manifest is valid and remains provisional")
    print("PASS: required secret/signing ignore rules are present")
    print("PASS: no prohibited credential files or obvious secret values found")
    return 0


if __name__ == "__main__":
    sys.exit(main())
