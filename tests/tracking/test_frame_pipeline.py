import threading
import time
from types import SimpleNamespace
import unittest

import numpy as np

from frame_pipeline import Frame, run_frames
from recorded_worker import normalize


class TestFrameSource:
    def __init__(self, *frames):
        self.frames = iter(frames)

    def read(self, stop):
        return next(self.frames, None)


class FramePipelineTests(unittest.TestCase):
    def setUp(self):
        self.stop = threading.Event()
        self.inputs = []
        self.wire = []
        self.mp = SimpleNamespace(ImageFormat=SimpleNamespace(SRGB="sRGB"),
                                  Image=lambda image_format, data: (image_format, data))

    def detect_for_video(self, image, timestamp):
        self.inputs.append((image, timestamp))
        return SimpleNamespace(hand_landmarks=[], handedness=[])

    def run_source(self, *frames):
        run_frames(TestFrameSource(*frames), self.stop, self, self.mp, np,
                   normalize, lambda kind, **fields: self.wire.append((kind, fields)))

    def test_bgr_is_converted_and_missing_frame_emits_nothing(self):
        pixels = np.array([[[10, 20, 30], [90, 100, 110]]], dtype=np.uint8)[:, ::-1]
        self.run_source(Frame(pixels, time.perf_counter_ns(), bgr=True))
        self.assertEqual(len(self.inputs), 1)
        (format_name, converted), stamp = self.inputs[0]
        self.assertEqual(format_name, "sRGB")
        self.assertTrue(converted.flags.c_contiguous)
        self.assertEqual(converted.tolist(), [[[110, 100, 90], [30, 20, 10]]])
        self.assertEqual(len(self.wire), 1)
        self.assertEqual(self.wire[0][1]["frame"], {"width": 2, "height": 1})
        self.assertFalse(self.wire[0][1]["tracked"])
        self.assertEqual(self.wire[0][1]["captureMonoMs"], stamp)

    def test_native_sequence_reordering_is_rejected_even_with_increasing_timestamps(self):
        pixels=np.zeros((50,50,3),np.uint8)
        now=time.perf_counter_ns()-10000000
        with self.assertRaisesRegex(ValueError,"Out-of-order"):
            self.run_source(Frame(pixels,now,sequence=9),Frame(pixels,now+1000000,sequence=8))
        self.assertEqual(len(self.inputs),1)

    def test_invalid_layout_is_rejected_before_inference_or_wire_output(self):
        for pixels in (np.zeros((1, 1, 4), np.uint8), np.zeros((1, 1, 3), np.float32)):
            with self.subTest(shape=pixels.shape, dtype=pixels.dtype):
                with self.assertRaises(ValueError):
                    self.run_source(Frame(pixels, time.perf_counter_ns()))
        self.assertEqual(self.inputs, [])
        self.assertEqual(self.wire, [])

    def test_reordered_frame_cannot_overwrite_valid_result(self):
        pixels = np.zeros((1, 1, 3), np.uint8)
        stamp = time.perf_counter_ns()
        with self.assertRaisesRegex(ValueError, "frame clock"):
            self.run_source(Frame(pixels, stamp), Frame(pixels, stamp - 1))
        self.assertEqual(len(self.inputs), 1)
        self.assertEqual(len(self.wire), 1)

    def test_cancel_during_inference_cannot_emit_an_observation(self):
        def cancel(image, timestamp):
            self.stop.set()
            return self.detect_for_video(image, timestamp)

        tracker = SimpleNamespace(detect_for_video=cancel)
        source = TestFrameSource(Frame(np.zeros((1, 1, 3), np.uint8), time.perf_counter_ns()))
        events = []
        diagnostic = SimpleNamespace(timing=lambda *_: None, count=events.append)
        run_frames(source, self.stop, tracker, self.mp, np, normalize,
                   lambda kind, **fields: self.wire.append((kind, fields)), diagnostic=diagnostic)
        self.assertEqual(len(self.inputs), 1)
        self.assertEqual(self.wire, [])
        self.assertEqual(events, ["validated", "cancelAfterInference"])
