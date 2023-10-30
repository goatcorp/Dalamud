using System.Diagnostics;
using System.IO;

using Dalamud.Configuration.Internal;
using Dalamud.Storage;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying configuration info.
/// </summary>
internal class VfsWidget : IDataWindowWidget
{
    private int numBytes = 1024;
    private int reps = 1;
    
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "vfs" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "VFS"; 

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var service = Service<ReliableFileStorage>.Get();
        var dalamud = Service<Dalamud>.Get();

        ImGui.InputInt("Num bytes", ref this.numBytes);
        ImGui.InputInt("Reps", ref this.reps);

        var path = Path.Combine(dalamud.StartInfo.WorkingDirectory!, "test.bin");

        if (ImGui.Button("Write"))
        {
            Log.Information("=== WRITING ===");
            var data = new byte[this.numBytes];
            var stopwatch = new Stopwatch();
            var acc = 0L;
            
            for (var i = 0; i < this.reps; i++)
            {
                stopwatch.Restart();
                service.WriteAllBytesAsync(path, data).GetAwaiter().GetResult();
                stopwatch.Stop();
                acc += stopwatch.ElapsedMilliseconds;
                Log.Information("Turn {Turn} took {Ms}ms", i, stopwatch.ElapsedMilliseconds);
            }
            
            Log.Information("Took {Ms}ms in total", acc);
        }

        if (ImGui.Button("Read"))
        {
            Log.Information("=== READING ===");
            var stopwatch = new Stopwatch();
            var acc = 0L;
            
            for (var i = 0; i < this.reps; i++)
            {
                stopwatch.Restart();
                service.ReadAllBytes(path);
                stopwatch.Stop();
                acc += stopwatch.ElapsedMilliseconds;
                Log.Information("Turn {Turn} took {Ms}ms", i, stopwatch.ElapsedMilliseconds);
            }
            
            Log.Information("Took {Ms}ms in total", acc);
        }

        if (ImGui.Button("Test Config"))
        {
            var config = Service<DalamudConfiguration>.Get();
            
            Log.Information("=== READING ===");
            var stopwatch = new Stopwatch();
            var acc = 0L;
            
            for (var i = 0; i < this.reps; i++)
            {
                stopwatch.Restart();
                config.ForceSave();
                stopwatch.Stop();
                acc += stopwatch.ElapsedMilliseconds;
                Log.Information("Turn {Turn} took {Ms}ms", i, stopwatch.ElapsedMilliseconds);
            }
            
            Log.Information("Took {Ms}ms in total", acc);
        }
    }
}
