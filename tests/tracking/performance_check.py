"""Metadata-only native A/B comparison inside ONE guarded source lifetime. No inference/actions."""
import time
from windows_camera import CameraError


def run_capture_check(source, stop, send, diagnostic, seconds=30, clock=time.perf_counter):
    if diagnostic is None:
        raise ValueError("Performance check requires bounded diagnostics")
    send("ready")
    for mode in ("raw", "conversion"):
        if stop.is_set():
            break
        if mode == "conversion":
            source.enable_conversion()
        diagnostic.begin_mode(mode)
        deadline, health = clock() + seconds, clock()
        delivered, ended = 0, False
        while not stop.is_set() and clock() < deadline:
            frame = source.read(stop)
            if frame is None:
                ended = True
                break
            delivered += 1
            diagnostic.count("performanceDelivered")
            if clock() >= health:
                send("health")
                diagnostic.publish()
                health = clock() + .5
        diagnostic.end_mode("stopped" if stop.is_set() else "source-ended" if ended else "interval-complete")
        if not delivered and not stop.is_set():
            raise CameraError("no-frames")
    send("stopped")
