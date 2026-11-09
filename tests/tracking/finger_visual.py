"""Bounded warm patch correspondence. Never a cold/anatomical finger detector.

Only full model observations may establish/renew the 600 ms anchor. Sparse points
remain authoritative but cannot indefinitely renew it. Images stay in memory.
"""
import math
import numpy as np


class FingerVisual:
    radius = 8
    search = 12
    lifetime_ms = 600

    def __init__(self):
        self.reset("cold")

    def reset(self, reason):
        self.template = self.previous = self.position = None
        self.side = None
        self.anchor_ms = self.last_ms = -1
        self.last_sequence = -1
        self.geometry = None
        self.full_count = 0
        self.fallback = False
        self.reason = reason

    @staticmethod
    def gray(pixels):
        # Bounded 640-wide nearest sample, no capture queue or full-resolution float copy.
        stride = max(1, math.ceil(pixels.shape[1] / 640))
        return pixels[::stride, ::stride].mean(axis=2).astype(np.float32)

    def patch(self, image, point):
        x, y = point; r = self.radius
        if x-r < 0 or y-r < 0 or x+r >= image.shape[1] or y+r >= image.shape[0]:
            return None
        return image[y-r:y+r+1, x-r:x+r+1].copy()

    def match(self, image, template, center):
        x, y = center; r, s = self.radius, self.search
        if x-r-s < 0 or y-r-s < 0 or x+r+s >= image.shape[1] or y+r+s >= image.shape[0]:
            return None, "edge", None
        region = image[y-r-s:y+r+s+1, x-r-s:x+r+s+1]
        windows = np.lib.stride_tricks.sliding_window_view(region, template.shape)
        # Absolute brightness residual also rejects exposure/occlusion changes. No score is a probability.
        errors = np.sqrt(np.mean((windows-template)**2, axis=(-2,-1)))
        iy, ix = np.unravel_index(np.argmin(errors), errors.shape)
        best = float(errors[iy,ix])
        competitors = errors.copy()
        competitors[max(0,iy-2):iy+3,max(0,ix-2):ix+3] = np.inf
        if best > 12:
            return None, "appearance", best
        if float(competitors.min()) - best < 4:
            return None, "nonunique", best
        if ix in (0,2*s) or iy in (0,2*s):
            return None, "motion-limit", best
        return (x+int(ix)-s,y+int(iy)-s), "valid", best

    def update(self, pixels, sequence, stamp, points, fields):
        if (not isinstance(pixels,np.ndarray) or pixels.dtype != np.uint8 or pixels.ndim != 3
                or pixels.shape[2] != 3 or min(pixels.shape[:2]) < 48):
            self.reset("invalid-frame"); return [], {}
        geometry = pixels.shape[:2]
        if (self.last_ms >= 0 and (sequence <= self.last_sequence or not 0 < stamp-self.last_ms <= 150)):
            self.reset("frame-gap-or-order")
            return [], {}
        if self.geometry is not None and geometry != self.geometry:
            self.reset("geometry"); return [], {}
        if fields.get("ambiguous"):
            self.reset("ambiguous"); return points, fields
        side = fields.get("handedness")
        if side is not None and self.side is not None and side != self.side:
            self.reset("hand-change"); return [], {}
        image = self.gray(pixels)
        self.last_ms, self.last_sequence, self.geometry = stamp, sequence, geometry
        tip = points[8] if points else fields.get("points", [None]*21)[8]
        if tip is not None and side is not None:
            target = (round(tip[0]*(image.shape[1]-1)),round(tip[1]*(image.shape[0]-1)))
            if self.fallback and self.position is not None and math.dist(target,self.position) > 16:
                self.reset("reacquisition-conflict"); return [], {}
            self.side = side
            self.fallback = False
            if points:
                self.full_count += 1
                patch = self.patch(image,target)
                if self.full_count >= 4 and patch is not None and patch.std() >= 8 and patch[4:-4,4:-4].std() >= 8:
                    self.template, self.anchor_ms = patch, stamp
                    self.position, self.previous = target, image
                    self.reason = "full-anchor"
                else:
                    self.template = None; self.reason = "anchor-texture-or-warmup"
            else:
                self.position, self.previous = target, image
                self.reason = "partial-model"
            return points, fields
        self.full_count = 0
        if self.template is None or self.position is None or stamp-self.anchor_ms > self.lifetime_ms:
            self.reset("anchor-expired-or-missing"); return points, fields
        target, reason, residual = self.match(image,self.template,self.position)
        if target is not None:
            patch = self.patch(image,target)
            reverse, reverse_reason, _ = self.match(self.previous,patch,self.position)
            if reverse is None or math.dist(reverse,self.position) > 1.5:
                target, reason = None, "forward-backward"
        if target is None:
            self.reset(reason); return [], {}
        self.position, self.previous, self.fallback = target, image, True
        self.reason = "visual"
        return [], {"v":3, "handedness":self.side, "visual":{
            "x":target[0]/(image.shape[1]-1), "y":target[1]/(image.shape[0]-1),
            "anchorMonoMs":self.anchor_ms, "residual":residual}}
