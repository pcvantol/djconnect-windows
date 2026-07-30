# TDE 1.1.1 Dependency-Health Review

Status: documented compatibility blockers

## Scope

This review uses the public `technical-debt-engine-runtime==1.1.1` contract,
the `standard` profile and the existing observe-mode configuration unchanged.
It records the dependency-health inventory produced by:

```sh
dotnet list src/DJConnect.Windows/DJConnect.Windows.csproj package --outdated --include-transitive --format json
```

The assessment reports `dependency_health.outdated_dependencies = 15`. All
15 packages are transitive; the app's direct MAUI dependency is
`Microsoft.Maui.Controls` `10.0.90`.

## Compatibility decision

`Microsoft.Maui.Controls` `10.0.90` is the newest stable MAUI 10 package
available to this project. Its Windows target deliberately pins
`Microsoft.WindowsAppSDK` `1.8.260508005`, `Microsoft.Web.WebView2`
`1.0.3179.45`, `Microsoft.Graphics.Win2D` `1.3.2` and
`Microsoft.Windows.SDK.BuildTools` `10.0.26100.4654`.

The package manager advertises later targets for all packages below. The
WindowsAppSDK targets are 2.x major versions, including the components that
must remain mutually version-aligned. Adding direct overrides would bypass
the MAUI package's tested graph; it is not a safe compatible update. No
dependency, lockfile or source-code change is made for this review.

Revisit this inventory when a stable MAUI 10 servicing release adopts the
corresponding WindowsAppSDK versions, or when an explicitly scoped MAUI /
WindowsAppSDK major-upgrade project has compatibility validation for both
Windows and Mac Catalyst.

## Remaining package inventory

| Package | Resolved | Package-manager target | Kind | Compatibility blocker |
| --- | --- | --- | --- | --- |
| Microsoft.Graphics.Win2D | 1.3.2 | 1.4.0 | Transitive | MAUI 10.0.90 Windows graph pins 1.3.2. |
| Microsoft.Web.WebView2 | 1.0.3179.45 | 1.0.4078.44 | Transitive | MAUI 10.0.90 Windows graph pins 1.0.3179.45. |
| Microsoft.Windows.SDK.BuildTools | 10.0.26100.4654 | 10.0.28000.2526 | Transitive | MAUI 10.0.90 Windows graph pins 10.0.26100.4654. |
| Microsoft.Windows.SDK.BuildTools.MSIX | 1.7.20250829.1 | 1.7.260610101 | Transitive | Transitively selected by the MAUI-pinned WindowsAppSDK graph. |
| Microsoft.WindowsAppSDK | 1.8.260508005 | 2.3.1 | Transitive | Major upgrade; MAUI pins 1.8.260508005. |
| Microsoft.WindowsAppSDK.AI | 1.8.76 | 2.3.4 | Transitive | WindowsAppSDK component; must remain aligned with the 1.8 graph. |
| Microsoft.WindowsAppSDK.Base | 1.8.251216001 | 2.0.4 | Transitive | WindowsAppSDK component; target is a major upgrade. |
| Microsoft.WindowsAppSDK.DWrite | 1.8.25122902 | 2.1.0 | Transitive | WindowsAppSDK component; target is a major upgrade. |
| Microsoft.WindowsAppSDK.Foundation | 1.8.260505001 | 2.3.5 | Transitive | WindowsAppSDK component; target is a major upgrade. |
| Microsoft.WindowsAppSDK.InteractiveExperiences | 1.8.260430001 | 2.1.3 | Transitive | WindowsAppSDK component; target is a major upgrade. |
| Microsoft.WindowsAppSDK.ML | 1.8.2197 | 2.1.74 | Transitive | WindowsAppSDK component; target is a major upgrade. |
| Microsoft.WindowsAppSDK.Runtime | 1.8.260508005 | 2.3.1 | Transitive | WindowsAppSDK component; target is a major upgrade. |
| Microsoft.WindowsAppSDK.Widgets | 1.8.251231004 | 2.0.5 | Transitive | WindowsAppSDK component; target is a major upgrade. |
| Microsoft.WindowsAppSDK.WinUI | 1.8.260505002 | 2.3.2 | Transitive | WindowsAppSDK component; target is a major upgrade. |
| System.Numerics.Tensors | 9.0.0 | 10.0.10 | Transitive | Introduced through the MAUI-pinned Windows graph; no standalone direct reference exists to update safely. |

## TDE outcome

The existing observe workflow remains non-blocking and unchanged. Its CI run
for this review produced the existing `tde-observe-evidence` artifact with:

- runtime `technical-debt-engine-runtime==1.1.1`;
- profile `standard`;
- `code_size`, `complexity`, `coverage` and `dependency_health` executed;
- assessment decision `PASS_WITH_WARNINGS`;
- repository qualification `QUALIFIED`; and
- `dependency_health.outdated_dependencies = 15`.

No policy, schema, runtime, required-check or workflow-mode exception has
been introduced.
