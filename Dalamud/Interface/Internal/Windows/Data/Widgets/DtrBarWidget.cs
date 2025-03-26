using System.Linq;
using System.Threading;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying dtr test.
/// </summary>
internal class DtrBarWidget : IDataWindowWidget, IDisposable
{
    private IDtrBarEntry? dtrTest1;
    private IDtrBarEntry? dtrTest2;
    private IDtrBarEntry? dtrTest3;

    private Thread? loadTestThread;
    private CancellationTokenSource? loadTestThreadCt;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "dtr", "dtrbar" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "DTR Bar";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.ClearState();
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Dispose() => this.ClearState();

    /// <inheritdoc/>
    public void Draw()
    {
        if (this.loadTestThread?.IsAlive is not true)
        {
            if (ImGui.Button("Do multithreaded add/remove operation"))
            {
                var ct = this.loadTestThreadCt = new();
                var dbar = Service<DtrBar>.Get();
                var fw = Service<Framework>.Get();
                var rng = new Random();
                this.loadTestThread = new(
                    () =>
                    {
                        var threads = Enumerable
                                      .Range(0, Environment.ProcessorCount)
                                      .Select(
                                          i => new Thread(
                                              (i % 4) switch
                                              {
                                                  0 => () =>
                                                  {
                                                      try
                                                      {
                                                          while (true)
                                                          {
                                                              var n = $"DtrBarWidgetTest{rng.NextInt64(8)}";
                                                              dbar.Get(n, n[^5..]);
                                                              fw.DelayTicks(1, ct.Token).Wait(ct.Token);
                                                              ct.Token.ThrowIfCancellationRequested();
                                                          }
                                                      }
                                                      catch (OperationCanceledException)
                                                      {
                                                          // ignore
                                                      }
                                                  },
                                                  1 => () =>
                                                  {
                                                      try
                                                      {
                                                          while (true)
                                                          {
                                                              dbar.Remove($"DtrBarWidgetTest{rng.NextInt64(8)}");
                                                              fw.DelayTicks(1, ct.Token).Wait(ct.Token);
                                                              ct.Token.ThrowIfCancellationRequested();
                                                          }
                                                      }
                                                      catch (OperationCanceledException)
                                                      {
                                                          // ignore
                                                      }
                                                  },
                                                  2 => () =>
                                                  {
                                                      try
                                                      {
                                                          while (true)
                                                          {
                                                              var n = $"DtrBarWidgetTest{rng.NextInt64(8)}_";
                                                              dbar.Get(n, n[^6..]);
                                                              ct.Token.ThrowIfCancellationRequested();
                                                          }
                                                      }
                                                      catch (OperationCanceledException)
                                                      {
                                                          // ignore
                                                      }
                                                  },
                                                  _ => () =>
                                                  {
                                                      try
                                                      {
                                                          while (true)
                                                          {
                                                              dbar.Remove($"DtrBarWidgetTest{rng.NextInt64(8)}_");
                                                              ct.Token.ThrowIfCancellationRequested();
                                                          }
                                                      }
                                                      catch (OperationCanceledException)
                                                      {
                                                          // ignore
                                                      }
                                                  },
                                              }))
                                      .ToArray();
                        foreach (var t in threads) t.Start();
                        foreach (var t in threads) t.Join();
                        for (var i = 0; i < 8; i++) dbar.Remove($"DtrBarWidgetTest{i % 8}");
                        for (var i = 0; i < 8; i++) dbar.Remove($"DtrBarWidgetTest{i % 8}_");
                    });
                this.loadTestThread.Start();
            }
        }
        else
        {
            if (ImGui.Button("Stop multithreaded add/remove operation"))
                this.ClearState();
        }

        ImGui.Separator();
        this.DrawDtrTestEntry(ref this.dtrTest1, "DTR Test #1");

        ImGui.Separator();
        this.DrawDtrTestEntry(ref this.dtrTest2, "DTR Test #2");

        ImGui.Separator();
        this.DrawDtrTestEntry(ref this.dtrTest3, "DTR Test #3");

        ImGui.Separator();
        ImGui.Text("IDtrBar.Entries:");
        foreach (var e in Service<DtrBar>.Get().Entries)
            ImGui.Text(e.Title);

        var configuration = Service<DalamudConfiguration>.Get();
        if (configuration.DtrOrder != null)
        {
            ImGui.Separator();
            ImGui.Text("DtrOrder:");
            foreach (var order in configuration.DtrOrder)
                ImGui.Text(order);
        }
    }

    private void ClearState()
    {
        this.loadTestThreadCt?.Cancel();
        this.loadTestThread?.Join();
        this.loadTestThread = null;
        this.loadTestThreadCt = null;
    }

    private void DrawDtrTestEntry(ref IDtrBarEntry? entry, string title)
    {
        var dtrBar = Service<DtrBar>.Get();

        if (entry != null)
        {
            ImGui.Text(title);

            var text = entry.Text?.TextValue ?? string.Empty;
            if (ImGui.InputText($"Text###{entry.Title}t", ref text, 255))
                entry.Text = text;

            var shown = entry.Shown;
            if (ImGui.Checkbox($"Shown###{entry.Title}s", ref shown))
                entry.Shown = shown;

            if (ImGui.Button($"Remove###{entry.Title}r"))
            {
                entry.Remove();
                entry = null;
            }
        }
        else
        {
            if (ImGui.Button($"Add###{title}"))
            {
                entry = dtrBar.Get(title, title);
            }
        }
    }
}
