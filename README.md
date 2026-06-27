# HeroManager

HeroManager is a .NET 8 C# Windows desktop application inspired by Windows Task Manager. It shows:

- A system-wide list of running processes.
- Total, used, and free physical RAM.
- Per-process CPU percentage and private RAM usage, matching the style of Task Manager process memory values.
- Separate real-time CPU and RAM graphs for the selected process, each showing the current value.

## Requirements

- Windows 10/11 or Windows Server with desktop experience.
- .NET 8 SDK.

## Run

```powershell
dotnet run --project src/HeroManager/HeroManager.csproj
```

The app polls four times per second for a more real-time view. Some protected or short-lived processes may be skipped if Windows denies access or they exit while being read.
