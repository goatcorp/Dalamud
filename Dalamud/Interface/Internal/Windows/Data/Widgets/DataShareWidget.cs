using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Ipc.Internal;

using ImGuiNET;

using Newtonsoft.Json;

using Formatting = Newtonsoft.Json.Formatting;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying plugin data share modules.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal class DataShareWidget : IDataWindowWidget
{
    private const ImGuiTabItemFlags NoCloseButton = (ImGuiTabItemFlags)(1 << 20);

    private readonly List<(string Name, byte[]? Data)> dataView = new();
    private int nextTab = -1;
    private IReadOnlyDictionary<string, CallGateChannel>? gates;
    private List<CallGateChannel>? gatesSorted;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "datashare" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Data Share & Call Gate";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public unsafe void Draw()
    {
        using var tabbar = ImRaii.TabBar("##tabbar");
        if (!tabbar.Success)
            return;

        var d = true;
        using (var tabitem = ImRaii.TabItem(
                   "Data Share##tabbar-datashare",
                   ref d,
                   NoCloseButton | (this.nextTab == 0 ? ImGuiTabItemFlags.SetSelected : 0)))
        {
            if (tabitem.Success)
                this.DrawDataShare();
        }

        using (var tabitem = ImRaii.TabItem(
                   "Call Gate##tabbar-callgate",
                   ref d,
                   NoCloseButton | (this.nextTab == 1 ? ImGuiTabItemFlags.SetSelected : 0)))
        {
            if (tabitem.Success)
                this.DrawCallGate();
        }

        for (var i = 0; i < this.dataView.Count; i++)
        {
            using var idpush = ImRaii.PushId($"##tabbar-data-{i}");
            var (name, data) = this.dataView[i];
            d = true;
            using var tabitem = ImRaii.TabItem(
                name,
                ref d,
                this.nextTab == 2 + i ? ImGuiTabItemFlags.SetSelected : 0);
            if (!d)
                this.dataView.RemoveAt(i--);
            if (!tabitem.Success)
                continue;

            if (ImGui.Button("Refresh"))
                data = null;

            if (data is null)
            {
                try
                {
                    var dataShare = Service<DataShare>.Get();
                    var data2 = dataShare.GetData<object>(name);
                    try
                    {
                        data = Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(
                                data2,
                                Formatting.Indented,
                                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }));
                    }
                    finally
                    {
                        dataShare.RelinquishData(name);
                    }
                }
                catch (Exception e)
                {
                    data = Encoding.UTF8.GetBytes(e.ToString());
                }

                this.dataView[i] = (name, data);
            }

            ImGui.SameLine();
            if (ImGui.Button("Copy"))
            {
                fixed (byte* pData = data)
                    ImGuiNative.igSetClipboardText(pData);
            }

            fixed (byte* pLabel = "text"u8)
            fixed (byte* pData = data)
            {
                ImGuiNative.igInputTextMultiline(
                    pLabel,
                    pData,
                    (uint)data.Length,
                    ImGui.GetContentRegionAvail(),
                    ImGuiInputTextFlags.ReadOnly,
                    null,
                    null);
            }
        }

        this.nextTab = -1;
    }

    private static string ReprMethod(MethodInfo? mi, bool withParams)
    {
        if (mi is null)
            return "-";
        
        var sb = new StringBuilder();
        sb.Append(ReprType(mi.DeclaringType))
          .Append("::")
          .Append(mi.Name);
        if (!withParams)
            return sb.ToString();
        sb.Append('(');
        var parfirst = true;
        foreach (var par in mi.GetParameters())
        {
            if (!parfirst)
                sb.Append(", ");
            else
                parfirst = false;
            sb.AppendLine()
              .Append('\t')
              .Append(ReprType(par.ParameterType))
              .Append(' ')
              .Append(par.Name);
        }

        if (!parfirst)
            sb.AppendLine();
        sb.Append(')');
        if (mi.ReturnType != typeof(void))
            sb.Append(" -> ").Append(ReprType(mi.ReturnType));
        return sb.ToString();

        static string WithoutGeneric(string s)
        {
            var i = s.IndexOf('`');
            return i != -1 ? s[..i] : s;
        }

        static string ReprType(Type? t) =>
            t switch
            {
                null => "null",
                _ when t == typeof(string) => "string",
                _ when t == typeof(object) => "object",
                _ when t == typeof(void) => "void",
                _ when t == typeof(decimal) => "decimal",
                _ when t == typeof(bool) => "bool",
                _ when t == typeof(double) => "double",
                _ when t == typeof(float) => "float",
                _ when t == typeof(char) => "char",
                _ when t == typeof(ulong) => "ulong",
                _ when t == typeof(long) => "long",
                _ when t == typeof(uint) => "uint",
                _ when t == typeof(int) => "int",
                _ when t == typeof(ushort) => "ushort",
                _ when t == typeof(short) => "short",
                _ when t == typeof(byte) => "byte",
                _ when t == typeof(sbyte) => "sbyte",
                _ when t == typeof(nint) => "nint",
                _ when t == typeof(nuint) => "nuint",
                _ when t.IsArray && t.HasElementType => ReprType(t.GetElementType()) + "[]",
                _ when t.IsPointer && t.HasElementType => ReprType(t.GetElementType()) + "*",
                _ when t.IsGenericTypeDefinition =>
                    t.Assembly == typeof(object).Assembly
                        ? t.Name + "<>"
                        : (t.FullName ?? t.Name) + "<>",
                _ when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>) =>
                    ReprType(t.GetGenericArguments()[0]) + "?",
                _ when t.IsGenericType =>
                    WithoutGeneric(ReprType(t.GetGenericTypeDefinition())) +
                    "<" + string.Join(", ", t.GetGenericArguments().Select(ReprType)) + ">",
                _ => t.Assembly == typeof(object).Assembly ? t.Name : t.FullName ?? t.Name,
            };
    }

    private void DrawTextCell(string s, Func<string>? tooltip = null, bool framepad = false)
    {
        ImGui.TableNextColumn();
        var offset = ImGui.GetCursorScreenPos() + new Vector2(0, framepad ? ImGui.GetStyle().FramePadding.Y : 0);
        if (framepad)
            ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(s);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetNextWindowPos(offset - ImGui.GetStyle().WindowPadding);
            var vp = ImGui.GetWindowViewport();
            var wrx = (vp.WorkPos.X + vp.WorkSize.X) - offset.X;
            ImGui.SetNextWindowSizeConstraints(Vector2.One, new(wrx, float.MaxValue));
            using (ImRaii.Tooltip())
            {
                ImGui.PushTextWrapPos(wrx);
                ImGui.TextWrapped((tooltip?.Invoke() ?? s).Replace("%", "%%"));
                ImGui.PopTextWrapPos();
            }
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(tooltip?.Invoke() ?? s);
            Service<NotificationManager>.Get().AddNotification(
                $"Copied {ImGui.TableGetColumnName()} to clipboard.",
                this.DisplayName,
                NotificationType.Success);
        }
    }

    private void DrawCallGate()
    {
        var callGate = Service<CallGate>.Get();
        if (ImGui.Button("Purge empty call gates"))
            callGate.PurgeEmptyGates();

        using var table = ImRaii.Table("##callgate-table", 5);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort);
        ImGui.TableSetupColumn("Action");
        ImGui.TableSetupColumn("Func");
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Subscriber");
        ImGui.TableHeadersRow();

        var gates2 = callGate.Gates;
        if (!ReferenceEquals(gates2, this.gates) || this.gatesSorted is null)
        {
            this.gatesSorted = (this.gates = gates2).Values.ToList();
            this.gatesSorted.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in this.gatesSorted)
        {
            var subs = item.Subscriptions;
            for (var i = 0; i < subs.Count || i == 0; i++)
            {
                ImGui.TableNextRow();
                this.DrawTextCell(item.Name);
                this.DrawTextCell(
                    ReprMethod(item.Action?.Method, false),
                    () => ReprMethod(item.Action?.Method, true));
                this.DrawTextCell(
                    ReprMethod(item.Func?.Method, false),
                    () => ReprMethod(item.Func?.Method, true));
                if (subs.Count == 0)
                {
                    this.DrawTextCell("0");
                    continue;
                }

                this.DrawTextCell($"{i + 1}/{subs.Count}");
                this.DrawTextCell($"{subs[i].Method.DeclaringType}::{subs[i].Method.Name}");
            }
        }
    }

    private void DrawDataShare()
    {
        if (!ImGui.BeginTable("###DataShareTable", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg))
            return;

        try
        {
            ImGui.TableSetupColumn("Shared Tag");
            ImGui.TableSetupColumn("Show");
            ImGui.TableSetupColumn("Creator Assembly");
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Consumers");
            ImGui.TableHeadersRow();
            foreach (var share in Service<DataShare>.Get().GetAllShares())
            {
                ImGui.TableNextRow();
                this.DrawTextCell(share.Tag, null, true);

                ImGui.TableNextColumn();
                if (ImGui.Button($"Show##datasharetable-show-{share.Tag}"))
                {
                    var index = 0;
                    for (; index < this.dataView.Count; index++)
                    {
                        if (this.dataView[index].Name == share.Tag)
                            break;
                    }

                    if (index == this.dataView.Count)
                        this.dataView.Add((share.Tag, null));
                    else
                        this.dataView[index] = (share.Tag, null);
                    this.nextTab = 2 + index;
                }

                this.DrawTextCell(share.CreatorAssembly, null, true);
                this.DrawTextCell(share.Users.Length.ToString(), null, true);
                this.DrawTextCell(string.Join(", ", share.Users), null, true);
            }
        }
        finally
        {
            ImGui.EndTable();
        }
    }
}
