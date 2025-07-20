using System.Collections.Generic;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <inheritdoc cref="TimelineTree"/>
public readonly partial struct TimelineTree
{
    /// <summary>
    /// An interface for retrieving and printing the contents of a given column in an animation timeline table.
    /// </summary>
    public interface IKeyGroupColumn
    {
        /// <summary>Gets the column's name/heading.</summary>
        public string Name { get; }

        /// <summary>Gets the number of cells in the column.</summary>
        public int Count { get; }

        /// <summary>Gets the column's width.</summary>
        public float Width { get; }

        /// <summary>
        /// Calls this column's print function for a given row.
        /// </summary>
        /// <param name="i">The row number.</param>
        public void PrintValueAt(int i);
    }

    /// <summary>
    /// A column within an animation timeline table, representing a particular KeyGroup.
    /// </summary>
    /// <typeparam name="T">The value type of the KeyGroup.</typeparam>
    public struct KeyGroupColumn<T> : IKeyGroupColumn
    {
        /// <summary>The values of each cell in the column.</summary>
        public List<T> Values;

        /// <summary>The method that should be used to format and print values in this KeyGroup.</summary>
        public Action<T> PrintFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyGroupColumn{T}"/> struct.
        /// </summary>
        /// <param name="name">The column's name/heading.</param>
        /// <param name="printFunc">The method that should be used to format and print values in this KeyGroup.</param>
        internal KeyGroupColumn(string name, Action<T>? printFunc = null)
        {
            this.Name = name;
            this.PrintFunc = printFunc ?? PlainTextCell;
            this.Values = [];
            this.Width = 50;
        }

        /// <inheritdoc/>
        public string Name { get; set; }

        /// <inheritdoc/>
        public float Width { get; init; }

        /// <inheritdoc/>
        public readonly int Count => this.Values.Count;

        /// <summary>
        /// The default print function, if none is specified.
        /// </summary>
        /// <param name="value">The value to print.</param>
        public static void PlainTextCell(T value) => ImGui.TextUnformatted($"{value}");

        /// <summary>
        /// Adds a value to this column.
        /// </summary>
        /// <param name="val">The value to add.</param>
        public readonly void Add(T val) => this.Values.Add(val);

        /// <inheritdoc/>
        public readonly void PrintValueAt(int i)
        {
            if (this.Values.Count > i)
            {
                this.PrintFunc.Invoke(this.Values[i]);
            }
            else
            {
                ImGui.TextDisabled("..."u8);
            }
        }
    }
}
