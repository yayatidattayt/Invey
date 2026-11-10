"""Generated image ground truth is an assigned reference point, not human annotation."""
import cv2
import numpy as np


def image(x=170,y=110,seed=26,texture=False,angle=0,scale=1,lighting=0,blur=False,
          visible=True,background_motion=0,occluded=False,competitor=False):
    rng=np.random.default_rng(seed)
    background=rng.integers(20,55,(280,400,3),dtype=np.uint8)
    background=np.roll(background,background_motion,axis=1)
    if not visible:return background
    yy,xx=np.mgrid[:81,:81];mask=(((xx-40)/18)**2+((yy-43)/27)**2<1).astype(np.uint8)*255
    tile=np.full((81,81,3),(182,137,110),np.uint8)
    if texture:tile=np.clip(tile.astype(int)+rng.integers(-20,21,(81,81,1)),0,255).astype(np.uint8)
    cv2.rectangle(tile,(33,27),(47,34),(209,166,143),-1)
    transform=cv2.getRotationMatrix2D((40,40),angle,scale)
    tile=cv2.warpAffine(tile,transform,(81,81));mask=cv2.warpAffine(mask,transform,(81,81))
    def paste(cx):
        roi=background[y-40:y+41,cx-40:cx+41]
        roi[mask>240]=tile[mask>240]
    paste(x)
    if competitor:paste(x+34)
    if occluded:background[y-22:y+14,x-21:x+22]=(32,32,32)
    if lighting:background=np.clip(background.astype(int)+lighting,0,255).astype(np.uint8)
    if blur:background=cv2.GaussianBlur(background,(3,3),.6)
    return background


def model(x=170,y=110,side="right"):
    return [[x/399,y/279,0] for _ in range(21)],{"handedness":side}
