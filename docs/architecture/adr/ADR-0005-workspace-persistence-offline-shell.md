# ADR-0005: Versioned workspace persistence and offline shell boundary

- **Status:** Accepted for S07 software-first implementation
- **Date:** 2026-10-30
- **Tasks:** T-S07-01, T-S07-02, T-S07-03, T-S07-04

## Context

S07 must restore panel layout after restart, remain useful without the backend and show
an Agent activity timeline without introducing arbitrary command execution. The stored
state includes layout and navigation preferences, but must not expose plaintext casually
or allow corrupt data to mutate the live workspace.

## Decision

1. Persist an explicit versioned DTO rather than scene objects. Schema v2 stores panel
   ID/kind/visibility/bounds/z-order/focus/pin, selected app and save time. JSON is an
   internal plaintext representation only before authenticated encryption.
2. Accept schema v1 only through a deterministic migration to v2. Missing v1 pin state
   becomes `false`; unknown versions and invalid values fail closed.
3. Encrypt with AES-256-CBC and authenticate the complete version/length/IV/ciphertext
   envelope with HMAC-SHA-256 using independent key halves. Verify the MAC before
   decrypting. Use a random installation key and random IV from platform cryptography.
4. Store the key and envelope under fixed filenames inside `Application.persistentDataPath`.
   This protects against casual plaintext disclosure and path injection but is explicitly
   not claimed as Keychain-backed protection against a fully compromised device.
5. Stage writes and then atomically replace/move the destination. If replacement is not
   supported, fail without deleting the prior object. Never remove the last valid object
   before validation, encryption and staged write succeed.
6. Offline Home/Settings exposes typed actions only. Science 3D, Settings and reset are
   local; Agent is rejected while offline. Reset recreates a validated default workspace.
7. The S07 Agent timeline accepts bounded fixture events with enum statuses and never
   executes a command. The required 30 states include empty and stream-error cases.
8. Automated/device markers can prove state restoration and fallback selection, but G02
   remains open until the required device video and human viewer interaction exist.

## Consequences

- Migration, tamper, wrong-key, malformed-state, offline and reset paths are independently
  testable without a backend or paid service.
- Layout files are not readable as plaintext, while the limitation of a same-sandbox key
  is documented rather than overstated.
- A future security sprint may replace the key-file provider with iOS Keychain without
  changing the serializer, envelope format or repository contract.
- Restoring layout replays the existing versioned S05 reducer; invalid overlap or state
  transitions reject the whole candidate instead of partially restoring it.
