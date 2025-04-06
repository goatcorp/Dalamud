// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HexaGen.Runtime;
using System.Numerics;

namespace Dalamud.Bindings.ImGui
{
	/// <summary>
	/// Sorting specification for one column of a table (sizeof == 12 bytes)<br/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImGuiTableColumnSortSpecs
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public uint ColumnUserID;

		/// <summary>
		/// To be documented.
		/// </summary>
		public short ColumnIndex;

		/// <summary>
		/// To be documented.
		/// </summary>
		public short SortOrder;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImGuiSortDirection SortDirection;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiTableColumnSortSpecs(uint columnUserId = default, short columnIndex = default, short sortOrder = default, ImGuiSortDirection sortDirection = default)
		{
			ColumnUserID = columnUserId;
			ColumnIndex = columnIndex;
			SortOrder = sortOrder;
			SortDirection = sortDirection;
		}


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImGuiTableColumnSortSpecs* @this = &this)
			{
				ImGui.DestroyNative(@this);
			}
		}

	}

	/// <summary>
	/// To be documented.
	/// </summary>
	#if NET5_0_OR_GREATER
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	#endif
	public unsafe struct ImGuiTableColumnSortSpecsPtr : IEquatable<ImGuiTableColumnSortSpecsPtr>
	{
		public ImGuiTableColumnSortSpecsPtr(ImGuiTableColumnSortSpecs* handle) { Handle = handle; }

		public ImGuiTableColumnSortSpecs* Handle;

		public bool IsNull => Handle == null;

		public static ImGuiTableColumnSortSpecsPtr Null => new ImGuiTableColumnSortSpecsPtr(null);

		public ImGuiTableColumnSortSpecs this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImGuiTableColumnSortSpecsPtr(ImGuiTableColumnSortSpecs* handle) => new ImGuiTableColumnSortSpecsPtr(handle);

		public static implicit operator ImGuiTableColumnSortSpecs*(ImGuiTableColumnSortSpecsPtr handle) => handle.Handle;

		public static bool operator ==(ImGuiTableColumnSortSpecsPtr left, ImGuiTableColumnSortSpecsPtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImGuiTableColumnSortSpecsPtr left, ImGuiTableColumnSortSpecsPtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImGuiTableColumnSortSpecsPtr left, ImGuiTableColumnSortSpecs* right) => left.Handle == right;

		public static bool operator !=(ImGuiTableColumnSortSpecsPtr left, ImGuiTableColumnSortSpecs* right) => left.Handle != right;

		public bool Equals(ImGuiTableColumnSortSpecsPtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImGuiTableColumnSortSpecsPtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImGuiTableColumnSortSpecsPtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref uint ColumnUserID => ref Unsafe.AsRef<uint>(&Handle->ColumnUserID);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref short ColumnIndex => ref Unsafe.AsRef<short>(&Handle->ColumnIndex);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref short SortOrder => ref Unsafe.AsRef<short>(&Handle->SortOrder);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImGuiSortDirection SortDirection => ref Unsafe.AsRef<ImGuiSortDirection>(&Handle->SortDirection);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			ImGui.DestroyNative(Handle);
		}

	}

}
