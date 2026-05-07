# Alien | RimWorld

Alien | RimWorld is a RimWorld mod focused on Alien/Xenomorph-themed content and compatibility integrations for RimWorld 1.6.

## Building from source

The C# projects target .NET Framework 4.8 and build with MSBuild or Visual Studio.

Build the main mod assembly from the repository root:

```sh
msbuild 1.6/Source/XMT.csproj /p:Configuration=Debug
```

On Linux, the .NET SDK can build the project with Mono's .NET Framework reference assemblies:

```sh
dotnet msbuild 1.6/Source/XMT.csproj /p:Configuration=Debug /p:FrameworkPathOverride=/usr/lib/mono/4.8-api
```

The debug build writes the mod DLL to:

```text
1.6/Assemblies/XMT.dll
```

By default, the projects use the existing relative paths for RimWorld and Steam Workshop dependencies. To use local paths without editing tracked project files, create `Directory.Build.local.props` in the repository root:

```xml
<Project>
  <PropertyGroup>
    <RimWorldInstallDir>/path/to/RimWorld/RimWorld*_Data/</RimWorldInstallDir>
    <WorkshopContentDir>/path/to/steamapps/workshop/content/294100/</WorkshopContentDir>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="netstandard">
      <HintPath>$(RimWorldManagedDir)netstandard.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="WindowsBase">
      <HintPath>/usr/lib/mono/4.8-api/WindowsBase.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>
</Project>
```

`Directory.Build.local.props` is ignored by git. You can also override individual assembly folders such as `HarmonyAssembliesDir`, `AlienRaceAssembliesDir`, `VanillaExpandedFrameworkAssembliesDir`, `CombatExtendedAssembliesDir`, or `RimEffectAssembliesDir` if a dependency is installed outside the normal workshop layout.

## Project Layout

- `About/` contains mod metadata.
- `1.6/` contains RimWorld 1.6-specific content.
- `Common/` contains shared content used across versions or load folders.
- `Compatibility/` contains optional integrations and patches for other mods.
- `LoadFolders.xml` controls version and compatibility folder routing.

## Working On Compatibility

Compatibility folders usually mirror another mod, mod ID, or feature area. Before editing one, inspect nearby defs and patches to match local naming, indentation, and patch structure.

Keep changes scoped to the relevant compatibility folder unless shared definitions or load-folder routing clearly need to change.

## Validation

After changing XML, check that touched files are well-formed and search for related def names or patch targets to confirm the change matches existing references.

## License

See `LICENSE`.
