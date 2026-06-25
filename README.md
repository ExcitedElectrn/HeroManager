# HeroManager

HeroManager is a .NET 8 C# Windows desktop application inspired by Windows Task Manager. It shows:

- A system-wide list of running processes.
- Total, used, and free physical RAM.
- Per-process CPU percentage and working-set RAM usage.
- A real-time CPU/RAM graph for the selected process.

## Requirements

- Windows 10/11 or Windows Server with desktop experience.
- .NET 8 SDK.

## Run

```powershell
dotnet run --project src/HeroManager/HeroManager.csproj
```

The app polls once per second. Some protected or short-lived processes may be skipped if Windows denies access or they exit while being read.
