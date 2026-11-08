from contextlib import contextmanager
import threading
import time
import unittest
from types import SimpleNamespace
from frame_pipeline import Frame
from latest_capture import LatestCapture


class LatestCaptureTests(unittest.TestCase):
    def test_30_and_60_hz_sources_drop_old_frames_under_slow_consumer_and_release_on_owner(self):
        for fps in (30, 60):
            with self.subTest(fps=fps):
                stop = threading.Event()
                released, owner, counts = [], [], {}
                class Source:
                    def read(self, cancel):
                        cancel.wait(1 / fps)
                        return None if cancel.is_set() else Frame(object(), time.perf_counter_ns())
                @contextmanager
                def factory():
                    owner.append(threading.get_ident())
                    try:
                        yield Source()
                    finally:
                        released.append(threading.get_ident())
                def count(name):
                    counts[name] = counts.get(name, 0) + 1
                sequences = []
                with LatestCapture(factory, stop, SimpleNamespace(count=count)) as capture:
                    for _ in range(5):
                        frame = capture.read(stop)
                        self.assertIsNotNone(frame)
                        sequences.append(frame.sequence)
                        time.sleep(.08)
                self.assertEqual(owner, released)
                self.assertNotEqual(owner[0], threading.get_ident())
                self.assertFalse(capture.thread.is_alive())
                self.assertEqual(sequences, sorted(set(sequences)))
                self.assertGreater(counts.get("captureFramesSuperseded", 0), 0)
                self.assertLess(len(sequences), counts["uniqueCapturedFrames"])
                self.assertIsNone(capture.latest)

    def test_open_failure_propagates_without_hanging(self):
        @contextmanager
        def factory():
            raise OSError("device missing")
            yield
        capture = LatestCapture(factory, threading.Event())
        with self.assertRaisesRegex(OSError, "device missing"):
            capture.__enter__()
        capture.thread.join(1)
        self.assertFalse(capture.thread.is_alive())

    def test_stop_invalidates_pending_frame(self):
        stop = threading.Event()
        @contextmanager
        def factory():
            yield SimpleNamespace(read=lambda cancel: None)
        with LatestCapture(factory, stop) as capture:
            stop.set()
            self.assertIsNone(capture.read(stop))
