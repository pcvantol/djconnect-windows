# TDE 1.1.1 Dependency-Health Review

Status: WindowsAppSDK 2.3 upgrade under validation

## Scope

This review uses the public `technical-debt-engine-runtime==1.1.1` contract,
the `standard` profile and the existing observe-mode configuration unchanged.
It records the dependency-health inventory produced by:

```sh
dotnet list src/DJConnect.Windows/DJConnect.Windows.csproj package --outdated --include-transitive --format json
```

Before the update, the assessment reported
`dependency_health.outdated_dependencies = 15`. All 15 packages were
transitive from the MAUI Windows graph. After the Windows-only direct package
update below, the package-manager inventory reports zero outdated packages.

## Compatibility decision

The Windows target now explicitly uses the WindowsAppSDK 2.3 set. This is an
intentional Windows-only major upgrade: Mac Catalyst remains on the MAUI 10
graph. NuGet restore resolves this set without package downgrade or conflict
warnings; the Windows CI build is the required runtime compatibility gate.

## Updated Windows package inventory

| Package | Updated version |
| --- | --- |
| Microsoft.Graphics.Win2D | 1.4.0 |
| Microsoft.Web.WebView2 | 1.0.4078.44 |
| Microsoft.Windows.SDK.BuildTools | 10.0.28000.2526 |
| Microsoft.Windows.SDK.BuildTools.MSIX | 1.7.260610101 |
| Microsoft.Windows.AI.MachineLearning | 2.2.12 |
| Microsoft.WindowsAppSDK | 2.3.1 |
| Microsoft.WindowsAppSDK.WinUI | 2.3.2 |
| System.Numerics.Tensors | 10.0.10 |

## TDE outcome

The existing observe workflow remains non-blocking and unchanged. It retains
runtime `technical-debt-engine-runtime==1.1.1` and the `standard` profile. The
PR CI run must publish the existing `tde-observe-evidence` artifact and record
the final assessment and qualification result. No policy, schema, runtime,
required-check or workflow-mode exception has been introduced.
