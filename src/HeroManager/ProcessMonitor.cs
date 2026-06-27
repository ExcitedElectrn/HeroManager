using System.Diagnostics;

namespace HeroManager;

internal sealed class ProcessMonitor
{
    private readonly Dictionary<int, ProcessSample> _lastSamples = new();

    public IReadOnlyList<ProcessInfo> GetProcesses()
    {
        var now = DateTime.UtcNow;
        var activeIds = new HashSet<int>();
        var processes = new List<ProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    activeIds.Add(process.Id);
                    var totalProcessorTime = process.TotalProcessorTime;
                    var privateMemory = process.PrivateMemorySize64;
                    var cpuPercent = CalculateCpuPercent(process.Id, totalProcessorTime, now);

                    processes.Add(new ProcessInfo(
                        process.Id,
                        string.IsNullOrWhiteSpace(process.ProcessName) ? "Unknown" : process.ProcessName,
                        cpuPercent,
                        privateMemory));
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    // Processes can exit or deny access while Task Manager-style polling is underway.
                }
            }
        }

        foreach (var staleProcessId in _lastSamples.Keys.Except(activeIds).ToArray())
        {
            _lastSamples.Remove(staleProcessId);
        }

        return processes
            .OrderByDescending(process => process.CpuPercent)
            .ThenBy(process => process.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private double CalculateCpuPercent(int processId, TimeSpan totalProcessorTime, DateTime timestamp)
    {
        if (!_lastSamples.TryGetValue(processId, out var previous))
        {
            _lastSamples[processId] = new ProcessSample(totalProcessorTime, timestamp);
            return 0;
        }

        _lastSamples[processId] = new ProcessSample(totalProcessorTime, timestamp);
        var processorDelta = (totalProcessorTime - previous.TotalProcessorTime).TotalMilliseconds;
        var wallClockDelta = (timestamp - previous.Timestamp).TotalMilliseconds;
        if (processorDelta <= 0 || wallClockDelta <= 0)
        {
            return 0;
        }

        return Math.Clamp(processorDelta / wallClockDelta * 100d, 0, 100);
    }
}

internal sealed record ProcessInfo(int Id, string Name, double CpuPercent, long MemoryBytes);

internal sealed record ProcessSample(TimeSpan TotalProcessorTime, DateTime Timestamp);
