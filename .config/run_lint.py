#!/usr/bin/env python3
"""
Локальный запуск проверок: формат, сборка, тесты.
Использование:
  python .config/run_lint.py          # все проверки
  python .config/run_lint.py format  # только формат
  python .config/run_lint.py build   # только сборка
  python .config/run_lint.py test    # только тесты
"""
import subprocess
import sys
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def run(cmd: list[str], step: str) -> bool:
    r = subprocess.run(cmd, cwd=ROOT)
    if r.returncode != 0:
        print(f"[FAIL] {step}", file=sys.stderr)
        return False
    print(f"[OK] {step}")
    return True


def main() -> int:
    os.chdir(ROOT)
    steps = sys.argv[1:] if len(sys.argv) > 1 else ["format", "build", "test"]
    ok = True
    if "format" in steps:
        ok = run(
            ["dotnet", "format", "Lite.Procedures.sln", "--verify-no-changes", "--verbosity", "diagnostic"],
            "dotnet format",
        ) and ok
    if "build" in steps:
        ok = run(["dotnet", "build", "Lite.Procedures.sln", "-c", "Release"], "dotnet build") and ok
    if "test" in steps:
        ok = run(["dotnet", "test", "Lite.Procedures.sln", "-c", "Release", "--no-build"], "dotnet test") and ok
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
