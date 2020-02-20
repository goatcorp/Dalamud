using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface;

namespace Dalamud.Plugin.Features
{
    /// <summary>
    /// This interface represents a Dalamud plugin that has user interface which can be drawn.
    /// </summary>
    public interface IHasUi : IDalamudPlugin
    {
        /// <summary>
        /// A function that gets called when Dalamud is ready to draw your UI.
        /// </summary>
        /// <param name="uiBuilder">An <see cref="UiBuilder"/> object you can use to e.g. load images.</param>
        void Draw(UiBuilder uiBuilder);
    }
}
