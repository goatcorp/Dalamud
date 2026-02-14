// ReSharper disable once CheckNamespace

using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around ImGui tables. </summary>
    public ref struct TableDisposable : IDisposable
    {
        /// <summary> Whether creating the table succeeded. This needs to be checked before calling any of the member methods. </summary>
        public readonly bool Success;

        /// <summary> Whether the table is already ended. </summary>
        public bool Alive { get; private set; }

        /// <inheritdoc cref="TableDisposable(ImU8String,int,ImGuiTableFlags,Vector2,float)"/>
        internal TableDisposable(ImU8String id, int columns)
        {
            this.Success = ImGui.BeginTable(id, columns);
            this.Alive   = true;
        }

        /// <inheritdoc cref="TableDisposable(ImU8String,int,ImGuiTableFlags,Vector2,float)"/>
        internal TableDisposable(ImU8String id, int columns, ImGuiTableFlags flags)
        {
            this.Success = ImGui.BeginTable(id, columns, flags);
            this.Alive   = true;
        }

        /// <inheritdoc cref="TableDisposable(ImU8String,int,ImGuiTableFlags,Vector2,float)"/>
        internal TableDisposable(ImU8String id, int columns, ImGuiTableFlags flags, Vector2 outerSize)
        {
            this.Success = ImGui.BeginTable(id, columns, flags, outerSize);
            this.Alive   = true;
        }

        /// <summary>Initializes a new instance of the <see cref="TableDisposable"/> struct. </summary>
        /// <param name="id"> The table ID as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
        /// <param name="columns"> The number of columns in the table. </param>
        /// <param name="flags"> Additional flags to control the table's behaviour. </param>
        /// <param name="outerSize">
        /// The size the table is fixed to.
        /// <list type="bullet">
        ///     <item> If this is non-positive in X, right-align from the available region. (0 means full available width). </item>
        ///     <item> If this is positive in X, set a fixed width. </item>
        ///     <item> If both scroll-bars are disabled and this is negative in Y, right-align from the available region. (0 means full available width). </item>
        /// </list>
        /// The behaviour in Y is dependent on the existence of scroll-bars and other <paramref name="flags"/> (see <see href="https://github.com/ocornut/imgui/blob/master/imgui_tables.cpp" >imgui_tables.cpp</see>).
        /// </param>
        /// <param name="innerWidth"> The inner width in case the horizontal scroll-bar is enabled. If 0, fits into <paramref name="outerSize"/>.X, otherwise overrides the scrolling width. Negative values make no sense. </param>
        /// <returns> A disposable object that evaluates to true if any part of the begun table is currently visible and should be checked before using table functionality. Use with using. </returns>
        internal TableDisposable(ImU8String id, int columns, ImGuiTableFlags flags, Vector2 outerSize, float innerWidth)
        {
            this.Success = ImGui.BeginTable(id, columns, flags, outerSize, innerWidth);
            this.Alive   = true;
        }

        /// <summary> Conversion to bool. </summary>
        public static implicit operator bool(TableDisposable value)
            => value.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator true(TableDisposable i)
            => i.Success;

        /// <summary> Conversion to bool. </summary>
        public static bool operator false(TableDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on NOT operators. </summary>
        public static bool operator !(TableDisposable i)
            => !i.Success;

        /// <summary> Conversion to bool on AND operators. </summary>
        public static bool operator &(TableDisposable i, bool value)
            => i.Success && value;

        /// <summary> Conversion to bool on OR operators. </summary>
        public static bool operator |(TableDisposable i, bool value)
            => i.Success || value;

        /// <summary> End the Table on leaving scope. </summary>
        public void Dispose()
        {
            if (!this.Alive)
                return;

            if (this.Success)
                ImGui.EndTable();
            this.Alive = false;
        }

        /// <summary> End a Table without using an IDisposable. </summary>
        public static void EndUnsafe()
            => ImGui.EndTable();
    }
}
