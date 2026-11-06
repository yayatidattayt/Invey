"""Camera-free MediaPipe qualification against an attributed still image and blank RGB frames."""

import argparse
import hashlib
import importlib.metadata
import json
import math
from pathlib import Path
import platform
import statistics
import time

import mediapipe as mp
import numpy as np
from PIL import Image, ImageOps

MODEL_HASH = "fbc2a30080c3c557093b5ddfc334698132eb341044ccee322ccf8bcf3607cde1"
PHOTO_HASH = "0bc2f3caef9dce603ea7c18ad3f0f033896dc6a11b03b3bb2105c1cfd93b68db"


def checked_bytes(path, expected):
    data = path.read_bytes()
    if hashlib.sha256(data).hexdigest() != expected:
        raise ValueError(f"Unexpected bytes in qualification asset {path.name}")
    return data


def timed(call):
    start = time.perf_counter()
    result = call()
    return result, (time.perf_counter() - start) * 1000


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def validate_palm(result, require_in_frame=True):
    require(len(result.hand_landmarks) == len(result.handedness) == 1, "Expected one detected hand")
    points = result.hand_landmarks[0]
    require(len(points) == 21, "Expected 21 image landmarks")
    require(all(math.isfinite(value) for p in points for value in (p.x, p.y, p.z)), "Non-finite model landmark")
    outside = sum(not (0 <= p.x <= 1 and 0 <= p.y <= 1) for p in points)
    require(not require_in_frame or outside == 0, "Landmarks outside the v1 normalized image domain")
    side, = result.handedness[0]
    require(side.category_name in {"Left", "Right"} and 0 <= side.score <= 1, "Invalid handedness")
    return {"hands": 1, "landmarks_per_hand": len(points), "handedness": side.category_name,
            "handedness_score": side.score, "outside_image_landmarks": outside}


def validate_blank(result):
    require(not result.hand_landmarks and not result.handedness, "Expected no hand in an all-zero image")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model", type=Path, required=True)
    parser.add_argument("--palm", type=Path, required=True)
    args = parser.parse_args()
    model = checked_bytes(args.model, MODEL_HASH)
    checked_bytes(args.palm, PHOTO_HASH)
    with Image.open(args.palm) as photo:
        rgb = photo.convert("RGB")
        edge_pixels = np.array(ImageOps.pad(rgb, (640, 480)))
        inset = ImageOps.contain(rgb, (320, 240))
        canvas = Image.new("RGB", (640, 480), (150, 150, 150))
        canvas.paste(inset, ((640 - inset.width) // 2, (480 - inset.height) // 2))
        pixels = np.array(canvas)
    palm = mp.Image(image_format=mp.ImageFormat.SRGB, data=pixels)
    edge_palm = mp.Image(image_format=mp.ImageFormat.SRGB, data=edge_pixels)
    blank = mp.Image(image_format=mp.ImageFormat.SRGB, data=np.zeros_like(pixels))
    landmarker = mp.tasks.vision.HandLandmarker

    def create(mode, asset=model):
        options = mp.tasks.vision.HandLandmarkerOptions(
            base_options=mp.tasks.BaseOptions(model_asset_buffer=asset, delegate=mp.tasks.BaseOptions.Delegate.CPU),
            running_mode=mode, num_hands=1,
        )
        return landmarker.create_from_options(options)

    image_mode = mp.tasks.vision.RunningMode.IMAGE
    tracker, open_ms = timed(lambda: create(image_mode))
    try:
        result, first_ms = timed(lambda: tracker.detect(palm))
        positive = validate_palm(result)
        edge = validate_palm(tracker.detect(edge_palm), require_in_frame=False)
        require(edge["outside_image_landmarks"] > 0, "Edge fixture no longer exercises out-of-image coordinates")
        validate_blank(tracker.detect(blank))
    finally:
        _, close_ms = timed(tracker.close)

    tracker, reopen_ms = timed(lambda: create(mp.tasks.vision.RunningMode.VIDEO))
    times = []
    try:
        validate_palm(tracker.detect_for_video(palm, 0))
        for index in range(1, 51):
            result, elapsed = timed(lambda: tracker.detect_for_video(palm, index * 33))
            validate_palm(result)
            times.append(elapsed)
        for index in range(51, 56):
            result = tracker.detect_for_video(blank, index * 33)
        validate_blank(result)
    finally:
        _, video_close_ms = timed(tracker.close)

    corrupt_rejected = False
    try:
        with create(image_mode, b"invalid model"):
            pass
    except (RuntimeError, ValueError):
        corrupt_rejected = True
    require(corrupt_rejected, "Corrupt model was accepted")
    print(json.dumps({
        "evidence_class": "REAL_MODEL_INFERENCE",
        "mode": "CPU, still input",
        "python": platform.python_version(),
        "platform": platform.platform(),
        "mediapipe": importlib.metadata.version("mediapipe"),
        "model_sha256": MODEL_HASH,
        "photo_sha256": PHOTO_HASH,
        "positive": positive,
        "edge_fixture": edge,
        "blank_hands": 0,
        "video_held_image_samples": len(times),
        "video_final_blank_hands": 0,
        "corrupt_model_rejected": corrupt_rejected,
        "model_open_ms": open_ms,
        "first_image_inference_ms": first_ms,
        "model_close_ms": close_ms,
        "model_reopen_ms": reopen_ms,
        "video_model_close_ms": video_close_ms,
        "video_inference_median_ms": statistics.median(times),
        "video_inference_p95_ms": sorted(times)[math.ceil(len(times) * .95) - 1],
        "camera_frames": 0,
    }, indent=2))


if __name__ == "__main__":
    main()
