# LivingAtlas Antigravity Rules

## Project

LivingAtlas is a desktop-first C#/.NET Avalonia map editor for tabletop RPG maps.

## Current stable state

The project was recovered from working binaries after a Codex worktree incident and then stabilized.

Current status:
- Git works.
- GitHub is updated.
- bin/obj are not tracked.
- Build passes with 0 warnings and 0 errors.
- Tests pass: 18/18.
- Manual UI smoke test passed.

## Tech stack

- C#
- .NET 10
- Avalonia UI
- MVVM
- xUnit
- System.Text.Json
- SkiaSharp dependency exists in Rendering, but current viewport drawing is in Desktop.

## Architecture rules

- Domain must not reference Avalonia.
- Domain must not reference SkiaSharp.
- Domain must not use file system APIs.
- Domain must not use AI APIs.
- UI code belongs in LivingAtlas.Desktop.
- Editor logic belongs in LivingAtlas.Editor.
- Save/load belongs in LivingAtlas.ProjectSystem.
- Export belongs in LivingAtlas.Export.
- AI integrations belong in LivingAtlas.AI.

## Required checks

Before reporting success, run:

```powershell
$env:AVALONIA_TELEMETRY_OPTOUT='1'
dotnet build
dotnet test .\tests\LivingAtlas.Tests\LivingAtlas.Tests.csproj --no-restore --verbosity normal
```

Expected:

build: 0 warnings, 0 errors
tests: all pass

## Recovery safety rules
- Do not touch backup folders.
- Do not use old Codex worktrees.
- Do not copy folders manually over the repo.
- Do not add bin/obj to Git.
- Do not change JSON format unless explicitly requested.
- Do not do large refactors without approval.
- Do not add new features during cleanup tasks.

## Implemented through Phase 22
- Avalonia editor shell
- viewport with pan/zoom/grid
- MapDocument display
- ProjectSystem JSON save/load
- map objects: DistrictShape, RoadLine, PointOfInterest, MapLabel
- selection + Inspector
- move tool
- undo/redo
- tool modes: SelectMove, Pan, District, Road, POI, Label
- POI creation tool
- Label creation tool
- Road drawing tool
- District polygon tool
- Delete
- Save/Open UI
- Dirty state
- Child maps
- Child map preview
- Breadcrumb navigation
- Inspector editing Name/Text
- Hotkey guard while editing Inspector TextBox

## Do not implement yet unless explicitly requested
- AI generation
- asset library
- Foundry export
- semantic zoom
- cached previews
- region/world maps
- full rendering layer extraction
- advanced style editor
