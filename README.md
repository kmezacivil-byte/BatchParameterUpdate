# Batch Parameter Update

A Revit add-in that updates a single text instance parameter across every
currently selected element, reporting exactly which elements were updated,
left unchanged, or skipped - and why.

## Prerequisites

- Autodesk Revit 2025
- .NET 8 SDK (or newer; newer SDKs can still build a `net8.0` target - see
  *Assumptions & limitations*)
- Visual Studio 2022 17.8+ (earlier 17.x versions do not support .NET 8
  targets)

## Supported Revit version(s)

**Revit 2025 only.** The add-in was built and tested exclusively against
Revit 2025.3. No other version was installed or exercised during development,
so no other version is claimed compatible.

`RevitVersion` and `RevitApiPackageVersion` are centralized in
`Directory.Build.props`, so retargeting a different Revit year is a
single-line change - but that change has not been made or tested.

## Build

```
dotnet restore
dotnet build -c Release
```

Or open `BatchParameterUpdate.sln` in Visual Studio and Build → Rebuild
Solution. Building in `Debug` configuration also deploys the add-in directly
to `%AppData%\Autodesk\Revit\Addins\2025\` (see `DeployToRevit` target in
`BatchParameterUpdate.Revit.csproj`), so Revit picks it up immediately for
local development without running the installer.

## Test

```
dotnet test
```

Runs the unit test suite for `BatchParameterUpdate.Core` (11 test cases across
2 files). See *Design decisions* below for what is - and is not - covered.

## Install

1. Download the installer from the
   [v1.0.0 Release](https://github.com/kmezacivil-byte/BatchParameterUpdate/releases/tag/v1.0.0),
   or build it yourself: build in `Release` configuration first (see *Build*),
   then open `installer/BatchParameterUpdate.iss` with Inno Setup and compile
   it.
2. Run `BatchParameterUpdate-Setup-1.0.0.exe`.
   - Installs per-user, no administrator privileges required.
   - Close Revit before installing; the installer blocks if `Revit.exe` is
     detected running, since the add-in files would otherwise be locked.
   - If Revit 2025 is not found at its default install path, the installer
     warns but allows continuing (see *Assumptions & limitations*).
3. Uninstall from Windows Settings → Apps, same as any other application.

The installer and the add-in DLL are **not code-signed**. On first launch
after installing, Revit will show an "Unsigned add-in" security prompt -
choose "Load Once" or "Always Load". This is expected for an evaluation
build, not a defect.

## Usage

1. In Revit, select one or more model elements.
2. Run **Batch Parameter Update** from the Add-Ins ribbon panel.
3. Enter the parameter name and the new value, then click OK.
4. A summary dialog reports how many elements were updated, left unchanged,
   or skipped, broken down by reason.

If nothing is selected when the command runs, or the active document is a
family document or read-only, the command reports this and exits without
opening the input dialog.

## Design decisions

**Core has zero reference to the Revit API.** `Autodesk.Revit.DB.Element` and
`Parameter` are sealed / non-virtual and expose no interfaces, so they cannot
be mocked. Rather than wrap ~40 Revit types in custom interfaces purely to
enable mocking - which would add a large layer of indirection and produce
tests that mostly verify the wrapper itself - the classification enums
(`SkipReason`, `UpdateOutcome`), the result DTO (`ElementOutcome`, using
`long` for the element id instead of `Autodesk.Revit.DB.ElementId`), the input
validation (`ParameterUpdateRequest`), and the aggregation logic
(`BatchUpdateResult`) were isolated into a project with no Revit dependency
at all. These are covered by 11 unit tests. `ParameterResolver`, which does
touch live `Element`/`Parameter` instances, is verified manually against a
real Revit session instead, and is deliberately excluded from the test
project for the same reason.

**`GetParameters(name)` instead of `LookupParameter(name)`.** Autodesk's own
API documentation notes that when multiple parameters share a display name,
`LookupParameter` returns one of them and that choice is effectively
arbitrary. A shared parameter and a project parameter with the same name on
the same category is a realistic scenario, not an edge case invented for this
exercise. `GetParameters` returns every match; if there is more than one, the
element is skipped as `AmbiguousParameterName` rather than guessing which
parameter to write.

**Parameter name matching is exact and case-sensitive**
(`Definition.Name` string match). No official Autodesk documentation states a
single, consistent case-sensitivity rule across the API - `FamilyManager.
get_Parameter` is documented elsewhere as case-insensitive, while
`Element.GetParameters` gives no such guarantee - so behavior was made
explicit in this add-in's own code rather than relying on undocumented,
possibly inconsistent framework behavior.

**One transaction for the entire batch, not one per element.** Per-element
failures (missing parameter, read-only, ambiguous name, etc.) are
pre-validated inside `ParameterResolver` and returned as a `Skipped` outcome;
they never throw, so they never affect the transaction. Only a genuine,
unexpected exception rolls back the entire batch. This satisfies the
requirement that the model is never left partially modified: skips do not
break atomicity, but a real failure aborts everything rather than leaving a
half-updated selection.

**`Unchanged` is a distinct outcome from `Updated`.** Writing back a value
that already matches is not a failure, but counting it as an update would
overstate how many elements actually changed.

**No `IExternalCommandAvailability` on the ribbon button.** Disabling the
button on an empty selection would hide the "no elements selected" feedback
path that this add-in is required to handle - the button stays enabled and
the empty-selection message is shown when the command runs instead.

**The `.addin` manifest declares only an `Application` entry, not a separate
`Command` entry.** An earlier version declared both, as a fallback path
through Add-Ins → External Tools. Testing in Revit showed this caused the
"unsigned add-in" security prompt to appear twice on startup, since Revit
evaluates trust once per manifest entry referencing an assembly - even when
both entries point at the same DLL. The ribbon button registered in
`App.OnStartup` already provides tested, reliable access to the command, so
the redundant entry was removed.

**No external MVVM library.** The input dialog has two fields and two
buttons. A ~15-line `RelayCommand` implementing `ICommand` was written by
hand instead of adding CommunityToolkit.Mvvm or a similar package - one less
dependency to justify in the installer, for a problem that small amount of
code already solves. The dialog's validation is not reimplemented in the
ViewModel: it calls `ParameterUpdateRequest.IsValid`, the same rule already
covered by unit tests, so the UI and the tests cannot silently disagree.

**Installer installs per-user (`%AppData%`), not per-machine
(`%ProgramData%`).** This matches the folder the `DeployToRevit` MSBuild
target already uses for local development, and avoids a UAC elevation prompt
for what is expected to be a single-user evaluation install.

**Installer detects Revit by checking the default install folder, not the
Windows registry.** Simpler to write and to verify by hand, at the cost of
missing installations at a non-default path - judged an acceptable trade-off
since only one Revit version and installation pattern was ever tested. If not
found, the installer warns and asks the user whether to continue, rather than
blocking outright - the check is a heuristic, not a guarantee.

## Assumptions & limitations

- Only text-type (`StorageType.String`) instance parameters are supported, as
  specified. Numeric or type parameters are always skipped, not converted.
- The full list of skip reasons, and how each is detected: not a model
  element instance (e.g. an `ElementType`), parameter not found, parameter
  exists on the type only, ambiguous parameter name, not a text parameter,
  read-only parameter, Revit rejected the write, or element not owned
  (worksharing).
- Worksharing/ownership failures are handled defensively
  (`Autodesk.Revit.Exceptions.ApplicationException` is caught and reported as
  a skip), but this was not exercised against an actual multi-user workshared
  model.
- No `global.json` pins the .NET SDK version. Considered, but omitted since
  this is a single-developer submission with no CI pipeline that would
  benefit from a reproducible, pinned SDK version.
- The installer's Revit-running check shells out to `tasklist.exe` rather
  than using a Windows API call, to avoid adding a P/Invoke declaration to
  the installer script for a single boolean check.
- The installer and add-in binaries are unsigned (see *Install*).

## Considered and intentionally excluded

These were evaluated and deliberately left out, to keep effort inside the
declared scope rather than adding functionality beyond what was requested:

- **Autocomplete for parameter names** from the current selection - out of
  scope; the exercise specifies a manually entered name.
- **Preview of changes before applying** - would require a second pass over
  the selection and a second UI surface for a batch operation expected to be
  reviewed via its result summary instead.
- **Undo/logging to a file** - Revit's own Undo already reverts the
  transaction; a separate log was judged unnecessary duplication.
- **Support for numeric or type parameters** - explicitly out of scope (see
  *Assumptions & limitations*).
- **Multi-targeting multiple Revit versions** - `RevitVersion` is centralized
  and the change would be small, but was not made or tested, so it is not
  claimed as supported.
