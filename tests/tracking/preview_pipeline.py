"""Opt-in, memory-only RGB preview. One replaceable frame and one writer thread."""
import os
import re
import struct
import threading


class PreviewSender:
    def __init__(self, name):
        if not re.fullmatch(r"InveyPreview-[0-9a-f]{32}", name):
            raise ValueError("Invalid local preview pipe.")
        self.name = name
        self.latest = None
        self.ready = threading.Condition()
        self.closed = False
        self.thread = threading.Thread(target=self._write, daemon=True)
        self.thread.start()

    def offer(self, sequence, pixels):
        height, width = pixels.shape[:2]
        stride = max(1, (width + 639) // 640, (height + 359) // 360)
        # This copy is a bounded display thumbnail. Inference keeps its original input resolution.
        image = pixels[::stride, ::stride].copy()
        height, width = image.shape[:2]
        payload = image.tobytes()
        if len(payload) != width * height * 3 or width > 640 or height > 360:
            raise ValueError("Invalid preview dimensions.")
        frame = (struct.pack("<Iqiii", 0x49565031, sequence, width, height, len(payload)), payload)
        with self.ready:
            if not self.closed:
                self.latest = frame
                self.ready.notify()

    def _write(self):
        try:
            with open("\\\\.\\pipe\\" + self.name, "wb", buffering=0) as pipe:
                while True:
                    with self.ready:
                        self.ready.wait_for(lambda: self.closed or self.latest is not None)
                        if self.closed:
                            return
                        header, payload = self.latest
                        self.latest = None
                    for part in (header, payload):
                        remaining = memoryview(part)
                        while remaining:
                            count = pipe.write(remaining)
                            if not count:
                                return
                            remaining = remaining[count:]
        except OSError:
            # The optional view can close independently. It never authorizes capture or actions.
            pass
        finally:
            with self.ready:
                self.closed = True
                self.latest = None

    def __enter__(self):
        return self

    def __exit__(self, *_):
        with self.ready:
            self.closed = True
            self.latest = None
            self.ready.notify()
        self.thread.join(timeout=.5)
