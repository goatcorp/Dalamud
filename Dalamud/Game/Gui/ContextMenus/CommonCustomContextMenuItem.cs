using System;

using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus;

/// <inheritdoc />
public abstract class CustomContextMenuItem<T> : BaseContextMenuItem
    where T : Delegate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomContextMenuItem{T}"/> class.
        /// Create a new context menu item.
        /// </summary>
        /// <param name="name">the English name of the item, copied to other languages.</param>
        /// <param name="action">the action to perform on click.</param>
        internal CustomContextMenuItem(SeString name, T action)
        {
            this.NameEnglish = name;
            this.NameJapanese = name;
            this.NameFrench = name;
            this.NameGerman = name;

            this.Action = action;
        }

        /// <summary>
        /// Gets or sets the name of the context item to be shown for English clients.
        /// </summary>
        public SeString NameEnglish { get; set; }

        /// <summary>
        /// Gets or sets the name of the context item to be shown for Japanese clients.
        /// </summary>
        public SeString NameJapanese { get; set; }

        /// <summary>
        /// Gets or sets the name of the context item to be shown for French clients.
        /// </summary>
        public SeString NameFrench { get; set; }

        /// <summary>
        /// Gets or sets the name of the context item to be shown for German clients.
        /// </summary>
        public SeString NameGerman { get; set; }

        /// <summary>
        /// Gets or sets the action to perform when this item is clicked.
        /// </summary>
        public T Action { get; set; }

        /// <summary>
        /// Gets or sets the Agent pointer.
        /// </summary>
        internal IntPtr Agent { get; set; }
    }
