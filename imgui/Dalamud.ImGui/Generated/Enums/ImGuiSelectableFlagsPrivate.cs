// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using HexaGen.Runtime;
using System.Numerics;

namespace Dalamud.Bindings.ImGui
{
	/// <summary>
	/// To be documented.
	/// </summary>
	[Flags]
	public enum ImGuiSelectableFlagsPrivate : int
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		NoHoldingActiveId = unchecked(1048576),

		/// <summary>
		/// (WIP) Auto-select when moved into. This is not exposed in public API as to handle multi-select and modifiers we will need user to explicitly control focus scope. May be replaced with a BeginSelection() API.<br/>
		/// </summary>
		SelectOnNav = unchecked(2097152),

		/// <summary>
		/// Override button behavior to react on Click (default is Click+Release)<br/>
		/// </summary>
		SelectOnClick = unchecked(4194304),

		/// <summary>
		/// Override button behavior to react on Release (default is Click+Release)<br/>
		/// </summary>
		SelectOnRelease = unchecked(8388608),

		/// <summary>
		/// Span all avail width even if we declared less for layout purpose. FIXME: We may be able to remove this (added in 6251d379, 2bcafc86 for menus)<br/>
		/// </summary>
		SpanAvailWidth = unchecked(16777216),

		/// <summary>
		/// To be documented.
		/// </summary>
		DrawHoveredWhenHeld = unchecked(33554432),

		/// <summary>
		/// Set NavFocus ID on mouse hover (used by MenuItem)<br/>
		/// </summary>
		SetNavIdOnHover = unchecked(67108864),

		/// <summary>
		/// Disable padding each side with ItemSpacing * 0.5f<br/>
		/// </summary>
		NoPadWithHalfSpacing = unchecked(134217728),
	}
}
