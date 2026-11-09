"""Pure offered-mode policy; never opens a device or invents a media type."""
import math

def score_mode(mode):
    w, h, fps = mode["width"], mode["height"], mode["fps"]
    if not (0 < w <= 1920 and 0 < h <= 1920 and w*h <= 1920*1080 and
            isinstance(fps,(int,float)) and math.isfinite(fps) and 0 < fps <= 240 and mode.get("supported", True)):
        return None
    # Prefer >=30Hz, then a useful VGA/720p workload. Preserve slower modes as fallback.
    return (fps < 29.9, w < 640 or h < 360, abs(w-640)+abs(h-480), abs(fps-30))

def ranked_modes(modes):
    return sorted((i for i,m in enumerate(modes) if score_mode(m) is not None),
                  key=lambda i: score_mode(modes[i]))
