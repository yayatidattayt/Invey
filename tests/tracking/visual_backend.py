"""Optional local optical backend with explicit baseline comparison and safe fallback."""
import os
import sys
from finger_visual import FingerVisual


def create_visual(diagnostic=None):
    if os.environ.get("INVEY_VISUAL_BASELINE")=="1":
        name="execution25-patch"
        tracker=FingerVisual()
    else:
        try:
            from hybrid_visual import HybridFingerVisual
            tracker=HybridFingerVisual();name="hybrid-pyramidal-lk"
        except (ImportError,OSError):
            print("GHOST_VISUAL_BACKEND_UNAVAILABLE_USING_BASELINE",file=sys.stderr,flush=True)
            tracker=FingerVisual();name="execution25-fallback-opencv-unavailable"
    if diagnostic:diagnostic.data["visualBackend"]=name
    print("GHOST_VISUAL_BACKEND="+name,file=sys.stderr,flush=True)
    return tracker
