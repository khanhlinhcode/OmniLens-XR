# Initial license and cost register

**Review date:** 2026-10-29; continued 2026-10-30  
**Rule:** No download, dependency, model weight, asset, paid API, cloud billing, or redistribution is approved merely because it appears in this register.

| Component/content | Purpose | License/cost finding | Current decision | Source and constraints |
|---|---|---|---|---|
| Unity Hub `3.22.5` | Official Editor installer/manager | Proprietary Unity terms; signed and notarized macOS application. | Installed from Unity's official arm64 CDN after SHA-256 and Apple signature/notarization checks. Optional CLI telemetry declined. | https://unity.com/unity-hub/release-notes and https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-arm64.dmg |
| Unity `6000.3.27f1` | XR client/editor | Proprietary Unity terms. Unity Personal is listed as free in 2026 only for eligible users with no more than USD 200,000 in annual revenue and funding. | User confirmed eligible personal use and EULA; Personal license activated. Editor/iOS module/project/package compile verified locally. | https://unity.com/products/pricing-updates and https://unity.com/releases/editor/whats-new/6000.3.27f1 |
| Google Cardboard XR Plugin `1.36.0` | Mobile stereoscopic rendering and head tracking | Repository is Apache License 2.0. Underlying notices and bundled dependencies must remain with distributions. | Installed in the Unity project from pinned tag/commit for compatibility evaluation; compile and 3/3 foundation tests pass. | https://github.com/googlevr/cardboard-xr-plugin and https://github.com/googlevr/cardboard-xr-plugin/releases/tag/v1.36.0 |
| Xcode `27.1` | Generate/sign/run iOS build | Apple proprietary terms; Xcode can be downloaded for development. Basic personal-device testing does not require paid membership. | Installed from the Mac App Store, selected, signature assessed, and Xcode/SDK license accepted. iOS `27.1` platform components/runtime were installed after explicit confirmation. Never store Apple Account or signing material in Git. | https://developer.apple.com/news/releases/?id=10152026a and https://developer.apple.com/help/account/membership/programs-overview/ |
| Apple WWDR G3 intermediate | Validate Apple Development certificate trust chain | Apple public intermediate certificate for software signing; expires 2030-02-20. | Downloaded from Apple PKI, SHA-256 verified, and imported into System Keychain after explicit confirmation; one valid local Apple Development identity resulted. | https://www.apple.com/certificateauthority/AppleWWDRCAG3.cer and https://developer.apple.com/help/account/certificates/wwdr-intermediate-certificates/ |
| Apple Personal Team | Local iPhone testing | Free but constrained. Apple documents temporary provisioning, up to 10 App IDs, 3 devices, and 3 apps per device, with 7-day expiry. | Acceptable for local PoC; not for App Store distribution. | https://developer.apple.com/help/account/basics/about-your-developer-account/ |
| FastAPI `0.162.0` | Mac API framework | MIT. | Approved and pinned in the repository-local `.venv`; no cloud service activated. | https://github.com/fastapi/fastapi and https://pypi.org/project/fastapi/ |
| Pydantic `2.14.1` | Typed validation | MIT. | Approved and pinned in the repository-local `.venv`. | https://github.com/pydantic/pydantic and https://pypi.org/project/pydantic/ |
| Uvicorn `0.54.0` | Local ASGI server | BSD-3-Clause. | Approved and pinned; bind to loopback by default. | https://github.com/Kludex/uvicorn and https://pypi.org/project/uvicorn/ |
| httpx2 `2.16.0` | Backend test client used by current Starlette/FastAPI | BSD-3-Clause. | Development/test dependency only; selected to avoid Starlette's deprecation path for legacy `httpx`. | https://github.com/pydantic/httpx2 and https://pypi.org/project/httpx2/ |
| pytest `9.2.0` | Backend tests | MIT. | Development/test dependency only. | https://github.com/pytest-dev/pytest and https://pypi.org/project/pytest/ |
| Ruff `0.17.3` | Linting | MIT. | Development/CI dependency only. | https://github.com/astral-sh/ruff and https://pypi.org/project/ruff/ |
| SQLite core | Local structured storage | Public domain; some optional proprietary extensions are not included. | Approved in principle for later local storage; no extension approved. | https://www.sqlite.org/copyright.html |
| FAISS | Candidate local vector search | MIT; package also carries third-party notices. | Candidate only; binary/platform compatibility and exact version must be tested later. | https://github.com/facebookresearch/faiss |
| whisper.cpp code | Candidate local speech fallback | Main repository is MIT, but optional examples/integrations can introduce different licenses. | Candidate only. Do not assume bundled FFmpeg or every example inherits MIT. | https://github.com/ggml-org/whisper.cpp |
| Pretrained speech/embedding/CV weights | Models | License, source, training restrictions, redistribution rights, hash, memory and accuracy vary per weight. | **No weights approved or downloaded.** Each model requires an individual row and SHA-256 before use. | TBD per selected model |
| NASA 3D resources | Astronomy assets | NASA describes the resources as free to download/use and points to NASA media usage guidelines. Individual provenance, trademarks, people, and attribution still require review. | Candidate assets only; register each selected file separately. | https://science.nasa.gov/3d-resources/ |
| NIH 3D entries | Anatomy assets | License is selected per entry; NIH does not enforce it. Entries may be public domain, Creative Commons, or more restrictive. NIH does not guarantee medical accuracy. | **No blanket approval.** Verify each file, license, source, anatomy accuracy, privacy, and reviewer state. | https://3d.nih.gov/terms |
| OpenStax Anatomy & Physiology 2e | Potential medical text | CC BY-NC-SA 4.0 for permitted reuse, plus an explicit prohibition on LLM/generative-AI ingestion without prior written permission. | **Blocked from RAG, training, embedding, or generative-AI ingestion without written permission.** | https://openstax.org/books/anatomy-and-physiology-2e/pages/preface |
| Cloud LLM/free tiers | Optional later inference | Quotas, privacy terms and prices can change; "free" is not unlimited or permanent. | Disabled. No billing or paid fallback without explicit opt-in and a budget cap. | Provider-specific review required immediately before activation |

## Download decision log

| Item | Decision | Reason |
|---|---|---|
| Unity Hub `3.22.5` arm64 | Installed | Official DMG SHA-256 `c767b8c3df4a9361d8a2aeb2cd7ef1730956bcb63a89c83140858be314c6dbf0`; Apple accepted as notarized Developer ID `9QW8UQUTAA` |
| Unity Editor `6000.3.27f1` arm64 + iOS Build Support | Installed and locally verified | Official Hub completed both tasks; Editor bundle, module, Personal license, generated project, Cardboard package resolution, compile and EditMode tests checked |
| Full Xcode `27.1` (`27A9275`) + iOS `27.1` runtime (`24A94425`) | Installed/selected/verified | Mac App Store/Xcode component source; only iOS installed, optional watchOS/tvOS/visionOS/AI/Metal downloads left disabled |
| Apple WWDR G3 | Installed/verified | Official Apple PKI DER certificate; SHA-256 `DCF21878C77F4198E4B4614F03D696D89C66C66008D4244E1B99161AAC91601F`; System Keychain |
| Cardboard package `1.36.0` | Installed/resolved | Git tag `v1.36.0` locked to commit `7fb2c0e438d2ef686f51cd59b53da16cc07b1b68`; sample import remains S02 |
| Python backend packages | Installed in repository-local `.venv` only | Exact direct/transitive versions locked; clean install, tests, `pip check`, and license metadata audit captured in `docs/evidence/S01/` |
| Model weights | Not downloaded | Exact model/license/hash/performance decision is not yet authorized |
| 3D assets/textbooks | Not downloaded or ingested | Per-asset license and scientific validation are future gates |
| S08 generated cube | Authored and approved locally | Deterministic 444-byte baseline mesh; SHA-256 and provenance recorded in `S08-generated-cube.md`; no external download or third-party rights |

## Mandatory per-artifact fields before ingestion

`artifact_id`, exact version/revision, source URL, download date, SHA-256, license/SPDX or terms, attribution text, redistribution rule, AI-ingestion rule, commercial-use rule, scientific review state, and approval owner.
