"""Fail-closed local settings for the S01 backend foundation."""

from __future__ import annotations

import os
from dataclasses import dataclass
from typing import Literal, cast

Environment = Literal["development", "test", "production"]
ALLOWED_ENVIRONMENTS: frozenset[str] = frozenset({"development", "test", "production"})


class ConfigurationError(ValueError):
    """Raised when configuration would violate the active safety boundary."""


def _parse_bool(name: str, value: str) -> bool:
    normalized = value.strip().lower()
    if normalized == "true":
        return True
    if normalized == "false":
        return False
    raise ConfigurationError(f"{name} must be exactly 'true' or 'false'")


@dataclass(frozen=True, slots=True)
class Settings:
    """Settings permitted during S01."""

    environment: Environment
    allow_paid_services: bool = False
    store_raw_media: bool = False

    @classmethod
    def from_env(cls) -> Settings:
        environment_value = os.getenv("OMNILENS_ENV", "development").strip().lower()
        if environment_value not in ALLOWED_ENVIRONMENTS:
            raise ConfigurationError(
                "OMNILENS_ENV must be development, test, or production"
            )

        allow_paid_services = _parse_bool(
            "OMNILENS_ALLOW_PAID_SERVICES",
            os.getenv("OMNILENS_ALLOW_PAID_SERVICES", "false"),
        )
        store_raw_media = _parse_bool(
            "OMNILENS_STORE_RAW_MEDIA",
            os.getenv("OMNILENS_STORE_RAW_MEDIA", "false"),
        )

        if allow_paid_services:
            raise ConfigurationError(
                "Paid services are disabled until a separate budget approval"
            )
        if store_raw_media:
            raise ConfigurationError(
                "Raw media storage is disabled until a privacy/retention approval"
            )

        return cls(
            environment=cast(Environment, environment_value),
            allow_paid_services=False,
            store_raw_media=False,
        )
