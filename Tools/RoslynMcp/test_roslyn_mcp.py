"""Windows stdio integration/regression tests. Python 3.11+, stdlib only.

Uses saved real repository files without modifying them; freshness tests create
their own external temporary solution. Raw protocol/logs and timings are retained.
"""
import argparse
import ctypes
import json
import hashlib
import os
from pathlib import Path
import queue
import subprocess
import tempfile
import threading
import time
import traceback

ROOT = Path(__file__).resolve().parents[2]


class MemoryCounters(ctypes.Structure):
    _fields_ = [("cb", ctypes.c_ulong), ("PageFaultCount", ctypes.c_ulong)] + [
        (name, ctypes.c_size_t) for name in (
            "PeakWorkingSetSize", "WorkingSetSize", "QuotaPeakPagedPoolUsage",
            "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "QuotaNonPagedPoolUsage",
            "PagefileUsage", "PeakPagefileUsage", "PrivateUsage")]


class Client:
    def __init__(self, command, output, solution):
        self.raw = (output / "protocol.jsonl").open("x", encoding="utf-8")
        self.stderr = (output / "stderr.log").open("x", encoding="utf-8")
        self.process = subprocess.Popen(command, cwd=ROOT, stdin=subprocess.PIPE,
            stdout=subprocess.PIPE, stderr=self.stderr, text=True, encoding="utf-8",
            creationflags=subprocess.CREATE_NO_WINDOW)
        self.messages = queue.Queue()
        self.sequence = 0
        self.samples = []
        self.catalog = {}
        self.solution = solution
        self.reader = threading.Thread(target=self.read, daemon=True)
        self.reader.start()

    def record(self, value):
        self.raw.write(json.dumps(value, ensure_ascii=False) + "\n")
        self.raw.flush()

    def read(self):
        for line in self.process.stdout:
            try:
                self.messages.put(json.loads(line))
            except ValueError:
                self.messages.put({"invalidProtocolLine": line})
        self.messages.put({"eof": True})

    def memory(self):
        counters = MemoryCounters()
        counters.cb = ctypes.sizeof(counters)
        if not ctypes.windll.psapi.GetProcessMemoryInfo(
                ctypes.c_void_p(int(self.process._handle)), ctypes.byref(counters), counters.cb):
            raise ctypes.WinError()
        return {"processId": self.process.pid,
            "workingSetMiB": round(counters.WorkingSetSize / 2**20, 1),
            "privateMiB": round(counters.PrivateUsage / 2**20, 1),
            "peakWorkingSetMiB": round(counters.PeakWorkingSetSize / 2**20, 1)}

    def tree_memory(self):
        # One named scenario snapshot, not polling. Include only this client's tree.
        script = '''$all = Get-CimInstance Win32_Process
$ids = [Collections.Generic.HashSet[int]]::new()
$null = $ids.Add(ROOTPID)
do {
    $added = $false
    foreach ($item in $all) {
        if ($ids.Contains([int]$item.ParentProcessId) -and $ids.Add([int]$item.ProcessId)) { $added = $true }
    }
} while ($added)
@($all | Where-Object { $ids.Contains([int]$_.ProcessId) } | ForEach-Object {
    Get-Process -Id $_.ProcessId -ErrorAction Stop | Select-Object Id,ProcessName,WorkingSet64,PrivateMemorySize64
}) | ConvertTo-Json -Compress'''.replace("ROOTPID", str(self.process.pid))
        sample = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", script],
            capture_output=True, text=True, timeout=30, check=True, creationflags=subprocess.CREATE_NO_WINDOW)
        processes = json.loads(sample.stdout)
        if isinstance(processes, dict):
            processes = [processes]
        return {"processes": processes,
            "sumWorkingSetMiB": round(sum(p["WorkingSet64"] for p in processes) / 2**20, 1),
            "sumPrivateBytesMiB": round(sum(p["PrivateMemorySize64"] for p in processes) / 2**20, 1),
            "scope": "Single snapshot; working-set sum can double-count shared pages; private bytes include nonresident commit"}

    def request(self, method, params, label, timeout=240):
        self.sequence += 1
        request = {"jsonrpc": "2.0", "id": self.sequence, "method": method, "params": params}
        self.record({"request": request, "label": label})
        started = time.perf_counter()
        self.process.stdin.write(json.dumps(request) + "\n")
        self.process.stdin.flush()
        while True:
            remaining = timeout - (time.perf_counter() - started)
            if remaining <= 0:
                raise TimeoutError(label)
            message = self.messages.get(timeout=remaining)
            self.record({"response": message})
            if "invalidProtocolLine" in message or "eof" in message:
                raise AssertionError(f"Protocol failed: {message}")
            if message.get("id") == self.sequence:
                elapsed = round(time.perf_counter() - started, 3)
                self.samples.append({"label": label, "seconds": elapsed, "memory": self.memory()})
                print(f"{label}: {elapsed}s", flush=True)
                if "error" in message:
                    raise AssertionError(message["error"])
                return message["result"]

    def initialize(self):
        self.request("initialize", {"protocolVersion": "2025-11-25", "capabilities": {},
            "clientInfo": {"name": "Cerneala-Roslyn-regression", "version": "1"}}, "initialize")
        self.process.stdin.write(json.dumps({"jsonrpc": "2.0", "method": "notifications/initialized"}) + "\n")
        self.process.stdin.flush()
        self.catalog = {tool["name"]: tool for tool in self.request("tools/list", {}, "catalog")["tools"]}

    def call(self, name, arguments, label=None, expect_error=None):
        arguments = dict(arguments)
        schema = self.catalog[name]["inputSchema"]
        for key in schema.get("required", []):
            if key not in arguments:
                assert "null" in schema["properties"][key]["type"], (name, key)
                arguments[key] = None
        result = self.request("tools/call", {"name": name, "arguments": arguments}, label or name)
        texts = [item["text"] for item in result["content"] if item["type"] == "text"]
        assert len(texts) == 1, result
        payload = json.loads(texts[0])
        if expect_error:
            assert result.get("isError") and payload["error"]["code"] == expect_error, result
            return payload
        assert not result.get("isError") and "error" not in payload, result
        return payload

    def close(self):
        self.process.stdin.close()
        forced = False
        try:
            self.process.wait(timeout=30)
        except subprocess.TimeoutExpired:
            forced = True
            subprocess.run(["taskkill", "/PID", str(self.process.pid), "/T", "/F"],
                capture_output=True, timeout=10, check=True)
            self.process.wait(timeout=10)
        self.reader.join(timeout=10)
        self.process.stdout.close()
        self.stderr.close()
        self.raw.close()
        assert not self.reader.is_alive(), "stdio reader still running"
        assert not forced and self.process.returncode == 0, f"unclean exit {self.process.returncode}"


def position(path, needle, token):
    matches = [(i + 1, line.index(token) + 1 + int(token.startswith(" ")))
        for i, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines()) if needle in line]
    assert len(matches) == 1, (path, needle, matches)
    return {"filePath": str(path), "line": matches[0][0], "column": matches[0][1]}


def generated_regression(client):
    at = position(ROOT / "CernealaPresentation/PresentationWindow.crn.cs",
        "ServoApi.SetId(NextButton,", "NextButton")
    symbol = client.call("hover", at, "generated-NextButton-hover")["result"]["symbol"]
    assert symbol["name"] == "NextButton" and symbol["kind"] == "Property", symbol
    assert symbol["containingType"] == "Cerneala.Presentation.PresentationWindow", symbol
    definitions = client.call("goto_definition", at, "generated-NextButton-definition")["result"]["definitions"]
    assert definitions and all(d["filePath"].endswith(".g.cs") for d in definitions), definitions
    refs = client.call("find_references", {"symbolId": symbol["symbolId"]}, "generated-NextButton-references")["result"]["references"]
    assert any(Path(r["filePath"]) == Path(at["filePath"]) and r["line"] == at["line"] for r in refs), refs


def navigation(client):
    payload = client.call("workspace_symbol", {"query": "TextMeasurer.Measure", "kinds": ["Method"], "maxResults": 20}, "overloads")
    symbols = [s for s in payload["result"]["symbols"] if s.get("containingType") == "Cerneala.UI.Text.TextMeasurer"]
    assert len(symbols) == 2 and len({s["symbolId"] for s in symbols}) == 2, symbols
    path = ROOT / "UI/Text/TextMeasurer.cs"
    at = position(path, "return Measure(text, aspect, new LayoutSize", "Measure")
    target = position(path, "public virtual TextMeasureResult Measure(string text, TextAspect aspect, LayoutSize", " Measure(")
    definitions = client.call("goto_definition", at, "overload-binding")["result"]["definitions"]
    assert any(d["line"] == target["line"] and Path(d["filePath"]) == path for d in definitions), definitions
    implementations = client.call("find_implementations", {"symbolId": "T:Cerneala.Drawing.IDrawingBackend"}, "cross-project-implementations")["result"]["implementations"]
    assert any(d["filePath"].endswith("SdlGpuDrawingBackend.cs") for d in implementations), implementations
    private = "M:Cerneala.UI.Controls.TextBlock.GetTextMeasurer"
    refs = client.call("find_references", {"symbolId": private}, "private-references")["result"]["references"]
    assert len(refs) == 2 and all(r["filePath"].endswith("TextBlock.cs") for r in refs), refs
    callers = client.call("find_callers", {"symbolId": private}, "private-callers")["result"]["callers"]
    assert len(callers) == 2 and all(c["caller"]["containingType"] == "Cerneala.UI.Controls.TextBlock" for c in callers), callers
    analysis = client.call("analyze_symbol", {"symbolId": private}, "composite-analysis")["result"]
    assert analysis["references"]["count"] == 2 and analysis["callers"]["count"] == 2, analysis
    derived = client.call("find_derived_types", {"symbolId": "T:Cerneala.UI.Controls.ContentControl", "transitive": True}, "derived-types")["result"]["derivedTypes"]
    assert any(s["symbolId"] == "T:Cerneala.UI.Controls.Button" for s in derived), derived
    overview = client.call("project_overview", {"maxProjects": 100, "maxProjectReferencesPerProject": 100, "maxPackagesPerProject": 100}, "project-overview")["result"]
    assert Path(overview["solutionPath"]) == client.solution, overview
    presentation = next(p for p in overview["projects"] if p["name"] == "CernealaPresentation")
    assert "Cerneala" in presentation["projectReferences"] and "Cerneala.Backends.SdlGpu" in presentation["projectReferences"], presentation
    float_method = next(s for s in symbols if "System.Single" in s["symbolId"])
    callees = client.call("find_callees", {"symbolId": float_method["symbolId"]}, "callees")["result"]["callees"]
    assert any(c["callee"]["primaryLocation"]["line"] == target["line"] for c in callees), callees
    base_at = position(ROOT / "UI/Controls/Button.cs", "return base.MeasureCore(context)", "MeasureCore")
    definitions = client.call("goto_definition", base_at, "base-override")["result"]["definitions"]
    assert any(d["filePath"].endswith("ContentControl.cs") for d in definitions), definitions
    outline = client.call("list_document_symbols", {"filePath": str(path)}, "document-symbols")["result"]["symbols"]
    assert any(s["name"] == "layoutCache" and s["accessibility"] == "Private" for s in outline), outline
    client.call("hover", {"filePath": str(path), "line": 1, "column": 99999}, "invalid-position", expect_error="POSITION_INVALID")
    client.call("workspace_symbol", {"query": "TextMeasurer.Measure", "kinds": ["Method"], "maxResults": 20}, "warm-overloads")


def freshness(client, output, temporary):
    # All file mutations are in a newly owned, external, unique temporary directory.
    # Main owns its lifetime until after server exit: Windows keeps analyzer DLLs
    # mapped even after a workspace generation is retired.
    if temporary is not None:
        directory = temporary.name
        fixture = Path(directory)
        generator = fixture / "Generator"
        generator.mkdir()
        compiler_version = json.loads((ROOT / "Tools/RoslynMcp/manifest.json").read_text())["roslynVersion"]
        (generator / "Generator.csproj").write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework>'
            '<LangVersion>latest</LangVersion><EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules></PropertyGroup><ItemGroup>'
            f'<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="{compiler_version}" PrivateAssets="all" />'
            '</ItemGroup></Project>', encoding="utf-8")
        (generator / "Generator.cs").write_text('''using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
[Generator] public sealed class ProbeGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var names = context.AdditionalTextsProvider.Where(file => file.Path.EndsWith("name.txt"))
            .Select((file, cancellation) => file.GetText(cancellation).ToString().Trim());
        context.RegisterSourceOutput(names, (output, name) => output.AddSource("Subject.g.cs",
            SourceText.From("namespace Freshness { public partial class Subject { public int " + name + " => 1; } }", Encoding.UTF8)));
    }
}''', encoding="utf-8")
        (fixture / "Probe.csproj").write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>'
            '<ItemGroup><Compile Remove="Generator/**/*.cs" />'
            '<ProjectReference Include="Generator/Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />'
            '<AdditionalFiles Include="name.txt" /></ItemGroup></Project>', encoding="utf-8")
        additional = fixture / "name.txt"
        additional.write_text("GeneratedOne", encoding="utf-8")
        source = fixture / "Subject.cs"
        source.write_text("namespace Freshness; public partial class Subject { public void BeforeSave() {} public int Consume() => GeneratedOne; }", encoding="utf-8")
        solution = fixture / "Probe.slnx"
        solution.write_text('<Solution><Project Path="Probe.csproj" /><Project Path="Generator/Generator.csproj" /></Solution>', encoding="utf-8")
        build = subprocess.run(["dotnet", "build", str(fixture / "Probe.csproj"), "--nologo"], capture_output=True, text=True, timeout=120)
        (output / "fixture-build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
        assert build.returncode == 0, build.stdout + build.stderr
        client.call("reload_workspace", {"solutionPath": str(solution)}, "fixture-load")
        search = lambda query, label: client.call("workspace_symbol", {"query": query, "kinds": None, "maxResults": 20}, label)
        assert search("BeforeSave", "before-save")["result"]["symbols"]
        generated_at = position(source, "public int Consume()", "GeneratedOne")
        assert client.call("hover", generated_at, "fixture-generator-5.9")["result"]["symbol"]["name"] == "GeneratedOne"
        source.write_text("namespace Freshness; public partial class Subject { public void AfterSave() {} public int Consume() => GeneratedOne; }", encoding="utf-8")
        assert search("AfterSave", "after-save")["result"]["symbols"]
        assert not search("BeforeSave", "old-symbol-removed")["result"]["symbols"]
        added = fixture / "Added.cs"
        added.write_text("namespace Freshness; public class AddedAfterLoad {}", encoding="utf-8")
        before = search("AddedAfterLoad", "new-file-before-reload")
        assert not before["result"]["symbols"], before
        client.call("reload_workspace", {}, "new-file-reload")
        assert search("AddedAfterLoad", "new-file-after-reload")["result"]["symbols"]
        additional.write_text("GeneratedTwo", encoding="utf-8")
        source.write_text("namespace Freshness; public partial class Subject { public void AfterSave() {} public int Consume() => GeneratedTwo; }", encoding="utf-8")
        # AdditionalFiles are a reload-required contract; never claim per-call auto-refresh.
        client.call("reload_workspace", {}, "additional-file-reload")
        generated_at = position(source, "public int Consume()", "GeneratedTwo")
        assert client.call("hover", generated_at, "generated-after-additional-save")["result"]["symbol"]["name"] == "GeneratedTwo"
        assert client.call("goto_definition", generated_at, "generated-definition-after-save")["result"]["definitions"]
        # Restore the real solution, but do not delete the loaded analyzer before process exit.
        client.call("reload_workspace", {"solutionPath": str(client.solution)}, "restore-repository")


def launcher_guards(launcher, output):
    manifest = json.loads((ROOT / "Tools/RoslynMcp/manifest.json").read_text())
    results = []
    with tempfile.TemporaryDirectory(prefix="cerneala-roslyn-launch-guard-") as directory:
        environment = dict(os.environ, LOCALAPPDATA=directory)
        command = ["powershell.exe", "-NoLogo", "-NoProfile", "-NonInteractive", "-File", str(launcher)]
        missing = subprocess.run(command, cwd=ROOT, env=environment, input="", capture_output=True,
            text=True, timeout=30, creationflags=subprocess.CREATE_NO_WINDOW)
        assert missing.returncode == 1 and not missing.stdout and "Install-RoslynMcp.ps1" in missing.stderr, missing
        results.append({"scenario": "missing-installation", "exitCode": missing.returncode, "stderr": missing.stderr})
        installation = Path(directory) / "CernealaTools/mcpRoslyn" / (manifest["commit"] + "-" + manifest["variant"])
        installation.mkdir(parents=True)
        (installation / "installation.json").write_text(json.dumps({"commit": "wrong", "variant": manifest["variant"]}), encoding="utf-8")
        mismatch = subprocess.run(command, cwd=ROOT, env=environment, input="", capture_output=True,
            text=True, timeout=30, creationflags=subprocess.CREATE_NO_WINDOW)
        assert mismatch.returncode == 1 and not mismatch.stdout and "does not match" in mismatch.stderr, mismatch
        results.append({"scenario": "mismatched-receipt", "exitCode": mismatch.returncode, "stderr": mismatch.stderr})
    (output / "launcher-guards.json").write_text(json.dumps(results, indent=2), encoding="utf-8")


def main():
    if not __debug__:
        raise RuntimeError("Test assertions must be enabled; do not run Python with -O.")
    parser = argparse.ArgumentParser(description=__doc__)
    launch = parser.add_mutually_exclusive_group(required=True)
    launch.add_argument("--exe", type=Path)
    launch.add_argument("--launcher", type=Path)
    parser.add_argument("--output", type=Path, required=True, help="New, nonexistent evidence directory")
    parser.add_argument("--generated-only", action="store_true")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=False)
    source_hash = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
    if args.launcher:
        launcher_guards(args.launcher, args.output)
    command = [str(args.exe), "--solution", str(ROOT / "Cerneala.slnx")] if args.exe else [
        "powershell.exe", "-NoLogo", "-NoProfile", "-NonInteractive", "-File", str(args.launcher)]
    client = Client(command, args.output, ROOT / "Cerneala.slnx")
    temporary = None if args.generated_only else tempfile.TemporaryDirectory(prefix="cerneala-roslyn-mcp-")
    passed = False
    error = None
    tree_memory = None
    try:
        client.initialize()
        enabled = json.loads((ROOT / "Tools/RoslynMcp/manifest.json").read_text())["enabledTools"]
        assert set(enabled).issubset(client.catalog) and "rename_symbol" not in enabled, enabled
        generated_regression(client)
        tree_memory = client.tree_memory()
        if not args.generated_only:
            navigation(client)
            freshness(client, args.output, temporary)
            generated_regression(client)
        passed = True
    except Exception:
        error = traceback.format_exc()
        print(error, flush=True)
    finally:
        try:
            client.close()
        except Exception:
            passed = False
            error = (error or "") + traceback.format_exc()
        if temporary is not None:
            try:
                temporary.cleanup()
            except Exception:
                passed = False
                error = (error or "") + traceback.format_exc()
        (args.output / "results.json").write_text(json.dumps({"passed": passed,
            "error": error, "samples": client.samples, "exitCode": client.process.returncode,
            "command": command, "testSourceSha256": source_hash,
            "processTreeSnapshot": tree_memory,
            "memoryScope": "Launcher process only" if args.launcher else "Main process only; child BuildHosts excluded"}, indent=2), encoding="utf-8")
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
