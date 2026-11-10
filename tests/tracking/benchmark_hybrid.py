"""Paired same-image A/B. No camera API; known generated transformations only."""
import argparse,json,time,tracemalloc
import numpy as np
import cv2
from finger_visual import FingerVisual
from hybrid_visual import HybridFingerVisual
from hybrid_fixtures import image,model


def evaluate():
    rows=[]
    for seed in (26,99174):
        for scenario in ("low-texture","textured","stationary-noise","tiny-subpixel","reversal","fast","rotation-scale","lighting-blur","background","occlusion","disappearance","competitor","expiry"):
            for mode,kind in (("baseline",FingerVisual),("flow",HybridFingerVisual)):
                tracker=kind();errors=[];false=0;valid=0;times=[];lost=0;max_blackout=0;blackout=0
                frames=32 if scenario=="expiry" else 20
                for i in range(frames):
                    after=max(0,i-3)
                    x=170+min(after,12)*(4 if scenario=="fast" else 1)
                    if scenario=="stationary-noise":x=170
                    if scenario=="tiny-subpixel":x=170+after*.2
                    if scenario=="reversal":x=170+(after if after<8 else 16-after)
                    arguments=dict(x=x,seed=seed,texture=scenario=="textured",
                        angle=min(after,10) if scenario=="rotation-scale" else 0,
                        scale=1+min(after,10)*.004 if scenario=="rotation-scale" else 1,
                        lighting=min(after,8) if scenario=="lighting-blur" else 0,
                        blur=scenario=="lighting-blur" and i>=4,
                        background_motion=after if scenario=="background" else 0,
                        visible=not(scenario=="disappearance" and i>=4),
                        occluded=scenario=="occlusion" and i>=4,competitor=scenario=="competitor" and i>=4)
                    if scenario=="tiny-subpixel":arguments['x']=170
                    pixels=image(**arguments)
                    if scenario=="tiny-subpixel":pixels=cv2.warpAffine(pixels,np.float32([[1,0,x-170],[0,1,0]]),(400,280),borderValue=(35,35,35))
                    if scenario=="stationary-noise" and i>=4:
                        pixels=np.clip(pixels.astype(int)+np.random.default_rng(seed+i).integers(-1,2,pixels.shape),0,255).astype(np.uint8)
                    start=time.perf_counter_ns()
                    points,fields=tracker.update(pixels,i,1000+i*33,*(model(x) if i<4 else ([],{})))
                    times.append((time.perf_counter_ns()-start)/1e6)
                    if i<4:continue
                    v=fields.get("visual")
                    should_reject=scenario in ("occlusion","disappearance","competitor") or i*33-99>600
                    if v:
                        valid+=1;blackout=0
                        error=float(np.hypot(v["x"]*399-x,v["y"]*279-110));errors.append(error)
                        if should_reject or error>3:false+=1
                    else:
                        lost+=1;blackout+=33;max_blackout=max(max_blackout,blackout)
                rows.append(dict(seed=seed,scenario=scenario,mode=mode,valid=valid,falseContinuations=false,
                    lost=lost,maxBlackoutMs=max_blackout,maxErrorPixels=max(errors,default=None),
                    rmsPixels=float(np.sqrt(np.mean(np.square(errors)))) if errors else None,
                    medianMs=float(np.median(times[4:])),p95Ms=float(np.percentile(times[4:],95))))
    return rows


if __name__=="__main__":
    parser=argparse.ArgumentParser();parser.add_argument("--output",required=True);args=parser.parse_args()
    rows=evaluate()
    with open(args.output,"w") as f:json.dump(dict(evidence="SYNTHETIC_IMAGES_ASSIGNED_REFERENCE_POINT",rows=rows),f,indent=2)
    print(json.dumps(rows))
