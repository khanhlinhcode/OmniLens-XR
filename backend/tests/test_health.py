from __future__ import annotations

import json
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from omnilens_api.contracts import HealthResponse
from omnilens_api.main import create_app
from omnilens_api.settings import ConfigurationError, Settings

ROOT = Path(__file__).resolve().parents[2]


@pytest.fixture
def client(monkeypatch: pytest.MonkeyPatch) -> TestClient:
    monkeypatch.setenv("OMNILENS_ENV", "test")
    monkeypatch.setenv("OMNILENS_ALLOW_PAID_SERVICES", "false")
    monkeypatch.setenv("OMNILENS_STORE_RAW_MEDIA", "false")
    return TestClient(create_app())


def test_health_is_typed_and_non_sensitive(client: TestClient) -> None:
    response = client.get("/healthz")

    assert response.status_code == 200
    assert response.json() == {
        "status": "ok",
        "service": "omnilens-api",
        "schema_version": "v1",
        "scope": "s01-backend-foundation",
        "environment": "test",
        "paid_services_enabled": False,
        "raw_media_storage_enabled": False,
    }


def test_unknown_route_is_rejected(client: TestClient) -> None:
    response = client.post("/tools/arbitrary", json={"code": "do not execute"})

    assert response.status_code == 404


def test_openapi_exposes_only_authorized_product_path(client: TestClient) -> None:
    schema = client.get("/openapi.json").json()

    assert set(schema["paths"]) == {"/healthz"}


def test_shared_health_contract_matches_model() -> None:
    schema_path = ROOT / "contracts" / "health-response.schema.json"
    stored_schema = json.loads(schema_path.read_text(encoding="utf-8"))

    assert (
        stored_schema.pop("$schema") == "https://json-schema.org/draft/2020-12/schema"
    )
    assert stored_schema == HealthResponse.model_json_schema()


@pytest.mark.parametrize("value", ["1", "yes", "enabled", ""])
def test_invalid_boolean_is_rejected(
    monkeypatch: pytest.MonkeyPatch, value: str
) -> None:
    monkeypatch.setenv("OMNILENS_ALLOW_PAID_SERVICES", value)

    with pytest.raises(ConfigurationError, match="must be exactly"):
        Settings.from_env()


def test_paid_service_permission_fails_closed(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("OMNILENS_ALLOW_PAID_SERVICES", "true")

    with pytest.raises(ConfigurationError, match="Paid services are disabled"):
        Settings.from_env()


def test_raw_media_permission_fails_closed(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("OMNILENS_STORE_RAW_MEDIA", "true")

    with pytest.raises(ConfigurationError, match="Raw media storage is disabled"):
        Settings.from_env()


def test_unknown_environment_is_rejected(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("OMNILENS_ENV", "staging-with-secrets")

    with pytest.raises(ConfigurationError, match="OMNILENS_ENV"):
        Settings.from_env()
