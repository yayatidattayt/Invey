"""One native source owned and released by its capture thread; a single replaceable frame."""
import threading
import time
from dataclasses import replace


class LatestCapture:
    def __init__(self, factory, stop, diagnostic=None):
        self.factory, self.stop, self.diagnostic = factory, stop, diagnostic
        self.ready = threading.Condition()
        self.closed = threading.Event()
        self.started = threading.Event()
        self.latest = None
        self.error = None
        self.done = False
        self.thread = threading.Thread(target=self._capture, name="InveyCapture", daemon=True)

    def is_set(self):
        return self.closed.is_set() or self.stop.is_set()

    def wait(self, duration):
        end = time.monotonic() + duration
        while not self.is_set() and time.monotonic() < end:
            self.closed.wait(min(.01, max(0, end - time.monotonic())))
        return self.is_set()

    def _capture(self):
        sequence = 0
        try:
            with self.factory() as source:
                self.started.set()
                while not self.is_set():
                    frame = source.read(self)
                    if frame is None or self.is_set():
                        break
                    frame = replace(frame, sequence=sequence)
                    sequence += 1
                    with self.ready:
                        if self.latest is not None and self.diagnostic:
                            self.diagnostic.count("captureFramesSuperseded")
                        self.latest = frame
                        if self.diagnostic:
                            self.diagnostic.count("uniqueCapturedFrames")
                        self.ready.notify_all()
        except Exception as error:
            self.error = error
        finally:
            with self.ready:
                self.done = True
                self.started.set()
                self.ready.notify_all()

    def __enter__(self):
        self.thread.start()
        if not self.started.wait(8):
            self.closed.set()
            raise TimeoutError("Capture startup did not complete.")
        if self.error:
            raise self.error
        return self

    def read(self, stop):
        with self.ready:
            while self.latest is None and not self.done and not stop.is_set():
                self.ready.wait(.02)
            if stop.is_set():
                self.latest = None
                return None
            if self.error:
                raise self.error
            frame, self.latest = self.latest, None
            return frame

    def __exit__(self, *_):
        self.closed.set()
        with self.ready:
            self.latest = None
            self.ready.notify_all()
        self.thread.join(2)
        if self.thread.is_alive():
            raise RuntimeError("Capture release unconfirmed; worker supervisor must terminate it.")
