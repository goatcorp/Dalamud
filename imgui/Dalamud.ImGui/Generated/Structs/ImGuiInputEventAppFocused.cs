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
	/// To be documented.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImGuiInputEventAppFocused
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public byte Focused;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiInputEventAppFocused(bool focused = default)
		{
			Focused = focused ? (byte)1 : (byte)0;
		}


	}

}
