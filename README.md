# Living Atlas

Living Atlas is a desktop-first C#/.NET map editor for tabletop RPG campaigns.

The long-term goal is to support nested living maps:

region -> city -> district -> building -> battle map

The first milestone is to create a city map, open a district as a child map, edit it, and show it back on the city map as a scaled preview.

## Stack

- C#
- .NET 10
- Avalonia UI
- SkiaSharp
- MVVM
- xUnit

## Build

```bash
dotnet build
```

## Test

```bash
dotnet test
```

## Run

```bash
dotnet run --project src/LivingAtlas.Desktop/LivingAtlas.Desktop.csproj
```
