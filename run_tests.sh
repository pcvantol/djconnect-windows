#!/usr/bin/env bash
set -euo pipefail

python3 tools/validate-localization.py
dotnet run --project tests/DJConnect.Tests/DJConnect.Tests.csproj
