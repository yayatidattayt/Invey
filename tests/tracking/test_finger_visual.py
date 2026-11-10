import json
import os
import time
import unittest
import numpy as np
from finger_visual import FingerVisual


def frame(x=150, y=100, seed=73021, visible=True):
    rng=np.random.default_rng(seed)
    image=np.full((240,320,3),35,np.uint8)
    if visible:
        # Synthetic textured rounded fingertip and shaft; no private/recorded imagery.
        yy,xx=np.mgrid[-18:27,-17:18]
        mask=(xx/17)**2+((yy-4)/25)**2<1
        texture=rng.integers(-22,23,mask.shape)
        nail=(abs(xx)<8)&(yy>-10)&(yy<7)
        for c,value in enumerate((182,133,105)):
            patch=np.clip(value+texture+nail*38,0,255).astype(np.uint8)
            image[y-18:y+27,x-17:x+18,c]=np.where(mask,patch,35)
    return image


def full(x=150,y=100,side="right"):
    return [[x/319,y/239,0]]*21, {"handedness":side}


class VisualTests(unittest.TestCase):
    def anchor(self, seed=73021):
        t=FingerVisual()
        for i in range(4): t.update(frame(seed=seed),i,1000+i*33,*full())
        self.assertIsNotNone(t.template)
        return t

    def test_translated_visible_finger_continues_and_reanchors(self):
        rows=[]
        for seed in (73021,99174):
            t=self.anchor(seed); watch=time.perf_counter(); error=[]
            for i in range(4,19):
                x=150+(i-3)*2
                points,fields=t.update(frame(x,seed=seed),i,1000+i*33,[],{})
                self.assertFalse(points); self.assertEqual(fields.get("v"),3,t.reason)
                error.append(abs(fields["visual"]["x"]*319-x))
                self.assertEqual(fields["handedness"],"right")
            age=1000+18*33-t.anchor_ms
            points,fields=t.update(frame(182,seed=seed),19,1627,*full(182))
            self.assertEqual(len(points),21); self.assertNotIn("visual",fields)
            rows.append({"seed":seed,"observedContinuationMs":age,"frames":15,"maximumErrorPixels":max(error),
                         "cpuWallMs":(time.perf_counter()-watch)*1000,"reacquisitionFrames":1})
        if os.environ.get("INVEY_VISUAL_BENCHMARK"):
            with open(os.environ["INVEY_VISUAL_BENCHMARK"],"w") as f:
                json.dump({"evidence":"SYNTHETIC_IMAGE_SEQUENCE","rows":rows},f,indent=2)

    def test_disappearance_background_lighting_rotation_and_corruption_reject(self):
        for label,image in (
            ("disappearance",frame(visible=False)),
            ("background-motion",np.roll(frame(visible=False),5,axis=1)),
            ("lighting",np.clip(frame().astype(int)+80,0,255).astype(np.uint8)),
            ("rotation",np.rot90(frame(),2).copy()),
            ("other-texture",frame(seed=13)),
            ("rapid",frame(190)),
            ("corrupt",np.zeros((10,10,4),np.uint8))):
            with self.subTest(label=label):
                t=self.anchor(); points,fields=t.update(image,4,1132,[],{})
                self.assertNotIn("visual",fields,t.reason)
                self.assertIsNone(t.template)

    def test_nonunique_correspondence_is_rejected(self):
        t=self.anchor(); image=frame()
        image[92:109,154:171]=t.template[:,:,None].astype(np.uint8)
        _,fields=t.update(image,4,1132,[],{})
        self.assertNotIn("visual",fields)

    def test_small_photometric_noise_is_tolerated_without_template_drift(self):
        t=self.anchor(); original=t.template.copy()
        pixels=np.clip(frame(153).astype(int)+3,0,255).astype(np.uint8)
        _,fields=t.update(pixels,4,1132,[],{})
        self.assertIn("visual",fields)
        np.testing.assert_array_equal(original,t.template)

    def test_timeout_order_geometry_hand_and_ambiguity_reset(self):
        for label,seq,stamp,image,points,fields in (
            ("duplicate",3,1132,frame(),[],{}),
            ("order",2,1132,frame(),[],{}),
            ("pause",4,1500,frame(),[],{}),
            ("geometry",4,1132,frame()[:200],[],{}),
            ("wrong-hand",4,1132,frame(),*full(side="left")),
            ("ambiguous",4,1132,frame(),[],{"ambiguous":True})):
            with self.subTest(label=label):
                t=self.anchor(); _,result=t.update(image,seq,stamp,points,fields)
                self.assertNotIn("visual",result); self.assertIsNone(t.template)
        t=self.anchor()
        for i in range(4,25): _,result=t.update(frame(),i,1000+i*33,[],{})
        self.assertNotIn("visual",result); self.assertIsNone(t.template)

    def test_conflicting_reacquisition_is_hidden_and_cannot_swap_owner(self):
        t=self.anchor(); t.update(frame(152),4,1132,[],{})
        points,fields=t.update(frame(195),5,1165,*full(195))
        self.assertEqual(points,[]); self.assertNotIn("visual",fields)
        self.assertEqual(t.reason,"reacquisition-conflict")

    def test_cold_candidates_and_stop_cannot_invent_anatomy(self):
        watch=time.perf_counter()
        for seed in (2,73021,99174):
            t=FingerVisual()
            for i in range(10):
                _,fields=t.update(frame(150+i,seed=seed),i,1000+i*33,[],{})
                self.assertNotIn("visual",fields)
        t=self.anchor(); t.reset("stop")
        self.assertNotIn("visual",t.update(frame(),5,1165,[],{})[1])
        self.assertLess(time.perf_counter()-watch,5)

    def test_sparse_model_has_priority_but_does_not_renew_full_anchor(self):
        t=self.anchor(); initial=t.anchor_ms
        sparse=[None]*21; sparse[8]=[151/319,100/239,0]
        fields={"v":2,"partial":True,"points":sparse,"handedness":"right"}
        self.assertEqual(t.update(frame(151),4,1132,[],fields)[1],fields)
        self.assertEqual(t.anchor_ms,initial)

    def test_weak_full_patch_does_not_destroy_recent_anchor_but_fresh_match_is_required(self):
        t=self.anchor(); original=t.template.copy(); anchor=t.anchor_ms
        weak=np.full_like(frame(),80)
        t.update(weak,4,1132,*full())
        np.testing.assert_array_equal(t.template,original)
        self.assertEqual(t.anchor_ms,anchor)
        _,fields=t.update(frame(152),5,1165,[],{})
        self.assertEqual(fields.get("v"),3,t.reason)
        self.assertEqual(fields["visual"]["anchorMonoMs"],anchor)
        self.assertAlmostEqual(fields["visual"]["x"]*319,152,delta=1)
        t=self.anchor();t.update(weak,4,1132,*full())
        self.assertNotIn("visual",t.update(frame(visible=False),5,1165,[],{})[1])

    def test_weak_full_patches_cannot_extend_visual_deadline(self):
        t=self.anchor();anchor=t.anchor_ms
        for i in range(4,24):t.update(np.full_like(frame(),80),i,1000+i*33,*full())
        self.assertEqual(t.anchor_ms,anchor)
        self.assertIsNone(t.template)
        self.assertNotIn("visual",t.update(frame(),24,1792,[],{})[1])


if __name__=="__main__": unittest.main()
