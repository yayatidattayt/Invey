# Execution25 — Operation Real Ghost

**Camera-free preparation; physical gate BLOCKED pending a new explicit cap-policy decision. Historical physical motion quality remains FAILED.** Final delivery receipt carries the exact published SHA/CI linkage. No physical30FPS or jitter improvement is claimed.

## 1–3. Baseline, configuration and physical evidence

Continued `codex/execution-20-interaction-engine` / draft PR9 from `421376cb4efc3f4ed402e8cd39699fc003e91706`, initially clean. Remote PR head matched; CI42689977929 succeeded. Initial sandbox fetch failed (`getaddrinfo thread`); approved fetch succeeded. PR remains open/draft/unmerged; no parent merge, reset or force push. Forbidden PR metadata updates were not retried.

Read repository rules, current handoff, Execution23/24 reports, real-test/precision/research/presentation/QA guides and the actual capture, inference, controller, mapper, filter, preview, desktop and gesture paths. Original Release core baseline171/171 passed before modification. Initial restricted restore exited with no diagnostic output; approved locked restore passed. Early build attempts overlapped new files and exposed transient compile errors; they are not represented as a clean before-change Debug/Release baseline. Logs remain in `artifacts/execution-25`.

Historical ASUS FHD mode:1920×1080 advertised30Hz, about9.786 processed results/s. ReadSample median94.65ms/p95111.09ms; inference18.60/24.90ms; source-to-result48.12/62.20ms. Creator reported choppiness, jitter, disappearing pointer and failed interpolation. Second session had1243 host observations:893 full,129 partial,221 loss,0 ambiguous. Moving-hand feedback is not stationary jitter evidence. The old timing intervals overlap and cannot be summed.

## 4–6. Three guarded measurement modes

Existing Tracking UI now has **Ghost Camera Performance Check**, with normal tracking, raw→conversion and full pointer choices. The new camera-off launcher is `scripts/Start-Invey-PerformanceCheck.ps1`; instructions are in `GHOST_CAMERA_PERFORMANCE_CHECK.md`. Performance modes require the existing diagnostic configuration/ledger and use the same host job, independent guardian, selected-camera lookup, deliberate Enable and Stop. There is no new acquisition backend or automatic amendment.

Raw A selects an actually offered native type through the existing Source Reader, without RGB output negotiation, sample buffer copying, MediaPipe, image preview or pointer output. It counts unique native timestamps separately from repeated/nonmonotonic/stale samples and measures PTS intervals and read completion/blocking. It is native Source Reader delivery, not direct sensor instrumentation. Type-change uncertainty fails closed rather than silently switching the benchmark format.

Conversion B follows after about30 seconds, in the SAME camera/source/reader lifetime. It requires the identical offered native format, negotiates the existing RGB32 output with the qualified legacy fallback, and measures another approximately30 seconds without MediaPipe. Native read/copy/delivery, produced RGB bytes and converted-frame counters are bounded. Copy timing includes owned-buffer allocation; separate allocator-only overhead is not measured. Phase transition resets timestamp alignment, not device identity. Stop prevents entering the second phase. ReadSample can block;170-second cancellation and178-second independent cutoff remain under the180-second session ceiling.

Full C uses the unchanged model, latest-frame capture, single calibrated/personalized preview filter, shared logical pointer and local image preview. Native/converted/inference counters have distinct phase-average rates. No live action issuer exists; camera/recorded preview cannot Arm. No images/video/raw landmark streams are persisted. Metadata summaries overwrite a bounded file at most once per second during sampling, then final lifecycle/host/pointer summaries are written.

## 7–9. PTS, negotiation and bottleneck classification

Per-mode records retain selected native and output format/negotiation. Same-source native-file tests verify raw pixels are absent, conversion produces owned arrays, format identity remains equal and Stop releases the reader. File tests are not device FPS evidence. Actual ASUS negotiated types/PTS/cadence have not been measured in this execution.

Timeline boundaries: native PTS→ReadSample completion (clock-aligned minimum observed offset), completion→owned RGB (nativeCopyMs), capture→submission age (includes alignment/copy/handoff), image construction, model inference, result/IPC write, host receipt→dispatcher delivery, mapping→logical overlay update. Each duration uses compatible clocks within its boundary. No cross-process clock-origin subtraction, overlapping-duration sum, compositor latency or optical-to-display claim is made. UI host-age samples are bounded to1500; mapper reason vocabulary and worker timing samples are bounded.

**True physical bottleneck remains UNRESOLVED.** Historical94ms read blocking makes source/driver/MF delivery a stronger hypothesis than inference alone, but no raw A/B experiment has run. Raw≈10 implicates delivery before app copying; raw≈30/conversion≈10 implicates output processing/copy; conversion≈30/model≈10 implicates inference/scheduling; model≈30/pointer≈10 implicates host/rendering. Multiple causes can coexist. Exposure, lighting and driver limitations remain hypotheses; no global properties changed.

## 10–11. UI cadence and observation delivery repair

Confirmed the desktop callback was inside `RecordedTrackingWindow.Render`. Its base timer is80ms, but existing code changes it to16ms with image preview and25ms with calibration. Thus12.5Hz was a real conditional dependency, not proof of the historical10Hz camera bottleneck.

Accepted worker messages now signal a dispatcher-safe bounded notification. At most one wakeup is queued; it reads the CURRENT session snapshot rather than retaining old worker observations. Stop invalidates state before its wakeup; queued callbacks cannot resurrect stopped or replaced sessions. Close unsubscribes and disposes the wakeup. Labels/calibration/local-image polling retain their existing rates; fresh desktop positions no longer wait for label repaint or focus. Health/expiry polling still clears invalid data. Existing continuity counters preserve recognized interruptions across coalescing.

Core fixtures deliver30/60Hz sequences independently of80ms label polling;1001 coalesced notifications consume only latest Stop state and disposed callbacks do nothing. Actual isolated WPF fixture disables label polling: nominal30Hz yielded27 accepted pointer updates in1004.3ms (three acquisition observations), nominal60Hz yielded60 in981.5ms. These are logical software updates, not webcam/compositor FPS. Data: run `20261110T051544808-68f6f30bccb34ca69a30ccee5b7fef24/observation-delivery.json`.

## 12–15. Disappearance, filtering, recovery and overlay

Mapper distinguishes no hand, stale observation, ambiguity, selected-hand mismatch, unavailable calibration/frame context, working-region exit, warmup and inconsistent large movement. UI reason entries and cumulative durations are separate from repeated rejection polls and worker break counters. Final pointer metadata labels its scope UI-observed, not sensor events. Presentation failure retains its separate desktop status.

Deterministic recovery verifies one omitted10Hz frame (200ms capture gap) retains the preview claim; recognized no-hand loss resets it; reacquisition requires four valid observations; wrong hand and300ms gap cannot inherit the claim. Four-observation acquisition spans three intervals (~300ms at10Hz/~100ms at30Hz). Genuine missing/ambiguous/stale evidence still hides the pointer; no fabricated point or indefinite stale hold. Partial/visual evidence remains preview-only, with strict150ms gesture freshness and600ms visual-anchor bound unchanged. Historical221 no-hand results plus acquisition delay are a plausible contributor to blackouts; per-reason physical attribution is not established.

No filter tuning or preset was adopted. Existing paired raw/original/precision fixtures cover stationary noise, tiny/slow/rapid movement, step, reversal, spike, irregular spacing and dropout. At1920px synthetic stationary RMS: original1.436px/precision1.054px; reversal delay80/40ms; step90%80/40ms, but whole-step RMS27.62/41.52px. The step tradeoff is preserved rather than hidden. Current precision parameters are the inherited baseline, not a new physically validated candidate. No interpolation/prediction was restored.

Shared passive overlay, source generations, native cursor independence, action ownership and presentation-failure cleanup remain. Pointer-update counters now increment only when the shared engine accepts a tracking position. Local unique preview presentation remains a separate counter; nominal refresh is never reported as camera FPS.

## 16–21. Hardware, authorization, real rates and independence

No physical device opened. The existing12-start cap is exhausted: version6,12 released rows,total1054077ms,zero pending. SHA256 `E578B2FE328B260AE02277E318BAFA66DE8525A7B1B3CAC34459B0CC15A59486`. No amendment or historical charge alteration. Remaining arithmetic time does not authorize a start.

Proposed decision AFTER camera-free verification: at most two new supervised user-enabled sessions≤180s, raising total starts12→14, adding at most360000ms charged time. All history stays; resulting maximum charged total1414077ms remains under the unchanged1800000ms time cap. First session A/B; second full-pointer session requires safe first release and a separate go-ahead. No images/landmarks retained, no Pocket actions, cooperative Stop170s/hard cutoff178s, creator present and responsible for Enable. This proposal is not permission or an applied policy.

Physical source/inference/pointer FPS, actual stationary jitter, fine/fast movement and mouse independence remain NOT TESTED in Execution25. Historical quality remains FAILED. Deterministic/native fixture independence is separately recorded below; it does not qualify physical tracking.

## 22–27. Verification, failures and cleanup

Pinned locked restore PASS; final Debug/Release builds0 warnings/errors;176/176 core tests each,0 skips;36/36 workers including native MP4, raw/conversion/no-inference/Stop, capture policy, owned RGB/signed stride, latest-frame and recorded-source tests,0 skips. Evidence `final-*-build.log`, `final-*.trx`, `final-worker.log`, `filter-paired-final.json` under `artifacts/execution-25`.

Preserved failures: restricted empty restore; transient new-file/nullability/import compile errors; first native run `20261110T051357141-6781d1d7dd544fd69612f6ac22371121` timed out because the new4-second throughput fixture extended an existing bounded practice command. Split it into its own shorter2-second command; did not relax the global timeout or skip checks. Repaired focused run25002 PASS, actual Pocket/card/pin/restart, Workspace independent ownership, guided gesture proposals/actions denied, WPF preview/dedup/Stop and new polling-independent throughput. Native cursor stationary evidence may be INCONCLUSIVE if actual external movement is observed; do not promote it to PASS. Presentation CheckOnly initially correctly refused the uncommitted tree; final clean committed preflight is checked at delivery.

Full native run25003 and final committed-source verification are recorded in the addendum/receipt. Foreground guards are unchanged. No GameInputService changes or ordinary-desktop automated input. All failed artifacts and previous evidence remain. Independent Default-desktop/process audit and final ledger hash are checked before delivery.

## 28–30. Publication, remaining problems and next action

Deliver to the SAME PR9 branch without rewriting history, parent merges or metadata workarounds. Exact published SHA, Actions job/result and launchable binary hash are in `artifacts/execution-25/final-delivery.json` and the final response. Hosted CI is software verification, not interactive hardware acceptance.

Remaining: device/source30FPS, real motion quality, per-reason physical blackout attribution, wrist-out/finger-only reliability, stationary measurements, mixed-DPI/human acceptance, qualified physical gestures and live Pocket actions. Camera-free preparation does not close those gates. Next required action is an explicit bounded-cap policy decision, then supervised creator Enable only after audited code/ledger amendment and safe release verification.

References: [Microsoft Source Reader media negotiation](https://learn.microsoft.com/en-us/windows/win32/api/mfreadwrite/nf-mfreadwrite-imfsourcereader-setcurrentmediatype), [native/output processing](https://learn.microsoft.com/en-us/windows/win32/medfound/processing-media-data-with-the-source-reader), [WPF dispatcher ownership](https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher.begininvoke?view=windowsdesktop-10.0).

## Final local verification addendum

Full extended native seed25003 run20261110T051726407-20385cd2d13246919daf0e185fe1fb98:40 PASS,0 FAIL,1 BLOCKED (Explorer/Alt+Tab absent on private desktop). No GameInput interference recurred on that fresh desktop. Its30/60Hz WPF fixture delivered27 updates/1006.5ms and60/988.5ms with polling disabled. No camera opened. The later accounting-only changes are additionally checked in final focused run25004.

Verified-Debug175/176 failed in the inherited budget fixture TargetsAmendmentPreservesEightRowsAndAllowsOnlyTwoFurtherStartsAcrossRestart: File.Replace could not remove the destination. Failure/TRX retained, no budget implementation change. One unchanged fresh-temp rerun passed176/176; verified Release176/176, builds0 warnings/errors. The intermittent filesystem failure remains an observed limitation, not a reproduced defect in the new delivery path. An ancillary documentation update failed default Windows text decoding before writing; repeated with explicit UTF-8. Research synchronization used an identical byte copy after local script signing policy refused direct invocation; no global policy changed.

Next hardware policy is a proposal only; no starts are available until explicitly approved and audited. Source implementation is camera-free delivered even if permission is declined or unanswered. Native/physical acceptance are separate.

Final focused seed25004 run20261110T052416509-76945aaa5e7f40ec99ba3aa3c9e55d93 PASS, including native cursor control and complete controlled product route after final accounting fixes. Polling-off WPF:27 updates/1005.9ms at nominal30Hz;59/981.4ms at nominal60Hz. These are accepted logical positions, not webcam frames. No further application changes followed.
