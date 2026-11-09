"""Camera-free conversion comparison, no device APIs or frame persistence."""
import ctypes as c,json,time,statistics,sys
import numpy as np
from windows_camera import copy_bgr,copy_rgb
from frame_pipeline import Frame,to_srgb
rows=[]
for w,h in ((640,480),(1280,720),(1920,1080)):
    pixels=np.random.default_rng(24).integers(0,256,(h,w,4),dtype=np.uint8)
    p=pixels.ctypes.data
    expected=to_srgb(Frame(copy_bgr(np,w,h,p,pixels.nbytes,p,w*4),1,True),np)
    for name,fn in [('baseline',lambda:to_srgb(Frame(copy_bgr(np,w,h,p,pixels.nbytes,p,w*4),1,True),np)),('direct-rgb',lambda:copy_rgb(np,w,h,p,pixels.nbytes,p,w*4))]:
        values=[]
        for i in range(40):
            start=time.perf_counter_ns();actual=fn();values.append((time.perf_counter_ns()-start)/1e6)
        np.testing.assert_array_equal(expected,actual)
        rows.append(dict(source='synthetic-unpaced-memory',width=w,height=h,candidate=name,medianMs=statistics.median(values),p95Ms=sorted(values)[37],equalPixels=True,ownedCopyBytesPerPixel=7 if name=='baseline' else 3))
print(json.dumps(dict(physicalCamera=False,rows=rows),indent=2))
