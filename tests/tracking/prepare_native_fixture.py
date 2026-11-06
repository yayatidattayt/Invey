"""Build a local MP4 from the public qualification still and generated blanks."""

import argparse
from pathlib import Path
import subprocess

from frame_pipeline import RecordedFrameSource
from qualify_hand_landmarker import PHOTO_HASH, Image, ImageOps, checked_bytes, np


class Unpaced:
    def wait(self, seconds):
        pass

    def is_set(self):
        return False


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--palm", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--frames", type=int, choices=range(1, 1001), metavar="1–1000", default=184)
    args = parser.parse_args()
    checked_bytes(args.palm, PHOTO_HASH)
    source = RecordedFrameSource(args.palm, np, Image, ImageOps)
    with subprocess.Popen(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-f", "rawvideo",
                           "-pixel_format", "rgb24", "-video_size", "640x480", "-framerate", "25/2",
                           "-i", "-", "-an", "-c:v", "libx264", "-pix_fmt", "yuv420p",
                           str(args.out)], stdin=subprocess.PIPE) as encoder:
        for _ in range(args.frames):
            pixels = source.read(Unpaced()).pixels.copy()
            pixels[:24, :24] = [240, 16, 16]
            pixels[-24:, :24] = [16, 16, 240]
            encoder.stdin.write(pixels.tobytes())
        encoder.stdin.close()
        if encoder.wait() != 0:
            raise RuntimeError("Controlled MP4 generation failed.")


if __name__ == "__main__":
    main()
