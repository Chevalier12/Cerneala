"""Regression tests for the --exe mode of test_roslyn_mcp.py. Python 3.11+, stdlib only.

Run from the repository root: python ./Tools/RoslynMcp/test_exe_environment.py
"""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import test_roslyn_mcp as harness  # noqa: E402


def make_dotnet(root, fxr_versions):
    """A fake dotnet folder: dotnet.exe plus host/fxr/<version> folders; True means hostfxr.dll exists."""
    root.mkdir(parents=True)
    (root / "dotnet.exe").write_bytes(b"")
    for version, has_dll in fxr_versions.items():
        folder = root / "host" / "fxr" / version
        folder.mkdir(parents=True)
        if has_dll:
            (folder / "hostfxr.dll").write_bytes(b"")
    return root


class ExeEnvironmentTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="cerneala-roslyn-exe-env-")
        self.root = Path(self.directory.name)

    def tearDown(self):
        self.directory.cleanup()

    def test_prepends_newest_hostfxr_folder_of_first_dotnet_on_path(self):
        unrelated = self.root / "unrelated"
        unrelated.mkdir()
        first = make_dotnet(self.root / "first", {
            "8.0.11": True, "10.0.2": True, "10.0.0-rc.1.25451.107": True, "11.0.0": False})
        second = make_dotnet(self.root / "second", {"12.0.0": True})
        path = os.pathsep.join([str(unrelated), str(first), str(second)])
        source = {"PATH": path, "OTHER": "kept"}

        environment = harness.exe_environment(source)

        newest = first / "host" / "fxr" / "10.0.2"
        self.assertEqual(environment["PATH"], str(newest) + os.pathsep + path)
        self.assertEqual(environment["OTHER"], "kept")
        self.assertEqual(source["PATH"], path, "the caller's environment must not change")

    def test_missing_hostfxr_is_a_setup_error(self):
        dotnet = make_dotnet(self.root / "dotnet", {"10.0.2": False})
        with self.assertRaisesRegex(harness.SetupError, "hostfxr.dll"):
            harness.exe_environment({"PATH": str(dotnet)})

    def test_missing_dotnet_is_a_setup_error(self):
        with self.assertRaisesRegex(harness.SetupError, "dotnet"):
            harness.exe_environment({"PATH": str(self.root)})

    def test_harness_reports_setup_error_instead_of_protocol_failure(self):
        dotnet = make_dotnet(self.root / "dotnet", {"10.0.2": False})
        environment = dict(os.environ, PATH=str(dotnet))
        result = subprocess.run([sys.executable, str(Path(harness.__file__)),
            "--exe", str(self.root / "missing.exe"), "--output", str(self.root / "evidence")],
            env=environment, capture_output=True, text=True, timeout=60)
        self.assertEqual(result.returncode, 1, result)
        self.assertIn("hostfxr.dll", result.stderr, result)
        self.assertNotIn("Protocol failed", result.stdout + result.stderr, result)

    def test_installed_server_passes_initialize_in_exe_mode(self):
        manifest = json.loads((harness.ROOT / "Tools/RoslynMcp/manifest.json").read_text())
        exe = (Path(os.environ["LOCALAPPDATA"]) / "CernealaTools/mcpRoslyn"
            / f"{manifest['commit']}-{manifest['variant']}" / "publish/mcpRoslyn.exe")
        if not exe.is_file():
            self.skipTest(f"Roslyn MCP is not installed: {exe}")
        output = self.root / "evidence"
        output.mkdir()
        client = harness.Client([str(exe), "--solution", str(harness.ROOT / "Cerneala.slnx")],
            output, harness.ROOT / "Cerneala.slnx", harness.exe_environment(os.environ))
        try:
            client.initialize()
            client.close()
        except Exception as error:
            if client.process.poll() is None:
                client.process.kill()
            client.process.wait(timeout=30)
            client.reader.join(timeout=10)
            client.process.stdout.close()
            client.stderr.close()
            client.raw.close()
            stderr = (output / "stderr.log").read_text(encoding="utf-8").splitlines()
            self.fail(f"{error!r}; server stderr: {stderr[:1]}")
        self.assertTrue(set(manifest["enabledTools"]).issubset(client.catalog), client.catalog.keys())


if __name__ == "__main__":
    unittest.main()
