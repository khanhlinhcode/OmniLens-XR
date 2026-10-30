"""Public API response contracts authorized for S01."""

from typing import Literal

from pydantic import BaseModel, ConfigDict


class HealthResponse(BaseModel):
    """Non-sensitive health information for local readiness checks."""

    model_config = ConfigDict(extra="forbid", frozen=True)

    status: Literal["ok"] = "ok"
    service: Literal["omnilens-api"] = "omnilens-api"
    schema_version: Literal["v1"] = "v1"
    scope: Literal["s01-backend-foundation"] = "s01-backend-foundation"
    environment: Literal["development", "test", "production"]
    paid_services_enabled: Literal[False] = False
    raw_media_storage_enabled: Literal[False] = False
