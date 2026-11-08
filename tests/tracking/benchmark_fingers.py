"""Camera-free, aggregate-only comparison on the previously qualified attributed image."""
import argparse
import json
from pathlib import Path
import statistics
import time

from qualify_hand_landmarker import checked_bytes, MODEL_HASH, PHOTO_HASH, mp, np, Image, ImageOps
from recorded_worker import normalize


def summarize(values):
    ordered = sorted(values)
    return {"median": statistics.median(ordered), "p95": ordered[max(0, int(len(ordered) * .95 + .999) - 1)]}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", type=Path, required=True)
    parser.add_argument("--palm", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    model = checked_bytes(args.model, MODEL_HASH)
    checked_bytes(args.palm, PHOTO_HASH)
    with Image.open(args.palm) as source:
        hand = ImageOps.contain(source.convert("RGB"), (320, 240))
        canvas = Image.new("RGB", (640, 480), (150, 150, 150))
        canvas.paste(hand, ((640 - hand.width) // 2, (480 - hand.height) // 2))
        full = np.array(canvas)
    # Real pixels are cropped, with synthetic padding inside the controlled test canvas.
    crop = np.zeros_like(full)
    crop[0:240] = full[240:480]
    rows = []
    for threshold in (.5, .3):
        for video in (False, True):
            options = mp.tasks.vision.HandLandmarkerOptions(
                base_options=mp.tasks.BaseOptions(model_asset_buffer=model, delegate=mp.tasks.BaseOptions.Delegate.CPU),
                running_mode=mp.tasks.vision.RunningMode.VIDEO if video else mp.tasks.vision.RunningMode.IMAGE,
                num_hands=2, min_hand_detection_confidence=threshold)
            with mp.tasks.vision.HandLandmarker.create_from_options(options) as tracker:
                times, kinds = [], {"full": 0, "partial": 0, "none": 0, "ambiguous": 0}
                cpu = time.process_time()
                for i in range(24):
                    pixels = full if video and i < 6 else crop
                    image = mp.Image(image_format=mp.ImageFormat.SRGB, data=pixels)
                    start = time.perf_counter()
                    result = tracker.detect_for_video(image, i * 80) if video else tracker.detect(image)
                    times.append((time.perf_counter() - start) * 1000)
                    points, fields = normalize(result)
                    kind = "ambiguous" if fields.get("ambiguous") else "partial" if fields.get("partial") else "full" if points else "none"
                    kinds[kind] += 1
                rows.append({"detectionThreshold": threshold, "mode": "video-with-full-start" if video else "image-cold",
                             "samples": 24, "results": kinds, "inferenceMs": summarize(times),
                             "cpuSeconds": time.process_time() - cpu})
    flow = {"status": "not-installed"}
    try:
        import cv2
        # A controlled corner translated by four pixels. This tests temporal continuation, not acquisition.
        a = np.zeros((80, 80), np.uint8)
        a[25:45, 25:45] = 255
        b = np.zeros_like(a)
        b[25:45, 29:49] = 255
        points = np.array([[[25., 25.]], [[44., 25.]], [[25., 44.]], [[44., 44.]]], np.float32)
        start = time.perf_counter()
        moved, status, error = cv2.calcOpticalFlowPyrLK(a, b, points, None)
        valid = status[:, 0] == 1
        flow = {"status": "synthetic-only", "version": cv2.__version__, "validCorners": int(valid.sum()),
                "meanDx": float((moved - points)[valid, 0, 0].mean()),
                "elapsedMs": (time.perf_counter() - start) * 1000}
    except ImportError:
        pass
    args.output.write_text(json.dumps({"source": "qualified-attributed-still-transformed-in-memory",
        "annotation": "No independent fingertip ground truth. The crop is not a validated finger-only dataset.",
        "modelSha256": MODEL_HASH, "mediapipe": mp.__version__, "comparisons": rows, "opticalFlow": flow}, indent=2) + "\n")


if __name__ == "__main__":
    main()
