using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

// Most ImGui widgets with IDisposable interface that automatically destroys them
// when created with using variables.
public static partial class ImRaii
{
    public static ChildDisposable Child(ImU8String strId)
        => new(strId);

    public static ChildDisposable Child(ImU8String strId, Vector2 size)
        => new(strId, size);

    public static ChildDisposable Child(ImU8String strId, Vector2 size, bool border)
        => new(strId, size, border);

    public static ChildDisposable Child(ImU8String strId, Vector2 size, bool border, ImGuiWindowFlags flags)
        => new(strId, size, border, flags);

    public static ColorDisposable PushColor(ImGuiCol idx, uint color, bool condition = true)
        => new ColorDisposable().Push(idx, color, condition);

    public static ColorDisposable PushColor(ImGuiCol idx, Vector4 color, bool condition = true)
        => new ColorDisposable().Push(idx, color, condition);

    public static ColorDisposable DefaultColors()
        => ColorDisposable.DefaultColors();

    public static StyleDisposable PushStyle(ImGuiStyleVar idx, float value, bool condition = true)
        => new StyleDisposable().Push(idx, value, condition);

    public static StyleDisposable PushStyle(ImGuiStyleVar idx, Vector2 value, bool condition = true)
        => new StyleDisposable().Push(idx, value, condition);

    public static StyleDisposable DefaultStyle()
        => StyleDisposable.DefaultStyle();

    public static FontDisposable PushFont(ImFontPtr font, bool condition = true)
        => new FontDisposable().Push(font, condition);

    public static IdDisposable PushId(ImU8String id, bool enabled = true)
        => new IdDisposable().Push(id, enabled);

    public static IdDisposable PushId(int id, bool enabled = true)
        => new IdDisposable().Push(id, enabled);

    public static IdDisposable PushId(nint id, bool enabled = true)
        => new IdDisposable().Push(id, enabled);

    public static IndentDisposable PushIndent(float f, bool scaled = true, bool condition = true)
        => new IndentDisposable().Indent(f, scaled, condition);

    public static IndentDisposable PushIndent(int i = 1, bool condition = true)
        => new IndentDisposable().Indent(i, condition);

    public static DragDropTargetDisposable DragDropTarget()
        => new();

    public static DragDropSourceDisposable DragDropSource()
        => new();

    public static DragDropSourceDisposable DragDropSource(ImGuiDragDropFlags flags)
        => new(flags);

    public static HeaderDisposable Header(ImU8String label, ImGuiTreeNodeFlags flags)
        => new(label, flags);

    public static HeaderDisposable Header(ImU8String label, ref bool visible, ImGuiTreeNodeFlags flags)
        => new(label, ref visible, flags);

    public static PopupDisposable Popup(ImU8String id)
        => new(id);

    public static PopupDisposable Popup(ImU8String id, ImGuiWindowFlags flags)
        => new(id, flags);

    public static PopupDisposable PopupModal(ImU8String id)
        => PopupDisposable.Modal(id);

    public static PopupDisposable PopupModal(ImU8String id, ImGuiWindowFlags flags)
        => PopupDisposable.Modal(id, flags);

    public static PopupDisposable PopupModal(ImU8String id, ref bool open)
        => PopupDisposable.Modal(id, ref open);

    public static PopupDisposable PopupModal(ImU8String id, ref bool open, ImGuiWindowFlags flags)
        => PopupDisposable.Modal(id, ref open, flags);

    public static PopupDisposable ContextPopup(ImU8String id)
        => PopupDisposable.ContextWindow(id);

    public static PopupDisposable ContextPopup(ImU8String id, ImGuiPopupFlags flags)
        => PopupDisposable.ContextWindow(id, flags);

    public static PopupDisposable ContextPopupItem(ImU8String id)
        => PopupDisposable.ContextItem(id);

    public static PopupDisposable ContextPopupItem(ImU8String id, ImGuiPopupFlags flags)
        => PopupDisposable.ContextItem(id, flags);

    public static ComboDisposable Combo(ImU8String label, ImU8String previewValue)
        => new(label, previewValue);

    public static ComboDisposable Combo(ImU8String label, ImU8String previewValue, ImGuiComboFlags flags)
        => new(label, previewValue, flags);

    public static MenuDisposable Menu(ImU8String label)
        => new(label);

    public static MenuDisposable Menu(ImU8String label, bool enabled)
        => new(label, enabled);

    public static MenuBarDisposable MenuBar()
        => new();

    public static MainMenuBarDisposable MainMenuBar()
        => new();

    public static GroupDisposable Group()
        => new();

    public static TooltipDisposable Tooltip()
        => new();

    public static ItemWidthDisposable ItemWidth(float width)
        => new ItemWidthDisposable().Push(width);

    public static ItemWidthDisposable ItemWidth(float width, bool condition)
        => new ItemWidthDisposable().Push(width, condition);

    public static TextWrapDisposable TextWrapPos(float pos)
        => new TextWrapDisposable().Push(pos, true);

    public static TextWrapDisposable TextWrapPos(float pos, bool condition)
        => new TextWrapDisposable().Push(pos, condition);

    public static ListBoxDisposable ListBox(ImU8String label)
        => new(label);

    public static ListBoxDisposable ListBox(ImU8String label, Vector2 size)
        => new(label, size);

    public static TableDisposable Table(ImU8String table, int numColumns)
        => new(table, numColumns);

    public static TableDisposable Table(ImU8String table, int numColumns, ImGuiTableFlags flags)
        => new(table, numColumns, flags);

    public static TableDisposable Table(ImU8String table, int numColumns, ImGuiTableFlags flags, Vector2 outerSize)
        => new(table, numColumns, flags, outerSize);

    public static TableDisposable Table(ImU8String table, int numColumns, ImGuiTableFlags flags, Vector2 outerSize, float innerWidth)
        => new(table, numColumns, flags, outerSize, innerWidth);

    public static TabBarDisposable TabBar(ImU8String label)
        => new(label);

    public static TabBarDisposable TabBar(ImU8String label, ImGuiTabBarFlags flags)
        => new(label, flags);

    public static TabItemDisposable TabItem(ImU8String label)
        => new(label);

    public static unsafe TabItemDisposable TabItem(byte* label, ImGuiTabItemFlags flags)
        => new(label, flags);

    public static TabItemDisposable TabItem(ImU8String label, ImGuiTabItemFlags flags)
        => new(label, flags);

    public static TabItemDisposable TabItem(ImU8String label, ref bool open)
        => new(label, ref open);

    public static TabItemDisposable TabItem(ImU8String label, ref bool open, ImGuiTabItemFlags flags)
        => new(label, ref open, flags);

    public static TreeNodeDisposable TreeNode(ImU8String label)
        => new(label);

    public static TreeNodeDisposable TreeNode(ImU8String label, ImGuiTreeNodeFlags flags)
        => new(label, flags);

    public static DisabledDisposable Disabled()
        => new DisabledDisposable().Push();

    public static DisabledDisposable Disabled(bool disabled)
        => new DisabledDisposable().Push(disabled);

    public static EnabledDisposable Enabled()
        => new(DisabledDisposable.GlobalCount != 0);

    /* Only in OtterGui for now
    public static IEndObject FramedGroup(string label)
    {
        Widget.BeginFramedGroup(label, Vector2.Zero);
        return new EndUnconditionally(Widget.EndFramedGroup, true);
    }

    public static IEndObject FramedGroup(string label, Vector2 minSize, string description = "")
    {
        Widget.BeginFramedGroup(label, minSize, description);
        return new EndUnconditionally(Widget.EndFramedGroup, true);
    }
    */

    // Exported interface for RAII.
    public interface IEndObject : IDisposable
    {
        public bool Success { get; }

        public static bool operator true(IEndObject i)
            => i.Success;

        public static bool operator false(IEndObject i)
            => !i.Success;

        public static bool operator !(IEndObject i)
            => !i.Success;

        public static bool operator &(IEndObject i, bool value)
            => i.Success && value;

        public static bool operator |(IEndObject i, bool value)
            => i.Success || value;
    }

    // Use end-function regardless of success.
    // Used by Child, Group and Tooltip.
    public ref struct EndUnconditionally : IEndObject
    {
        private Action EndAction { get; }

        public bool Success { get; }

        public bool Disposed { get; private set; }

        public EndUnconditionally(Action endAction, bool success)
        {
            this.EndAction = endAction;
            this.Success = success;
            this.Disposed = false;
        }

        public void Dispose()
        {
            if (this.Disposed)
                return;

            this.EndAction();
            this.Disposed = true;
        }

        public static bool operator true(EndUnconditionally i)
            => i.Success;

        public static bool operator false(EndUnconditionally i)
            => !i.Success;

        public static bool operator !(EndUnconditionally i)
            => !i.Success;

        public static bool operator &(EndUnconditionally i, bool value)
            => i.Success && value;

        public static bool operator |(EndUnconditionally i, bool value)
            => i.Success || value;
    }

    // Use end-function only on success.
    public ref struct EndConditionally : IEndObject
    {
        public EndConditionally(Action endAction, bool success)
        {
            this.EndAction = endAction;
            this.Success = success;
            this.Disposed = false;
        }

        public bool Success { get; }

        public bool Disposed { get; private set; }

        private Action EndAction { get; }

        public void Dispose()
        {
            if (this.Disposed)
                return;

            if (this.Success)
                this.EndAction();
            this.Disposed = true;
        }

        public static bool operator true(EndConditionally i)
            => i.Success;

        public static bool operator false(EndConditionally i)
            => !i.Success;

        public static bool operator !(EndConditionally i)
            => !i.Success;

        public static bool operator &(EndConditionally i, bool value)
            => i.Success && value;

        public static bool operator |(EndConditionally i, bool value)
            => i.Success || value;
    }
}
