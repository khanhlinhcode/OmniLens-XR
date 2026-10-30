# Shared contract boundary

This directory holds versioned JSON fixtures and schemas shared by C# and Python. S01 exposes `health-response.schema.json`, generated from and drift-tested against the FastAPI `HealthResponse` model. Detailed Agent schemas are scheduled for later sprints, so S01 still defines only their invariants and does not invent those production contracts prematurely.

## Required invariants

- Every action is selected from a typed allowlist.
- Every command has a unique `command_id`, bounded timeout, and explicit final status.
- Scene mutations carry an expected scene version and return an observed scene version.
- `accepted` is not equivalent to `completed`.
- Unknown actions and parameters are rejected.
- Arbitrary executable code, reflection targets, shell fragments, and unbounded retry instructions are prohibited.
- Logs retain correlation/status metadata while excluding secrets and raw sensitive media by default.

No Agent action contract is authorized until its owning WBS task is active.
