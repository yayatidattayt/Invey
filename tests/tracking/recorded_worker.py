"""Private GHOST qualification worker. Source is an attributed still transformed in memory."""

import argparse
from contextlib import ExitStack, nullcontext
import json
import math
import os
from pathlib import Path
import sys
import time
import uuid

def phase(name):
    print("GHOST_TRACKING_PHASE=" + name, file=sys.stderr, flush=True)


class ParentPipe:
    """The parent signals stop by closing its stdin writer."""

    def __init__(self):
        import ctypes
        from ctypes import wintypes
        import msvcrt

        self.handle = msvcrt.get_osfhandle(sys.stdin.fileno())
        self.peek = ctypes.WinDLL("kernel32", use_last_error=True).PeekNamedPipe
        self.peek.argtypes = [wintypes.HANDLE, ctypes.c_void_p, wintypes.DWORD,
                              ctypes.c_void_p, ctypes.c_void_p, ctypes.c_void_p]
        self.peek.restype = wintypes.BOOL

    def is_set(self):
        return not self.peek(self.handle, None, 0, None, None, None)

    def wait(self, duration):
        until = time.monotonic() + duration
        while not self.is_set():
            remaining = until - time.monotonic()
            if remaining <= 0:
                return
            # Poll closure between frames. The host owns the deadline for blocked inference.
            time.sleep(min(remaining, 0.025))


class InvalidLandmarkOutput(ValueError):
    pass


def normalize(result):
    if not result.hand_landmarks:
        return [], {}
    if len(result.hand_landmarks) != 1:
        return [], {"ambiguous": True}
    points = [[point.x, point.y, point.z] for point in result.hand_landmarks[0]]
    if len(points) != 21 or any(not all(map(math.isfinite, point)) for point in points):
        raise InvalidLandmarkOutput("Invalid hand model geometry.")
    partial = any(not (0 <= x <= 1 and 0 <= y <= 1) for x, y, _ in points)
    fields = {}
    if len(result.handedness) == 1 and len(result.handedness[0]) == 1:
        category = result.handedness[0][0]
        side = category.category_name.lower()
        if side in {"left", "right"} and 0 <= category.score <= 1:
            fields = {"handedness": side, "quality": {"handednessScore": category.score}}
    if partial:
        # In-frame model estimates are useful for preview. Null does not invent an unseen joint.
        return [], {**fields, "v": 2, "partial": True,
                    "points": [p if 0 <= p[0] <= 1 and 0 <= p[1] <= 1 else None for p in points]}
    return points, fields


def finger_preview_fixture(send, stop, preview=None):
    send("ready")
    base = time.monotonic_ns() // 1_000_000
    for sequence in range(90):
        if stop.is_set():
            return
        points = [[.50, .8, 0.0]]
        for finger in range(5):
            for joint in range(4):
                points.append([.3 + sequence * .002 + finger * .035, .65 - joint * .04, 0.0])
        fields = {"tracked": True, "landmarks": points, "handedness": "right"}
        if 15 <= sequence < 55:
            sparse = [None, *points[1:]]
            if sequence >= 40:
                sparse[8] = None
            fields = {"v": 2, "tracked": False, "landmarks": [], "points": sparse,
                      "partial": True, "handedness": "right"}
        elif 55 <= sequence < 65:
            fields = {"tracked": False, "landmarks": [], "ambiguous": True}
        stamp = base + sequence * 80
        send("observation", seq=sequence, captureMonoMs=stamp, resultMonoMs=stamp + 1,
             frame={"width": 640, "height": 480}, **fields)
        if preview is not None:
            import numpy as np
            image = np.zeros((360, 640, 3), dtype=np.uint8)
            image[:] = (30, 80, 120)
            preview.offer(sequence, image)
        stop.wait(.08)
    send("stopped")


def open_landmarker(mp, asset):
    options = mp.tasks.vision.HandLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(model_asset_buffer=asset, delegate=mp.tasks.BaseOptions.Delegate.CPU),
        running_mode=mp.tasks.vision.RunningMode.VIDEO, num_hands=2,
    )
    return mp.tasks.vision.HandLandmarker.create_from_options(options)


def practice_fixture(send, stop):
    """Bounded, explicitly simulated local calibration-practice sequence."""
    send("ready")
    for sequence in range(240):
        if stop.is_set():
            break
        # Longer deliberate phases tolerate UI scheduling/coalescing without weakening
        # the actual practice reducer's consecutive-observation safety requirements.
        cycle = sequence % 80
        closed = 20 <= cycle < 40 or 55 <= cycle < 72
        ambiguous = 72 <= cycle < 78
        points = [[.5, .5, 0] for _ in range(21)]
        points[4][0] = .51 if closed else .62
        points[5][0], points[17][0] = .4, .6
        stamp = time.perf_counter_ns() // 1_000_000
        send("observation", seq=sequence, captureMonoMs=stamp, resultMonoMs=stamp,
             tracked=not ambiguous, landmarks=[] if ambiguous else points,
             frame={"width": 640, "height": 480},
             **({"ambiguous": True} if ambiguous else {"handedness": "right"}))
        stop.wait(.08)
    send("stopped")


def stream(args, pipe, stop, diagnostic=None):
    def send(kind, **fields):
        line = json.dumps({"v": 1, "kind": kind, "session": args.session, **fields}, allow_nan=False)
        payload = (line + "\n").encode("utf-8")
        if pipe.write(payload) != len(payload):
            raise BrokenPipeError("Short tracker write.")
        if diagnostic and kind == "observation":
            diagnostic.result(fields)

    if args.fault == "startup-hang":
        stop.wait(30)
        return
    if args.fault in ("finger-preview", "finger-visual"):
        if args.camera or args.fixture_file:
            raise ValueError("Pointer fixture requires the recorded source.")
        sys.path.insert(0, str(args.site_packages))
        sys.path.insert(0, str(Path(__file__).resolve().parent))
        from preview_pipeline import PreviewSender
        with PreviewSender(args.preview_pipe) if args.preview_pipe else nullcontext() as preview:
            if args.fault == "finger-visual":
                from precision_fixture import visual_fixture
                visual_fixture(send, stop, preview)
            else:
                finger_preview_fixture(send, stop, preview)
        return
    if args.fault == "calibration-practice":
        if args.camera or args.fixture_file:
            raise ValueError("Practice fixture requires the recorded test source.")
        practice_fixture(send, stop)
        return

    sys.path.insert(0, str(args.site_packages))
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    if args.performance == "capture":
        from windows_camera import open_source, CameraError
        from performance_check import run_capture_check
        import numpy as np
        if not args.camera or diagnostic is None:
            raise ValueError("Capture check requires the selected guarded camera and diagnostics")
        # The host guardian has already reserved the session before sending start.
        try:
            with open_source(np, device=args.camera, diagnostic=diagnostic, native_only=True) as source:
                run_capture_check(source, stop, send, diagnostic)
        except CameraError as error:
            diagnostic.data["error"] = error.code
            if error.native_code is not None:
                diagnostic.data["hresult"] = f"0x{error.native_code:08X}"
            send("error", code=error.code)
        except (RuntimeError, ValueError, OSError) as error:
            if isinstance(error, BrokenPipeError):
                raise
            send("error", code="runtime-failure")
        return
    try:
        phase("importing")
        from qualify_hand_landmarker import MODEL_HASH, PHOTO_HASH, checked_bytes, mp, np, Image, ImageOps
        from frame_pipeline import RecordedFrameSource, run_frames
        from windows_camera import CameraError, open_source
        if sys.version_info[:3] != (3, 12, 10) or mp.__version__ != "0.10.32":
            raise RuntimeError("Recorded qualification runtime differs from the pinned candidate.")
    except (ImportError, RuntimeError, OSError):
        send("error", code="runtime-failure")
        return

    try:
        with ExitStack() as resources:
            from preview_pipeline import PreviewSender
            preview = resources.enter_context(PreviewSender(args.preview_pipe)) if args.preview_pipe else None
            try:
                phase("opening-model")
                asset = checked_bytes(args.model, MODEL_HASH)
                if args.fault == "bad-model":
                    asset = b"invalid model"
                tracker = resources.enter_context(open_landmarker(mp, asset))
                if not args.camera and not args.fixture_file:
                    checked_bytes(args.palm, PHOTO_HASH)
                    source = RecordedFrameSource(args.palm, np, Image, ImageOps)
            except (ValueError, RuntimeError, OSError):
                send("error", code="invalid-model")
                return
            if stop.is_set():
                return
            if args.camera or args.fixture_file:
                phase("opening-camera")
                if args.fault in ("camera-denied", "camera-busy", "camera-missing"):
                    raise CameraError({"camera-denied": "permission-denied", "camera-busy": "device-busy",
                                       "camera-missing": "no-device"}[args.fault])
                if args.camera:
                    from latest_capture import LatestCapture
                    source = resources.enter_context(LatestCapture(
                        lambda: open_source(np, args.camera, None, diagnostic), stop, diagnostic))
                else:
                    source = resources.enter_context(open_source(np, None, args.fixture_file, diagnostic))
            send("ready")
            phase("ready")
            if diagnostic:
                diagnostic.begin_mode("full")
            if args.fault == "crash":
                os._exit(7)
            if args.fault == "malformed":
                pipe.write(b"{broken}\n")
                return
            if args.fault == "ignore-stop":
                time.sleep(30)
                return

            def delay_result(sequence, *_):
                if sequence == 12:
                    stop.wait(0.9)

            measured = diagnostic.measure_tracker(tracker) if diagnostic else tracker
            from finger_visual import FingerVisual
            visual = None if os.environ.get("INVEY_LEGACY_POINTER") == "1" else FingerVisual()
            run_frames(source, stop, measured, mp, np, normalize, send,
                       diagnostic.sample if diagnostic else delay_result if args.fault == "result-gap" else None,
                       diagnostic, preview, visual)
    except InvalidLandmarkOutput:
        if diagnostic:
            diagnostic.count("invalidModelOutputs")
        send("error", code="invalid-model")
        return
    except CameraError as error:
        if diagnostic:
            diagnostic.data["error"] = error.code
            if error.native_code is not None:
                diagnostic.data["hresult"] = f"0x{error.native_code:08X}"
        if error.native_code is not None:
            print(f"GHOST_TRACKING_NATIVE_CODE={error.native_code:08X}", file=sys.stderr, flush=True)
        send("error", code=error.code)
        return
    except (RuntimeError, ValueError, OSError) as error:
        if isinstance(error, BrokenPipeError):
            raise
        if getattr(error, "winerror", None) is not None:
            print(f"GHOST_TRACKING_NATIVE_CODE={error.winerror & 0xFFFFFFFF:08X}", file=sys.stderr, flush=True)
        send("error", code="runtime-failure")
    send("stopped")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--list-cameras", action="store_true")
    parser.add_argument("--pipe")
    parser.add_argument("--session", type=lambda value: str(uuid.UUID(value)))
    parser.add_argument("--site-packages", type=Path)
    parser.add_argument("--model", type=Path)
    parser.add_argument("--palm", type=Path)
    parser.add_argument("--diagnostics", type=Path)
    parser.add_argument("--preview-pipe")
    parser.add_argument("--performance", choices=["capture", "full"])
    source = parser.add_mutually_exclusive_group()
    source.add_argument("--camera")
    source.add_argument("--fixture-file", type=Path)
    parser.add_argument("--fault", choices=["startup-hang", "crash", "malformed", "ignore-stop",
                                          "bad-model", "result-gap", "camera-denied", "camera-busy",
                                          "camera-missing", "calibration-practice", "finger-preview", "finger-visual"])
    args = parser.parse_args()
    if not args.list_cameras and not all((args.pipe, args.session, args.site_packages, args.model, args.palm)):
        parser.error("The tracking worker requires a pipe, session, runtime, and asset paths.")

    # No pipe connection or inference before the host assigns this process to its job.
    phase("waiting-for-start")
    if sys.stdin.readline().strip() != "start":
        return
    if args.list_cameras:
        sys.path.insert(0, str(Path(__file__).resolve().parent))
        from windows_camera import CameraError, list_cameras
        try:
            print(json.dumps({"devices": list_cameras()}), flush=True)
        except (CameraError, OSError) as error:
            print(json.dumps({"error": error.code if isinstance(error, CameraError) else "runtime-failure"}), flush=True)
        return
    phase("connecting")
    stop = ParentPipe()
    diagnostic = None
    try:
        descriptor = os.open("\\\\.\\pipe\\" + args.pipe, os.O_WRONLY | os.O_BINARY)
        with os.fdopen(descriptor, "wb", buffering=0) as pipe:
            phase("connected")
            with ExitStack() as apartment:
                if args.camera or args.fixture_file:
                    # Initialize MTA before the inference imports can initialize this thread as STA.
                    sys.path.insert(0, str(Path(__file__).resolve().parent))
                    from windows_camera import multithreaded_com
                    if args.camera and args.diagnostics:
                        from camera_diagnostics import CameraDiagnostics
                        diagnostic = CameraDiagnostics(args.diagnostics, args.session)
                        diagnostic.save()
                    with diagnostic.stage("CoInitializeEx") if diagnostic else nullcontext():
                        apartment.enter_context(multithreaded_com())
                stream(args, pipe, stop, diagnostic)
    except BrokenPipeError:
        pass
    finally:
        if diagnostic:
            diagnostic.finish()


if __name__ == "__main__":
    main()
