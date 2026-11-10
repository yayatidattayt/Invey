"""Same-frame model/optical arbitration. Warm identity, current pixels, preview only.

No camera API, prediction output, geometry invention or new gesture authority.
The v3 consumer's600ms full-anchor deadline is intentionally unchanged.
"""
import math
import time
import cv2
import numpy as np


class HybridFingerVisual:
    lifetime_ms = 600

    def __init__(self):
        cv2.setNumThreads(1)  # Sidecar-local bounded CPU; no application/system setting.
        self.checks = {}
        self.durations = {}
        self.reset("cold")

    def reset(self, reason):
        self.previous = self.anchor = self.features = self.anchor_features = None
        self.tip = self.anchor_tip = self.patch = None
        self.side = self.geometry = None
        self.anchor_ms = self.last_ms = self.last_sequence = -1
        self.full_count = 0
        self.fallback = False
        self.reason = reason
        self.source = "lost"

    @staticmethod
    def gray(pixels):
        h, w = pixels.shape[:2]
        factor=min(1,640/max(h,w))
        small = cv2.resize(pixels, (max(1,round(w*factor)),max(1,round(h*factor))), interpolation=cv2.INTER_AREA)
        return cv2.cvtColor(small, cv2.COLOR_RGB2GRAY), small

    @staticmethod
    def fit(a, b):
        ac, bc = a.mean(0), b.mean(0)
        x, y = a-ac, b-bc
        u, singular, vt = np.linalg.svd(x.T@y)
        rotation = vt.T@u.T
        if np.linalg.det(rotation) < 0:
            return None
        denominator = float((x*x).sum())
        if denominator < 8:
            return None
        scale = float(singular.sum()/denominator)
        linear = scale*rotation
        translation = bc-linear@ac
        residual = np.linalg.norm(a@linear.T+translation-b,axis=1)
        if float(np.max(residual)) > 1.5:
            return None
        return linear, translation, scale

    @staticmethod
    def flow(before, current, points):
        params = dict(winSize=(15,15), maxLevel=2,
                      criteria=(cv2.TERM_CRITERIA_EPS|cv2.TERM_CRITERIA_COUNT,20,.01), minEigThreshold=.0001)
        start = points.astype(np.float32).reshape(-1,1,2)
        q, status, error = cv2.calcOpticalFlowPyrLK(before,current,start,None,**params)
        if q is None:
            return None
        back, reverse, _ = cv2.calcOpticalFlowPyrLK(current,before,q,None,**params)
        if back is None:
            return None
        valid = status.ravel().astype(bool)&reverse.ravel().astype(bool)&(error.ravel()<18)&(
            np.linalg.norm(back.reshape(-1,2)-points,axis=1)<.75)
        if valid.sum()<4 or valid.mean()<.75:
            return None
        return points[valid],q.reshape(-1,2)[valid],valid

    def initialize(self, image, rgb, tip, side, stamp):
        h,w=image.shape; x,y=np.rint(tip).astype(int)
        if min(x,y,w-1-x,h-1-y)<20:
            self.checks["anchor"]="edge";return
        x0,x1=max(0,x-25),min(w,x+26);y0,y1=max(0,y-25),min(h,y+26)
        yy,xx=np.ogrid[y0:y1,x0:x1]
        color=np.median(rgb[y-2:y+3,x-2:x+3],axis=(0,1))
        mask=np.zeros(image.shape,np.uint8)
        mask[y0:y1,x0:x1]=(((xx-x)**2+(yy-y)**2<=24**2)&(
            np.max(np.abs(rgb[y0:y1,x0:x1].astype(np.int16)-color),axis=2)<65)).astype(np.uint8)*255
        features=cv2.goodFeaturesToTrack(image,12,.04,3,mask=mask,blockSize=3)
        if features is None or len(features)<4:
            self.checks["anchor"]="features";return
        points=features.reshape(-1,2)
        if np.linalg.det(np.cov(points.T))<12:
            self.checks["anchor"]="aperture";return
        self.anchor=self.previous=image
        self.anchor_features=self.features=points
        self.tip=self.anchor_tip=np.array(tip,dtype=float)
        self.patch=cv2.getRectSubPix(image,(37,37),tuple(map(float,tip)))
        self.side=side;self.anchor_ms=stamp
        self.checks["anchor"]="valid"

    def candidate(self,image,stamp):
        if self.anchor is None:
            return None,"no-anchor",None
        if stamp-self.anchor_ms>self.lifetime_ms:
            return None,"anchor-expired",None
        tracked=self.flow(self.previous,image,self.features)
        if tracked is None:
            return None,"forward-backward",None
        a,b,valid=tracked
        if np.linalg.det(np.cov(a.T))<8:
            return None,"aperture",None
        step=self.fit(a,b)
        if step is None or not .90<=step[2]<=1.10:
            return None,"coherence",None
        linear,translation,_=step
        tip=linear@self.tip+translation
        if np.linalg.norm(tip-self.tip)>40:
            return None,"motion-limit",None
        # After a qualified full-hand refresh these are the same frame/features. Reuse
        # the exact computation; it is not a second independent confidence measurement.
        direct=tracked if self.anchor is self.previous and self.anchor_features is self.features else self.flow(self.anchor,image,self.anchor_features)
        if direct is None:
            return None,"anchor-correspondence",None
        fixed=self.fit(direct[0],direct[1])
        if fixed is None or not .75<=fixed[2]<=1.33 or np.linalg.norm(fixed[0]@self.anchor_tip+fixed[1]-tip)>2:
            return None,"drift",None
        h,w=image.shape
        if min(tip[0],tip[1],w-1-tip[0],h-1-tip[1])<20:
            return None,"edge",None
        # Rectify current pixels into the original tip-centered patch. Tests include tip
        # occlusion while shaft features survive; tracking nearby corners alone is insufficient.
        offset=self.anchor_tip-18
        matrix=np.column_stack((fixed[0],fixed[0]@offset+fixed[1])).astype(np.float32)
        patch=cv2.warpAffine(image,matrix,(37,37),flags=cv2.INTER_LINEAR|cv2.WARP_INVERSE_MAP).astype(float)
        old=self.patch.astype(float)
        brightness=abs(float(patch.mean()-old.mean()))
        residual=float(np.sqrt(np.mean(((patch-patch.mean())-(old-old.mean()))**2)))
        if brightness>30 or residual>12:
            return None,"appearance",None
        # Reject nearby competing image matches; a score is a heuristic, not probability.
        x,y=np.rint(tip).astype(int)
        x0,x1=max(0,x-58),min(w,x+59);y0,y1=max(0,y-58),min(h,y+59)
        local=cv2.getRectSubPix(image,(37,37),tuple(map(float,tip)))
        scores=cv2.matchTemplate(image[y0:y1,x0:x1],local,cv2.TM_SQDIFF)
        cy,cx=y-y0-18,x-x0-18
        scores[max(0,cy-3):cy+4,max(0,cx-3):cx+4]=np.inf
        if np.sqrt(max(0,float(scores.min()))/1369)<4:
            return None,"nonunique",None
        return (tip,b,valid),"valid",residual

    def update(self,pixels,sequence,stamp,points,fields):
        self.checks={}
        self.durations={}
        if not isinstance(pixels,np.ndarray) or pixels.dtype!=np.uint8 or pixels.ndim!=3 or pixels.shape[2]!=3 or min(pixels.shape[:2])<48:
            self.reset("invalid-frame");return [],{}
        geometry=pixels.shape[:2]
        if self.last_ms>=0 and (sequence<=self.last_sequence or not 0<stamp-self.last_ms<=150):
            self.reset("frame-gap-or-order");return [],{}
        if self.geometry is not None and geometry!=self.geometry:
            self.reset("geometry");return [],{}
        if fields.get("ambiguous"):
            self.reset("ambiguous");return points,fields
        side=fields.get("handedness")
        if side is not None and self.side is not None and side!=self.side:
            self.reset("hand-change");return [],{}
        image,rgb=self.gray(pixels)
        self.geometry=geometry;self.last_ms=stamp;self.last_sequence=sequence
        flow_at=time.perf_counter_ns()
        measured,reason,residual=self.candidate(image,stamp)
        self.durations["correspondenceMs"]=time.perf_counter_ns()-flow_at
        select_at=time.perf_counter_ns()
        self.checks["flow"]=reason
        tip=points[8] if points else fields.get("points",[None]*21)[8]
        if tip is not None and side is not None:
            model=np.array((tip[0]*(image.shape[1]-1),tip[1]*(image.shape[0]-1)))
            conflict=measured is not None and np.linalg.norm(model-measured[0])>8
            if conflict or self.fallback and measured is None:
                self.reset("reacquisition-conflict" if self.fallback else "source-conflict")
                self.checks["arbitration"]="conflict"
                self.durations["sourceArbitrationMs"]=time.perf_counter_ns()-select_at
                return [],{}
            self.fallback=False;self.side=side;self.source="model" if points else "partial"
            self.reason="full-model" if points else "partial-model"
            self.checks["arbitration"]=self.reason
            if points:
                self.full_count+=1
                if self.full_count>=4:
                    anchor_at=time.perf_counter_ns()
                    if measured is not None and not conflict and np.linalg.norm(model-measured[0])<=3:
                        self.anchor=self.previous=image;self.anchor_features=self.features=measured[1]
                        self.tip=self.anchor_tip=model;self.side=side;self.anchor_ms=stamp
                        self.patch=cv2.getRectSubPix(image,(37,37),tuple(map(float,model)))
                        self.checks["anchor"]="model-confirmed"
                    else:self.initialize(image,rgb,model,side,stamp)
                    self.durations["featureInitializationMs"]=time.perf_counter_ns()-anchor_at
            elif measured is not None:
                # Engine B advances from its current pixel correspondence even when A's
                # sparse point wins. Partial evidence never renews full identity age.
                self.tip=measured[0];self.features=measured[1];self.previous=image
            self.durations["sourceArbitrationMs"]=max(0,time.perf_counter_ns()-select_at-self.durations.get("featureInitializationMs",0))
            return points,fields
        self.full_count=0
        if measured is None:
            self.reset(reason);self.checks["arbitration"]="lost"
            self.durations["sourceArbitrationMs"]=time.perf_counter_ns()-select_at
            return points,fields
        tip,features,valid=measured
        self.tip=tip;self.features=features;self.previous=image;self.fallback=True
        self.source="visual";self.reason="visual";self.checks["arbitration"]="visual"
        self.durations["sourceArbitrationMs"]=time.perf_counter_ns()-select_at
        return [],{"v":3,"handedness":self.side,"visual":{"x":float(tip[0]/(image.shape[1]-1)),
            "y":float(tip[1]/(image.shape[0]-1)),"anchorMonoMs":self.anchor_ms,"residual":residual}}
