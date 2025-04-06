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
	/// Simple column measurement, currently used for MenuItem() only.. This is very short-sightedthrow-away code and NOT a generic helper.<br/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImGuiMenuColumns
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public uint TotalWidth;

		/// <summary>
		/// To be documented.
		/// </summary>
		public uint NextTotalWidth;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ushort Spacing;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ushort OffsetIcon;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ushort OffsetLabel;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ushort OffsetShortcut;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ushort OffsetMark;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ushort Widths_0;
		public ushort Widths_1;
		public ushort Widths_2;
		public ushort Widths_3;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiMenuColumns(uint totalWidth = default, uint nextTotalWidth = default, ushort spacing = default, ushort offsetIcon = default, ushort offsetLabel = default, ushort offsetShortcut = default, ushort offsetMark = default, ushort* widths = default)
		{
			TotalWidth = totalWidth;
			NextTotalWidth = nextTotalWidth;
			Spacing = spacing;
			OffsetIcon = offsetIcon;
			OffsetLabel = offsetLabel;
			OffsetShortcut = offsetShortcut;
			OffsetMark = offsetMark;
			if (widths != default(ushort*))
			{
				Widths_0 = widths[0];
				Widths_1 = widths[1];
				Widths_2 = widths[2];
				Widths_3 = widths[3];
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiMenuColumns(uint totalWidth = default, uint nextTotalWidth = default, ushort spacing = default, ushort offsetIcon = default, ushort offsetLabel = default, ushort offsetShortcut = default, ushort offsetMark = default, Span<ushort> widths = default)
		{
			TotalWidth = totalWidth;
			NextTotalWidth = nextTotalWidth;
			Spacing = spacing;
			OffsetIcon = offsetIcon;
			OffsetLabel = offsetLabel;
			OffsetShortcut = offsetShortcut;
			OffsetMark = offsetMark;
			if (widths != default(Span<ushort>))
			{
				Widths_0 = widths[0];
				Widths_1 = widths[1];
				Widths_2 = widths[2];
				Widths_3 = widths[3];
			}
		}


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImGuiMenuColumns* @this = &this)
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
	public unsafe struct ImGuiMenuColumnsPtr : IEquatable<ImGuiMenuColumnsPtr>
	{
		public ImGuiMenuColumnsPtr(ImGuiMenuColumns* handle) { Handle = handle; }

		public ImGuiMenuColumns* Handle;

		public bool IsNull => Handle == null;

		public static ImGuiMenuColumnsPtr Null => new ImGuiMenuColumnsPtr(null);

		public ImGuiMenuColumns this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImGuiMenuColumnsPtr(ImGuiMenuColumns* handle) => new ImGuiMenuColumnsPtr(handle);

		public static implicit operator ImGuiMenuColumns*(ImGuiMenuColumnsPtr handle) => handle.Handle;

		public static bool operator ==(ImGuiMenuColumnsPtr left, ImGuiMenuColumnsPtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImGuiMenuColumnsPtr left, ImGuiMenuColumnsPtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImGuiMenuColumnsPtr left, ImGuiMenuColumns* right) => left.Handle == right;

		public static bool operator !=(ImGuiMenuColumnsPtr left, ImGuiMenuColumns* right) => left.Handle != right;

		public bool Equals(ImGuiMenuColumnsPtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImGuiMenuColumnsPtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImGuiMenuColumnsPtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref uint TotalWidth => ref Unsafe.AsRef<uint>(&Handle->TotalWidth);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref uint NextTotalWidth => ref Unsafe.AsRef<uint>(&Handle->NextTotalWidth);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ushort Spacing => ref Unsafe.AsRef<ushort>(&Handle->Spacing);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ushort OffsetIcon => ref Unsafe.AsRef<ushort>(&Handle->OffsetIcon);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ushort OffsetLabel => ref Unsafe.AsRef<ushort>(&Handle->OffsetLabel);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ushort OffsetShortcut => ref Unsafe.AsRef<ushort>(&Handle->OffsetShortcut);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ushort OffsetMark => ref Unsafe.AsRef<ushort>(&Handle->OffsetMark);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe Span<ushort> Widths
		
		{
			get
			{
				return new Span<ushort>(&Handle->Widths_0, 4);
			}
		}
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			ImGui.DestroyNative(Handle);
		}

	}

}
