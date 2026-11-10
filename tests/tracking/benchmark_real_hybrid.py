"""Existing CC-BY still, transformed in memory. No webcam or saved imagery.

Initial model landmark is not anatomical ground truth. Relative translation is known.
"""
import argparse,json,time,tracemalloc
from pathlib import Path
import cv2
import numpy as np
from qualify_hand_landmarker import checked_bytes,MODEL_HASH,PHOTO_HASH,mp,Image,ImageOps
from recorded_worker import open_landmarker,normalize
from finger_visual import FingerVisual
from hybrid_visual import HybridFingerVisual


def main(args):
    asset=checked_bytes(args.model,MODEL_HASH);checked_bytes(args.palm,args.photo_hash)
    with Image.open(args.palm) as photo:
        hand=ImageOps.contain(photo.convert('RGB'),(args.fit_width,args.fit_height))
        canvas=Image.new('RGB',(640,480),(150,150,150));canvas.paste(hand,((640-hand.width)//2,(480-hand.height)//2))
        original=np.asarray(canvas)
    with open_landmarker(mp,asset) as model:
        points,fields=normalize(model.detect_for_video(mp.Image(image_format=mp.ImageFormat.SRGB,data=original),1000))
    if not points:raise RuntimeError('Approved still did not yield a full model anchor')
    tip=np.array(points[8][:2])*np.array([639,479]);rows=[]
    for scenario in ('translated-still','isolated-index-region','tip-removed'):
        for name,kind in (('baseline',FingerVisual),('hybrid',HybridFingerVisual)):
            t=kind();errors=[];valid=0;false=0;timings=[]
            for i in range(24):
                dx=max(0,i-7)*1.3
                pixels=original.copy()
                if i>=8 and scenario!='translated-still':
                    if scenario=='tip-removed':pixels[:]=(150,150,150)
                    else:
                        keep=np.zeros(pixels.shape[:2],bool);x,y=np.rint(tip).astype(int)
                        keep[max(0,y-42):y+43,max(0,x-42):x+43]=True
                        pixels[~keep]=(150,150,150)
                pixels=cv2.warpAffine(pixels,np.float32([[1,0,dx],[0,1,0]]),(640,480),borderValue=(150,150,150))
                start=time.perf_counter_ns()
                p,f=t.update(pixels,i,1000+i*33,*( (points,fields) if i<8 else ([],{}) ))
                timings.append((time.perf_counter_ns()-start)/1e6)
                if i>=8 and f.get('visual'):
                    v=f['visual'];valid+=1;error=float(np.linalg.norm(np.array([v['x']*639,v['y']*479])-tip-[dx,0]));errors.append(error)
                    false+=int(scenario=='tip-removed' or error>3)
            rows.append(dict(scenario=scenario,mode=name,valid=valid,falseContinuations=false,
                maxRelativeErrorPixels=max(errors,default=None),medianMs=float(np.median(timings[8:])),p95Ms=float(np.percentile(timings[8:],95))))
    combined=[]
    for name,kind in (('baseline',FingerVisual),('hybrid',HybridFingerVisual)):
        tracker=kind();timings=[];detected=0
        with open_landmarker(mp,asset) as model:
            for i in range(40):
                start=time.perf_counter_ns();result=model.detect_for_video(mp.Image(image_format=mp.ImageFormat.SRGB,data=original),1000+i*33)
                p,f=normalize(result);detected+=bool(p);tracker.update(original,i,1000+i*33,p,f)
                timings.append((time.perf_counter_ns()-start)/1e6)
        combined.append(dict(mode=name,frames=40,detected=detected,medianModelPlusVisualMs=float(np.median(timings[5:])),p95ModelPlusVisualMs=float(np.percentile(timings[5:],95))))
    memory=[]
    for name,kind in (('baseline',FingerVisual),('hybrid',HybridFingerVisual)):
        tracemalloc.start();t=kind()
        for i in range(20):t.update(original,i,1000+i*33,points,fields)
        current,peak=tracemalloc.get_traced_memory();tracemalloc.stop()
        arrays={id(a):a for attr in ('anchor','previous','features','anchor_features','patch','template')
                if isinstance((a:=getattr(t,attr,None)),np.ndarray)}
        memory.append(dict(mode=name,pythonPeakBytes=peak,uniqueOwnedArrayBytes=sum(a.nbytes for a in arrays.values())))
    result=dict(evidence='ATTRIBUTED_STILL_GENERATED_TRANSFORMS_PRIMARY_ABSENCE_INJECTED',
        source=args.source_name,sourceUrl=args.source_url,
        anatomicalGroundTruth=False,rows=rows,combined=combined,memory=memory,
        memoryLimit='OpenCV native allocations not covered by tracemalloc; arrays may share storage')
    args.output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--model',type=Path,required=True);p.add_argument('--palm',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    p.add_argument('--photo-hash',default=PHOTO_HASH);p.add_argument('--fit-width',type=int,default=320);p.add_argument('--fit-height',type=int,default=240)
    p.add_argument('--source-name',default='Human Palm.jpg by ESEMES, CC BY3.0');p.add_argument('--source-url',default='https://commons.wikimedia.org/wiki/File:Human_Palm.jpg')
    main(p.parse_args())
