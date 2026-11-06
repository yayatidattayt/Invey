import ctypes
import os
from pathlib import Path
import threading
import unittest
from unittest.mock import patch

import numpy as np

from frame_pipeline import to_srgb
from windows_camera import CameraError, CameraFrameSource, check, copy_bgr, open_source


class CameraTests(unittest.TestCase):
    def test_rgb32_bottom_up_padding_and_ownership(self):
        rows = (ctypes.c_ubyte * 16)(255, 0, 0, 0, 11, 11, 11, 11,
                                     0, 0, 255, 0, 22, 22, 22, 22)
        start = ctypes.addressof(rows)
        result = copy_bgr(np, 1, 2, start, len(rows), start + 8, -8)
        rows[10] = 0
        self.assertEqual(result.tolist(), [[[0, 0, 255]], [[255, 0, 0]]])
        with self.assertRaisesRegex(CameraError, "unsupported-format"):
            copy_bgr(np, 1, 2, start, 8, start + 8, -8)

    def test_windows_camera_errors_are_distinct(self):
        for hresult, code in [(0x80070005, "permission-denied"), (0x80070020, "device-busy"),
                              (0xC00D3EA2, "device-lost"), (0xC00D36B4, "unsupported-format"),
                              (0x80004005, "runtime-failure")]:
            with self.subTest(code=code):
                with self.assertRaisesRegex(CameraError, code):
                    check(ctypes.c_int32(hresult).value)

    def test_unlisted_device_is_rejected_without_activation(self):
        with patch("windows_camera.devices", return_value={}):
            with self.assertRaisesRegex(CameraError, "no-device"):
                with open_source(np, device="missing"):
                    self.fail("An unlisted source was opened.")

    def test_camera_end_of_stream_is_loss_and_never_emits_a_frame(self):
        stop = threading.Event()
        source = CameraFrameSource(None, (640, 480, 2560), np, False, None)

        def end_of_stream(_reader, _slot, _signature, _stream, _control, _actual, flags, *_args):
            flags._obj.value = 2
            return 0

        with patch("windows_camera.call", side_effect=end_of_stream):
            with self.assertRaisesRegex(CameraError, "device-lost"):
                source.read(stop)
        self.assertEqual(source.samples_read, 0)

    def test_reader_error_is_terminal_even_when_media_type_change_is_also_flagged(self):
        source = CameraFrameSource(None, (640, 480, 2560), np, False, None)

        def reader_error(_reader, slot, _signature, *args):
            self.assertEqual(slot, 9, "Reader called another method after the terminal error flag.")
            args[3]._obj.value = 0x21
            return 0

        with patch("windows_camera.call", side_effect=reader_error) as native:
            with self.assertRaisesRegex(CameraError, "stream-interrupted"):
                source.read(threading.Event())
        native.assert_called_once()

    @unittest.skipUnless(os.environ.get("GHOST_NATIVE_VIDEO_FIXTURE"), "Requires generated local MP4 fixture.")
    def test_native_reader_decodes_correct_orientation_and_can_reopen(self):
        path = Path(os.environ["GHOST_NATIVE_VIDEO_FIXTURE"]).resolve()
        stop = threading.Event()
        for _ in range(2):
            with open_source(np, fixture_file=path) as source:
                first = source.read(stop)
                rgb = to_srgb(first, np)
                self.assertEqual(rgb.shape, (480, 640, 3))
                self.assertGreater(int(rgb[10, 10, 0]), 180)
                self.assertGreater(int(rgb[-10, 10, 2]), 180)
                second = source.read(stop)
                self.assertGreater(second.acquired_ns, first.acquired_ns)
        stop.set()
        with open_source(np, fixture_file=path) as source:
            self.assertIsNone(source.read(stop))
