import io
import struct
import threading
import unittest
from unittest.mock import patch

import numpy as np
from preview_pipeline import PreviewSender


class PreviewPipelineTests(unittest.TestCase):
    def test_rejects_nonlocal_or_unbounded_pipe_names(self):
        for name in ("C:/picture.png", "OtherPipe", "InveyPreview-" + "a" * 500):
            with self.subTest(name=name):
                with self.assertRaises(ValueError):
                    PreviewSender(name)

    def test_rgb_packet_is_bounded_and_queue_replaces_pending_frames(self):
        entered = threading.Event()
        release = threading.Event()
        packets = []
        completed = threading.Event()

        class Pipe:
            def __enter__(self):
                return self

            def __exit__(self, *_):
                pass

            def write(self, data):
                if len(data) == 24:
                    packets.append(struct.unpack("<Iqiii", data))
                    entered.set()
                    release.wait(2)
                elif packets[-1][1] == 19:
                    completed.set()
                return len(data)

        with patch("builtins.open", return_value=Pipe()):
            sender = PreviewSender("InveyPreview-" + "a" * 32)
            image = np.zeros((1080, 1920, 3), dtype=np.uint8)
            sender.offer(0, image)
            self.assertTrue(entered.wait(2))
            for sequence in range(1, 20):
                sender.offer(sequence, image)
            with sender.ready:
                self.assertEqual(struct.unpack("<Iqiii", sender.latest[0])[1], 19)
                self.assertLessEqual(len(sender.latest[1]), 640 * 360 * 3)
            release.set()
            self.assertTrue(completed.wait(2))
            sender.__exit__()
            self.assertFalse(sender.thread.is_alive())
        self.assertEqual([packet[1] for packet in packets], [0, 19])
        self.assertEqual(packets[0][2:], (640, 360, 640 * 360 * 3))


if __name__ == "__main__":
    unittest.main()
