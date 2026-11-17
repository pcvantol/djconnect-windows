"""Exercise the exact public-release workflow identity gates without GitHub access."""

import json
import os
from pathlib import Path
import re
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[1]
ORG_RELEASES = "djconnect-music-dj/djconnect-app-releases"
OLD_RELEASES = "pcvantol/djconnect-app-releases"


class PublicReleaseIdentityTests(unittest.TestCase):
    def test_access_workflow_filters_actual_repository_metadata(self):
        workflow = (ROOT / ".github/workflows/public-release-access.yml").read_text()
        match = re.search(r"gh api repositories/1265963920 \| jq -e '([^']+)'", workflow)
        self.assertIsNotNone(match, "access check must query the fixed repository ID")
        for repository_id, visibility, name, push, accepted in (
            (1265963920, "public", ORG_RELEASES, True, True),
            (1265963921, "public", ORG_RELEASES, True, False),
            (1265963920, "private", ORG_RELEASES, True, False),
            (1265963920, "public", OLD_RELEASES, True, False),
            (1265963920, "public", ORG_RELEASES, False, False),
        ):
            with self.subTest(repository_id=repository_id, visibility=visibility, name=name, push=push):
                payload = {"id": repository_id, "visibility": visibility, "full_name": name, "permissions": {"push": push}}
                result = subprocess.run(["jq", "-e", match.group(1)], input=json.dumps(payload), text=True, capture_output=True, check=False)
                self.assertEqual(result.returncode == 0, accepted)

    def test_publisher_rejects_old_and_unexpected_destinations(self):
        workflow = (ROOT / ".github/workflows/public-unsigned-release.yml").read_text()
        self.assertIn("repositories/1265963920", workflow)
        match = re.search(r'case "\$PUBLIC_REPO" in\n(.*?)\n\s+esac', workflow, re.S)
        self.assertIsNotNone(match, "publisher must use a fail-closed repository selector")
        for name, accepted in ((ORG_RELEASES, True), (OLD_RELEASES, False), ("unexpected/repository", False), ("", False)):
            with self.subTest(name=name):
                result = subprocess.run(
                    ["bash", "-c", 'case "$PUBLIC_REPO" in\n' + match.group(1) + '\nesac'],
                    env={**os.environ, "PUBLIC_REPO": name}, capture_output=True, text=True, check=False,
                )
                self.assertEqual(result.returncode == 0, accepted)


if __name__ == "__main__":
    unittest.main()
