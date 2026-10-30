"""Minimal FastAPI application authorized for S01."""

from __future__ import annotations

from fastapi import FastAPI

from .contracts import HealthResponse
from .settings import Settings


def create_app(settings: Settings | None = None) -> FastAPI:
    """Create an app using fail-closed settings."""

    resolved_settings = settings or Settings.from_env()
    app = FastAPI(
        title="OmniLens Local API",
        version="0.1.0",
        docs_url=None,
        redoc_url=None,
    )

    @app.get("/healthz", response_model=HealthResponse, tags=["system"])
    def health() -> HealthResponse:
        return HealthResponse(environment=resolved_settings.environment)

    return app


app = create_app()
