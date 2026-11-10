import os
from pathlib import Path
import tempfile
import threading
import unittest
import shutil
import subprocess
import numpy as np
from camera_diagnostics import CameraDiagnostics
from windows_camera import open_source, CameraError
from performance_check import run_capture_check


class PerformanceCheckTests(unittest.TestCase):
    @unittest.skipUnless(os.environ.get("GHOST_NATIVE_VIDEO_FIXTURE"), "Requires native file fixture")
    def test_native_raw_then_conversion_same_source_releases_and_saves_metadata_only(self):
        with tempfile.TemporaryDirectory() as folder:
            diagnostic = CameraDiagnostics(Path(folder)/"check.json", "file-check")
            # Each sample is independently decodable: raw sampling intentionally consumes
            # the initial compressed packet, so a subsequent RGB decoder needs a new keyframe.
            try:
                import imageio_ffmpeg
                encoder = imageio_ffmpeg.get_ffmpeg_exe()
            except ImportError:
                encoder = shutil.which("ffmpeg")
            if not encoder:
                raise RuntimeError("Native phase-transition fixture requires the existing test encoder")
            fixture = Path(folder)/"independent-frames.mp4"
            subprocess.run([encoder, "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i",
                            "color=c=black:s=640x480:r=10", "-frames:v", "12", "-an", "-c:v", "libx264",
                            "-g", "1", "-pix_fmt", "yuv420p", str(fixture)], check=True, timeout=10,
                           stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
            stop = threading.Event()
            with open_source(np, fixture_file=fixture,
                             diagnostic=diagnostic, native_only=True) as source:
                initial_reader = source.reader.value
                raw = source.read(stop)
                self.assertIsNone(raw.pixels)
                self.assertEqual(source.converted, 0)
                selected = dict(diagnostic.data["selectedNativeFormat"])
                source.enable_conversion()
                converted = source.read(stop)
                self.assertEqual(source.reader.value, initial_reader)
                self.assertEqual(selected, diagnostic.data["selectedNativeFormat"])
                self.assertIsInstance(converted.pixels, np.ndarray)
                self.assertEqual(source.converted, 1)
                stop.set()
                self.assertIsNone(source.read(stop))
            diagnostic.finish()
            self.assertNotIn("landmarks", diagnostic.path.read_text())
            self.assertEqual(diagnostic.data["uniqueNativeSamples"], 2)

    def test_comparison_has_bounded_phases_health_and_no_inference_or_observations(self):
        with tempfile.TemporaryDirectory() as folder:
            diagnostic = CameraDiagnostics(Path(folder)/"check.json", "synthetic")
            now = [0.0]
            class Source:
                conversions = 0
                def enable_conversion(self): self.conversions += 1
                def read(self, stop): now[0] += .1; return object()
            events = []
            source = Source()
            run_capture_check(source, threading.Event(), lambda kind: events.append(kind), diagnostic,
                              seconds=1, clock=lambda: now[0])
            diagnostic.finish()
            self.assertEqual(source.conversions, 1)
            self.assertEqual(events[0], "ready")
            self.assertEqual(events[-1], "stopped")
            self.assertIn("health", events)
            self.assertNotIn("observation", events)
            self.assertEqual(set(diagnostic.data["measurementModes"]), {"raw", "conversion"})
            self.assertEqual(diagnostic.data["processed"], 0)

    def test_stop_never_enters_second_phase(self):
        with tempfile.TemporaryDirectory() as folder:
            stop = threading.Event()
            class Source:
                def read(self, token): stop.set(); return None
                def enable_conversion(self): raise AssertionError("Stop must prevent renegotiation")
            diagnostic = CameraDiagnostics(Path(folder)/"check.json", "stop")
            run_capture_check(Source(), stop, lambda _: None, diagnostic)
            self.assertNotIn("conversion", diagnostic.data["measurementModes"])

    def test_empty_conversion_is_failure_and_never_reported_as_completed_measurement(self):
        with tempfile.TemporaryDirectory() as folder:
            now = [0.0]
            class Source:
                conversion = False
                def enable_conversion(self): self.conversion = True
                def read(self, stop):
                    now[0] += 1
                    return None if self.conversion else object()
            events = []
            diagnostic = CameraDiagnostics(Path(folder)/"check.json", "empty-conversion")
            with self.assertRaisesRegex(CameraError, "no-frames"):
                run_capture_check(Source(), threading.Event(), events.append, diagnostic, seconds=.5, clock=lambda: now[0])
            self.assertEqual(diagnostic.data["measurementModes"]["conversion"]["outcome"], "source-ended")
            self.assertNotIn("stopped", events)
