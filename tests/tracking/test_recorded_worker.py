import math
from types import SimpleNamespace
import unittest

from recorded_worker import normalize


class NormalizationTests(unittest.TestCase):
    def test_finite_in_frame_points_and_handedness_survive(self):
        points = [SimpleNamespace(x=0.35, y=0.65, z=-0.02)] * 21
        classification = SimpleNamespace(category_name="Right", score=0.8)
        observed, fields = normalize(SimpleNamespace(hand_landmarks=[points], handedness=[[classification]]))
        self.assertEqual(observed, [[0.35, 0.65, -0.02]] * 21)
        self.assertEqual(fields, {"handedness": "right", "quality": {"handednessScore": 0.8}})

    def test_edge_prediction_is_partial_but_nonfinite_is_invalid(self):
        points = [SimpleNamespace(x=0.5, y=0.5, z=0)] * 20
        edge = points + [SimpleNamespace(x=-0.01, y=0.3, z=0)]
        observed, fields = normalize(SimpleNamespace(hand_landmarks=[edge], handedness=[]))
        self.assertEqual(observed, [])
        self.assertEqual(fields["v"], 2)
        self.assertTrue(fields["partial"])
        self.assertIsNone(fields["points"][20])
        self.assertEqual(fields["points"][8], [0.5, 0.5, 0])
        corrupt = points + [SimpleNamespace(x=0.5, y=0.5, z=math.nan)]
        with self.assertRaisesRegex(ValueError, "Invalid hand model geometry"):
            normalize(SimpleNamespace(hand_landmarks=[corrupt], handedness=[]))

    def test_missing_ambiguous_and_malformed_hands_are_distinct(self):
        point = SimpleNamespace(x=0.5, y=0.5, z=0)
        self.assertEqual(normalize(SimpleNamespace(hand_landmarks=[], handedness=[])), ([], {}))
        self.assertEqual(normalize(SimpleNamespace(hand_landmarks=[[point] * 21] * 2, handedness=[])), ([], {"ambiguous": True}))
        with self.assertRaises(ValueError):
            normalize(SimpleNamespace(hand_landmarks=[[point] * 20], handedness=[]))

    def test_unavailable_quality_is_omitted(self):
        points = [SimpleNamespace(x=0.35, y=0.65, z=-0.02)] * 21
        for score in (math.nan, 1.5):
            category = SimpleNamespace(category_name="Right", score=score)
            _, fields = normalize(SimpleNamespace(hand_landmarks=[points], handedness=[[category]]))
            self.assertEqual(fields, {})


if __name__ == "__main__":
    unittest.main()
