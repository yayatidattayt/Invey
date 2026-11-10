"""Single-frame boundary used by the local VIDEO worker and controlled sources."""

from dataclasses import dataclass
import time


@dataclass(frozen=True)
class Frame:
    pixels: object
    acquired_ns: int
    bgr: bool = False
    sequence: int | None = None


class RecordedFrameSource:
    """Generate the qualified still/blank sequence with no frame backlog."""

    interval = 0.08

    def __init__(self, photo, np, image, image_ops):
        self.np, self.image = np, image
        with image.open(photo) as still:
            self.hand = image_ops.contain(still.convert("RGB"), (320, 240))
        self.sequence = 0
        self.next_frame = time.perf_counter()

    def read(self, stop):
        stop.wait(max(0.0, self.next_frame - time.perf_counter()))
        if stop.is_set():
            return None
        acquired_ns = time.perf_counter_ns()
        self.next_frame = acquired_ns / 1e9 + self.interval
        phase = self.sequence % 46
        self.sequence += 1
        if phase < 8 or 22 <= phase < 33:
            pixels = self.np.zeros((480, 640, 3), dtype=self.np.uint8)
        else:
            canvas = self.image.new("RGB", (640, 480), (150, 150, 150))
            shift = (phase - 8) * 4 - 26 if phase < 22 else (phase - 33) * 4 - 24
            canvas.paste(self.hand, ((640 - self.hand.width) // 2 + shift, (480 - self.hand.height) // 2))
            pixels = self.np.asarray(canvas)
        return Frame(pixels, acquired_ns)


def to_srgb(frame, np):
    pixels = frame.pixels
    if (not isinstance(pixels, np.ndarray) or pixels.dtype != np.uint8 or pixels.ndim != 3
            or pixels.shape[2] != 3 or not all(pixels.shape) or frame.acquired_ns <= 0):
        raise ValueError("Expected a nonempty timestamped RGB or BGR uint8 frame.")
    return np.ascontiguousarray(pixels[:, :, ::-1] if frame.bgr else pixels)


def run_frames(source, stop, tracker, mp, np, normalize, send, sample=None, diagnostic=None, preview=None, visual=None):
    """Read one frame at a time. None means end of source, with no observation emitted."""
    sequence = 0
    previous_ns = -1
    previous_ms = -1
    previous_sequence = -1
    while not stop.is_set():
        read_at = time.perf_counter_ns()
        frame = source.read(stop)
        if diagnostic:
            diagnostic.timing("sourceReadMs", time.perf_counter_ns() - read_at)
        if frame is None:
            return
        if stop.is_set():
            if diagnostic:
                diagnostic.count("cancelBeforeSubmission")
            return
        if frame.sequence is not None:
            sequence = frame.sequence
        if sequence <= previous_sequence:
            raise ValueError("Out-of-order source sequence.")
        previous_sequence = sequence
        before_conversion = time.perf_counter_ns()
        if diagnostic:diagnostic.timing("captureToSubmissionWaitMs",before_conversion-frame.acquired_ns)
        pixels = to_srgb(frame, np)
        if not previous_ns < frame.acquired_ns <= before_conversion:
            raise ValueError("Invalid frame clock.")
        previous_ns = frame.acquired_ns
        capture_ms = max(frame.acquired_ns // 1_000_000, previous_ms + 1)
        previous_ms = capture_ms
        image = mp.Image(image_format=mp.ImageFormat.SRGB, data=pixels)
        if diagnostic:
            diagnostic.count("validated")
            diagnostic.timing("rgbAndImageMs", time.perf_counter_ns() - before_conversion)
        if stop.is_set():
            if diagnostic:
                diagnostic.count("cancelBeforeSubmission")
            return
        before_inference = time.perf_counter_ns()
        result = tracker.detect_for_video(image, capture_ms)
        after_inference = time.perf_counter_ns()
        points, classification = normalize(result)
        if visual is not None:
            visual_at = time.perf_counter_ns()
            points, classification = visual.update(pixels, sequence, capture_ms, points, classification)
            if diagnostic:
                diagnostic.timing("visualTrackingMs", time.perf_counter_ns() - visual_at)
                diagnostic.count("visual-" + visual.reason)
        if stop.is_set():
            if diagnostic:
                diagnostic.count("cancelAfterInference")
            return
        finished = time.perf_counter_ns()
        try:
            send("observation", seq=sequence, captureMonoMs=capture_ms,
                 resultMonoMs=max(capture_ms, finished // 1_000_000),
                 tracked=bool(points), landmarks=points,
                 frame={"width": pixels.shape[1], "height": pixels.shape[0]}, **classification)
        except BrokenPipeError:
            if diagnostic:
                diagnostic.count("transmitInterrupted")
            raise
        if diagnostic:
            diagnostic.timing("pipeWriteMs", time.perf_counter_ns() - finished)
            diagnostic.publish()
        if preview is not None:
            preview_at = time.perf_counter_ns()
            preview.offer(sequence, pixels)
            if diagnostic:
                diagnostic.timing("previewCopyMs", time.perf_counter_ns() - preview_at)
        if sample is not None:
            sample(sequence, before_inference - before_conversion,
                   after_inference - before_inference, finished - frame.acquired_ns)
        sequence += 1
