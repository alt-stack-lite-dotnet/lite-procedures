# Установка окружения для разработки (Windows)
# Запуск: .\install.ps1

$ErrorActionPreference = "Stop"

# .NET SDK 10
Write-Host "Checking .NET SDK..."
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Install .NET SDK 10 from https://dotnet.microsoft.com/download"
    exit 1
}
dotnet --version

# Dotnet tools (CSharpier и др.)
Write-Host "Installing dotnet tools..."
dotnet tool restore

# Pre-commit (опционально)
if (Get-Command pre-commit -ErrorAction SilentlyContinue) {
    Write-Host "Installing pre-commit hooks..."
    pre-commit install
} else {
    Write-Host "pre-commit not found. Install: pip install pre-commit (or run 'mise install')"
}

Write-Host "Done. Run: dotnet build, dotnet test, python .config/run_lint.py"
