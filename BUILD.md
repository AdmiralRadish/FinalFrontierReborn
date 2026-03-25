# FinalFrontierReborn — Build & Deploy

## Overview

Crew ribbon/achievement system for KSP. Awards ribbons to Kerbals for milestones (first orbit, Mun landing, etc.). Includes custom ribbon packs and planet pack support.

## Prerequisites

- Visual Studio or MSBuild with .NET Framework 4.5 targeting pack
- KSP managed assemblies (Assembly-CSharp.dll, UnityEngine DLLs)

## Setup

The `.csproj` has hardcoded references to a KSP 1.9.1 development path. Override by creating `FinalFrontier.csproj.user`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <KSPDIR>G:\Steam\steamapps\common\Kerbal Space Program</KSPDIR>
  </PropertyGroup>
</Project>
```

Or set the `KSPDIR` environment variable before building.

**Note:** The assembly reference HintPaths in the `.csproj` still point to `1.9.1-0_development` relative paths. If building with `KSPDIR`, you may need to update these HintPaths to use `$(KSPDIR)\KSP_x64_Data\Managed\` instead.

## Build

```powershell
cd FinalFrontierReborn
msbuild FinalFrontierReborn.sln /p:Configuration=Release /p:KSPDIR="G:\Steam\steamapps\common\Kerbal Space Program"
```

Output: `bin\Release\FinalFrontier.dll`

## Deploy

Manual deployment to KSP GameData:

```powershell
$KSP = "G:\Steam\steamapps\common\Kerbal Space Program"
$Dest = "$KSP\GameData\FinalFrontierReborn"

# Create plugin folder
New-Item -Path "$Dest\Plugins" -ItemType Directory -Force

# DLL
Copy-Item "bin\Release\FinalFrontier.dll" "$Dest\Plugins\" -Force

# Config files
Copy-Item "cfg\*" "$Dest\" -Recurse -Force

# Ribbons and ribbon packs
Copy-Item "Ribbons" "$Dest\Ribbons" -Recurse -Force
Copy-Item "RibbonPacks" "$Dest\RibbonPacks" -Recurse -Force

# Planet pack configs
Copy-Item "PlanetPacks" "$Dest\PlanetPacks" -Recurse -Force

# Text resources
Copy-Item "txt" "$Dest\txt" -Recurse -Force
```

## Deployed Files

| Path | Purpose |
|------|---------|
| `Plugins\FinalFrontier.dll` | Main plugin assembly |
| `cfg\` | Part configs and settings |
| `Ribbons\` | Stock ribbon images |
| `RibbonPacks\` | Additional ribbon packs |
| `PlanetPacks\` | Planet-pack-specific ribbon configs |
| `txt\` | Localization / text resources |

## Notes

- This is an older-style `.csproj` (ToolsVersion 4.0), not SDK-style. Requires MSBuild or Visual Studio.
- Target framework is .NET 4.5 — older than most KSP mods but still compatible.
- The `.csproj` HintPaths may need manual editing for your environment.
