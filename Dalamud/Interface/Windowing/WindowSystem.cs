using System;
using System.Collections.Generic;
using System.Linq;

using ImGuiNET;

namespace Dalamud.Interface.Windowing
{
    /// <summary>
    /// Class running a WindowSystem using <see cref="Window"/> implementations to simplify ImGui windowing.
    /// </summary>
    public class WindowSystem
    {
        private readonly List<Window> windows = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowSystem"/> class.
        /// </summary>
        /// <param name="imNamespace">The name/ID-space of this <see cref="WindowSystem"/>.</param>
        public WindowSystem(string? imNamespace = null)
        {
            this.Namespace = imNamespace;
        }

        /// <summary>
        /// Gets a value indicating whether any <see cref="WindowSystem"/> contains any <see cref="Window"/>
        /// that has focus and is not marked to be excluded from consideration.
        /// </summary>
        public static bool HasAnyWindowSystemFocus { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether any window in this <see cref="WindowSystem"/> has focus and is
        /// not marked to be excluded from consideration.
        /// </summary>
        public bool HasAnyFocus { get; private set; }

        /// <summary>
        /// Gets or sets the name/ID-space of this <see cref="WindowSystem"/>.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Add a window to this <see cref="WindowSystem"/>.
        /// </summary>
        /// <param name="window">The window to add.</param>
        public void AddWindow(Window window)
        {
            if (this.windows.Any(x => x.WindowName == window.WindowName))
                throw new ArgumentException("A window with this name/ID already exists.");

            this.windows.Add(window);
        }

        /// <summary>
        /// Remove a window from this <see cref="WindowSystem"/>.
        /// </summary>
        /// <param name="window">The window to remove.</param>
        public void RemoveWindow(Window window)
        {
            if (!this.windows.Contains(window))
                throw new ArgumentException("This window is not registered on this WindowSystem.");

            this.windows.Remove(window);
        }

        /// <summary>
        /// Remove all windows from this <see cref="WindowSystem"/>.
        /// </summary>
        public void RemoveAllWindows() => this.windows.Clear();

        /// <summary>
        /// Draw all registered windows using ImGui.
        /// </summary>
        public void Draw()
        {
            var hasNamespace = !string.IsNullOrEmpty(this.Namespace);

            if (hasNamespace)
                ImGui.PushID(this.Namespace);

            foreach (var window in this.windows)
            {
#if DEBUG
                // Log.Verbose($"[WS{(hasNamespace ? "/" + this.Namespace : string.Empty)}] Drawing {window.WindowName}");
#endif

                window.DrawInternal();
            }

            this.HasAnyFocus = this.windows.Any(x => x.IsFocused && x.RespectCloseHotkey);

            if (this.HasAnyFocus)
                HasAnyWindowSystemFocus = true;

            if (hasNamespace)
                ImGui.PopID();
        }
    }
}
