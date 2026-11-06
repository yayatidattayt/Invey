"""Measure the production frame stages on the bounded, camera-free fixture."""

import argparse
from contextlib import ExitStack
import ctypes
import json
import math
from pathlib import Path
from statistics import median
import threading
import time

from frame_pipeline import RecordedFrameSource, run_frames
from recorded_worker import normalize, open_landmarker
from windows_camera import multithreaded_com, open_source


class MeasuredTracker:
    def __init__(self, tracker):
        self.tracker = tracker
        self.submitted = self.processed = 0

    def detect_for_video(self, image, stamp):
        self.submitted += 1
        result = self.tracker.detect_for_video(image, stamp)
        self.processed += 1
        return result


def latency(samples):
    ordered = sorted(samples)
    return {"median": median(ordered) / 1e6, "p95": ordered[math.ceil(.95 * len(ordered)) - 1] / 1e6}


def working_set():
    class Counters(ctypes.Structure):
        _fields_ = [("size", ctypes.c_uint32), ("faults", ctypes.c_uint32),
                    ("peak", ctypes.c_size_t), ("working", ctypes.c_size_t), ("others", ctypes.c_size_t * 6)]

    current = ctypes.WinDLL("kernel32").GetCurrentProcess
    current.restype = ctypes.c_void_p
    info = ctypes.WinDLL("psapi").GetProcessMemoryInfo
    info.argtypes = [ctypes.c_void_p, ctypes.POINTER(Counters), ctypes.c_uint32]
    counters = Counters()
    counters.size = ctypes.sizeof(counters)
    return counters.working if info(current(), ctypes.byref(counters), counters.size) else None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model", type=Path, required=True)
    parser.add_argument("--palm", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--native-file", type=Path)
    parser.add_argument("--frames", type=int, choices=range(2, 1001), metavar="2–1000", default=120)
    args = parser.parse_args()
    with ExitStack() as apartment:
        if args.native_file:
            apartment.enter_context(multithreaded_com())
        qualify(args)


def qualify(args):
    import cv2
    from qualify_hand_landmarker import Image, ImageOps, MODEL_HASH, PHOTO_HASH, checked_bytes, mp, np

    asset = checked_bytes(args.model, MODEL_HASH)
    checked_bytes(args.palm, PHOTO_HASH)
    stop = threading.Event()
    times = []
    memory = []
    emitted = tracked = last_stamp = 0

    def send(kind, **fields):
        nonlocal emitted, tracked, last_stamp
        if kind != "observation" or fields["seq"] != emitted or fields["captureMonoMs"] <= last_stamp:
            raise RuntimeError("Invalid observation ordering.")
        emitted += 1
        tracked += int(fields["tracked"])
        last_stamp = fields["captureMonoMs"]

    def sample(_, conversion, inference, age):
        times.append((conversion, inference, age))
        if len(times) % 120 == 0:
            memory.append(working_set())
        if len(times) == args.frames:
            stop.set()

    with ExitStack() as resources:
        model_start = time.perf_counter()
        tracker = resources.enter_context(open_landmarker(mp, asset))
        model_ms = (time.perf_counter() - model_start) * 1000
        start = time.perf_counter()
        source = (resources.enter_context(open_source(np, fixture_file=args.native_file.resolve()))
                  if args.native_file else RecordedFrameSource(args.palm, np, Image, ImageOps))
        source_ms = (time.perf_counter() - start) * 1000
        measured = MeasuredTracker(tracker)
        memory.append(working_set())
        begin = time.perf_counter()
        cpu_begin = time.process_time()
        run_frames(source, stop, measured, mp, np, normalize, send, sample)
        seconds = time.perf_counter() - begin
        cpu_seconds = time.process_time() - cpu_begin
        memory.append(working_set())
    acquired = source.samples_read if args.native_file else source.sequence
    skipped = source.skipped if args.native_file else 0
    if not (acquired - skipped == measured.submitted == measured.processed == emitted == len(times) == args.frames
            and 0 < tracked < emitted):
        raise RuntimeError(f"Unexpected frame counts. Acquired={acquired}, skipped={skipped}, "
                           f"submitted={measured.submitted}, processed={measured.processed}, "
                           f"emitted={emitted}, tracked={tracked}.")
    conversion, inference, age = zip(*times)
    report = {
        "source": "native Media Foundation file reader" if args.native_file else "controlled still/blank sequence",
        "cameraFrames": 0, "seconds": seconds,
        "sourceInitMs": source_ms, "modelInitMs": model_ms,
        "acquired": acquired, "submitted": measured.submitted, "processed": measured.processed,
        "emittedToLocalSink": emitted, "tracked": tracked,
        "acquiredPerSecond": acquired / seconds, "droppedByApplication": acquired - emitted,
        "cpuSeconds": cpu_seconds, "workingSetSamplesBytes": memory,
        "pendingFrameCapacity": 0, "conversionMs": latency(conversion), "inferenceMs": latency(inference),
        "sourceToResultMs": latency(age),
        "backendProbe": {
            "opencv": cv2.__version__,
            "msmfRegistered": cv2.CAP_MSMF in cv2.videoio_registry.getCameraBackends(),
            "msmfBackendAvailable": cv2.videoio_registry.hasBackend(cv2.CAP_MSMF),
        },
    }
    args.out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
