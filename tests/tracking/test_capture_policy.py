import unittest,ctypes as c
import numpy as np
from capture_policy import ranked_modes
from windows_camera import copy_rgb,copy_bgr,CameraError
from frame_pipeline import Frame,to_srgb

class CapturePolicyTests(unittest.TestCase):
    def test_only_offered_valid_thirty_hertz_modes_win(self):
        modes=[dict(width=640,height=480,fps=10),dict(width=1280,height=720,fps=30),dict(width=640,height=480,fps=30),dict(width=640,height=480,fps=float('nan')),dict(width=640,height=480,fps=60,supported=False)]
        self.assertEqual(ranked_modes(modes),[2,1,0])
        self.assertEqual(ranked_modes([dict(width=1920,height=1080,fps=30)]),[0])
    def test_direct_rgb_matches_baseline_signed_stride_and_owns_memory(self):
        for stride in (16,-16):
            memory=(c.c_ubyte*32)(*range(32));start=c.addressof(memory);first=start+(16 if stride<0 else 0)
            rgb=copy_rgb(np,3,2,start,32,first,stride)
            baseline=to_srgb(Frame(copy_bgr(np,3,2,start,32,first,stride),1,True),np)
            np.testing.assert_array_equal(rgb,baseline);self.assertTrue(rgb.flags.c_contiguous)
            before=rgb.copy();memory[0]=255;np.testing.assert_array_equal(rgb,before)
    def test_invalid_native_span_rejected_before_copy(self):
        memory=(c.c_ubyte*8)();p=c.addressof(memory)
        with self.assertRaises(CameraError):copy_rgb(np,3,2,p,8,p,12)
