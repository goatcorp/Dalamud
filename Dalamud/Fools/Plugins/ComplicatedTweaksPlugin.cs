using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace Dalamud.Fools.Plugins;

public class ComplicatedTweaksPlugin : IFoolsPlugin
{
    enum Widget
    {
        Button,
        Checkbox,
        DragFloat,
        InputFloat,
    }

    private List<Widget> widgets;

    public ComplicatedTweaksPlugin()
    {
        this.widgets = new List<Widget>();

        var random = new Random();
        var possibleWidgets = Enum.GetValues(typeof(Widget)).Cast<Widget>().ToList();

        for (var i = 0; i < 100; i++)
        {
            var widget = possibleWidgets[random.Next(possibleWidgets.Count)];
            this.widgets.Add(widget);
        }
    }

    public void DrawUi()
    {
        if (ImGui.Begin("Complicated Tweaks"))
        {
            foreach (var widget in this.widgets)
            {
                switch (widget)
                {
                    case Widget.Button:
                        ImGui.Button("Click me!");
                        break;

                    case Widget.Checkbox:
                        var iHateImgui = false;
                        ImGui.Checkbox(string.Empty, ref iHateImgui);
                        break;

                    case Widget.DragFloat:
                        var dragFloat = 0f;
                        ImGui.DragFloat(string.Empty, ref dragFloat);
                        break;

                    case Widget.InputFloat:
                        var inputFloat = 0f;
                        ImGui.InputFloat(string.Empty, ref inputFloat);
                        break;
                }
            }
        }

        ImGui.End();
    }

    public void Dispose() { }
}
