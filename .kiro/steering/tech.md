# Technology Stack

## Framework & Language
- .NET 8.0 (C#)
- MonoGame Framework 3.8.4 (DesktopGL)
- Target Platform: Desktop (Windows, Linux, macOS via DesktopGL)

## Build System
- MSBuild via .NET SDK
- MonoGame Content Builder for asset pipeline

## Key Libraries
- MonoGame.Framework.DesktopGL (3.8.4)
- MonoGame.Content.Builder.Task (3.8.4)

## Common Commands

### Build
```
dotnet build
```

### Run
```
dotnet run
```

### Restore Tools
```
dotnet tool restore
```

### Content Pipeline
Content is managed through `Content/Content.mgcb` using the MonoGame Content Pipeline. Assets are automatically built during project compilation.

## Asset Pipeline
- Textures: Use PNG format, processed with TextureImporter
- Fonts: Use SpriteFont (.spritefont) format
- Audio: Use OGG format with OggImporter and SoundEffectProcessor
- Content root directory: `Content/`
