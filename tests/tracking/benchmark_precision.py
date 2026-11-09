"""Reproducible generated-image throughput/memory evidence. No camera acquisition."""
import argparse
import gc
import json
import time
import tracemalloc
import numpy as np
from finger_visual import FingerVisual
from test_finger_visual import frame, full


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",required=True)
    args=parser.parse_args()
    timings=[]; accepted=0; frames=0
    tracemalloc.start()
    def cycle(base):
        nonlocal accepted,frames
        t=FingerVisual()
        for i in range(20):
            x=150+max(0,i-3)
            image=frame(x,seed=99174)
            start=time.perf_counter()
            _,fields=t.update(image,base+i,base*33+i*33+1000,*(full(x) if i<4 else ([],{})))
            timings.append((time.perf_counter()-start)*1000)
            accepted+=int("visual" in fields); frames+=1
        return t
    cycle(0); gc.collect(); initial=tracemalloc.get_traced_memory()[0]
    cpu=time.process_time(); start=time.perf_counter()
    for j in range(1,31): tracker=cycle(j*20)
    elapsed=time.perf_counter()-start; cpu=time.process_time()-cpu
    gc.collect(); retained,peak=tracemalloc.get_traced_memory(); tracemalloc.stop()
    timings.sort()
    result={"evidence":"SYNTHETIC_IMAGE_SEQUENCE", "frames":frames,"visualFrames":accepted,
        "timedFrames":600,"frameLoopPerSecond":600/elapsed,"cpuSeconds":cpu,"wallSeconds":elapsed,
        "visualStageMedianMs":float(np.median(timings)),"visualStageP95Ms":timings[int(len(timings)*.95)],
        "tracemallocRetainedDeltaBytes":retained-initial,"tracemallocPeakBytes":peak,
        "retainedImageBytes":sum(a.nbytes for a in (tracker.template,tracker.previous) if a is not None),
        "limitation":"Python allocation sampling, not OS working set or a sustained leak qualification; includes one live tracker and metric storage."}
    with open(args.output,"w") as f: json.dump(result,f,indent=2)


if __name__=="__main__": main()
