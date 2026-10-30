# Backend foundation

This directory contains the executable S01 FastAPI foundation. It deliberately exposes only a non-sensitive health endpoint; Agent tools and product modules remain governed by their later WBS tasks.

## Structure

```text
backend/
  src/omnilens_api/
    contracts.py
    main.py
    settings.py
  tests/
  requirements.txt
  requirements-dev.txt
  requirements-lock.txt
  pyproject.toml
```

Direct runtime and development dependencies are exactly pinned. `requirements-lock.txt` captures the transitive environment verified on Python 3.12.10. Dependencies are installed only in the repository `.venv`.

The public `HealthResponse` model is mirrored at `contracts/health-response.schema.json`; a test fails if the stored cross-language schema drifts from Pydantic.

## Security baseline

- Bind to `127.0.0.1` by default.
- Paid services remain disabled unless the user explicitly opts in.
- Never log API keys, raw private documents, raw medical records, precise location, camera frames, or audio by default.
- Agent actions must use typed allowlisted tools, bounded execution, verification, and cancellation.
- Never evaluate arbitrary model-generated Python or shell code.

## Local setup

From the repository root:

```bash
python3 -m venv .venv
.venv/bin/python -m pip install -r backend/requirements-lock.txt
.venv/bin/ruff check backend/src backend/tests
.venv/bin/pytest -c backend/pyproject.toml backend/tests
.venv/bin/uvicorn omnilens_api.main:app --app-dir backend/src --host 127.0.0.1 --port 8000
```

`OMNILENS_ALLOW_PAID_SERVICES=true` and `OMNILENS_STORE_RAW_MEDIA=true` are rejected during S01. A later policy decision and tests are required before either can be enabled.
