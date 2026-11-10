import unittest
from unittest.mock import patch
import numpy as np
from hybrid_visual import HybridFingerVisual
from hybrid_fixtures import image,model
from benchmark_hybrid import evaluate


class HybridTests(unittest.TestCase):
    def anchor(self):
        t=HybridFingerVisual()
        for i in range(4):t.update(image(),i,1000+i*33,*model())
        self.assertIsNotNone(t.anchor,t.checks)
        return t

    def test_paired_image_suite_improves_low_texture_without_added_false_points(self):
        rows=evaluate()
        for seed in (26,99174):
            group=[r for r in rows if r['seed']==seed]
            for row in group:
                self.assertEqual(row['falseContinuations'],0,row)
            old=next(r for r in group if r['scenario']=='low-texture' and r['mode']=='baseline')
            new=next(r for r in group if r['scenario']=='low-texture' and r['mode']=='flow')
            self.assertGreaterEqual(new['valid'],12);self.assertGreater(new['valid'],old['valid'])
            self.assertLess(new['maxErrorPixels'],1)

    def test_model_and_partial_observations_keep_real_geometry_and_never_averaged(self):
        t=self.anchor();points,fields=model(171)
        p,f=t.update(image(171),4,1132,points,fields)
        self.assertIs(p,points);self.assertIs(f,fields);self.assertEqual(t.source,'model')
        sparse=[None]*21;sparse[8]=[172/399,110/279,0]
        partial={'v':2,'partial':True,'points':sparse,'handedness':'right'}
        anchor_time=t.anchor_ms;previous=t.previous
        p,f=t.update(image(172),5,1165,[],partial)
        self.assertIs(f,partial);self.assertEqual(t.source,'partial')
        self.assertIsNot(t.previous,previous);self.assertEqual(t.anchor_ms,anchor_time)
        _,f=t.update(image(173),6,1198,[],{})
        self.assertEqual(f.get('v'),3,t.reason);self.assertNotIn('landmarks',f)
        self.assertNotIn('points',f);self.assertEqual(t.source,'visual')

    def test_returning_conflicting_model_cancels_instead_of_averaging_or_jumping(self):
        t=self.anchor();t.update(image(171),4,1132,[],{})
        p,f=t.update(image(172),5,1165,*model(225))
        self.assertFalse(p);self.assertNotIn('visual',f);self.assertEqual(t.reason,'reacquisition-conflict')
        self.assertIsNone(t.anchor)

    def test_identity_ambiguity_clock_geometry_and_expiry_cancel(self):
        for args in ((image(),4,1132,*model(side='left')),
                     (image(),4,1132,[],{'ambiguous':True}),
                     (image(),3,1132,[],{}),(image(),4,1600,[],{}),
                     (image()[:220],4,1132,[],{})):
            t=self.anchor();_,f=t.update(*args)
            self.assertNotIn('visual',f);self.assertIsNone(t.anchor)
        t=self.anchor()
        for i in range(4,25):_,f=t.update(image(),i,1000+i*33,[],{})
        self.assertNotIn('visual',f);self.assertIsNone(t.anchor)

    def test_cold_background_low_texture_and_stop_do_not_acquire_finger(self):
        t=HybridFingerVisual()
        for i in range(8):
            self.assertNotIn('visual',t.update(image(170+i),i,1000+i*33,[],{})[1])
        t=self.anchor();t.reset('stop')
        self.assertNotIn('visual',t.update(image(),5,1165,[],{})[1])

    def test_occluded_tip_is_rejected_even_if_shaft_remains(self):
        t=self.anchor();_,f=t.update(image(171,occluded=True),4,1132,[],{})
        self.assertNotIn('visual',f);self.assertIsNone(t.anchor)

    def test_conflicting_independent_sources_are_not_averaged_even_before_fallback(self):
        t=self.anchor();p,f=t.update(image(171),4,1132,*model(230))
        self.assertFalse(p);self.assertFalse(f);self.assertEqual(t.reason,'source-conflict')
        self.assertIsNone(t.anchor)

    def test_missing_opencv_falls_back_explicitly_and_baseline_switch_is_reversible(self):
        from visual_backend import create_visual
        from finger_visual import FingerVisual
        with patch.dict('os.environ',{'INVEY_VISUAL_BASELINE':'1'}):self.assertIsInstance(create_visual(),FingerVisual)
        with patch.dict('os.environ',{'INVEY_VISUAL_BASELINE':'0'}):
            with patch.dict('sys.modules',{'hybrid_visual':None}):self.assertIsInstance(create_visual(),FingerVisual)

    def test_fixed_anchor_disagreement_rejects_incremental_drift(self):
        t=self.anchor();t.update(image(171),4,1132,[],{})
        def drift(before,current,points):
            delta=np.array([5 if before is t.previous else 2,0])
            return points,points+delta,np.ones(len(points),bool)
        with patch.object(t,'flow',side_effect=drift):
            _,f=t.update(image(172),5,1165,[],{})
        self.assertNotIn('visual',f);self.assertEqual(t.reason,'drift');self.assertIsNone(t.anchor)

    def test_genuine_loss_cannot_reacquire_without_four_new_model_observations(self):
        t=self.anchor();t.update(image(visible=False),4,1132,[],{})
        self.assertNotIn('visual',t.update(image(),5,1165,[],{})[1])
        for i in range(6,10):t.update(image(),i,1000+i*33,*model())
        self.assertIn('visual',t.update(image(171),10,1330,[],{})[1])

    def test_featureless_pixels_never_create_optical_authority(self):
        t=HybridFingerVisual();blank=np.full_like(image(),120)
        for i in range(4):t.update(blank,i,1000+i*33,*model())
        self.assertIsNone(t.anchor)
        self.assertNotIn('visual',t.update(blank,4,1132,[],{})[1])
