# Getting Started

🌐 **English** · [Русский](../../ef2dg_Docs/05_Guides/Начало%20работы.md)

## Requirements

You need the .NET 10 SDK, a driver with OpenGL 3.3 and, for audio, an available OpenAL device. The project uses Silk.NET, ImGui.NET, Newtonsoft.Json and StbImageSharp.

## Build and run

Open a terminal in the project folder and run:

```powershell
dotnet build
dotnet run
```

The working directory matters: `project_config.json`, `bindings.json`, shaders, JSON levels and `assets/` are read through relative paths.

## Layout

`Core/` — runtime; `Framework/` — base entities, the map, serialization and scene lighting; `Rendering/` — OpenGL; `Components/` — reusable logic; `Game/` — example game logic; `Editor/` — the ImGui editor; `Shaders/` — lighting and post-processing shaders; `assets/` — resources.
