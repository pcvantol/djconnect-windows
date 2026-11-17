#!/usr/bin/env bash
set -euo pipefail

python3 tools/validate-localization.py
python3 tests/test_public_release_identity.py
dotnet run --project tests/DJConnect.Tests/DJConnect.Tests.csproj
