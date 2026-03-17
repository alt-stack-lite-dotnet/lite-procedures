#!/usr/bin/env bash
# Установка окружения для разработки (Linux/macOS)
set -e

echo "Checking .NET SDK..."
if ! command -v dotnet &>/dev/null; then
  echo "Install .NET SDK 10 from https://dotnet.microsoft.com/download"
  exit 1
fi
dotnet --version

echo "Installing dotnet tools..."
dotnet tool restore

echo "Installing pre-commit hooks (if available)..."
if command -v pre-commit &>/dev/null; then
  pre-commit install
else
  echo "pre-commit not found. Install: pip install pre-commit (or run 'mise install')"
fi

echo "Done. Run: dotnet build, dotnet test, python .config/run_lint.py"
