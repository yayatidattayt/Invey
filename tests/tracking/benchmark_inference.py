"""Repeated approved still image; processing cost only, not captured FPS or motion accuracy."""
import json,time,statistics
from pathlib import Path
import numpy as np
from PIL import Image,ImageOps
import mediapipe as mp
root=Path(__file__).resolve().parents[2]
asset=(root/'.tools/exec05-model/hand_landmarker.task').read_bytes()
rows=[]
with Image.open(root/'.tools/exec05-model/public-palm.jpg') as original:
    hand=ImageOps.contain(original.convert('RGB'),(320,240))
    canvas=Image.new('RGB',(640,480),(150,150,150));canvas.paste(hand,((640-hand.width)//2,(480-hand.height)//2))
    for w,h in ((640,480),(1280,720),(1920,1080)):
        photo=ImageOps.contain(canvas,(w,h))
        pixels=np.ascontiguousarray(np.asarray(photo));durations=[];detected=0
        options=mp.tasks.vision.HandLandmarkerOptions(base_options=mp.tasks.BaseOptions(model_asset_buffer=asset),running_mode=mp.tasks.vision.RunningMode.VIDEO,num_hands=2)
        with mp.tasks.vision.HandLandmarker.create_from_options(options) as model:
            for i in range(25):
                start=time.perf_counter_ns();image=mp.Image(image_format=mp.ImageFormat.SRGB,data=pixels);result=model.detect_for_video(image,(i+1)*40)
                durations.append((time.perf_counter_ns()-start)/1e6);detected+=bool(result.hand_landmarks)
        rows.append(dict(source='repeated-approved-still',width=photo.width,height=photo.height,frames=25,detected=detected,medianMs=statistics.median(durations[5:]),p95Ms=sorted(durations[5:])[18]))
print(json.dumps(dict(physicalCamera=False,motionAccuracy='UNVERIFIED',rows=rows),indent=2))
