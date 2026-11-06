"""Opt-in metadata for a single bounded camera session."""

from contextlib import contextmanager
import json
import math
import os
import sys
from statistics import median
import time
from types import SimpleNamespace


class CameraDiagnostics:
    def __init__(self, path, session):
        self.path = path
        self.started = time.perf_counter()
        self.cpu = time.process_time()
        self.inference, self.age = [], []
        self.timings = {}
        self.data = {"session": session, "pid": os.getpid(),
                     "startedUnix": time.time(), "stages": {}, "submitted": 0, "processed": 0,
                     "results": {"hand": 0, "partial": 0, "ambiguous": 0, "empty": 0}}
        self.write_failed = False

    def elapsed(self):
        return round((time.perf_counter() - self.started) * 1000, 3)

    def save(self):
        temporary = self.path.with_suffix(".pending")
        try:
            temporary.write_text(json.dumps(self.data, indent=2) + "\n", encoding="utf-8")
            temporary.replace(self.path)
        except OSError:
            if not self.write_failed:
                print("GHOST_TRACKING_DIAGNOSTICS_WRITE_FAILED", file=sys.stderr, flush=True)
                self.write_failed = True

    @contextmanager
    def stage(self, name):
        row = {"startMs": self.elapsed()}
        self.data["stages"][name] = row
        self.save()
        try:
            yield
            row["outcome"] = "ok"
        except Exception as error:
            row["outcome"] = getattr(error, "code", "runtime-failure")
            native = getattr(error, "native_code", None)
            if native is not None:
                row["hresult"] = f"0x{native:08X}"
            raise
        finally:
            row["endMs"] = self.elapsed()
            self.save()

    def result(self, fields):
        kind = ("hand" if fields["tracked"] else "ambiguous" if fields.get("ambiguous")
                else "partial" if fields.get("partial") else "empty")
        self.data["results"][kind] += 1
        self.data.setdefault("firstResultMs", self.elapsed())

    def measure_tracker(self, tracker):
        def detect(image, timestamp):
            self.data["submitted"] += 1
            result = tracker.detect_for_video(image, timestamp)
            self.data["processed"] += 1
            return result
        return SimpleNamespace(detect_for_video=detect)

    def sample(self, _sequence, _conversion, inference, age):
        if len(self.inference) < 1500:
            self.inference.append(inference)
            self.age.append(age)

    def timing(self, name, nanoseconds):
        values = self.timings.setdefault(name, [])
        if len(values) < 1500:
            values.append(nanoseconds)

    def count(self, name):
        self.data[name] = self.data.get(name, 0) + 1

    def finish(self):
        self.timings.update(inferenceMs=self.inference, sourceToResultMs=self.age)
        for name, values in self.timings.items():
            if values:
                ordered = sorted(values)
                self.data[name] = {"median": median(ordered) / 1e6,
                                   "p95": ordered[math.ceil(.95 * len(ordered)) - 1] / 1e6}
        self.data.update(elapsedMs=self.elapsed(), cpuSeconds=time.process_time() - self.cpu, maxApplicationQueue=0)
        self.save()
