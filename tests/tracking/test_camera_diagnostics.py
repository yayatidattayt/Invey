import json
from pathlib import Path
import tempfile
import unittest

from camera_diagnostics import CameraDiagnostics


class CameraDiagnosticsTests(unittest.TestCase):
    def test_reports_distinct_observations_without_landmarks_or_device_identity(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "counts.json"
            diagnostic = CameraDiagnostics(path, "test-session")
            for fields in ({"tracked": True, "landmarks": [[.5, .5, 0]]},
                           {"tracked": False, "partial": True},
                           {"tracked": False, "ambiguous": True},
                           {"tracked": False}):
                diagnostic.result(fields)
            diagnostic.finish()
            data = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(data["results"], {"hand": 1, "partial": 1, "ambiguous": 1, "empty": 1})
            self.assertNotIn("landmarks", data)
            self.assertNotIn("device", data)
