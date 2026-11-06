# GHOST — Product reconstruction and engineering review

2026-11-04. Deliverable: a specification package and executable implementation directive. This stage changes documentation and specification validation only. Application source has not been repaired, built, run or promoted to a passed gate.

## PART A — GHOST PRODUCT RECONSTRUCTION

GHOST is a Windows 11 x64 desktop reference utility. Users explicitly save cropped screen snapshots into POCKET, a hidden shelf. DUAL keeps the normal Windows mouse independent from a separate logical pointer, GHOST, eventually controlled through local webcam hand tracking. The second input must select, drag, resize, pin and scroll Pocket-owned content while the mouse continues working. Workspace must support genuinely simultaneous operations on different targets with independent release. This is the technical differentiator and the highest-risk acceptance test.

Evidence: AGENTS.md and MASTER_SPEC define the invariants; IMPLEMENTATION_RFC defines distinct passive/Workspace/capture windows; CODEX_START_PROMPT and CODEX_PHASES scope the first assignment to A1; design tokens and PNGs establish Quiet Instrument; existing source is a small unverified A1 attempt. The later creator statement broadens brand ambition but does not establish a new platform category, cloud business model or AI requirement. The latest meta-prompt requests research/specification rather than application implementation.

Inspected: workspace tree, handoff/start/verification files, active specifications/research/risk/scope/design/QA/decisions/evidence, all current application/core/test project files, solution, scripts, design tokens and both preview PNGs. Listed original ZIP entries without modifying/extracting it. archive/v1 remains historical; its existence contradicts claims in older text that it was excluded from the kit. No archived concept is imported as authority. The HTML/PNG concept is a visual reference, not native evidence; the dense demonstration page should not become the ordinary launch experience.

Actual solution root is the nested GHOST_CODEX_PROJECT_READY directory, not the parent Documents/ghost described by old setup text. Preserve this layout rather than moving files during discovery. Git discovery reaches an unrelated home-level repository and refuses access on ownership grounds; do not set global safe.directory or treat that repository as the project.

Current source inventory:

| Component | Evidence | Actual status |
|---|---|---|
| WPF shell | MainWindow with show/hide and local keyboard handler | Source present; not compiled |
| Overlay | Win32 styles and WPF Ghost/panel shapes | Source present; native behavior NOT RUN |
| Simulator | controller arrows/Space and NudgeGhost | Local-focus-dependent; insufficient for passive proof |
| Core | one rectangle, Ghost point and one global owner | One-panel example, cannot represent two concurrent claims |
| Tests | two xUnit methods | Unexecuted; missing visible Xunit import |
| Solution | handwritten configurations | Missing Build.0/Release mapping; build-discovery risk |
| Camera/UIA/capture/library/storage/package | specification only | Unimplemented |

The prior assistant chose .NET 8 to match installed runtimes. That was an unjustified deviation from the approved .NET 10 contract; this package records the correction for future implementation. The earlier description of a scaffold was not evidence that the app compiles or performs independent interaction.

## PART B — COMPLETE REQUIREMENTS REGISTRY

The stable registry is directive §4, with feature contracts in §15. F01–F19 are CONFIRMED; all remain unverified. I01 (bounded/virtualized library presentation) is INFERRED infrastructure. P01/P02 (search, quick slots, RAM pockets, compare, magnetism) are PROPOSED conveniences outside the core release. U01–U03 record unresolved exact dependencies/licenses, public branding, measured compatibility/resources.

Original native Windows, no cloud/paid dependency, camera opt-in, full camera-off path, no OS cursor hijacking, static snapshots, per-target dual ownership, reliable recovery, accessibility and offline packaging remain confirmed. No backend, identity, synchronization, monetization or generic AI system is implied. The commercial pricing/business model is unspecified; zero required runtime API spend does not establish a free distribution license.

Conflict register:

| Conflict | Resolution and effect |
|---|---|
| Nested actual folder vs flat setup examples | Use actual solution root; avoid broad file moves |
| net8 source vs approved .NET10 | A1 repair targets .NET10; missing SDK remains blocker |
| Early spec says release palm to dismiss vs latched Peek correction | Interactive Peek latches; idle/explicit close only; hold-to-show Glance deferred |
| Passive clickable controls vs physical mouse pass-through | Ghost internal hit-test; mouse uses conventional UI/tray/hotkey; no passive interception |
| Virtual desktop source vs single selected monitor | One selected primary display in A1; multi-monitor deferred |
| v1 “SHIPPED” heading vs unimplemented source | Treat heading as intended release scope; no actual shipped feature |
| Broad platform language vs concrete reference utility | Product identity remains reference utility; expansion requires explicit RFC |
| Design 372 DIP minimum vs 36%-monitor cap | Clamp to usable work area, allow scrolling; document layout rule in directive |
| Different old gate numbering schemes | A1,A2,B1,B2,C,D1,D2,E remains execution order |
| Camera preview vs no pixel IPC | Choose bounded volatile calibration-only preview route at B1 and document it; no general frame transport |

## PART C — RESEARCH FINDINGS

Sources below were opened during this pass. Access date 2026-11-04. Update date is reported only where exposed by the retrieved page; otherwise “not recorded.” Conclusions are API/product documentation, not empirical GHOST results. Confidence means confidence in the cited conclusion.

| Ref | Examined primary source and date | Conclusion / confidence / GHOST consequence |
|---|---|---|
| N01 | [Microsoft Window Features](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features); update not recorded | Layered + transparent style documents mouse pass-through. High for API semantics; actual WPF/native focus/drag still needs A1. |
| N02 | [UIA threading](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-threading); update not recorded | Desktop UIA calls belong on a separate windowless MTA thread. High; process watchdog is our additional recovery design, not a Microsoft guarantee of timeout. |
| N03 | [MediaPipe Hand Landmarker Python](https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker/python); update not recorded | Asynchronous live inference may skip busy input. High; use timestamps, latest data and independent stale cancellation; do not assume every frame produces output. |
| N04 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core); page says updated 2026-10-25 | .NET10 is active LTS; .NET8 is near its listed November 2026 end. High for choosing .NET10; confirm current patch during setup, no exact SDK pin invented here. |
| N05 | [Publishing modes](https://learn.microsoft.com/en-us/dotnet/core/deploying/); update not recorded | Self-contained publication bundles .NET runtime. High; tracker/native/model assets still require a separate package inventory and test. |
| N06 | [PowerToys Crop And Lock](https://learn.microsoft.com/en-us/windows/powertoys/crop-and-lock); update not recorded | Live thumbnail and reparent modes already address cropped references. High for documented capability; no empirical superiority claim. |
| N07 | [OnTopReplica repository](https://github.com/LorenzCK/OnTopReplica); README, update not recorded | Existing real-time always-on-top window replica. High for advertised intent; current compatibility/maintenance not tested here. |
| N08 | [SetWindowDisplayAffinity](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity); update not recorded | Capture exclusion applies to own top-level window with constraints; not a security/DRM guarantee. High; supplement capture ordering and inspect failure. |
| N09 | [High DPI desktop guidance](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows); update not recorded | Desktop scaling requires explicit handling. High; typed coordinate conversions and DPI tests. Page's older UWP recommendation is not adopted as stack guidance. |
| N10 | [MediaPipe Python setup](https://developers.google.com/edge/mediapipe/solutions/setup_python); update not recorded | Windows desktop is listed; broad Python support statement is not proof of any exact wheel/interpreter combination. High; real version/native dependency test at B1. |
| N11 | [MediaPipe LICENSE](https://github.com/google-ai-edge/mediapipe/blob/master/LICENSE); revision not pinned | Repository code is Apache-2.0. High for repository license; model/binary/transitive redistribution remains U01 and needs exact artifact review. |
| N12 | [Capturing an Image](https://learn.microsoft.com/en-us/windows/win32/gdi/capturing-an-image); update not recorded | Documented memory bitmap/BitBlt acquisition is a candidate for explicit one-shot snapshot. High for API availability; HDR/protected/compositor behavior unresolved. |
| N13 | [OpenAI AGENTS.md guidance](https://learn.chatgpt.com/docs/agent-configuration/agents-md); update not recorded | Durable project instructions and discovery hierarchy support a concise AGENTS plus linked handoff. High; no specific model/price, CLI behavior or unattended permission claim added. |

Competitive comparison (documentation comparison, not a usability study):

| Product | Established value | GHOST opportunity / tradeoff |
|---|---|---|
| PowerToys Crop And Lock | Cropped live thumbnail; interactive reparent subject to compatibility | GHOST offers saved static references and independent hand control, but adds camera/gesture effort and loses live updates in v1 |
| OnTopReplica | Live replica of selected window region | GHOST tests transient recall plus independent Pocket manipulation; no claim that ordinary floating crops are novel |
| Keyboard-only reference shelf | Fast predictable shortcuts and normal input | GHOST must justify hand fatigue/camera cost with concurrent use; keep full shortcut path |

Proposed product validation: five consenting users, counterbalanced task order, three tasks (create, recall during ongoing mouse operation, dismiss), compare with installed version of PowerToys and keyboard-only Pocket. Record time/errors/interruptions/fatigue, privacy concerns and next-day reuse. This is exploratory, not statistically conclusive or a claim of unmet market demand. No user frustration data was collected in this pass.

## PART D — LIMITATIONS AND SOLUTIONS

Existing R01–R28 in canonical research remain open as appropriate. Additional risks are design assessments; likelihood is qualitative and unmeasured.

| ID / rank | Failure/root cause; likelihood and impact | Detection / prevention / recovery | Tradeoff / proof / status |
|---|---|---|---|
| R29 / P0 | Controller must own keyboard focus to move Ghost; certain from source, invalidates A03/A04 demonstration | Inspect handler routing; timed internal replay after countdown; Stop cancels | Less interactive simulator; A03–A05,S02 native proof still NOT RUN |
| R30 / P0 | One Owner serializes all interactions; certain, prevents distinct-target dual drag | Reducer invariants; per-pointer states/per-target claims; cancel only owning pointer | More explicit state, S03/C09; inspected defect, unimplemented repair |
| R31 / P0 | Missing SDK/imports/solution build mappings; high, compiler unavailable or no-op build | Compile project directly and assert discovered tests/output; .NET10 pin and correct solution | Requires tooling setup; M00/M01 blocked/unverified |
| R32 / P1 | Cross-process monotonic epochs differ; medium if naively subtracted, invalid latency/stale decisions | Skew/offset synthetic tests; handshake/host receive age; reject session mismatch | More timing metadata; S05,S06; mitigation designed |
| R33 / P0 | Latest-sample coalescing can discard sole release; medium, stuck drag | Process gesture transitions before render coalescing, independent stale timer; cancel fail-closed | Can produce safe false cancel; S06/B06; mitigation designed |
| R34 / P0 | PNG and manifest cannot atomically commit together; high on interruption, orphan/lost card | Write-ahead order, generation/backup and orphan reconciliation; preserve plausible assets | Recovery code/extra disk; crash-at-each-stage S08; designed |
| R35 / P0 | Black capture is not reliable protected-content detector; high ambiguity, false success or false rejection | Preview warning, retry/cancel/informed confirmation; never security claim | Extra step for black images; C04/S07; designed |
| R36 / P0 | UIA name-based danger filtering misses sensitive controls; high if generic Invoke allowed | Explicit tested app/control allowlist, identity revalidation, no generic automation | Narrow compatibility; S04/A06–A09; designed |
| R37 / P0 | Host dies but tracker retains camera; medium, privacy/resource leak | stdin EOF plus child job lifetime, force termination; launch disarmed next time | Process lifecycle interop; S12; designed |
| R38 / P1 | Undo retains deleted assets and backups outlive Delete all; medium, trust violation | Durable tombstones and documented 10s retention, purge all app store copies/caches | Irreversible purge after window; S09/C12; designed |
| R39 / P1 | Animated visual and hit geometry diverge; medium, wrong target selected | One transform snapshot; deterministic overlap resolution; cancel on topology change | Per-frame geometry bookkeeping; S03/A11; designed |
| R40 / P1 | Canonicalized asset path escapes through reparse point; medium if store tampered, wrong read/delete | Validate root and resolved ancestry/reparse policy; reject untrusted path | Conservative recovery; S08; designed |
| R41 / P1 | Pause/Resume silently reopens camera or restores pinch; medium, privacy/phantom action | Off at launch, Pause closes camera; Resume explicit and disarmed; clutch separate | Camera reopen cost; B12/S05; designed |

Also unresolved: real WPF frame pacing, webcam comfort/identity stability, HDR/color management, secure-desktop/fullscreen transitions, camera backend permission behavior, antivirus/reputation and actual clean-host dependencies. Keep unsupported states rather than promising universal fixes. Proposed BitBlt acquisition must measure color correctness on HDR; report unsupported display configuration or use an approved capture API experiment if required. Do not bake untested HDR support into release claims.

## PART E — PRODUCT REFINEMENT

Refinements preserve independent hand manipulation and full mouse/keyboard utility:

| Change | Value | Cost / dependency / priority |
|---|---|---|
| Timed simulated Ghost replay | Makes passive input proof observable without a second person or keyboard focus conflict | Small, Core + A1 fixture, P0 |
| Always latched Interactive Peek | Same hand can pinch without losing shelf | Small state change, B2, P1; approved existing correction |
| Explicit Snapshot label and safe source action | Prevents expectation of live widget extraction | Small UI/metadata, C, P1 |
| Durable deletion/Undo contract | Predictable privacy and crash recovery | Medium storage work, C, P0 |
| Camera Off vs Pause vs Clutch distinction | Clear privacy and reliable resume | Medium lifecycle/FSM, B1/B2, P0 |
| Search by title/source | Helps 1,000-item library | Small-medium, C storage/list, P2 PROPOSED |
| RAM pocket/quick slots/compare/magnetism | Potential convenience and comfort | Medium, stable C/D first, P2 PROPOSED; no v1 requirement expansion |

Authentication/sync/cloud AI are omitted because they do not support the established local workflow and would add permissions, infrastructure and recurring operational cost.

## PART F — FEATURE-BY-FEATURE SPECIFICATION

The normative contracts are in directive §§4,8–15: each F01–F19 has classification/priority/gate, user entry, preconditions, inputs/outputs, state transitions, persistence/permission boundary, error/empty/loading behavior, recovery and concrete acceptance tests. Shared UX/accessibility rules in §§14,16–19 apply to every feature; data and performance requirements in §§12,21 avoid contradictory per-feature copies. F05 remains experimental. P01/P02 are identified proposals rather than unfinished visible buttons.

Completeness walk: F01 lifecycle; F02 overlay; F03 simulation; F04 ownership; F05 UIA; F06 camera; F07 calibration; F08 gestures; F09 recall; F10 capture; F11 storage; F12 library; F13 viewer/pins; F14 Workspace; F15 shortcuts/no-camera; F16 onboarding/settings/privacy; F17 diagnostics/design/accessibility; F18 distribution; F19 failure recovery. The complete capture→confirm→save→recall→manipulate→restart path is covered; so are all camera-off alternatives and independent-release tests.

## PART G — VISUAL AND INTERACTION SPECIFICATION

Use the existing Quiet Instrument palette/type/geometry in DESIGN_TOKENS.json. Directive §§16–18 translates the historical px names into WPF DIP sizes, supplies small-work-area behavior, component states, screen navigation, and exact interruptible motion tokens. Snapshot content is dominant, camera state is visible, normal launch returns to tray. No broad dashboard is required by the design concept.

Key values: Segoe UI Variable/Segoe UI; 14 DIP body, 15 heading, 20 brand; spacing 4/8/12/16/24/32; shelf 372 DIP default, work-area clamp; Ghost 18 DIP with 48–56 DIP deliberate hit regions; show/hide 260/195ms; reduced motion immediate/<=80ms fade. Ghost movement is not delayed by shelf easing. Focus rings and native accessibility peers apply from the first functional slice. Measure contrast, text scaling and native screenshots rather than approving colors by name.

## PART H — COMPLETE SYSTEM ARCHITECTURE

Directive §§6–13 defines selected stack, alternatives/reversal costs, module dependencies, HWND topology, coordinates, independent state/claim rules, tracker/worker protocol, bounded asynchronous work and persistence entities/transactions. Architecture is local and single-user: no remote frontend/backend split, REST routes, API keys, authentication server or distributed queue. Child isolation addresses camera/native/COM stalls; same-user local IPC is validated but not a defense against arbitrary already-compromised user code.

State ownership: dispatcher coordinator owns interaction/scene state; controls own local edit/focus state; writer owns persisted generation; children own camera/provider resources; UI caches are bounded derived state; server state is not applicable. Retry rules differ: save has operation idempotency, tracker has bounded restart, external non-idempotent actions never retry after uncertain outcome. File transactions preserve last committed manifest and recoverable assets. External observations cannot choose arbitrary process launch/path/deletion targets.

## PART I — FULL MERMAID FLOWCHART COLLECTION

Focused intended-system sources in [diagrams](diagrams/README.md): system architecture, primary journey, navigation, feature dependencies, data flow, state machines, error recovery, camera/automation permissions, startup, implementation flow, feature interactions, deployment and testing/quality gates. Thirteen separate .mmd files keep individual decisions legible. Architecture is distinct from deployment: tracker/UIA are local isolated children, while clean-host verification is a release environment. Permissions has no sign-in branch because the product has no account system.

System diagram maps ownership and process boundaries; data flow explicitly terminates full-screen buffers and never sends camera frames to storage; state diagrams show cancellation routes; journey separates capture focus from passive Peek; execution diagram preserves A1 review; deployment diagram shows tracker/model bundling and rollback. Mermaid syntax validation and any rendering limitation are recorded in PART M after execution.

## PART J — IMPLEMENTATION ROADMAP

Directive §23 and codex/EXECUTION-PHASES.md establish M00/M01–M08 with scope, dependencies, modules, implementation steps, validation commands and stop conditions. No arbitrary delivery timeline. First recover .NET10 toolchain and compile/configuration correctness, then prove native A1. A2 research has a formal supported/deferred outcome before B1; a failed optional UIA capability must not silently become a core release requirement. B1/B2 precede useful C; accessibility begins in every slice, then D1 profiles/polishes; D2 proves distribution. E requires separate scoped instruction.

## PART K — TESTING AND QUALITY ASSURANCE

Use directive §22/S01–S12 plus existing QA A01–D11. Core tests use fake clocks and synthetic numeric landmarks; storage integration injects faults at every commit boundary; process tests inject malformed IPC/hangs/death; native fixture logs styles/cursor/foreground/counters; actual camera/human trials prove gestures and physical-mouse behavior. Builds must show compiled projects, tests must show nonzero discovery. A successful no-op solution invocation is not validation.

Acceptance numbers remain targets: 100/100 click starts; 20 typing/reveal and 20 external drag trials; 30/30 distinct-target concurrent Workspace sessions; >=90% deliberate pinch success in 30 trials per lighting condition, two conditions; zero stuck presses in 100 release/loss tests; <=1 accidental reveal/20min; camera-to-visible median<=120ms/p95<=220ms; 60Hz p95 frame interval<=20ms. Record misses and measurement uncertainty. Failure recovery, offline user install, privacy and no-camera paths are release blockers, not optional quality polish.

Proposed startup/memory/cache budgets in directive §21 are initial investigation thresholds. Existing hardware CPU/GPU/display and real performance data are uncollected; no memory/FPS benchmark is claimed. UI callbacks do not establish photon-level latency.

## PART L — COMPLETE CODEX IMPLEMENTATION DIRECTIVE

The complete standalone document is [GHOST — COMPLETE CODEX IMPLEMENTATION DIRECTIVE](../codex/MASTER-IMPLEMENTATION-PROMPT.md). It includes purpose/users/platform, requirements/non-goals, module/stack decisions, entities/interfaces, state machines, UI/components/motion/navigation, boundaries, faults/performance/accessibility, diagrams, phases, exact build/test/package commands, rollback, continuity, autonomy, definitions of done and final verification. It can be read without this chat. Initial execution is intentionally bounded to A1; later gates are fully described but do not bypass review.

## PART M — FINAL READINESS AUDIT

These A–G are specification-completeness gates from the meta-prompt, distinct from native implementation A1/A2.

| Gate | Rating | Evidence / limitation |
|---|---|---|
| A Product clarity | PASS | Product, users, identity, workflow and proposals explicitly reconstructed from active sources |
| B Technical feasibility | PASS WITH DOCUMENTED LIMITATIONS | Official API evidence and coherent experiments; missing SDK, native pass-through/hand/packaging unproved |
| C Functional specification | PASS WITH DOCUMENTED LIMITATIONS | F01–F19 contracts/AC/tests complete; numerical gesture/resource defaults provisional and U01 assets unresolved |
| D Visual design | PASS WITH DOCUMENTED LIMITATIONS | Existing tokens/screens plus component/motion/accessibility rules; actual Windows visual/contrast/perf QA pending |
| E Architecture diagrams | PASS | Thirteen focused sources and three inline diagrams parsed and rendered; representative architecture/state/journey images visually inspected |
| F Implementation planning | PASS | Ordered gates/modules/commands/stop conditions/rollback and reviewable handoff |
| G Cross-document consistency | PASS WITH DOCUMENTED LIMITATIONS | Stable Fxx/AC/test traceability, conflict resolutions and history retained; old text preserved with explicit correction overlay |

Production readiness: BLOCKED. Native A1, camera, packaging, licenses, clean-host and performance testing remain unpassed. Static source review is not a failed runtime observation or passing runtime evidence.

Separate review passes (performed by one agent; not a claim of independent human reviewers):

| Perspective | Material weakness found | Resulting correction |
|---|---|---|
| Product | broad identity could become generic platform | Concrete reference utility and no-goals retained |
| Architecture | single Owner/virtual desktop/source drift | Per-target claims, typed selected-monitor coordinates, .NET10 correction |
| Engineering | focus-dependent simulator/solution no-op | Timed replay, direct-project build and test-discovery assertion |
| Design | tiny scaled work area and misleading passive controls | DIP rules, scrolling/clamping, conventional mouse access |
| Security | name-based UIA safety and reparse paths | Explicit tested target allowlist and resolved path checks |
| QA | compile used as substitute for OS proof | Native counters/cursor/focus and actual physical input trials |
| Performance | cross-clock latency/frame callbacks overclaimed | Timestamp handshake and honest measurement categories |
| Codex execution | large prompt could skip gates | Concise handoff, one active gate and review boundary |

Validation execution record is appended below after checks. No claim of complete native integration is made by the specification ratings.

### Executed documentation validation — 2026-11-04

- `python scripts/verify_kit.py`: exit 0; all 25 required original kit assets, design JSON, local links/fences and byte-identical research mirror passed.
- `node scripts/validate-spec.mjs`: exit 0; six authored documents, seventeen local links, nineteen feature contracts/acceptance/traceability rows, fourteen report parts, synchronized research, thirteen .mmd files and three inline diagrams parsed.
- First Node parse attempt failed because a DOM was absent (`purify.addHook is not a function`). Added jsdom initialization to the validator and reran successfully; original failure is recorded here.
- `node scripts/render-spec-diagrams.mjs`: exit 0; all sixteen diagrams rendered to SVG and PNG through installed Edge in headless mode. The page loads only generated diagram text and local renderer modules; no desktop, webcam or GHOST app screenshot is acquired. Architecture/state/journey renders were visually inspected; diagrams have their own visible titles.
- Actual artifacts: `artifacts/spec-diagrams/*.svg` and `*.png`. Source diagrams are in docs/diagrams. Validation tools are ignored under .spec-tools; Mermaid11.12.0, jsdom26.1.0 and playwright-core1.56.1 are documentation-only pinned tooling, not app/runtime dependencies.
- Runtime result unchanged: application build NOT RUN/BLOCKED by absent SDK; A1 input/focus, real camera, performance and clean-host package NOT RUN.

Reproduce documentation checks from solution root:

```powershell
npm install --prefix .spec-tools --cache .spec-tools/cache --no-save --no-package-lock --ignore-scripts mermaid@11.12.0 jsdom@26.1.0 playwright-core@1.56.1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Sync-Research.ps1
python .\scripts\verify_kit.py
node .\scripts\validate-spec.mjs
node .\scripts\render-spec-diagrams.mjs
```

Network is needed for first developer-tool download only; headless rendering uses the installed Edge browser and loopback module server. This is specification tooling and does not change the offline runtime contract of GHOST.

## PART N — EXACT EXECUTION INSTRUCTIONS

1. Open the repository root as the workspace. Do not re-extract the ZIP over source.
2. Read codex/HANDOFF.md and the standalone directive. In a new implementation chat instruct: “Read codex/MASTER-IMPLEMENTATION-PROMPT.md and codex/HANDOFF.md. Implement M00 and M01/Gate A1 only. Resolve the documented scaffold defects, run builds and deterministic tests, and record actual native evidence. Stop at A1 review. Do not start webcam or UIA.”
3. Install the official supported .NET10 x64 SDK via approved user/tooling flow, then rerun `dotnet --info` and preflight. Current source must be repaired before build success is expected; merely installing SDK does not finish A1.
4. Use directive §24 commands from this directory. For camera/screen testing use deliberate user activation with approved dummy content; otherwise record NOT RUN and provide protocol.
5. At review compare source and evidence against QA A01–A05/A10, selected-monitor DPI and independent-state tests. Preserve failures and append research; never mark gate passed from compilation alone.
6. After reviewed A1 use the next gate prompt in EXECUTION-PHASES. Keep exact dependency/license and clean-host tasks open until measured.

No SDK installation, webcam access, screenshot acquisition, publishing, or application-code mutation is performed by this specification stage. Original ZIP, historical docs and code remain preserved.
