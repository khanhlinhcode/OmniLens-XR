# OmniLens XR — Technical Delivery Specification V2

**Baseline:** 21 October 2026. **Hardware:** iPhone 11, Apple Silicon Mac, optional low-cost Cardboard viewer.
**Status:** Planning baseline; no software module is represented as built or tested.

## Executive scope

One Unity Cardboard XR spatial workspace with Mac-hosted AI Agent, RAG, Vision, Voice, document productivity, Science, Engineering, Medical Education, Learning Progress, Recommender and Maps. visionOS and custom headset are optional/deferred tracks.

## Cost, safety and device boundaries

- Additional purchased hardware budget estimate: 60,000–170,000 VND for viewer and possible shipping only; not a live quote.
- Cloud LLM is free-tier **within model/project quota only**, never guaranteed unlimited; no paid billing auto-enabled.
- iPhone 11 Cardboard remains **pending HelloCardboard device validation**. Native 6DoF, real eye/hand tracking and stereoscopic passthrough are not delivered by cardboard software.
- VR map preview must be used while stationary; real-world direction navigation hands off to 2D Apple Maps after removing headset.
- Medical anatomy is educational, not diagnosis. NIH 3D entry licenses and medical reviewer status must be checked; OpenStax A&P 2e cannot be ingested into LLM/AI without prior written permission.
- Workload hypothesis: 20 focused hours weekly; 68 two-week sprints x 34 assigned hours + 6 reserve each; actual velocity required to replan.

## Definition of Done (non-negotiable)

1. Code and reproducible build plus versions pinned.
2. Specific task acceptance test executed and result documented with a link.
3. Error conditions, boundary inputs, permissions and fallback exercised.
4. Performance measured on actual iPhone 11 where needed; no target marked as achieved without measurement.
5. Licensed content sourced with model hash and scientific review state.
6. No P0/P1 security or safety blocker; appropriate reviewer signs scientific domain packs.
7. Acceptance task and QA gate marked PASS only after evidence, not on intention.

## 20 modules — deep technical requirements

### M01: Device / XR Foundation

**Scope:** Pin Unity + Cardboard, HelloCardboard chạy trên iPhone 11, viewer QR, input gaze và recenter.
**Implementation strategy:** Frame loop, iOS signing, optical calibration, device QA.
**Module acceptance:** Build trên thiết bị, head rotation/recenter, readability đo qua kính
**Known limitation:** Chưa chạy thiết bị; không được tuyên bố PASS
**Dependencies:** Phone model + iOS + kính phù hợp
**Gates:** G00, G01
**Assigned tasks / effort:** 12 tasks, 102 estimated hours.

**Implementation tasks:**

- **T-S01-01 — Ghi bản kiểm kê Mac và iPhone 11** (7h, S01). Lưu sw_vers, uname, RAM, Xcode, Unity, iOS, dung lượng, cảm biến và cáp vào docs/device-matrix. **Acceptance:** Có model/build versions, chứng cứ lệnh đọc, không còn unknown bắt buộc trước build. **Depends:** none. **Test:** EV-S01-01.
- **T-S01-02 — Khoá compatibility versions** (8h, S01). Lập ADR Unity patch, Cardboard plugin, Xcode, iOS deployment target và rollback. **Acceptance:** Có manifest pin version kèm release URL và một phương án downgrade. **Depends:** T-S01-01. **Test:** EV-S01-02.
- **T-S01-03 — Khởi tạo repository sạch** (10h, S01). Tạo Unity client, FastAPI skeleton, contracts, CI lint và ignore file bí mật. **Acceptance:** Build/check khởi tạo chạy, không commit secrets, .gitignore đúng. **Depends:** T-S01-01. **Test:** EV-S01-03.
- **T-S01-04 — Kiểm toán license khởi đầu** (9h, S01). Ghi giấy phép Unity, SDK, pretrained models và điều kiện Apple signing. **Acceptance:** Có source URLs và decision log cho từng thành phần cần tải. **Depends:** T-S01-02, T-S01-03. **Test:** EV-S01-04.
- **T-S02-01 — Thiết lập Xcode signing cá nhân** (7h, S02). Tạo Unity iOS project, bundle ID, Personal Team, iOS deployment và device trust. **Acceptance:** Build Xcode thành công hoặc có blocker và log nguyên nhân. **Depends:** T-S01-04. **Test:** EV-S02-01.
- **T-S02-02 — Chạy HelloCardboard trên iPhone 11** (8h, S02). Import sample chính thức; build thiết bị qua Unity+Xcode. **Acceptance:** Ứng dụng khởi chạy, hiện stereo hoặc báo fail với log reproducible. **Depends:** T-S02-01. **Test:** EV-S02-02.
- **T-S02-03 — Kiểm tra head pose/recenter** (10h, S02). Ghi trace 20 hướng quay, recenter và tốc độ khung hình. **Acceptance:** Có PASS/FAIL theo động tác, video và frame-time log. **Depends:** T-S02-01. **Test:** EV-S02-03.
- **T-S02-04 — Kiểm tra lifecycle và permissions** (9h, S02). Test camera viewer QR, app background/resume, quyền camera và trạng thái bị từ chối. **Acceptance:** Các nhánh denied/retry/resume có kết quả xác định và không crash. **Depends:** T-S02-02, T-S02-03. **Test:** EV-S02-04.
- **T-S03-01 — Kiểm kê viewer compatibility** (7h, S03). Đo khay kính, camera obstruction, nút trigger, khoảng chỉnh lens và QR profile. **Acceptance:** Có profile, kích thước iPhone 11 và danh sách cấu hình kính hợp lệ. **Depends:** T-S02-04. **Test:** EV-S03-01.
- **T-S03-02 — Dựng interaction fallback không trigger** (8h, S03). Lập gaze dwell/recenter khi viewer không có nút hoặc trigger lỗi. **Acceptance:** Chọn được menu bằng dwell, hủy an toàn, có accessibility review. **Depends:** T-S03-01. **Test:** EV-S03-02.
- **T-S03-03 — Profile stereo comfort 20 phút** (10h, S03). Đo frame time, glare, méo lens, khả năng đọc chữ và nhiệt độ tại 5/10/20 phút. **Acceptance:** Có số đo thiết bị, nhận xét chịu đựng; không quảng cáo comfort nếu thiếu dữ liệu. **Depends:** T-S03-01. **Test:** EV-S03-03.
- **T-S03-04 — Đóng Gate G00/G01** (9h, S03). Đối chiếu device matrix, source version, build artifacts và báo cáo quyết định mua kính. **Acceptance:** PASS hoặc FAIL có chứng cứ, blocker và fallback 2D. **Depends:** T-S03-02, T-S03-03. **Test:** EV-S03-04.

### M02: Spatial Workspace

**Scope:** Launcher; focus/z-order; open/close/minimize; move/resize; restore; panels quản lý scene và document.
**Implementation strategy:** Window registry, state reducer, serialization, accessibility, gaze+dwell, deferred loading.
**Module acceptance:** 3 panel thao tác ổn, restore layout; đo FPS và text readability
**Known limitation:** Không quản lý app iOS tùy ý; chỉ panel nội bộ Unity
**Dependencies:** M01
**Gates:** G02
**Assigned tasks / effort:** 16 tasks, 136 estimated hours.

**Implementation tasks:**

- **T-S04-01 — Đặc tả layout XR UX** (7h, S04). Thiết kế safe zone, scale chữ, khoảng panel, curved placement và mono/VR parity. **Acceptance:** Có bản UI tokens và 3 wireframes đọc được trên kính. **Depends:** T-S03-04. **Test:** EV-S04-01.
- **T-S04-02 — Gaze raycast và dwell state** (8h, S04). Cài raycaster, hover/focus, timer dwell có progress và cancellation. **Acceptance:** Test 30 target tương tác, không kích nhầm khi rời gaze trước timeout. **Depends:** T-S04-01. **Test:** EV-S04-02.
- **T-S04-03 — Input event abstraction** (10h, S04). Bọc head pose, controller-button nếu có, back/home, long press, recenter. **Acceptance:** Event simulator và thiết bị dùng chung interface, missing input degrade được. **Depends:** T-S04-01. **Test:** EV-S04-03.
- **T-S04-04 — Kiểm tra access/reduced motion** (9h, S04). Thiết kế cỡ chữ tối thiểu qua kính, contrast, focus indicator, motion limits. **Acceptance:** Test checklist lens, focus và comfort có báo cáo. **Depends:** T-S04-02, T-S04-03. **Test:** EV-S04-04.
- **T-S05-01 — Định nghĩa PanelRegistry schema** (7h, S05). Chốt panel ID, z-order, bounds, focus, pinned, visibility, lifecycle events. **Acceptance:** State reducer deterministic; reject duplicate panel IDs. **Depends:** T-S04-04. **Test:** EV-S05-01.
- **T-S05-02 — Cài launcher và panel factory** (8h, S05). Khởi tạo Chat, 3D, Document, Notes bằng factory, prefab pooling. **Acceptance:** Mở/đóng ba panel không tạo duplicate hay memory leak rõ rệt. **Depends:** T-S05-01. **Test:** EV-S05-02.
- **T-S05-03 — Focus / minimize / restore lifecycle** (10h, S05). Implement focus stack, restore previous panel và close semantics. **Acceptance:** 15 transition tests pass, panels giữ state đã cho phép. **Depends:** T-S05-01. **Test:** EV-S05-03.
- **T-S05-04 — Snapshot window lifecycle** (9h, S05). Automated tests cho open-close-minimize-focus và snapshot scene event. **Acceptance:** Có log/ảnh chụp và hiện trạng panel đúng expected IDs. **Depends:** T-S05-02, T-S05-03. **Test:** EV-S05-04.
- **T-S06-01 — Thiết kế plane bounds và snapping** (7h, S06). Quy tắc vùng đặt, khoảng cách mắt, collision avoidance, no-overlap defaults. **Acceptance:** 20 layout fixtures không có panel chính ở ngoài trường nhìn. **Depends:** T-S05-04. **Test:** EV-S06-01.
- **T-S06-02 — Move / resize / pin panel** (8h, S06). Các command thao tác panel qua gaze và handles lớn. **Acceptance:** Các edge min/max được chặn, focus không mất khi kéo. **Depends:** T-S06-01. **Test:** EV-S06-02.
- **T-S06-03 — Hệ thống localization VN/EN** (10h, S06). Tách text keys, đổi ngôn ngữ runtime, date/number locale. **Acceptance:** 100% menu trong scoped screens có khóa dịch hoặc English fallback. **Depends:** T-S06-01. **Test:** EV-S06-03.
- **T-S06-04 — Kiểm tra thao tác dài và fatigue** (9h, S06). Thử 10 phút move/resize/navigate, đo misclick và thao tác quay đầu. **Acceptance:** Có checklist comfort, không yêu cầu người đeo kính gõ văn bản dài. **Depends:** T-S06-02, T-S06-03. **Test:** EV-S06-04.
- **T-S07-01 — Layout serializer versioned** (7h, S07). Lưu panel state, focus, positions, selected app sang local encrypted/sandbox storage. **Acceptance:** Round-trip schema và migrate version trước pass. **Depends:** T-S06-04. **Test:** EV-S07-01.
- **T-S07-02 — Offline Home và Settings** (8h, S07). Dựng welcome, onboarding, network status, Settings, reset workspace. **Acceptance:** Mất kết nối backend vẫn mở 3D local và dùng launcher được. **Depends:** T-S07-01. **Test:** EV-S07-02.
- **T-S07-03 — Agent activity panel mock** (10h, S07). Làm task timeline, pending/accepted/failed/cancelled placeholders từ fake command stream. **Acceptance:** 30 fixture states render đúng cả empty/error. **Depends:** T-S07-01. **Test:** EV-S07-03.
- **T-S07-04 — G02 Spatial Workspace gate** (9h, S07). Run tests 3 panels + restart + denied input + FPS, ghi video thiết bị. **Acceptance:** G02 evidence đầy đủ; fail nếu không restore state hoặc không thao tác được. **Depends:** T-S07-02, T-S07-03. **Test:** EV-S07-04.

### M03: Agent Orchestrator

**Scope:** Intent routing; tool registry; plan-execute-observe-verify-recover; context/session; cancellation; permissions.
**Implementation strategy:** Finite tool loop bounded, typed contracts, command ID, expected scene version, ACK, retries có hạn, trace.
**Module acceptance:** ≥90% 60 tác vụ baseline đề xuất, chưa đo; không báo thành công nếu Unity chưa ACK
**Known limitation:** Nhiều tool chưa có backend; không gọi Agent cho deterministic controls
**Dependencies:** M02
**Gates:** G04
**Assigned tasks / effort:** 28 tasks, 238 estimated hours.

**Implementation tasks:**

- **T-S10-01 — Định nghĩa CommandEnvelope schema** (7h, S10). Pydantic typed type/args/session/command_id/expected_scene_version/timeout. **Acceptance:** 100% unknown action hoặc invalid params bị 4xx reject. **Depends:** T-S09-04, T-S07-04. **Test:** EV-S10-01.
- **T-S10-02 — Viết tool permission matrix** (8h, S10). Tool read/write/irreversible và consent, resource scopes theo user/session. **Acceptance:** Tool vượt quyền không thể xuất CommandEnvelope. **Depends:** T-S10-01. **Test:** EV-S10-02.
- **T-S10-03 — Scene ACK protocol** (10h, S10). Schema status accepted/running/completed/rejected/failed và observed_scene_version. **Acceptance:** Không báo completed khi chỉ nhận accepted. **Depends:** T-S10-01. **Test:** EV-S10-03.
- **T-S10-04 — Contract fixtures across Python/C#** (9h, S10). Generate JSON golden fixtures, test deserialize mọi trạng thái từ hai phía. **Acceptance:** Fixtures pass và payload không chứa arbitrary executable code. **Depends:** T-S10-02, T-S10-03. **Test:** EV-S10-04.
- **T-S11-01 — Intent taxonomy xây có data** (7h, S11). Định nghĩa 40 câu mẫu cho chat, open, close, focus, RAG, vision và unknown. **Acceptance:** Có labels và ambiguity cases, không route mọi input sang LLM. **Depends:** T-S10-04. **Test:** EV-S11-01.
- **T-S11-02 — Deterministic command router** (8h, S11). Regex/grammar/explicit UI action map close/open/rotate trước model tool call. **Acceptance:** 20 command known đi local không tạo Cloud request. **Depends:** T-S11-01. **Test:** EV-S11-02.
- **T-S11-03 — Context resolution với active target** (10h, S11). Session state selected_part/active_model/active_panel, pronoun resolution. **Acceptance:** Câu “phóng to nó” dùng selected target; ambiguous hỏi lại. **Depends:** T-S11-01. **Test:** EV-S11-03.
- **T-S11-04 — Test routing baseline** (9h, S11). Thử 50 lệnh đơn/câu mơ hồ, confusion matrix và unknown fallback. **Acceptance:** Không auto tác động nếu target không xác định; báo metric per class. **Depends:** T-S11-02, T-S11-03. **Test:** EV-S11-04.
- **T-S12-01 — Implement bounded plan representation** (7h, S12). PlanSteps typed gồm tool_name, expected outcome, depends_on, timeout, retry_policy. **Acceptance:** Plan tối đa cấu hình, no unbounded cycles. **Depends:** T-S11-04. **Test:** EV-S12-01.
- **T-S12-02 — LLM adapter + rate guard** (8h, S12). Model adapter Gemini, token accounting, timeout 429 và circuit breaker. **Acceptance:** Thử fake 429/5xx; không phát sinh chi phí ngoài cấu hình. **Depends:** T-S12-01. **Test:** EV-S12-02.
- **T-S12-03 — Tool registry + dry run** (10h, S12). Registry tools có input/output schema, idempotency và mocked executor. **Acceptance:** Unit tests 20 tools valid/invalid pass. **Depends:** T-S12-01. **Test:** EV-S12-03.
- **T-S12-04 — Multi-step executor benchmark** (9h, S12). Thực thi 12 scenarios multi-step trên mock scene; trace step IDs. **Acceptance:** Không bỏ qua failed prerequisite, không tự tuyên bố completed. **Depends:** T-S12-02, T-S12-03. **Test:** EV-S12-04.
- **T-S13-01 — LAN authenticated transport** (7h, S13). Implement client session, WebSocket hoặc polling theo ADR, local token, request sequencing. **Acceptance:** Reconnect không nhân đôi command, transport logging có correlation id. **Depends:** T-S12-04. **Test:** EV-S13-01.
- **T-S13-02 — Unity execute + observed state** (8h, S13). C# adapters scene/workspace thực thi allowlisted methods và trả state. **Acceptance:** Không cho LLM gọi Unity reflection hoặc C# trực tiếp. **Depends:** T-S13-01. **Test:** EV-S13-02.
- **T-S13-03 — Optimistic concurrency control** (10h, S13). expected_scene_version, conflict/rebase/reject, command expiry. **Acceptance:** 20 conflicting commands không làm sai state hoặc lost update. **Depends:** T-S13-01. **Test:** EV-S13-03.
- **T-S13-04 — Integration bench 30 workflows** (9h, S13). Bài test Python ↔ Unity cho open/model/part/focus/cancel; video/log. **Acceptance:** ≥ 90% mục tiêu trên fixture, số thật được báo cáo. **Depends:** T-S13-02, T-S13-03. **Test:** EV-S13-04.
- **T-S14-01 — Verifier predicate từng tool** (7h, S14). Define expected vs observed predicates và failure categories. **Acceptance:** Tool return là fail không được biến thành success message. **Depends:** T-S13-04. **Test:** EV-S14-01.
- **T-S14-02 — Safe retries and compensation** (8h, S14). Retries idempotent có backoff; undo hành động có thể đảo ngược. **Acceptance:** Không retry non-idempotent unknown outcome bừa bãi. **Depends:** T-S14-01. **Test:** EV-S14-02.
- **T-S14-03 — Cancellation và interruption** (10h, S14). User cancel task, reject queued commands, reconcile running steps. **Acceptance:** Sau cancel không còn lệnh mới được thực thi. **Depends:** T-S14-01. **Test:** EV-S14-03.
- **T-S14-04 — Failure-injection suite** (9h, S14). Tests asset missing, lost ACK, timeout, stale scene, malformed tool response. **Acceptance:** Có trace + error code + correct final state mỗi case. **Depends:** T-S14-02, T-S14-03. **Test:** EV-S14-04.
- **T-S15-01 — Context store với namespaces** (7h, S15). Per-user local profile opt-in, per-task, per-scene và message store TTL. **Acceptance:** Không leak session data giữa hai user fixtures. **Depends:** T-S14-04. **Test:** EV-S15-01.
- **T-S15-02 — Agent context compression** (8h, S15). Chỉ gửi selected objects và summaries; token cap có đo. **Acceptance:** Trace prompt size giảm so với raw full-scene fixture. **Depends:** T-S15-01. **Test:** EV-S15-02.
- **T-S15-03 — Structured reasoning and explanation** (10h, S15). Separates tool observation, retrieved facts, model output; cite sources correctly. **Acceptance:** Không fabricate tool result khi tool unavailable. **Depends:** T-S15-01. **Test:** EV-S15-03.
- **T-S15-04 — Evaluate scene-aware tasks** (9h, S15). 60 task gold suite có simple, multi-step và ambiguous, measured success. **Acceptance:** Có baseline và error taxonomy; ≥90% là target chưa bảo đảm. **Depends:** T-S15-02, T-S15-03. **Test:** EV-S15-04.
- **T-S16-01 — Prompt injection adversarial harness** (7h, S16). Poisoned docs, tool output, user instruction collision, schema abuse tests. **Acceptance:** 0 unauthorized destructive actions trên threat suite. **Depends:** T-S15-04. **Test:** EV-S16-01.
- **T-S16-02 — Budget limiter and usage report** (8h, S16). Per-request max tools, tokens, concurrency, free-tier budget guard. **Acceptance:** Cắt request vượt giới hạn, UI báo quota thay vì crash. **Depends:** T-S16-01. **Test:** EV-S16-02.
- **T-S16-03 — Audit log with privacy controls** (10h, S16). Keep trace IDs/tool statuses, redact content, opt-in logs, export/delete. **Acceptance:** Không ghi ảnh âm thanh raw mặc định. **Depends:** T-S16-01. **Test:** EV-S16-03.
- **T-S16-04 — Gate G04 Agent readiness** (9h, S16). So sánh deterministic vs tool-agent vs verified-agent trên 60 tasks. **Acceptance:** Task success, cost, latency, recoveries đo và báo cáo trung thực. **Depends:** T-S16-02, T-S16-03. **Test:** EV-S16-04.

### M04: Knowledge RAG

**Scope:** Tài liệu PDF/MD/TXT, parser có provenance, chunking, FTS + FAISS, hybrid retrieval, citations.
**Implementation strategy:** Source span/page IDs, dedup, retrieval eval, abstention, document permission scopes, cache.
**Module acceptance:** Answer citation support do người chấm kiểm tra; test retrieval@k
**Known limitation:** Không ingest giáo trình có bản quyền thiếu quyền; không gửi file nhạy cảm lên free tier
**Dependencies:** M03
**Gates:** G05
**Assigned tasks / effort:** 16 tasks, 136 estimated hours.

**Implementation tasks:**

- **T-S17-01 — Document manifest & permission scopes** (7h, S17). doc_id, sha256, owner, mime, license, language, source_url, revision. **Acceptance:** Mỗi chunk truy ngược tài liệu/phiên bản chính xác. **Depends:** T-S16-04. **Test:** EV-S17-01.
- **T-S17-02 — PDF/TXT/MD parsers** (8h, S17). Pages, spans, structure, Unicode Vietnamese, whitespace normalization. **Acceptance:** 20 fixtures có page refs không mất ký tự dấu. **Depends:** T-S17-01. **Test:** EV-S17-02.
- **T-S17-03 — Scanned PDF OCR fork** (10h, S17). Detect scanned pages, opt-in OCR, preserve bounding boxes, failed extraction warning. **Acceptance:** Không trả lời từ trang không đọc được mà giả vờ có text. **Depends:** T-S17-01. **Test:** EV-S17-03.
- **T-S17-04 — Ingestion failure validation** (9h, S17). Corrupt PDF, large file, duplicate hash, restricted doc tests. **Acceptance:** Import fail atomically; không lộ tài liệu user khác. **Depends:** T-S17-02, T-S17-03. **Test:** EV-S17-04.
- **T-S18-01 — Chunker experiment** (7h, S18). Heading-aware chunks, overlap, metadata and page spans, chunk hash. **Acceptance:** Dataset ≥40 question-source pairs có gold spans. **Depends:** T-S17-04. **Test:** EV-S18-01.
- **T-S18-02 — FTS5 lexical index** (8h, S18). SQLite FTS tokenization Vietnamese evaluation, filters owner/license. **Acceptance:** Search Việt/Anh có log matched spans và relevance. **Depends:** T-S18-01. **Test:** EV-S18-02.
- **T-S18-03 — Local embedding + FAISS** (10h, S18). Model/version, batch embedding, index persistence + delete rebuild. **Acceptance:** Index rebuild deterministically từ file manifest. **Depends:** T-S18-01. **Test:** EV-S18-03.
- **T-S18-04 — Ranking baseline evaluation** (9h, S18). BM25 vs FAISS vs RRF hybrid trên gold set. **Acceptance:** Report Recall@5, MRR@10 và failure cases, không giả định thắng. **Depends:** T-S18-02, T-S18-03. **Test:** EV-S18-04.
- **T-S19-01 — Citation-bound answer schema** (7h, S19). Answer text + doc_id/page/span and unsupported claims flag. **Acceptance:** UI chỉ hiện citation khi source span có thật. **Depends:** T-S18-04. **Test:** EV-S19-01.
- **T-S19-02 — Retriever reranking spike** (8h, S19). Rerank candidate list với local rule hoặc cross-encoder nhỏ nếu đáp ứng Mac. **Acceptance:** Ablation đo cải thiện và latency, không thêm dependency khi vô ích. **Depends:** T-S19-01. **Test:** EV-S19-02.
- **T-S19-03 — Abstention & quote limits** (10h, S19). No-source/refusal, prompts restricted to retrieved text, quote budget. **Acceptance:** 20 adversarial/no-answer cases không sinh citation ảo. **Depends:** T-S19-01. **Test:** EV-S19-03.
- **T-S19-04 — Source-opening bridge** (9h, S19). From citation click open correct document page/span in Unity. **Acceptance:** At least 15 clickable citations resolve true source. **Depends:** T-S19-02, T-S19-03. **Test:** EV-S19-04.
- **T-S20-01 — Incremental reindex lifecycle** (7h, S20). Changed/deleted docs invalidate chunk vectors + cached answers. **Acceptance:** No stale citation after delete/reindex. **Depends:** T-S19-04. **Test:** EV-S20-01.
- **T-S20-02 — Bilingual domain retrieval eval** (8h, S20). ≥80 labeled questions across 5 domains with per-source attribution. **Acceptance:** Report per-domain retrieval error not aggregate only. **Depends:** T-S20-01. **Test:** EV-S20-02.
- **T-S20-03 — Restricted-license content guard** (10h, S20). Reject unapproved textbook/medical sources, configurable allowed-source policy. **Acceptance:** OpenStax no LLM ingestion absent prior permission; asset licensing logged. **Depends:** T-S20-01. **Test:** EV-S20-03.
- **T-S20-04 — G05 Knowledge release gate** (9h, S20). Trace 80 queries, citations and No Answer cases incl privacy. **Acceptance:** Gate requires gold eval, source open and data-delete tests. **Depends:** T-S20-02, T-S20-03. **Test:** EV-S20-04.

### M05: Productivity Workspace

**Scope:** Nhập file, so sánh tài liệu, tìm kiếm, notes CRUD, source open, workspace persistence.
**Implementation strategy:** File import permissions, attachment indexing, notes provenance, exports, conflict handling, user actions approval.
**Module acceptance:** WF: import 2 PDFs → so sánh có dẫn trang → lưu/sửa notes → khôi phục
**Known limitation:** Chưa phải macOS system-level multi-app window manager
**Dependencies:** M02,M04
**Gates:** G06
**Assigned tasks / effort:** 16 tasks, 136 estimated hours.

**Implementation tasks:**

- **T-S21-01 — iOS file import workflow** (7h, S21). Files picker copy into sandbox, size/type/duplicate/hash policies. **Acceptance:** Import three sample docs, unsupported type error UX. **Depends:** T-S20-04, T-S07-04. **Test:** EV-S21-01.
- **T-S21-02 — PDF page renderer strategy** (8h, S21). iOS PDFKit bridge or rasterize on Mac; preview cache page indexes. **Acceptance:** Pagination maps visible page to source page with 100% fixture match. **Depends:** T-S21-01. **Test:** EV-S21-02.
- **T-S21-03 — Reader XR presentation** (10h, S21). Large-text focus mode, page nav, zoom, two-panel layout, 2D text mode. **Acceptance:** Read sample article without text overflow or invisible buttons. **Depends:** T-S21-01. **Test:** EV-S21-03.
- **T-S21-04 — Device document comfort test** (9h, S21). Measure legibility in viewer and 2D fallback transition 10 min. **Acceptance:** Long-form text interaction has clear 2D alternative. **Depends:** T-S21-02, T-S21-03. **Test:** EV-S21-04.
- **T-S22-01 — Notes CRUD & local schema** (7h, S22). notes(id,title,body,source_ids,updated_at,version), migrations. **Acceptance:** Create/edit/delete/undo and restart persistence tests. **Depends:** T-S21-04. **Test:** EV-S22-01.
- **T-S22-02 — Citation-to-note workflow** (8h, S22). User selects source span and writes summary, stores provenance. **Acceptance:** Note opens real original page not hallucinated ref. **Depends:** T-S22-01. **Test:** EV-S22-02.
- **T-S22-03 — Document comparison tool** (10h, S22). Multi-file retrieval + explicitly source-labelled contrast and difference table. **Acceptance:** 5 gold compare queries cite both source docs or abstain. **Depends:** T-S22-01. **Test:** EV-S22-03.
- **T-S22-04 — Notes export & state recovery** (9h, S22). Export Markdown with source links, append safe file names and note versioning. **Acceptance:** Round-trip note export/import retains text/source references. **Depends:** T-S22-02, T-S22-03. **Test:** EV-S22-04.
- **T-S23-01 — Personal workspace task models** (7h, S23). Tasks/checklist, due items, activity states as local only. **Acceptance:** Actions idempotent; do not claim external email/calendar action. **Depends:** T-S22-04. **Test:** EV-S23-01.
- **T-S23-02 — Agent productivity tools** (8h, S23). workspace.search, notes.create, notes.edit, compare_docs with permission separation. **Acceptance:** Tool contract tests handle rejection and conflict. **Depends:** T-S23-01. **Test:** EV-S23-02.
- **T-S23-03 — Multi-panel workflows** (10h, S23). Open reader/notes/chat side-by-side, switch focus without losing scroll. **Acceptance:** Complete 8 scenario scripts without lost state. **Depends:** T-S23-01. **Test:** EV-S23-03.
- **T-S23-04 — Productivity eval set** (9h, S23). 20 work-oriented tasks with expected outputs, citations and note artifacts. **Acceptance:** Human-reviewed usefulness and retrieval trace. **Depends:** T-S23-02, T-S23-03. **Test:** EV-S23-04.
- **T-S24-01 — Sync Mac file library discovery** (7h, S24). Explicit folder allowlist, read-only metadata and import agent on Mac. **Acceptance:** No traversal outside allowed folders. **Depends:** T-S23-04. **Test:** EV-S24-01.
- **T-S24-02 — Local Mac-hosted API connectivity** (8h, S24). LAN auth/mTLS decision, Wi-Fi failure, resume and cached docs. **Acceptance:** Mac unavailable -> clear banner with local notes usable. **Depends:** T-S24-01. **Test:** EV-S24-02.
- **T-S24-03 — Document search regression** (10h, S24). Delete/reimport/provenance across two versions and multiple notes. **Acceptance:** Old document references marked stale not silently redirected. **Depends:** T-S24-01. **Test:** EV-S24-03.
- **T-S24-04 — G06 Productivity gate** (9h, S24). Demo import 2 PDFs → compare → note → source open → restart. **Acceptance:** All artifacts saved plus video and trace. **Depends:** T-S24-02, T-S24-03. **Test:** EV-S24-04.

### M06: Speech & Voice

**Scope:** Push-to-talk, VAD, ASR tiếng Việt, transcript edit, TTS playback, interruption, fallback.
**Implementation strategy:** iOS bridge; on-device language check; whisper.cpp Mac fallback; safe audio session states; WER test.
**Module acceptance:** WER tiếng Việt đo riêng, e2e voice command + TTS không chặn XR
**Known limitation:** Không hứa offline vi-VN trước khi kiểm tra
**Dependencies:** M03
**Gates:** G07
**Assigned tasks / effort:** 16 tasks, 136 estimated hours.

**Implementation tasks:**

- **T-S25-01 — iOS microphone native bridge** (7h, S25). AVAudioSession lifecycle, privacy strings, start/stop, route changes. **Acceptance:** Denial, background and headset audio route cases tested. **Depends:** T-S16-04. **Test:** EV-S25-01.
- **T-S25-02 — Push-to-talk UX state** (8h, S25). Idle/listening/transcribing/executing/speaking, visible mute/stop and timeout. **Acceptance:** No recording when idle; action to cancel within one step. **Depends:** T-S25-01. **Test:** EV-S25-02.
- **T-S25-03 — Audio transport protocol** (10h, S25). PCM encoding/resample chunk metadata, replay protection, duration cap. **Acceptance:** Audio fixtures round-trip correctly; no raw audio persistent by default. **Depends:** T-S25-01. **Test:** EV-S25-03.
- **T-S25-04 — Privacy and logs audit** (9h, S25). Retention, mic consent, sanitize transcripts, redaction and error notification. **Acceptance:** All start/stop/deny produce visible state, deletion works. **Depends:** T-S25-02, T-S25-03. **Test:** EV-S25-04.
- **T-S26-01 — Probe Apple Speech vi-VN** (7h, S26). Check supportedLocales/on-device flag, collect actual device findings. **Acceptance:** Document exact locale availability and online/offline path. **Depends:** T-S25-04. **Test:** EV-S26-01.
- **T-S26-02 — Mac whisper.cpp deployment** (8h, S26). Quantized model selection, Metal/CPU test, streaming chunk limits. **Acceptance:** Vietnamese sample transcribed locally; memory/latency logged. **Depends:** T-S26-01. **Test:** EV-S26-02.
- **T-S26-03 — Vietnamese gold utterance dataset** (10h, S26). ≥100 consented short commands with names, noise, accents; transcripts. **Acceptance:** Gold set labeled, privacy policy and WER script reproducible. **Depends:** T-S26-01. **Test:** EV-S26-03.
- **T-S26-04 — STT routing benchmark** (9h, S26). Apple Speech vs whisper.cpp on matched utterances. **Acceptance:** Publish WER, p95 latency, wrong-action rate and fallback decision. **Depends:** T-S26-02, T-S26-03. **Test:** EV-S26-04.
- **T-S27-01 — TTS voice inventory VN/EN** (7h, S27). Check available AVSpeechSynthesisVoice locales, voice quality and rate range. **Acceptance:** No false claim if vi-VN voice absent; fallback text. **Depends:** T-S26-04. **Test:** EV-S27-01.
- **T-S27-02 — Speech reply chunking** (8h, S27). Speak short grounded answer, pauses, reading citation summary as text. **Acceptance:** Stop/restart does not overlap audio chunks. **Depends:** T-S27-01. **Test:** EV-S27-02.
- **T-S27-03 — Interruption/echo controls** (10h, S27). Ignore mic during TTS, interrupt with PTT, max speech length and cancel task. **Acceptance:** 25 barge-in cases avoid unintended tool call. **Depends:** T-S27-01. **Test:** EV-S27-03.
- **T-S27-04 — Voice accessibility QA** (9h, S27). Volume, captions, repeat last response, silent mode variations. **Acceptance:** Text always available when output audio fails. **Depends:** T-S27-02, T-S27-03. **Test:** EV-S27-04.
- **T-S28-01 — Voice → Agent → Spatial tool** (7h, S28). Command microphone->STT->intent->ACK->TTS, record stage timings. **Acceptance:** 15 commands produce verified scene state. **Depends:** T-S27-04. **Test:** EV-S28-01.
- **T-S28-02 — Voice + document + 3D** (8h, S28). Speech query with selected piston/heart and referenced PDF source. **Acceptance:** Context correctly attached or clarification shown. **Depends:** T-S28-01. **Test:** EV-S28-02.
- **T-S28-03 — Negative speech injection suite** (10h, S28). Misheard delete/send commands, background conversation, unknown entity. **Acceptance:** Permission required; no destructive action from low confidence. **Depends:** T-S28-01. **Test:** EV-S28-03.
- **T-S28-04 — G07 Voice release gate** (9h, S28). Run 100 utterance WER, 20-minute headset comfort, latency percentiles. **Acceptance:** Gate includes measured evidence and explicit offline support limits. **Depends:** T-S28-02, T-S28-03. **Test:** EV-S28-04.

### M07: Vision Engine

**Scope:** Camera 2D scan, image import, OCR, object detection, diagram analysis, multimodal VLM.
**Implementation strategy:** Input consent, preprocessing, detector taxonomy, OCR language tests, eval dataset, structured results, confidence and fallback.
**Module acceptance:** Scan → detect/extract → confirm → asset match đúng; precision/recall theo nhóm
**Known limitation:** Không camera POV liên tục khi đeo VR Box; tách Scan Mode
**Dependencies:** M01,M03
**Gates:** G08
**Assigned tasks / effort:** 20 tasks, 170 estimated hours.

**Implementation tasks:**

- **T-S29-01 — 2D scan / VR result transition** (7h, S29). Remove-phone 2D camera capture -> VR cached analysis result and safe re-entry. **Acceptance:** Camera not assumed usable while enclosed in VR Box. **Depends:** T-S16-04. **Test:** EV-S29-01.
- **T-S29-02 — Image lifecycle/privacy model** (8h, S29). Pick photo/camera/Mac frame source; consent, EXIF stripping, hash, retention TTL. **Acceptance:** Denied permissions do not block entire app. **Depends:** T-S29-01. **Test:** EV-S29-02.
- **T-S29-03 — Image schema and region references** (10h, S29). VisionObservation(image_id, bbox, label, score, model_version, provenance). **Acceptance:** Invalid coordinate/unknown classes handled without fake certainty. **Depends:** T-S29-01. **Test:** EV-S29-03.
- **T-S29-04 — Capture quality benchmark** (9h, S29). 20 blurred/rotated/low-light images, EXIF orientation and compression. **Acceptance:** Document recognizable quality thresholds and failure message. **Depends:** T-S29-02, T-S29-03. **Test:** EV-S29-04.
- **T-S30-01 — VN/EN OCR pipeline selection** (7h, S30). Prototype Apple Vision Vision OCR and optional Mac OCR, orientation handling. **Acceptance:** Evaluate ≥50 text photos with ground truth CER/WER. **Depends:** T-S29-04. **Test:** EV-S30-01.
- **T-S30-02 — Layout extraction for forms** (8h, S30). Detect reading order, text regions and heading/table hints, retain coordinates. **Acceptance:** 12 golden layouts preserve key-value relationships. **Depends:** T-S30-01. **Test:** EV-S30-02.
- **T-S30-03 — OCR confidence & correction UX** (10h, S30). User can correct uncertain text before Agent operations. **Acceptance:** No unconfirmed low-confidence field sent to action tool. **Depends:** T-S30-01. **Test:** EV-S30-03.
- **T-S30-04 — OCR adversarial regression** (9h, S30). Rotated, partial, handwritten and reflective document cases. **Acceptance:** False certainty logged; unsupported script gracefully rejects. **Depends:** T-S30-02, T-S30-03. **Test:** EV-S30-04.
- **T-S31-01 — Class taxonomy for engineering objects** (7h, S31). Lock 15 initial classes, label rules, overlaps and unknown policy. **Acceptance:** Annotation guide with examples and ambiguity resolution. **Depends:** T-S30-04. **Test:** EV-S31-01.
- **T-S31-02 — Select detector and license audit** (8h, S31). Benchmark torchvision/Core ML compatible model; inspect trained-weight license. **Acceptance:** Source/weights and conversion evidence recorded before deployment. **Depends:** T-S31-01. **Test:** EV-S31-02.
- **T-S31-03 — Dataset & split registry** (10h, S31). ≥200 labeled images target across classes with grouped split, duplicates leakage checks. **Acceptance:** train/val/test split disjoint by source subject. **Depends:** T-S31-01. **Test:** EV-S31-03.
- **T-S31-04 — Detection model baseline evaluation** (9h, S31). Per-class precision/recall/mAP, inference latency Mac/iPhone choice. **Acceptance:** Publish confusion matrix and threshold decisions, no blanket accuracy claim. **Depends:** T-S31-02, T-S31-03. **Test:** EV-S31-04.
- **T-S32-01 — Diagram-specific symbol taxonomy** (7h, S32). Circuit/engine diagram symbols curated; distinguish detector from relationship parser. **Acceptance:** Gold 20 diagrams with boxes and connections. **Depends:** T-S31-04. **Test:** EV-S32-01.
- **T-S32-02 — Symbol connectivity extraction** (8h, S32). Graph building from OCR labels and detected connectors with unknown edge state. **Acceptance:** 20 graph fixtures compare edges with expected graph. **Depends:** T-S32-01. **Test:** EV-S32-02.
- **T-S32-03 — VLM adapter + safety** (10h, S32). Opt-in image to cloud, structured answer JSON, supported/unsupported regions. **Acceptance:** Do not upload without consent and do not fabricate absent components. **Depends:** T-S32-01. **Test:** EV-S32-03.
- **T-S32-04 — Diagram tests and user correction** (9h, S32). Overlay relationships, flag uncertain junction, manual override in UI. **Acceptance:** Every unresolved relation is explicitly marked uncertain. **Depends:** T-S32-02, T-S32-03. **Test:** EV-S32-04.
- **T-S33-01 — Entity linking to curated asset registry** (7h, S33). Match detected canonical labels to asset_id using taxonomy with confidence gate. **Acceptance:** Unknown detection never silently maps to a plausible wrong object. **Depends:** T-S32-04. **Test:** EV-S33-01.
- **T-S33-02 — Scan → confirm → open model** (8h, S33). Scan technical part/photo, confirmation panel, open matching scene part. **Acceptance:** 12 gold workflow traces show correct model and selected part. **Depends:** T-S33-01. **Test:** EV-S33-02.
- **T-S33-03 — CV + Agent provenance protocol** (10h, S33). Agent receives observation model/version/regions, no raw image by default. **Acceptance:** Answers cite observation or ask clarification if confidence low. **Depends:** T-S33-01. **Test:** EV-S33-03.
- **T-S33-04 — G08 CV gate** (9h, S33). Evaluate OCR, detector, diagrams, scan-to-3D, latency/privacy tests. **Acceptance:** Metrics per class + failures + demo; unsupported objects documented. **Depends:** T-S33-02, T-S33-03. **Test:** EV-S33-04.

### M08: 3D Asset/Scene Platform

**Scope:** Asset Registry, mesh-part IDs, spatial transforms, annotations, LOD, animation controller.
**Implementation strategy:** Source licenses, mesh budget, texture compression, lazy loading, part validation, command ACK.
**Module acceptance:** Load/focus/highlight/animate đều kiểm tra được qua scene state
**Known limitation:** Không giả định model có sẵn mọi phần/animation
**Dependencies:** M02,M03
**Gates:** G03, G09
**Assigned tasks / effort:** 16 tasks, 136 estimated hours.

**Implementation tasks:**

- **T-S08-01 — Scene manifest schema v1** (7h, S08). Định nghĩa model_id, part_id, hierarchy, animation names, LOD, licenses, units. **Acceptance:** Schema test reject missing targets và broken hierarchy. **Depends:** T-S07-04. **Test:** EV-S08-01.
- **T-S08-02 — Asset loader validation** (8h, S08). Cài loader bundle local với hash, timeout, placeholder và memory cleanup. **Acceptance:** Không load asset không có provenance; fallback model dùng được. **Depends:** T-S08-01. **Test:** EV-S08-02.
- **T-S08-03 — Scene selection API** (10h, S08). Implement select/isolate/highlight/focus/clear với part IDs. **Acceptance:** 20 scene commands xác nhận đúng target, unknown trả lỗi. **Depends:** T-S08-01. **Test:** EV-S08-03.
- **T-S08-04 — Scene baseline performance** (9h, S08). Benchmark 1 low-poly model, texture budget và leak after 50 open/close. **Acceptance:** Có metrics per asset và memory không tăng vô hạn. **Depends:** T-S08-02, T-S08-03. **Test:** EV-S08-04.
- **T-S09-01 — Animation controller v1** (7h, S09). State machine idle/play/pause/seek/speed cho animation có tên. **Acceptance:** Unknown animation bị reject; seek bounds được enforce. **Depends:** T-S08-04. **Test:** EV-S09-01.
- **T-S09-02 — Render event ACK v1** (8h, S09). C# queue với command_id, scene_version, status completed/failed. **Acceptance:** Duplicate command id không chạy lại thao tác không idempotent. **Depends:** T-S09-01. **Test:** EV-S09-02.
- **T-S09-03 — Integrate panel + model** (10h, S09). Dựng panel viewer hỗ trợ zoom, rotate, annotations và recenter. **Acceptance:** Chọn đúng part 20/20 fixture, không click xuyên panel. **Depends:** T-S09-01. **Test:** EV-S09-03.
- **T-S09-04 — G03 asset gate & registry test** (9h, S09). Test asset missing, malformed, hash mismatch, lifecycle và FPS. **Acceptance:** Báo cáo pass/fail từng loại và scene state logs. **Depends:** T-S09-02, T-S09-03. **Test:** EV-S09-04.
- **T-S34-01 — Model budget catalog** (7h, S34). Define file size, vertices, draw calls, texture limits and source hash per asset. **Acceptance:** All assets have limits and provenance rows. **Depends:** T-S33-04, T-S09-04. **Test:** EV-S34-01.
- **T-S34-02 — LOD/lazy loading pipeline** (8h, S34). LOD selection, async loading, cancellation and dispose, warm cache cap. **Acceptance:** Repeated swaps do not leak allocations; capture profiler traces. **Depends:** T-S34-01. **Test:** EV-S34-02.
- **T-S34-03 — Scene graph semantic hierarchy** (10h, S34). Normalized transforms, mesh-parent path, stable part ID mapping per version. **Acceptance:** Part ID survives export/import and animation replay. **Depends:** T-S34-01. **Test:** EV-S34-03.
- **T-S34-04 — Three-asset stress benchmark** (9h, S34). Heart, engine and planet scene concurrent test incl FPS and thermal. **Acceptance:** Measure frame-time on iPhone 11; scale down if target missed. **Depends:** T-S34-02, T-S34-03. **Test:** EV-S34-04.
- **T-S35-01 — Layered annotation system** (7h, S35). Billboard labels, occlusion policy, language translations and citation hooks. **Acceptance:** 30 label fixtures readable and target part association valid. **Depends:** T-S34-04. **Test:** EV-S35-01.
- **T-S35-02 — Domain simulation adapter contract** (8h, S35). Standard input units, params, time stepping, pause/seek, telemetry output. **Acceptance:** Validation rejects unsupported range/unit. **Depends:** T-S35-01. **Test:** EV-S35-02.
- **T-S35-03 — Content pack installer/versioning** (10h, S35). Manifest schema version, source credits, local packs, migration and rollback. **Acceptance:** Upgrade and downgrade tests preserve user progress where compatible. **Depends:** T-S35-01. **Test:** EV-S35-03.
- **T-S35-04 — G09 Shared 3D gate** (9h, S35). Run asset validator on all domain candidate packs before science milestones. **Acceptance:** Reject missing mesh IDs, bad license info and faulty animation names. **Depends:** T-S35-02, T-S35-03. **Test:** EV-S35-04.

### M09: Astronomy Pack

**Scope:** Solar System; orbit/time controls, Earth/Moon relation, fact cards, quiz.
**Implementation strategy:** Parameter definitions with units, bounded simulation, credited assets, factual source QA.
**Module acceptance:** 2 lesson pathways + 1 simulation có thể thay tốc độ; quiz có key
**Known limitation:** Minh họa quy mô không phải khoảng cách thật nếu scale đã chỉnh
**Dependencies:** M08,M04
**Gates:** G10
**Assigned tasks / effort:** 12 tasks, 102 estimated hours.

**Implementation tasks:**

- **T-S36-01 — NASA asset/legal register** (7h, S36). Select Sun/Earth/Moon/planets models with asset URL, attribution and unit scale. **Acceptance:** 9 model manifests contain rights check and source refs. **Depends:** T-S35-04, T-S20-04. **Test:** EV-S36-01.
- **T-S36-02 — Orbit data contract** (8h, S36). Semi-major axis/unit, orbital period, eccentricity representation and time scale. **Acceptance:** All displayed values linked to source/date and chosen approximation. **Depends:** T-S36-01. **Test:** EV-S36-02.
- **T-S36-03 — Implement Sun-Earth-Moon scene** (10h, S36). Orbital meshes, labels, day/night and camera/navigation constraints. **Acceptance:** Earth and Moon selectable and annotated, no collider confusion. **Depends:** T-S36-01. **Test:** EV-S36-03.
- **T-S36-04 — Gold astronomy content evaluation** (9h, S36). 20 fact cards and 15 questions reviewed with NASA/JPL data. **Acceptance:** Wrong unit or unlabelled artistic scaling causes gate fail. **Depends:** T-S36-02, T-S36-03. **Test:** EV-S36-04.
- **T-S37-01 — Time controller and orbit speed** (7h, S37). Pause, accelerate with visible factor, reset epoch, deterministic updates. **Acceptance:** Same input timestamp yields same orbit state. **Depends:** T-S36-04. **Test:** EV-S37-01.
- **T-S37-02 — Scale modes and scientific disclaimers** (8h, S37). True ratios optional illustration mode, always label visual scaling. **Acceptance:** No representation claims physical scale in illustration mode. **Depends:** T-S37-01. **Test:** EV-S37-02.
- **T-S37-03 — Agent planetary tool kit** (10h, S37). open_planet, focus_body, set_time_scale, show_orbit with typed args. **Acceptance:** 20 agent calls modify intended body and return ACK. **Depends:** T-S37-01. **Test:** EV-S37-03.
- **T-S37-04 — Solar simulation numerical checks** (9h, S37). Compare reference periods for Earth/Moon and timeline edge values. **Acceptance:** Error bounds specified; never claim real ephemeris accuracy. **Depends:** T-S37-02, T-S37-03. **Test:** EV-S37-04.
- **T-S38-01 — Two guided astronomy lessons** (7h, S38). Earth/Moon cycles and planetary comparison with objectives and prerequisites. **Acceptance:** Each lesson has 5 checkpoints linked to scene interactions. **Depends:** T-S37-04. **Test:** EV-S38-01.
- **T-S38-02 — Quiz with verified answer keys** (8h, S38). ≥20 astronomy items, rationale, sources and grading. **Acceptance:** No LLM-generated unreviewed answer key. **Depends:** T-S38-01. **Test:** EV-S38-02.
- **T-S38-03 — Cross-scene knowledge links** (10h, S38). Agent launches relevant docs/media via provenance and references. **Acceptance:** Citations open exact resource. **Depends:** T-S38-01. **Test:** EV-S38-03.
- **T-S38-04 — G10 Astronomy gate** (9h, S38). Run lesson completion, 30 AI requests, 20 quizzes and perf test. **Acceptance:** Reports PASS/FAIL and regression artifacts. **Depends:** T-S38-02, T-S38-03. **Test:** EV-S38-04.

### M10: Physics Pack

**Scope:** Con lắc/động học: thay tham số, đồ thị, units, animation và câu hỏi giải thích.
**Implementation strategy:** Deterministic numerical solver/baseline formula, numerical regression tests, value constraints.
**Module acceptance:** Thay g/l → period mô phỏng khớp công thức trong tolerance test
**Known limitation:** Không dùng animation giả làm mô phỏng chính xác
**Dependencies:** M08
**Gates:** G11
**Assigned tasks / effort:** 12 tasks, 102 estimated hours.

**Implementation tasks:**

- **T-S39-01 — Units and physics reference model** (7h, S39). Select SI units, pendulum small-angle approximation, assumptions and tolerance. **Acceptance:** Requirements distinguish numerical vs illustrative motions. **Depends:** T-S35-04. **Test:** EV-S39-01.
- **T-S39-02 — Deterministic pendulum solver** (8h, S39). ODE or analytic integration with timestep/initial conditions and validity warnings. **Acceptance:** Known reference periods match within explicit tolerance. **Depends:** T-S39-01. **Test:** EV-S39-02.
- **T-S39-03 — Physics UI adjustable parameters** (10h, S39). g,length,amplitude controls with boundary checks and inline units. **Acceptance:** Invalid values rejected and scenes remain usable. **Depends:** T-S39-01. **Test:** EV-S39-03.
- **T-S39-04 — Physics golden numerical tests** (9h, S39). 50 input tuples incl boundaries, energy drift and reproducibility. **Acceptance:** Numeric outputs regression-tracked with expected datasets. **Depends:** T-S39-02, T-S39-03. **Test:** EV-S39-04.
- **T-S40-01 — Graph renderer for displacement/time** (7h, S40). Plot data computed by solver, sample decimation, axis units and legend. **Acceptance:** Plot and animation use exactly same solver state. **Depends:** T-S39-04. **Test:** EV-S40-01.
- **T-S40-02 — Experiment saved state** (8h, S40). Save/load params, compare multiple runs, screenshots and result notes. **Acceptance:** Restoring run reproduces numerical results. **Depends:** T-S40-01. **Test:** EV-S40-02.
- **T-S40-03 — Agent controlled physics experiment** (10h, S40). Tool set_parameter, run/pause, compare_results with schema validation. **Acceptance:** Agent cannot exceed safe limits or invent measured results. **Depends:** T-S40-01. **Test:** EV-S40-03.
- **T-S40-04 — Scientific validation and QA** (9h, S40). Compare analytics within domain assumptions + regression suite. **Acceptance:** Warning when small-angle assumptions violated. **Depends:** T-S40-02, T-S40-03. **Test:** EV-S40-04.
- **T-S41-01 — Three physics lessons** (7h, S41). Pendulum period, gravity sensitivity, amplitude and approximation limits. **Acceptance:** Lessons have objectives, parameter tasks and references. **Depends:** T-S40-04. **Test:** EV-S41-01.
- **T-S41-02 — Physics glossary bilingual** (8h, S41). Curate ≥30 verified terms with definitions, symbols and units. **Acceptance:** VN/EN translation review complete. **Depends:** T-S41-01. **Test:** EV-S41-02.
- **T-S41-03 — Physics quiz and mastery sync** (10h, S41). ≥20 vetted items with numeric tolerance-aware grading. **Acceptance:** Wrong unit or unsupported approximation marked incorrect. **Depends:** T-S41-01. **Test:** EV-S41-03.
- **T-S41-04 — G11 Physics domain gate** (9h, S41). Demo vary g/l, graph, ask agent, answer quiz and restore run. **Acceptance:** Golden tests and evidence logged. **Depends:** T-S41-02, T-S41-03. **Test:** EV-S41-04.

### M11: Chemistry Pack

**Scope:** Molecule 3D, bond labels, rotate, compare geometry, learning card, quiz.
**Implementation strategy:** Curated molecule dataset, bond graph, units, geometry provenance, material rendering.
**Module acceptance:** Đúng cấu trúc và nhãn ít nhất 5 phân tử được kiểm duyệt
**Known limitation:** Không tự sinh cấu trúc hóa học sai bằng LLM
**Dependencies:** M08,M04
**Gates:** G12
**Assigned tasks / effort:** 12 tasks, 102 estimated hours.

**Implementation tasks:**

- **T-S42-01 — PubChem compound source policy** (7h, S42). Select ≥10 educational compounds, canonical CID/SMILES/InChIKey and data license. **Acceptance:** Every molecule linked to PubChem source and request timestamp. **Depends:** T-S35-04, T-S20-04. **Test:** EV-S42-01.
- **T-S42-02 — Import chemical structure dataset** (8h, S42). Fetch curated molecules once, cache SDF/mol, preserve stereochemistry and atom indices. **Acceptance:** Atom/bond count checked against source. **Depends:** T-S42-01. **Test:** EV-S42-02.
- **T-S42-03 — Molecule geometry pipeline** (10h, S42). Convert 2D/3D coords to Unity with bond types, scale, color legend. **Acceptance:** No invented bond and each element mapped correctly. **Depends:** T-S42-01. **Test:** EV-S42-03.
- **T-S42-04 — Chemical validation fixtures** (9h, S42). Valence/charges, stereochemistry exceptions, aromatic bonds and unknown handling. **Acceptance:** Every selected compound passes RDKit/reference checks or flagged. **Depends:** T-S42-02, T-S42-03. **Test:** EV-S42-04.
- **T-S43-01 — Space-filling / ball-and-stick renderer** (7h, S43). Render using pooled primitives/instancing and bond lengths from source coords. **Acceptance:** Switch style without destroying molecule identity. **Depends:** T-S42-04. **Test:** EV-S43-01.
- **T-S43-02 — Atom and bond selection** (8h, S43). Select atoms/bonds, display element, charge, neighbors, bond order. **Acceptance:** 20 interaction fixtures select correct IDs. **Depends:** T-S43-01. **Test:** EV-S43-02.
- **T-S43-03 — Compare molecules side by side** (10h, S43). Open two compounds, aligned legends and different structure annotations. **Acceptance:** No swapped source metadata after focus shift. **Depends:** T-S43-01. **Test:** EV-S43-03.
- **T-S43-04 — Structure QA and mobile profiling** (9h, S43). Large molecule thresholds, scene FPS test, stereo label readability. **Acceptance:** Render budget adhered or downgraded. **Depends:** T-S43-02, T-S43-03. **Test:** EV-S43-04.
- **T-S44-01 — Chemistry lesson pack** (7h, S44). Bond types, molecular geometry, isomer basics and safe scientific explanations. **Acceptance:** At least 3 lessons; claims cite curated source. **Depends:** T-S43-04. **Test:** EV-S44-01.
- **T-S44-02 — Explain-structure Agent tools** (8h, S44). focus_atom, highlight_bond, compare_compounds, show_formula. **Acceptance:** Tool outputs verified against graph and rendered part IDs. **Depends:** T-S44-01. **Test:** EV-S44-02.
- **T-S44-03 — Quizzes with chemical answer keys** (10h, S44). ≥25 assessed questions tied to molecule IDs, formula and geometry. **Acceptance:** All gold answers checked against cached PubChem structural data. **Depends:** T-S44-01. **Test:** EV-S44-03.
- **T-S44-04 — G12 Chemistry gate** (9h, S44). Demonstrate 10 compounds, selection, explain, quiz, license checks. **Acceptance:** No unsupported chemistry facts from LLM. **Depends:** T-S44-02, T-S44-03. **Test:** EV-S44-04.

### M12: Engineering Pack

**Scope:** 4-stroke engine exploded view, piston/valve/crankshaft animation và document-assisted Q&A.
**Implementation strategy:** CAD asset hygiene, rigid transforms, stroke states, named part registry, mechanical validation, technical QA.
**Module acceptance:** Chu trình nạp-nén-nổ-xả và part highlight đúng; quiz+doc source
**Known limitation:** Mô hình mô phỏng để học, không thay CAD/CAE chuyên nghiệp
**Dependencies:** M08,M04
**Gates:** G13
**Assigned tasks / effort:** 16 tasks, 136 estimated hours.

**Implementation tasks:**

- **T-S45-01 — Four-stroke engine part specification** (7h, S45). Define piston, crank, connecting rod, intake/exhaust valves, cylinder, cam timings. **Acceptance:** Hierarchy and motion diagram approved against reference. **Depends:** T-S35-04, T-S20-04. **Test:** EV-S45-01.
- **T-S45-02 — License-reviewed mechanical model** (8h, S45). Acquire/create segmented engine asset; normalize axes, joint pivots, mesh IDs. **Acceptance:** Part names match hierarchy, rights/provenance recorded. **Depends:** T-S45-01. **Test:** EV-S45-02.
- **T-S45-03 — Deterministic kinematics design** (10h, S45). Crank angle θ, connecting rod l, crank radius r, piston displacement formula. **Acceptance:** Golden dataset at crank angles incl top/bottom dead centers. **Depends:** T-S45-01. **Test:** EV-S45-03.
- **T-S45-04 — Engine mesh consistency tests** (9h, S45). Verify pivots, collision, rods attached across animation samples. **Acceptance:** No disconnected components or penetration in scoped model. **Depends:** T-S45-02, T-S45-03. **Test:** EV-S45-04.
- **T-S46-01 — Implement 720° cycle state machine** (7h, S46). Intake/compression/power/exhaust phase events mapped to crank angle. **Acceptance:** Correct ordering over two crank revolutions. **Depends:** T-S45-04. **Test:** EV-S46-01.
- **T-S46-02 — Piston and rod transformations** (8h, S46). Slider-crank constraints, crank rotation, rod articulation and valve cues. **Acceptance:** Compare animation keyframes to kinematic golden points. **Depends:** T-S46-01. **Test:** EV-S46-02.
- **T-S46-03 — Animation controls and labels** (10h, S46). Play/pause/slow motion/scrub/step-by-stroke with overlays. **Acceptance:** All modes show correct active phase and named part. **Depends:** T-S46-01. **Test:** EV-S46-03.
- **T-S46-04 — Mechanical regression and QA** (9h, S46). Compare 12 sampled crank angles and stop/resume/cycle reset. **Acceptance:** State transition golden tests PASS. **Depends:** T-S46-02, T-S46-03. **Test:** EV-S46-04.
- **T-S47-01 — Exploded assembly hierarchy** (7h, S47). Part assembly transform targets and reverse restore, snap labels. **Acceptance:** Exploded view reversible with positions intact. **Depends:** T-S46-04. **Test:** EV-S47-01.
- **T-S47-02 — Isolation/focus and guided inspection** (8h, S47). Select piston/valve/crank, hide others, focus path and highlights. **Acceptance:** 20 part commands select correct target. **Depends:** T-S47-01. **Test:** EV-S47-02.
- **T-S47-03 — Technical RAG and source linkage** (10h, S47). Curated docs with part_ids and page refs, source-limited explanation. **Acceptance:** Explain part cites mechanical sources; no hallucinated part. **Depends:** T-S47-01. **Test:** EV-S47-03.
- **T-S47-04 — Technical scenario and error tests** (9h, S47). User asks nonexistent component, incompatible animation or unsafe repair. **Acceptance:** Tool responds unavailable, no dangerous fabricated instructions. **Depends:** T-S47-02, T-S47-03. **Test:** EV-S47-04.
- **T-S48-01 — Engineering learning path** (7h, S48). Guided sequence basic components → cycle → diagnosis of simple conceptual mistakes. **Acceptance:** Clear educational-only limitation. **Depends:** T-S47-04. **Test:** EV-S48-01.
- **T-S48-02 — Verified assessment** (8h, S48). ≥30 labeled questions and 15 spatial tasks about stroke/part function. **Acceptance:** Independent review answer key and source links. **Depends:** T-S48-01. **Test:** EV-S48-02.
- **T-S48-03 — Integrated Agent voice demo** (10h, S48). Voice asks focus piston, step compression, compare parts, export notes. **Acceptance:** Client ACKs and demo trace every action. **Depends:** T-S48-01. **Test:** EV-S48-03.
- **T-S48-04 — G13 Engineering acceptance** (9h, S48). Run full cycle, golden kinematics, 30 questions, 20 min perf test. **Acceptance:** Failed part mapping blocks release of pack. **Depends:** T-S48-02, T-S48-03. **Test:** EV-S48-04.

### M13: Medical Anatomy Pack

**Scope:** Human heart mesh compartments, route of blood flow, annotations, verified explanatory content, quiz.
**Implementation strategy:** Licensed segmented model, anatomical graph, reviewer signoff, LLM abstention for diagnoses.
**Module acceptance:** Chọn đúng part IDs, đáp án quiz được kiểm duyệt, nguồn phù hợp
**Known limitation:** Chỉ giáo dục, không chẩn đoán/điều trị
**Dependencies:** M08,M04
**Gates:** G14
**Assigned tasks / effort:** 16 tasks, 136 estimated hours.

**Implementation tasks:**

- **T-S49-01 — Scope & non-clinical safety policy** (7h, S49). Human-heart education only; no diagnosis, treatment or symptom triage. **Acceptance:** Medical response gate rejects clinical advice requests. **Depends:** T-S35-04, T-S20-04. **Test:** EV-S49-01.
- **T-S49-02 — NIH 3D anatomy asset procurement** (8h, S49). Identify heart segmentation with individual license and accuracy review, inspect source model. **Acceptance:** Rights, region names, orientation evidence documented. **Depends:** T-S49-01. **Test:** EV-S49-02.
- **T-S49-03 — Anatomical ontology and part IDs** (10h, S49). RA, RV, LA, LV, aorta, venae cavae, pulmonary vessels, valves ontology. **Acceptance:** No duplicate anatomy IDs and each expected structure marked present/missing. **Depends:** T-S49-01. **Test:** EV-S49-03.
- **T-S49-04 — Segmentation review & missing regions** (9h, S49). Overlay labels on actual meshes, confirm orientation and use explicit fallback where parts missing. **Acceptance:** Missing structures never substituted with fictitious mesh. **Depends:** T-S49-02, T-S49-03. **Test:** EV-S49-04.
- **T-S50-01 — Heart mesh segmentation/visualization** (7h, S50). Material groups, anatomy labels and view presets based on validated parts. **Acceptance:** 20 region selections yield correct IDs. **Depends:** T-S49-04. **Test:** EV-S50-01.
- **T-S50-02 — Blood-flow relation graph** (8h, S50). Represent right/left flow through chambers and vessels; validate directional relations. **Acceptance:** Golden relation tests verify correct order and oxygenation labels. **Depends:** T-S50-01. **Test:** EV-S50-02.
- **T-S50-03 — Cross-section / layer control** (10h, S50). Implement only available segmented regions and isolate features. **Acceptance:** Never claim cross-section if asset mesh does not support. **Depends:** T-S50-01. **Test:** EV-S50-03.
- **T-S50-04 — Mobile heart scene profiling** (9h, S50). Measure mesh size/FPS/memory and optimize LOD/labels for iPhone. **Acceptance:** Perf results measured and documented. **Depends:** T-S50-02, T-S50-03. **Test:** EV-S50-04.
- **T-S51-01 — Medical education source register** (7h, S51). Choose permitted texts and websites, record redistribution/AI-ingestion rights. **Acceptance:** No unauthorized OpenStax ingestion; source decisions auditable. **Depends:** T-S50-04. **Test:** EV-S51-01.
- **T-S51-02 — Curated knowledge cards and relations** (8h, S51). ≥40 verified facts tied to part IDs and blood flow graph. **Acceptance:** All cards reviewed with exact source references. **Depends:** T-S51-01. **Test:** EV-S51-02.
- **T-S51-03 — Anatomy RAG guardrails** (10h, S51). Use source-limited queries, abstain clinical prompts, avoid invented disease advice. **Acceptance:** Adversarial clinical QAs trigger educational limitation. **Depends:** T-S51-01. **Test:** EV-S51-03.
- **T-S51-04 — Domain reviewer checklist** (9h, S51). Require anatomy reviewer signoff; if no reviewer mark content provisional. **Acceptance:** Do not claim medically validated until reviewer approval. **Depends:** T-S51-02, T-S51-03. **Test:** EV-S51-04.
- **T-S52-01 — Anatomy quiz item bank** (7h, S52). ≥30 questions with labels, spatial selection and circulation pathway answers. **Acceptance:** Keys aligned with ontology and licensed content. **Depends:** T-S51-04. **Test:** EV-S52-01.
- **T-S52-02 — Guided lesson design** (8h, S52). Four-chamber overview, vessel relation, blood flow 3 learning paths. **Acceptance:** Each lesson includes learning objectives and remediation. **Depends:** T-S52-01. **Test:** EV-S52-02.
- **T-S52-03 — Agent anatomy walkthrough** (10h, S52). Voice/text selects ventricle, explains source, highlights region, generates quiz. **Acceptance:** No nonexistent mesh highlight reported as completed. **Depends:** T-S52-01. **Test:** EV-S52-03.
- **T-S52-04 — G14 Medical QA release** (9h, S52). Review rights, sources, 30 questions, no diagnosis; anatomy reviewer state explicit. **Acceptance:** No clinical claims; pack labeled draft if review pending. **Depends:** T-S52-02, T-S52-03. **Test:** EV-S52-04.

### M14: Learning Progress

**Scope:** Course/lesson tracking, quiz scoring, error taxonomy, spaced review schedule.
**Implementation strategy:** Deterministic grading, event logging, per-course mastery, pre/post test.
**Module acceptance:** Không làm mất lịch sử sau restart; test scoring and review
**Known limitation:** Gợi ý chưa thể khẳng định nâng cao hiệu quả nếu không có experiment
**Dependencies:** M09,M10,M11,M12,M13
**Gates:** G15
**Assigned tasks / effort:** 8 tasks, 68 estimated hours.

**Implementation tasks:**

- **T-S53-01 — Lesson/quiz progress data model** (7h, S53). student_local_id, pack_version, attempt_id, question_id, result, timestamp. **Acceptance:** History recoverable after restart; no unconsented personal analytics. **Depends:** T-S38-04, T-S41-04, T-S44-04, T-S48-04, T-S52-04. **Test:** EV-S53-01.
- **T-S53-02 — Deterministic quiz grading** (8h, S53). Correct answer keys, numeric tolerance and partial credit per item type. **Acceptance:** 50 grading fixtures including wrong unit and missing answer. **Depends:** T-S53-01. **Test:** EV-S53-02.
- **T-S53-03 — Mastery update rules** (10h, S53). Define per-topic mistake categories and mastery thresholds, not LLM-invented progress. **Acceptance:** Same attempt sequence produces same mastery state. **Depends:** T-S53-01. **Test:** EV-S53-03.
- **T-S53-04 — Learning data deletion & export** (9h, S53). Export and delete progress, version migrations and local backup. **Acceptance:** Deletion removes all related records in tests. **Depends:** T-S53-02, T-S53-03. **Test:** EV-S53-04.
- **T-S54-01 — Prerequisite graph & lesson unlock** (7h, S54). Lessons contain skill prerequisites, explicit unmet reason. **Acceptance:** No cyclic graph and blocked lesson has explanation. **Depends:** T-S53-04. **Test:** EV-S54-01.
- **T-S54-02 — Spaced-review scheduling policy** (8h, S54). Simple deterministic Leitner intervals with timezone-safe dates and snooze. **Acceptance:** Test daylight/calendar boundaries and replay fixtures. **Depends:** T-S54-01. **Test:** EV-S54-02.
- **T-S54-03 — Cross-pack dashboard** (10h, S54). Aggregate completion/mastery for astronomy, physics, chemistry, engineering, anatomy. **Acceptance:** State never leaks from one domain to another. **Depends:** T-S54-01. **Test:** EV-S54-03.
- **T-S54-04 — G15 Learning acceptance** (9h, S54). Take quiz → grade → remediation → repeat → export and re-open. **Acceptance:** Progress/score accuracy and persistence verified. **Depends:** T-S54-02, T-S54-03. **Test:** EV-S54-04.

### M15: Recommendation Engine

**Scope:** Candidate generation + hybrid ranking cho course, docs, 3D asset, kỹ năng ôn, địa điểm.
**Implementation strategy:** Content metadata, implicit feedback, cold start, rule+content-based ranker, offline eval, exposure logging, consent.
**Module acceptance:** Không gợi ý nội dung blocked; benchmark Precision@K, nDCG@K, diversity, coverage
**Known limitation:** Bản đầu không cần deep-learning recommender; LLM không là ranking source-of-truth
**Dependencies:** M14,M04
**Gates:** G16
**Assigned tasks / effort:** 12 tasks, 102 estimated hours.

**Implementation tasks:**

- **T-S55-01 — Item feature taxonomy & eligibility** (7h, S55). Content IDs, domain, prerequisites, difficulty, duration, language, license, version. **Acceptance:** Catalog no stale/blocked assets recommended. **Depends:** T-S54-04. **Test:** EV-S55-01.
- **T-S55-02 — Opt-in user event logging** (8h, S55). Viewed/started/completed/disliked, consent gating and retention rules. **Acceptance:** No profiling without consent; opt-out stops collection. **Depends:** T-S55-01. **Test:** EV-S55-02.
- **T-S55-03 — Cold-start/popularity baseline** (10h, S55). Trending per course and prerequisite-aware deterministic candidate list. **Acceptance:** Never recommend inaccessible content without explanation. **Depends:** T-S55-01. **Test:** EV-S55-03.
- **T-S55-04 — Offline ranking dataset splits** (9h, S55). Synthetic/consented sessions, temporal split, no target leakage. **Acceptance:** Training/evaluation partitions reproducible and audited. **Depends:** T-S55-02, T-S55-03. **Test:** EV-S55-04.
- **T-S56-01 — Candidate generation** (7h, S56). Course-to-course similarity, keywords, co-occurrence only if real data exists. **Acceptance:** Candidates strictly from permitted catalog items. **Depends:** T-S55-04. **Test:** EV-S56-01.
- **T-S56-02 — Weighted scorer & calibration** (8h, S56). Combine similarity, mastery gap, difficulty, prerequisites, novelty and decay. **Acceptance:** Scores deterministic on frozen fixture data. **Depends:** T-S56-01. **Test:** EV-S56-02.
- **T-S56-03 — Diversity / fatigue control** (10h, S56). Cap repeated domains, dismiss, cooldown and no-recommend state. **Acceptance:** Cannot resurface dismissed item in suppression window. **Depends:** T-S56-01. **Test:** EV-S56-03.
- **T-S56-04 — Ranking offline evaluation** (9h, S56). Compare nDCG@5, Recall@5, coverage, diversity vs baselines. **Acceptance:** Publish small-sample uncertainty, no fabricated uplift. **Depends:** T-S56-02, T-S56-03. **Test:** EV-S56-04.
- **T-S57-01 — Learning next-step suggestions** (7h, S57). Cards for remedial lesson, model, notes with rationale codes. **Acceptance:** Each rationale maps to actual observed event. **Depends:** T-S56-04. **Test:** EV-S57-01.
- **T-S57-02 — Agent recommendation tools** (8h, S57). recommend_next, explain_why, dismiss, reset_preferences, no arbitrary destinations. **Acceptance:** Tool never invents catalog item or user history. **Depends:** T-S57-01. **Test:** EV-S57-02.
- **T-S57-03 — Domain cross-link recommendations** (10h, S57). After engine quiz suggest mechanics doc; after heart quiz suggest circulation lesson. **Acceptance:** 20 curated end-to-end scenarios reviewed. **Depends:** T-S57-01. **Test:** EV-S57-03.
- **T-S57-04 — G16 Recommender release** (9h, S57). Leakage, cold-start, privacy and ablation suite + human review. **Acceptance:** No claims of efficacy until A/B experimentation. **Depends:** T-S57-02, T-S57-03. **Test:** EV-S57-04.

### M16: Maps & Places

**Scope:** Nearby POI search, map preview, place cards, route alternatives, ETA, saved places, handoff Maps.
**Implementation strategy:** Native MapKit iOS adapter, Core Location consent, region coverage tests, route geometry, POI freshness, error fallback.
**Module acceptance:** POI → route → xem preview khi đứng yên → Maps 2D; đường phải do routing API cung cấp
**Known limitation:** Không lái xe/đi bộ đeo VR Box; tránh API public Nominatim production
**Dependencies:** M02,M03
**Gates:** G17
**Assigned tasks / effort:** 16 tasks, 136 estimated hours.

**Implementation tasks:**

- **T-S58-01 — Navigation scope & safety contract** (7h, S58). VR route preview stationary; navigation handoff to 2D with headset removed. **Acceptance:** No immersive walk/drive guidance while vision blocked. **Depends:** T-S16-04. **Test:** EV-S58-01.
- **T-S58-02 — Core Location bridge** (8h, S58). When-in-use permission, precise/approximate/denied fallback and revocation. **Acceptance:** No background location collection by default. **Depends:** T-S58-01. **Test:** EV-S58-02.
- **T-S58-03 — Place schema/provider adapter** (10h, S58). PlaceID, coordinates, category, address, provider attribution and freshness. **Acceptance:** No hallucinated places or unsupported open-now assertions. **Depends:** T-S58-01. **Test:** EV-S58-03.
- **T-S58-04 — Apple MapKit compatibility spike** (9h, S58). MKLocalSearch and map snapshot rendered through Unity native bridge. **Acceptance:** Search test device actually returns POI or record blocker. **Depends:** T-S58-02, T-S58-03. **Test:** EV-S58-04.
- **T-S59-01 — POI discovery with search/filter** (7h, S59). User-provided query/location, category results and missing-field handling. **Acceptance:** Coverage and no-result states well-defined. **Depends:** T-S58-04. **Test:** EV-S59-01.
- **T-S59-02 — Stationary VR map panel** (8h, S59). MapKit snapshot/tiles compliant representation, marker focus and pan/zoom. **Acceptance:** Coordinates align with labels; no provider attribution hidden. **Depends:** T-S59-01. **Test:** EV-S59-02.
- **T-S59-03 — Saved places and favorites** (10h, S59). Persist explicit choices, imported links, favorites/privacy deletion. **Acceptance:** No storing exact location without consent. **Depends:** T-S59-01. **Test:** EV-S59-03.
- **T-S59-04 — Place coverage benchmark** (9h, S59). Test ≥10 locations urban/rural, normalized provider result quality. **Acceptance:** Report missing categories and region failures honestly. **Depends:** T-S59-02, T-S59-03. **Test:** EV-S59-04.
- **T-S60-01 — MKDirections request adapter** (7h, S60). Walking/driving when provider supports, response decode polyline/steps/ETA. **Acceptance:** Never fabricate ETA if provider unavailable. **Depends:** T-S59-04. **Test:** EV-S60-01.
- **T-S60-02 — Route alternatives and error UX** (8h, S60). Transport mode availability, invalid coordinate, throttling/no-route cases. **Acceptance:** All no-route paths show provider error and offer manual fallback. **Depends:** T-S60-01. **Test:** EV-S60-02.
- **T-S60-03 — Route preview to iOS Maps** (10h, S60). User confirms headset removed before MKMapItem navigation launch. **Acceptance:** External 2D Maps handoff works; VR route does not navigate in motion. **Depends:** T-S60-01. **Test:** EV-S60-03.
- **T-S60-04 — Routing QA & cache policy** (9h, S60). Track origin/destination/mode/provider/cache TTL, data minimization. **Acceptance:** 10 route fixtures with valid response or explicit unsupported state. **Depends:** T-S60-02, T-S60-03. **Test:** EV-S60-04.
- **T-S61-01 — Place recommendation ranking** (7h, S61). Filter distance when grounded, explicit preferences, category; never invent opening hours. **Acceptance:** Ranking rationales supported by place/provider fields. **Depends:** T-S60-04. **Test:** EV-S61-01.
- **T-S61-02 — Agent map tools + confirmation** (8h, S61). places.search, route.preview, navigation.open_external with permission gate. **Acceptance:** Dangerous motion use case blocked and audit trace retained. **Depends:** T-S61-01. **Test:** EV-S61-02.
- **T-S61-03 — Maps provider/license guard** (10h, S61). MapKit terms, OSM attribution restrictions, no public Nominatim generic API/autocomplete. **Acceptance:** Provider policy checklist and quota behavior documented. **Depends:** T-S61-01. **Test:** EV-S61-03.
- **T-S61-04 — G17 Geospatial gate** (9h, S61). 20 POI queries, 10 routes, denied GPS, safety handoff and provider errors. **Acceptance:** Release blocked if misleading route or unsafe VR guidance. **Depends:** T-S61-02, T-S61-03. **Test:** EV-S61-04.

### M17: Mac Screen Bridge

**Scope:** Cho xem cửa sổ Mac qua streaming trong VR, audio sync, permission/quality switches.
**Implementation strategy:** ScreenCaptureKit → encode/transport → iPhone decode → panel texture, privacy UI; không mặc định remote-control.
**Module acceptance:** 1 cửa sổ được stream có consent; độ trễ và FPS ghi số đo thực
**Known limitation:** Không tạo thêm desktop vật lý hoặc hệ cửa sổ OS độc lập
**Dependencies:** M02
**Gates:** G18
**Assigned tasks / effort:** 4 tasks, 34 estimated hours.

**Implementation tasks:**

- **T-S62-01 — ScreenCaptureKit permission spike** (7h, S62). Choose one Mac window, user consent and protected content fallback. **Acceptance:** Permission denied displays descriptive error. **Depends:** T-S07-04. **Test:** EV-S62-01.
- **T-S62-02 — Encode/stream/decode prototype** (8h, S62). Test VideoToolbox/H264 or supported stream over local authenticated link. **Acceptance:** Document actual latency, resolution, bandwidth and heat. **Depends:** T-S62-01. **Test:** EV-S62-02.
- **T-S62-03 — Single spatial monitor UX** (10h, S62). Render one capture frame in Unity panel, clear disconnect status. **Acceptance:** No claim multi-monitor OS virtualization or remote input. **Depends:** T-S62-01. **Test:** EV-S62-03.
- **T-S62-04 — G18 feasibility decision** (9h, S62). Stop/go thresholds: p95 latency, 10-minute stability and permissions. **Acceptance:** Report outcome; feature not mandatory for Mobile VR core release. **Depends:** T-S62-02, T-S62-03. **Test:** EV-S62-04.

### M18: Security & Reliability

**Scope:** Secrets, API quotas, token caps, user consent, sandbox paths, audit, telemetry opt-in, backup.
**Implementation strategy:** Threat model, permission scopes, deterministic fallback, CI tests, in-device profiler, failure injection.
**Module acceptance:** 0 lộ secrets, tool validation 100%, app crash-free 20 phút thử
**Known limitation:** Chi phí cloud không đảm bảo 0 khi hết free-tier
**Dependencies:** M01
**Gates:** G20
**Assigned tasks / effort:** 12 tasks, 102 estimated hours.

**Implementation tasks:**

- **T-S64-01 — End-to-end data flow and threat model** (7h, S64). Trust boundaries iPhone/LAN/Mac/Cloud/3D imports, STRIDE review. **Acceptance:** Top threats linked to mitigations and test IDs. **Depends:** T-S61-04, T-S57-04, T-S52-04, T-S28-04. **Test:** EV-S64-01.
- **T-S64-02 — Permission audit matrix** (8h, S64). Camera/mic/location/docs/screen, revocation and retention per feature. **Acceptance:** Default denial does not break unrelated core app. **Depends:** T-S64-01. **Test:** EV-S64-02.
- **T-S64-03 — Agent prompt injection and action gate** (10h, S64). Malicious content and tools must not trigger unapproved actions. **Acceptance:** 0 unauthorized effect in adversarial set. **Depends:** T-S64-01. **Test:** EV-S64-03.
- **T-S64-04 — Release vulnerability review** (9h, S64). Check dependencies, secrets scan, HTTPS/LAN auth, data wipe and logs. **Acceptance:** Evidence per vulnerability and residual risks documented. **Depends:** T-S64-02, T-S64-03. **Test:** EV-S64-04.
- **T-S65-01 — Load testing FastAPI and LLM mocks** (7h, S65). Concurrent sessions, cancel/retry, queue backpressure and rate limits. **Acceptance:** Target p95 set after baseline; no unbounded queues. **Depends:** T-S64-04. **Test:** EV-S65-01.
- **T-S65-02 — Device frame-time & memory regression** (8h, S65). Open 3 panels + domain model, 20-minute repeated perf on iPhone 11. **Acceptance:** Measured median/p95 frame-time, thermal; log degradation. **Depends:** T-S65-01. **Test:** EV-S65-02.
- **T-S65-03 — Crash-free and upgrade tests** (10h, S65). 12 flows from fresh install/restart/reinstall and migration. **Acceptance:** No data loss across supported schemas. **Depends:** T-S65-01. **Test:** EV-S65-03.
- **T-S65-04 — QA defect triage and severity policy** (9h, S65). Block release on P0/P1 with reproduction, owner, evidence and regression. **Acceptance:** All blockers closed or explicit no-go. **Depends:** T-S65-02, T-S65-03. **Test:** EV-S65-04.
- **T-S66-01 — Full 7-app E2E matrix** (7h, S66). Run workspace, science, engine, medical, vision, recsys, maps and voice workflows. **Acceptance:** Each WF links test evidence or marked fail. **Depends:** T-S65-04. **Test:** EV-S66-01.
- **T-S66-02 — Content/license provenance release audit** (8h, S66). Models, weights, datasets, texts, illustrations attribution and restrictions. **Acceptance:** No unlicensed copyrighted model or forbidden RAG ingestion in release. **Depends:** T-S66-01. **Test:** EV-S66-02.
- **T-S66-03 — Reproducible build & rollback** (10h, S66). Pin deps, documented Mac/iOS build, migrations and version rollback. **Acceptance:** Fresh-machine walkthrough and checksums included. **Depends:** T-S66-01. **Test:** EV-S66-03.
- **T-S66-04 — G20 Mobile VR release gate** (9h, S66). Gate only when device G0, module gates, tests and risk signoff complete. **Acceptance:** Mark Ready only on evidence; otherwise Not Ready with blockers. **Depends:** T-S66-02, T-S66-03. **Test:** EV-S66-04.

### M19: visionOS Native Port

**Scope:** Windows, Volumes, RealityKit models, Agent contract compatibility chạy Simulator.
**Implementation strategy:** SwiftUI frontend riêng, shared schemas, asset converter; entitlement restrictions documented.
**Module acceptance:** Workflow Agent+3D sample chạy Simulator, parity scope được ghi rõ
**Known limitation:** Không tuyên bố thử nghiệm trên Vision Pro thật
**Dependencies:** M03,M08
**Gates:** G19
**Assigned tasks / effort:** 4 tasks, 34 estimated hours.

**Implementation tasks:**

- **T-S63-01 — Xcode/visionOS runtime compatibility** (7h, S63). Check macOS, Xcode version and simulator image support. **Acceptance:** Document actual start/build blocker or success. **Depends:** T-S16-04, T-S09-04. **Test:** EV-S63-01.
- **T-S63-02 — Minimal SwiftUI/RealityKit client** (8h, S63). Native window and one 3D asset model with permissions and lifecycle. **Acceptance:** Simulator scene interactions verified. **Depends:** T-S63-01. **Test:** EV-S63-02.
- **T-S63-03 — Shared AI contracts parity** (10h, S63). Call same backend tool schemas and render Agent result, reject unsupported sensors. **Acceptance:** Golden schema fixtures match Unity counterpart. **Depends:** T-S63-01. **Test:** EV-S63-03.
- **T-S63-04 — G19 simulator-only evidence** (9h, S63). Video, logs, limitations and missing device-level QA clearly stated. **Acceptance:** No claims of real eye/hand/camera hardware tests. **Depends:** T-S63-02, T-S63-03. **Test:** EV-S63-04.

### M20: Custom XR Hardware R&D

**Scope:** Viewer profile calibration, fit/optics, sensor feasibility, BOM, 6DoF research only.
**Implementation strategy:** Không mua trước khi PoC 3DoF quality gate; test VIO riêng nếu có nguồn camera; documented risk.
**Module acceptance:** BOM, feasibility report, decision log, chưa hứa standalone headset
**Known limitation:** Không nằm trong ngân sách VR Box
**Dependencies:** M01
**Gates:** G21
**Assigned tasks / effort:** 8 tasks, 68 estimated hours.

**Implementation tasks:**

- **T-S67-01 — Custom XR requirements traceability** (7h, S67). Map desired 6DoF, eye/hand tracking, passthrough to sensor/compute/calibration needs. **Acceptance:** No statement that cardboard software supplies missing hardware. **Depends:** T-S03-04, T-S61-04. **Test:** EV-S67-01.
- **T-S67-02 — Sensor/optics benchmarking plan** (8h, S67). Propose BOM ranges, measured motion/camera requirements, safety tests. **Acceptance:** Cost and availability TBD rather than assumed free. **Depends:** T-S67-01. **Test:** EV-S67-02.
- **T-S67-03 — Firmware/driver interface research** (10h, S67). DeviceCapabilityAdapter for IMU, camera frames, external tracking and versioning. **Acceptance:** Mock-only integration recorded as mock, no hardware PASS. **Depends:** T-S67-01. **Test:** EV-S67-03.
- **T-S67-04 — B0 hardware feasibility memo** (9h, S67). Define research hypotheses, stop criteria and funding triggers. **Acceptance:** Not part of first 200k budget; no purchase required. **Depends:** T-S67-02, T-S67-03. **Test:** EV-S67-04.
- **T-S68-01 — Pose and sensor fusion interfaces** (7h, S68). Coordinate spaces, timestamps, calibration matrices and uncertainty representation. **Acceptance:** Can record and replay mock pose without false 6DoF claims. **Depends:** T-S67-04. **Test:** EV-S68-01.
- **T-S68-02 — Privacy-first passthrough plan** (8h, S68). Stereo camera baseline, latency budget, stereo calibration, safety considerations. **Acceptance:** Identifies missing hardware and tests prior to feature promise. **Depends:** T-S68-01. **Test:** EV-S68-02.
- **T-S68-03 — Compatibility strategy for next headset** (10h, S68). OpenXR feasibility and migration checklist, shared agent contracts test. **Acceptance:** Engine-specific portability limitations explicitly documented. **Depends:** T-S68-01. **Test:** EV-S68-03.
- **T-S68-04 — G21 future hardware go/no-go** (9h, S68). Research report and BOM estimation with missing components, ownership and funding gate. **Acceptance:** Defer manufacturing until budget/measurement approved. **Depends:** T-S68-02, T-S68-03. **Test:** EV-S68-04.

## Cross-domain end-to-end verification

- **WF01: Spatial Home** (M02): Launch, 3 panels, focus, resize, restart. **Expected:** Expected registry + persistence + gaze. **Gates:** G02.
- **WF02: Agent multi-tool** (M03): Open engine, select piston, display source, quiz; verify each ACK. **Expected:** 6-step scene/state trace + 60-task evaluation. **Gates:** G04.
- **WF03: Document compare** (M04 M05): Import two PDFs, ask comparison, open cite, save note. **Expected:** All citations resolve to original pages. **Gates:** G05 G06.
- **WF04: Voice-only 3D operation** (M03 M06 M08): Vi-VN speech open model, select part, speak answer, cancel. **Expected:** WER trace + no duplicate commands. **Gates:** G07.
- **WF05: Scan-to-3D** (M07 M08): Capture while out of headset, detect item, confirm mapping, enter VR. **Expected:** Ground-truth class and exact asset_id. **Gates:** G08.
- **WF06: Astronomy lesson** (M09 M14): Sun-Earth-Moon, time scale, quiz, save progress. **Expected:** Period reference + graded lesson status. **Gates:** G10 G15.
- **WF07: Physics experiment** (M10 M14): Change g and length, plot period, agent compare runs. **Expected:** Numerical fixture and graph from same solver. **Gates:** G11 G15.
- **WF08: Chemistry structure** (M11 M14): Open molecules by CID, inspect bond, compare isomers, quiz. **Expected:** Bond graph verified with PubChem. **Gates:** G12 G15.
- **WF09: Engine cycle** (M12): 720° crank, phases, highlight piston, exploded view, docs. **Expected:** Mechanical keyframe + part IDs gold. **Gates:** G13.
- **WF10: Human heart education** (M13): Select ventricle, show blood flow, source QA, anatomy quiz. **Expected:** Anatomy and license review state. **Gates:** G14.
- **WF11: Recommender opt in** (M14 M15): Finish quiz, recommend prerequisite, dismiss and reset data. **Expected:** Ranking rationale, consent, suppression. **Gates:** G15 G16.
- **WF12: Geospatial VR discovery** (M16): Find POIs, show stationary map route preview, remove headset, Apple Maps. **Expected:** Provider ID, actual ETA; no in-motion VR. **Gates:** G17.
- **WF13: Offline workspace** (M02 M05 M18): Disconnect Mac/Cloud, reopen local 3D, notes, user settings. **Expected:** No local feature crash or fake AI completion. **Gates:** G02 G20.
- **WF14: Agent stale-scene race** (M03): Change active scene while Agent issuing command. **Expected:** Version mismatch -> reject/replan explicitly. **Gates:** G04.
- **WF15: Medical restricted request** (M13 M18): User requests diagnosis from anatomy module. **Expected:** No clinical diagnosis and clear educational scope. **Gates:** G14 G20.
- **WF16: Unknown vision object** (M07): Detect unsupported object or ambiguous diagram. **Expected:** Unknown status and manual confirmation; no wrong auto-match. **Gates:** G08.
- **WF17: Map permission denied** (M16): Deny location and request route from typed address. **Expected:** No tracking and fallback/no-result state. **Gates:** G17.
- **WF18: Recommendation cold start** (M15): No history, first lesson recommendation with explicit goals. **Expected:** Rule/curriculum baseline, no fabricated profile. **Gates:** G16.
- **WF19: Mac Bridge feasibility** (M17): Consent capture one window and preview; disconnect. **Expected:** Actual latency/permissions report, stop/go. **Gates:** G18.
- **WF20: visionOS simulator parity** (M19): Open native volume and send shared action schema. **Expected:** Simulator contract tests; no device hardware claim. **Gates:** G19.

## Source register and policy watchlist

- **SRC01 — Cardboard XR plugin official releases:** https://github.com/googlevr/cardboard-xr-plugin/releases. Official releases; check newest version at execution
- **SRC02 — Cardboard Unity Quickstart:** https://developers.google.com/cardboard/develop/unity/quickstart. Samples and build workflow; device compatibility unverified
- **SRC03 — iPhone 11 specifications:** https://support.apple.com/en-us/111865. Screen 1792x828, gyro; no VR app performance guarantee
- **SRC04 — Unity tier eligibility:** https://unity.com/pages/license-compliance. Unity Personal only if financial tier eligible
- **SRC05 — Gemini pricing:** https://ai.google.dev/gemini-api/docs/pricing. Model-specific free-tier, privacy and capacity can change
- **SRC06 — Gemini function calling:** https://ai.google.dev/gemini-api/docs/function-calling. Tool execution by developer application; validate all output
- **SRC07 — Apple Speech on-device feature:** https://developer.apple.com/documentation/speech/sfspeechrecognizer/supportsondevicerecognition. Verify vi-VN capability on actual phone
- **SRC08 — AVSpeechSynthesizer:** https://developer.apple.com/documentation/avfaudio/avspeechsynthesizer. Native TTS, check available voices
- **SRC09 — whisper.cpp:** https://github.com/ggml-org/whisper.cpp. Mac Apple Silicon fallback; evaluate Vietnamese WER
- **SRC10 — Apple Vision text recognition:** https://developer.apple.com/documentation/vision/vnrecognizetextrequest. OCR with real-device locale/quality tests
- **SRC11 — FAISS repository:** https://github.com/facebookresearch/faiss. Vector search component; track binary compatibility
- **SRC12 — SQLite FTS5 documentation:** https://www.sqlite.org/fts5.html. Test Vietnamese tokenization, not assume full support
- **SRC13 — NASA 3D resources:** https://science.nasa.gov/3d-resources/. Check attribution/rights per asset
- **SRC14 — JPL Horizons:** https://ssd.jpl.nasa.gov/horizons/. Ephemerides; simulation approximation clearly marked
- **SRC15 — PubChem PUG REST:** https://pubchem.ncbi.nlm.nih.gov/docs/pug-rest. Compound IDs and structures; provider policy and attribution
- **SRC16 — RDKit docs:** https://www.rdkit.org/docs/. Chemistry structure/validation, exact library license check
- **SRC17 — NIH 3D Terms:** https://3d.nih.gov/terms. Per-entry license; no implied medical validity
- **SRC18 — OpenStax A&P 2e Preface:** https://openstax.org/books/anatomy-and-physiology-2e/pages/preface. No ingestion into LLM/AI offerings without prior written permission; do not use for RAG by default
- **SRC19 — Apple MKLocalSearch:** https://developer.apple.com/documentation/mapkit/mklocalsearch. POI search; regional coverage unverified
- **SRC20 — Apple MKDirections:** https://developer.apple.com/documentation/mapkit/mkdirections. Actual provider routing, cannot invent ETA
- **SRC21 — Apple openInMaps:** https://developer.apple.com/documentation/mapkit/mkmapitem/openinmaps(launchoptions:). Safe 2D navigation handoff, remove headset first
- **SRC22 — OSM tile policy:** https://operations.osmfoundation.org/policies/tiles/. Attribution; no bulk tile prefetch from public servers
- **SRC23 — Nominatim usage policy:** https://operations.osmfoundation.org/policies/nominatim/. Public service 1req/s max, no autocomplete; avoid generic usage
- **SRC24 — Apple ScreenCaptureKit:** https://developer.apple.com/documentation/ScreenCaptureKit. User permissions; streaming implementation separate
- **SRC25 — Creating visionOS app:** https://developer.apple.com/documentation/visionos/creating-your-first-visionos-app. Simulator != actual Apple Vision Pro hardware
- **SRC26 — Apple local network privacy:** https://developer.apple.com/documentation/bundleresources/information-property-list/nslocalnetworkusagedescription. iPhone Mac LAN prompts, evaluate ATS
- **SRC27 — Apple Personal Team / enroll:** https://developer.apple.com/support/compare-memberships/. Free local build vs paid distribution; verify current terms
- **SRC28 — NASA units / general math sources:** https://physics.nist.gov/cuu/Units/. Unit conventions not a substitute for solver reference

## Release decision

G00–G17 and G20 must pass for Mobile VR. G18 Mac Screen Bridge, G19 visionOS Simulator and G21 funded hardware R&D remain optional/deferred and cannot be presented as delivered production capabilities.
