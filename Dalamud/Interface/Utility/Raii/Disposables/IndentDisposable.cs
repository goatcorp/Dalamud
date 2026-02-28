// ReSharper disable once CheckNamespace
using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around indentation. </summary>
    public sealed class IndentDisposable : IDisposable
    {
        /// <summary> The current indentation pushed by this object. </summary>
        public float CurrentIndent { get; private set; }

        /// <summary> Add to the current indentation. </summary>
        /// <param name="indent"> The value to change the indentation by. </param>
        /// <param name="scaled"> if this is true, applies global scale. </param>
        /// <param name="condition"> If this is false, the current indent is not changed. </param>
        /// <returns> A disposable object that can be used to change the indentation more and reverts to the prior indentation after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep indentation for longer than the current scope, use without using. </remarks>
        public IndentDisposable Indent(float indent, bool scaled = true, bool condition = true)
        {
            if (condition && indent is not 0)
            {
                if (scaled)
                    indent *= ImGuiHelpers.GlobalScale;

                if (indent < 0)
                    ImGui.Unindent(-indent);
                else
                    ImGui.Indent(indent);
                this.CurrentIndent += indent;
            }

            return this;
        }

        /// <summary> Add to the current indentation. </summary>
        /// <param name="indent"> The value to change the indentation by. </param>
        /// <param name="condition"> If this is false, the current indent is not changed. </param>
        /// <returns> A disposable object that can be used to change the indentation more and reverts to the prior indentation after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep indentation for longer than the current scope, use without using. </remarks>
        public IndentDisposable Indent(int indent = 1, bool condition = true)
        {
            if (condition && indent is not 0)
            {
                var spacing = indent * ImGui.GetStyle().IndentSpacing;
                if (indent < 0)
                    ImGui.Unindent(-spacing);
                else
                    ImGui.Indent(spacing);
                this.CurrentIndent += spacing;
            }

            return this;
        }

        /// <summary> Subtract from the current indentation. </summary>
        /// <param name="indent"> The value to change the indentation by. </param>
        /// <param name="condition"> If this is false, the current indent is not changed. </param>
        /// <returns> A disposable object that can be used to change the indentation more and reverts to the prior indentation after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep indentation for longer than the current scope, use without using. </remarks>
        public IndentDisposable Unindent(float indent, bool condition = true)
        {
            if (condition && indent is not 0)
            {
                if (indent < 0)
                    ImGui.Indent(-indent);
                else
                    ImGui.Unindent(indent);
                this.CurrentIndent -= indent;
            }

            return this;
        }

        /// <summary> Revert all indentation applied by this object. </summary>
        public void Dispose()
            => this.Unindent(this.CurrentIndent);
    }
}
